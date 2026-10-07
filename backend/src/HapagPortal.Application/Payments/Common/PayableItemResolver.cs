namespace HapagPortal.Application.Payments.Common;

using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Application.Invoices;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Payments;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Organización pagadora: alcance del usuario y condiciones comerciales de Nexus (M8-02, M8-03).</summary>
public sealed record PayerContext(AccessScope Scope, Client Organization, CommercialConditionsDto Conditions);

/// <summary>Ítem pagable validado, con el monto que corresponde pagar hoy.</summary>
public sealed record ResolvedPayableItem(
    string ItemType,
    Guid SourceId,
    BillOfLading? BillOfLading,
    string Country,
    string ConceptCode,
    string ConceptName,
    string? Description,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<string> AllowedCurrencies,
    string DefaultPaymentCurrency,
    IReadOnlyList<BillingTaxIdOptionDto> BillingOptions,
    Guid? OnBehalfOfClientId,
    Guid? AccessGrantId)
{
    public string Label => BillOfLading is null ? $"{ConceptCode}" : $"{BillOfLading.BLNumber} {ConceptCode}";

    public PayableItemDto ToDto() => new(
        ItemType, SourceId, BillOfLading?.Id, BillOfLading?.BLNumber, BillOfLading?.BookingNumber, Country,
        ConceptCode, ConceptName, Description, Amount, TaxAmount, TotalAmount, Currency, AllowedCurrencies,
        DefaultPaymentCurrency, BillingOptions);
}

/// <summary>
/// Valida un ítem antes de incorporarlo al carro o a un pago, siempre en el servidor (NF-05):
/// <list type="bullet">
/// <item>BL accesible y acción de M1-11 permitida con un perfil que opera (M1-02); acceso abierto sin
/// asociación de una agencia o transportista: debe asociarse (M1-18).</item>
/// <item>Carta de responsabilidad del FFWW pendiente: bloquea (M4-04).</item>
/// <item>Monto según las reglas de Nexus (exenciones M4-01/M4-02, IPO excluido con crédito M4-03,
/// descuento de demoras anticipadas en el MHD M3-16); sin monto, no se agrega (M4-02); Nexus sin
/// responder, no se agrega (RULES_UNAVAILABLE).</item>
/// <item>No pagado ni incluido en un pago en curso (NF-01).</item>
/// <item>Monedas de pago habilitadas (M5-04, M5-08) y RUT de facturación posibles (M5-09).</item>
/// </list>
/// Las evaluaciones por BL se reutilizan dentro de la misma solicitud.
/// </summary>
public sealed class PayableItemResolver(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IChargeRulesService chargeRulesService,
    DemurrageStatusBuilder demurrageStatusBuilder,
    IResponsibilityLetterStatus responsibilityLetterStatus)
{
    public const string BillingSourceOwn = "Own";
    public const string BillingSourceGrant = "Grant";
    public const string BillingSourceInvoice = "Invoice";

    private readonly Dictionary<Guid, (BillOfLading Bl, ShipmentPermissionSet Permissions)?> _bls = [];
    private readonly Dictionary<Guid, ChargeRulesEvaluation> _rules = [];
    private readonly Dictionary<Guid, DemurrageStatusDto> _demurrage = [];
    private readonly Dictionary<(string Country, string Concept, string Currency), IReadOnlyList<string>> _currencies = [];
    private readonly Dictionary<Guid, Client?> _organizations = [];
    private Dictionary<string, ChargeConcept>? _catalog;
    private InvoiceScope? _invoiceScope;

    /// <summary>
    /// Organización del usuario como pagadora. Pagar es una operación de la propia organización: la
    /// visibilidad total del administrador interno (M8-06) no habilita pagar.
    /// </summary>
    public async Task<Result<PayerContext>> LoadPayerAsync(bool requireOperate, CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (scope.IsAdmin)
            scope = scope with { IsAdmin = false };

        if (!scope.IsOperational || scope.OrganizationId is null || scope.OrganizationType == OrganizationTypes.Internal)
            return Result<PayerContext>.Failure(Error.Forbidden);

        if (requireOperate && !scope.CanOperate)
            return Result<PayerContext>.Failure(Error.Forbidden);

        var organization = await dbContext.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == scope.OrganizationId.Value, cancellationToken);
        if (organization is null)
            return Result<PayerContext>.Failure(Error.Unauthorized);

        var conditions = await chargeRulesService.GetConditionsAsync(organization, cancellationToken);
        return Result<PayerContext>.Success(new PayerContext(scope, organization, conditions));
    }

    public async Task<Result<ResolvedPayableItem>> ResolveAsync(
        PayerContext payer,
        string itemType,
        Guid? sourceId,
        string? reference,
        CancellationToken cancellationToken)
    {
        if (!payer.Conditions.Available)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.ChargeRules.ConditionsUnavailable);

        var resolved = itemType switch
        {
            PayableItemTypes.LocalCharge when sourceId is not null => await LocalChargeAsync(payer, sourceId.Value, cancellationToken),
            PayableItemTypes.Freight when sourceId is not null => await FreightAsync(payer, sourceId.Value, cancellationToken),
            PayableItemTypes.Demurrage when sourceId is not null => await DemurrageAsync(payer, sourceId.Value, cancellationToken),
            PayableItemTypes.WarehouseChange when sourceId is not null => await WarehouseChangeAsync(payer, sourceId.Value, cancellationToken),
            PayableItemTypes.Invoice => await InvoiceAsync(payer, sourceId, reference, cancellationToken),
            _ => Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.SourceNotFound)
        };

        if (resolved.IsFailure)
            return resolved;

        var paymentState = await PaymentStateAsync(resolved.Value.ItemType, resolved.Value.SourceId, cancellationToken);
        return paymentState.IsFailure ? Result<ResolvedPayableItem>.Failure(paymentState.Error) : resolved;
    }

    /// <summary>Monedas habilitadas para el concepto, el país y la moneda del cargo (M5-04, M5-08).</summary>
    public async Task<IReadOnlyList<string>> AllowedCurrenciesAsync(
        string country,
        string concept,
        string currency,
        CancellationToken cancellationToken)
    {
        if (_currencies.TryGetValue((country, concept, currency), out var cached))
            return cached;

        var allowed = await PaymentCurrencies.AllowedAsync(dbContext, country, concept, currency, cancellationToken);
        _currencies[(country, concept, currency)] = allowed;
        return allowed;
    }

    private async Task<Result<ResolvedPayableItem>> LocalChargeAsync(PayerContext payer, Guid chargeId, CancellationToken cancellationToken)
    {
        var charge = await dbContext.LocalCharges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chargeId, cancellationToken);
        if (charge is null)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.SourceNotFound);

        var catalog = await CatalogAsync(cancellationToken);
        var isDemurrageConcept = catalog.GetValueOrDefault(charge.ChargeType)?.Category == ChargeCategories.Demurrage;
        var action = isDemurrageConcept ? ShipmentActionCodes.PayImportDemurrage : ShipmentActionCodes.PayMandatoryLocalCharges;

        var access = await AuthorizeAsync(payer, charge.BillOfLadingId, action, cancellationToken);
        if (access.IsFailure)
            return Result<ResolvedPayableItem>.Failure(access.Error);

        var (bl, permissions) = access.Value;

        if (charge.Status == ChargeStatus.Paid)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.AlreadyPaid);
        if (charge.Status == ChargeStatus.CreditImputed)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.CreditImputed);

        decimal amount, tax, total;

        if (isDemurrageConcept)
        {
            // MHD y demoras anticipadas: el MHD descuenta lo pagado por adelantado (M3-16).
            var status = await DemurrageStatusAsync(bl, permissions, cancellationToken);
            var concept = status.OtherConcepts.FirstOrDefault(c => c.ChargeId == chargeId);
            if (concept is null)
                return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.NotPayable);
            if (concept.PayableTotal <= 0m)
                return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.ZeroValue);

            total = concept.PayableTotal;
            tax = charge.TotalAmount > 0m ? MoneyRounding.Round(charge.TaxAmount * total / charge.TotalAmount, charge.Currency) : 0m;
            amount = total - tax;
        }
        else
        {
            var evaluation = await RulesAsync(bl, payer.Organization, permissions, cancellationToken);
            if (!evaluation.Result.RulesAvailable)
                return Result<ResolvedPayableItem>.Failure(DomainErrors.ChargeRules.ConditionsUnavailable);

            // Un IPO no presentado es un cargo de un cliente con crédito (M4-03): no es pagable.
            var ruled = evaluation.Result.Charges.FirstOrDefault(c => c.ChargeId == chargeId);
            if (ruled is null)
                return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.NotPayable);
            if (ruled.PayableTotal <= 0m)
                return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.ZeroValue);
            if (ruled.Action != ChargeActions.AddToCart)
                return Result<ResolvedPayableItem>.Failure(BlockedReason(ruled.ActionBlockedReason));

            amount = ruled.PayableAmount;
            tax = ruled.PayableTaxAmount;
            total = ruled.PayableTotal;
        }

        return await BuildAsync(
            payer, PayableItemTypes.LocalCharge, charge.Id, bl, permissions, action, charge.ChargeType,
            catalog.GetValueOrDefault(charge.ChargeType)?.Name ?? charge.ChargeType, charge.Description,
            amount, tax, total, charge.Currency, cancellationToken);
    }

    private async Task<Result<ResolvedPayableItem>> FreightAsync(PayerContext payer, Guid blId, CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(payer, blId, ShipmentActionCodes.PayFreight, cancellationToken);
        if (access.IsFailure)
            return Result<ResolvedPayableItem>.Failure(access.Error);

        var (bl, permissions) = access.Value;

        var paidBefore = bl.FreightPaidAt is not null || await dbContext.Payments.AsNoTracking()
            .AnyAsync(p => p.BillOfLadingId == bl.Id && p.PaymentType == "Freight" && p.Status == PaymentStatus.Confirmed, cancellationToken);
        if (paidBefore)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.AlreadyPaid);
        if (bl.FreightAmount <= 0m)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.ZeroValue);

        // El flete internacional no lleva IVA local.
        return await BuildAsync(
            payer, PayableItemTypes.Freight, bl.Id, bl, permissions, ShipmentActionCodes.PayFreight, PaymentConcepts.Freight,
            "Flete", $"Flete {bl.PortOfLoading} - {bl.PortOfDischarge}", bl.FreightAmount, 0m, bl.FreightAmount,
            bl.FreightCurrency, cancellationToken);
    }

    private async Task<Result<ResolvedPayableItem>> DemurrageAsync(PayerContext payer, Guid lineId, CancellationToken cancellationToken)
    {
        var line = await dbContext.DemurrageCharges.AsNoTracking().FirstOrDefaultAsync(d => d.Id == lineId, cancellationToken);
        if (line is null)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.SourceNotFound);

        var access = await AuthorizeAsync(payer, line.BillOfLadingId, ShipmentActionCodes.PayImportDemurrage, cancellationToken);
        if (access.IsFailure)
            return Result<ResolvedPayableItem>.Failure(access.Error);

        var (bl, permissions) = access.Value;

        if (line.Status == DemurrageChargeStatus.Paid)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.AlreadyPaid);
        if (line.IsExempt || line.TotalAmount <= 0m)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.ZeroValue);

        // M3-18: con factura emitida se paga la factura (M7-01), no la línea calculada.
        if (line.InvoiceNumber is not null)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.PayDemurrageInvoice);

        var catalog = await CatalogAsync(cancellationToken);
        return await BuildAsync(
            payer, PayableItemTypes.Demurrage, line.Id, bl, permissions, ShipmentActionCodes.PayImportDemurrage,
            ChargeConceptCodes.Demurrage, catalog.GetValueOrDefault(ChargeConceptCodes.Demurrage)?.Name ?? "Demurrage",
            $"Demurrage {line.ContainerNumber} ({line.DemurrageDays} d)", line.TotalAmount, 0m, line.TotalAmount,
            line.Currency, cancellationToken);
    }

    private async Task<Result<ResolvedPayableItem>> WarehouseChangeAsync(PayerContext payer, Guid changeId, CancellationToken cancellationToken)
    {
        var change = await dbContext.WarehouseChanges.AsNoTracking().FirstOrDefaultAsync(w => w.Id == changeId, cancellationToken);
        if (change is null || (change.RequestedByClientId is not null && change.RequestedByClientId != payer.Organization.Id))
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.SourceNotFound);

        var access = await AuthorizeAsync(payer, change.BillOfLadingId, ShipmentActionCodes.RequestWarehouseChange, cancellationToken);
        if (access.IsFailure)
            return Result<ResolvedPayableItem>.Failure(access.Error);

        var (bl, permissions) = access.Value;

        if (change.IsFree || change.Amount <= 0m)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.ZeroValue);
        if (change.Status == WarehouseChangeStatus.Completed)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.AlreadyPaid);
        if (change.Status != WarehouseChangeStatus.PendingPayment)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.NotPayable);

        var catalog = await CatalogAsync(cancellationToken);
        return await BuildAsync(
            payer, PayableItemTypes.WarehouseChange, change.Id, bl, permissions, ShipmentActionCodes.RequestWarehouseChange,
            ChargeConceptCodes.WarehouseChange, catalog.GetValueOrDefault(ChargeConceptCodes.WarehouseChange)?.Name ?? "Cambio de almacén",
            $"Cambio de almacén {change.FromWarehouse} → {change.ToWarehouse}", change.Amount, 0m, change.Amount,
            change.Currency, cancellationToken);
    }

    private async Task<Result<ResolvedPayableItem>> InvoiceAsync(
        PayerContext payer,
        Guid? invoiceId,
        string? reference,
        CancellationToken cancellationToken)
    {
        CustomerInvoice? invoice = null;
        if (invoiceId is not null)
        {
            invoice = await dbContext.CustomerInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invoiceId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(reference))
        {
            var number = reference.Trim();
            invoice = await dbContext.CustomerInvoices.AsNoTracking()
                .Where(i => i.SourceNumber == number || i.SiiNumber == number)
                .Where(i => i.OrganizationId == payer.Organization.Id)
                .OrderByDescending(i => i.IssueDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        _invoiceScope ??= await InvoiceAccess.LoadAsync(dbContext, accessEvaluator, cancellationToken);
        if (invoice is null || !InvoiceAccess.CanView(_invoiceScope, invoice))
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.SourceNotFound);

        if (invoice.Status == InvoiceStatus.Paid)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.AlreadyPaid);
        if (invoice.Status == InvoiceStatus.Cancelled || !invoice.IsPayable || invoice.DocumentType == InvoiceDocumentTypes.CreditNote)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.NotPayable);
        if (invoice.TotalAmount <= 0m)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.ZeroValue);

        // La factura ya está emitida: se paga con el RUT facturado.
        BillOfLading? bl = null;
        Guid? onBehalfOf = null;
        Guid? grantId = null;
        if (invoice.BillOfLadingId is { } blId)
        {
            var access = await LoadBlAsync(payer, blId, cancellationToken);
            bl = access?.Bl;
            if (invoice.OrganizationId != payer.Organization.Id)
            {
                var grant = access?.Permissions.GrantFor(ShipmentActionCodes.ViewInvoicesAsPayer)
                    ?? access?.Permissions.GrantFor(ShipmentActionCodes.ViewInvoicesAsBilled);
                onBehalfOf = invoice.OrganizationId;
                grantId = grant?.GrantId;
            }
        }

        var concept = invoice.ConceptCode ?? PaymentConcepts.Invoice;
        var catalog = await CatalogAsync(cancellationToken);
        var allowed = await AllowedCurrenciesAsync(invoice.Country, concept, invoice.Currency, cancellationToken);
        var defaultCurrency = PaymentCurrencyPolicy.Default(invoice.Country, invoice.Currency, allowed);
        if (defaultCurrency is null)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.NoPaymentCurrency);

        var taxId = TaxIdNormalizer.Normalize(invoice.TaxId);
        return Result<ResolvedPayableItem>.Success(new ResolvedPayableItem(
            PayableItemTypes.Invoice,
            invoice.Id,
            bl,
            invoice.Country,
            concept,
            catalog.GetValueOrDefault(concept)?.Name ?? "Factura",
            $"Factura {invoice.SiiNumber ?? invoice.SourceNumber}",
            invoice.NetAmount,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.Currency,
            allowed,
            defaultCurrency,
            [new BillingTaxIdOptionDto(taxId, invoice.LegalName, invoice.OrganizationId, BillingSourceInvoice, null)],
            onBehalfOf,
            grantId));
    }

    /// <summary>BL accesible (propio, otorgado, autoasociado o por acceso abierto) y su evaluación de M1-11.</summary>
    private async Task<(BillOfLading Bl, ShipmentPermissionSet Permissions)?> LoadBlAsync(
        PayerContext payer,
        Guid blId,
        CancellationToken cancellationToken)
    {
        if (_bls.TryGetValue(blId, out var cached))
            return cached;

        var bl = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), payer.Scope)
                .FirstOrDefaultAsync(b => b.Id == blId, cancellationToken)
            ?? await accessEvaluator.FilterOpenAccess(dbContext.BillsOfLading.AsNoTracking(), payer.Scope)
                .FirstOrDefaultAsync(b => b.Id == blId, cancellationToken);

        (BillOfLading, ShipmentPermissionSet)? result = null;
        if (bl is not null)
        {
            var permissions = await accessEvaluator.EvaluateAsync(payer.Scope, bl, cancellationToken);
            if (permissions.Can(ShipmentActionCodes.ViewShipment))
                result = (bl, permissions);
        }

        _bls[blId] = result;
        return result;
    }

    private async Task<Result<(BillOfLading Bl, ShipmentPermissionSet Permissions)>> AuthorizeAsync(
        PayerContext payer,
        Guid blId,
        string action,
        CancellationToken cancellationToken)
    {
        var access = await LoadBlAsync(payer, blId, cancellationToken);
        if (access is null)
            return Result<(BillOfLading, ShipmentPermissionSet)>.Failure(DomainErrors.Cart.SourceNotFound);

        var (bl, permissions) = access.Value;

        // M1-18: la agencia o el transportista que ve el BL solo por acceso abierto debe asociarse antes.
        if (permissions.RequiresAssociationForPayment)
            return Result<(BillOfLading, ShipmentPermissionSet)>.Failure(DomainErrors.Cart.AssociationRequired);

        if (!permissions.CanExecute(action))
            return Result<(BillOfLading, ShipmentPermissionSet)>.Failure(Error.Forbidden);

        // M4-04: el FFWW autorizado no avanza sin la carta de responsabilidad.
        if (payer.Conditions.ResponsibilityLetterRequired)
        {
            var letter = await responsibilityLetterStatus.GetStatusAsync(bl.Id, payer.Organization.Id, cancellationToken);
            if (letter != ProcessRequirementStatus.Fulfilled)
                return Result<(BillOfLading, ShipmentPermissionSet)>.Failure(DomainErrors.Cart.ResponsibilityLetterRequired);
        }

        return Result<(BillOfLading, ShipmentPermissionSet)>.Success((bl, permissions));
    }

    private async Task<Result<ResolvedPayableItem>> BuildAsync(
        PayerContext payer,
        string itemType,
        Guid sourceId,
        BillOfLading bl,
        ShipmentPermissionSet permissions,
        string action,
        string concept,
        string conceptName,
        string? description,
        decimal amount,
        decimal tax,
        decimal total,
        string currency,
        CancellationToken cancellationToken)
    {
        var allowed = await AllowedCurrenciesAsync(bl.Country, concept, currency, cancellationToken);
        var defaultCurrency = PaymentCurrencyPolicy.Default(bl.Country, currency, allowed);
        if (defaultCurrency is null)
            return Result<ResolvedPayableItem>.Failure(DomainErrors.Cart.NoPaymentCurrency);

        var grant = permissions.GrantFor(action);
        var options = await BillingOptionsAsync(payer.Organization, permissions, action, cancellationToken);

        return Result<ResolvedPayableItem>.Success(new ResolvedPayableItem(
            itemType, sourceId, bl, bl.Country, concept, conceptName, description, amount, tax, total, currency,
            allowed, defaultCurrency, options, grant?.GrantorOrganizationId, grant?.GrantId));
    }

    /// <summary>
    /// RUT de facturación habilitados (M5-09): el de la organización del usuario y el de cada mandante
    /// cuyo acceso vigente sobre el BL habilita la acción.
    /// </summary>
    private async Task<IReadOnlyList<BillingTaxIdOptionDto>> BillingOptionsAsync(
        Client organization,
        ShipmentPermissionSet permissions,
        string action,
        CancellationToken cancellationToken)
    {
        var options = new List<BillingTaxIdOptionDto>
        {
            new(TaxIdNormalizer.Normalize(organization.TaxId), organization.Name, organization.Id, BillingSourceOwn, null)
        };

        foreach (var grant in permissions.Grants.Where(g => g.Actions.Contains(action)))
        {
            var grantor = await OrganizationAsync(grant.GrantorOrganizationId, cancellationToken);
            if (grantor is null)
                continue;

            var taxId = TaxIdNormalizer.Normalize(grantor.TaxId);
            if (options.All(o => o.TaxId != taxId))
                options.Add(new BillingTaxIdOptionDto(taxId, grantor.Name, grantor.Id, BillingSourceGrant, grant.GrantId));
        }

        return options;
    }

    /// <summary>NF-01: un ítem pagado o en un pago en curso no entra a otro pago.</summary>
    private async Task<Result> PaymentStateAsync(string itemType, Guid sourceId, CancellationToken cancellationToken)
    {
        var paymentIds = await dbContext.PaymentDetails.AsNoTracking()
            .Where(d => d.ItemType == itemType && d.SourceId == sourceId)
            .Select(d => d.PaymentId)
            .ToListAsync(cancellationToken);

        if (paymentIds.Count == 0)
            return Result.Success();

        var statuses = await dbContext.Payments.AsNoTracking()
            .Where(p => paymentIds.Contains(p.Id))
            .Select(p => p.Status)
            .ToListAsync(cancellationToken);

        if (statuses.Contains(PaymentStatus.Confirmed))
            return Result.Failure(DomainErrors.Cart.AlreadyPaid);

        return statuses.Any(PaymentStateMachine.IsInFlight)
            ? Result.Failure(DomainErrors.Cart.ItemInPayment)
            : Result.Success();
    }

    private static Error BlockedReason(string? reason) => reason switch
    {
        ChargeActionBlockReasons.AssociationRequired => DomainErrors.Cart.AssociationRequired,
        ChargeActionBlockReasons.RulesUnavailable => DomainErrors.ChargeRules.ConditionsUnavailable,
        ChargeActionBlockReasons.NoPermission => Error.Forbidden,
        _ => DomainErrors.Cart.NotPayable
    };

    private async Task<ChargeRulesEvaluation> RulesAsync(
        BillOfLading bl,
        Client payer,
        ShipmentPermissionSet permissions,
        CancellationToken cancellationToken)
    {
        if (_rules.TryGetValue(bl.Id, out var cached))
            return cached;

        var evaluation = await chargeRulesService.EvaluateAsync(bl, payer, permissions, cancellationToken);
        _rules[bl.Id] = evaluation;
        return evaluation;
    }

    private async Task<DemurrageStatusDto> DemurrageStatusAsync(
        BillOfLading bl,
        ShipmentPermissionSet permissions,
        CancellationToken cancellationToken)
    {
        if (_demurrage.TryGetValue(bl.Id, out var cached))
            return cached;

        var status = await demurrageStatusBuilder.BuildAsync(bl, permissions, cancellationToken);
        _demurrage[bl.Id] = status;
        return status;
    }

    private async Task<Client?> OrganizationAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_organizations.TryGetValue(id, out var cached))
            return cached;

        var organization = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        _organizations[id] = organization;
        return organization;
    }

    public async Task<IReadOnlyDictionary<string, ChargeConcept>> CatalogAsync(CancellationToken cancellationToken) =>
        _catalog ??= await dbContext.ChargeConcepts.AsNoTracking().ToDictionaryAsync(c => c.Code, cancellationToken);
}
