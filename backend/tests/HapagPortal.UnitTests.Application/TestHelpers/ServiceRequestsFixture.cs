namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.ReleaseLetter;
using HapagPortal.Application.Payments.Commands.Confirm;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.ServiceRequests.PostPayment;
using HapagPortal.Application.ServiceRequests.Queue;
using HapagPortal.Application.ServiceRequests.Requests;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.ServiceRequests;
using NSubstitute;

/// <summary>
/// Contexto de pruebas de la Ola G sobre el de pagos: impuestos por país, definiciones de servicio configurables
/// y los handlers del cliente y de la bandeja interna armados para un actor; el pago de la solicitud pasa por el
/// carro, el cierre, la confirmación y la cola de pasos posteriores como en producción.
/// </summary>
public sealed class ServiceRequestsFixture
{
    public const string OwnTaxId = "76123456-7";

    public ServiceRequestsFixture()
    {
        Db.TaxConfigurationList.Add(new TaxConfiguration { Country = CountryCodes.Chile, ServiceType = "General", TaxName = "IVA", TaxRate = 19m });
        Db.TaxConfigurationList.Add(new TaxConfiguration { Country = CountryCodes.Bolivia, ServiceType = "General", TaxName = "IVA", TaxRate = 13m });
        foreach (var code in new[]
                 {
                     ChargeConceptCodes.SealManagement, ChargeConceptCodes.DropOff, ChargeConceptCodes.ContainerAdministrationXom,
                     ChargeConceptCodes.BlCorrection, ChargeConceptCodes.BlHouseTransmission, ChargeConceptCodes.MatrixLate,
                 })
        {
            Db.ChargeConceptList.Add(new ChargeConcept { Code = code, Name = code, Category = ChargeCategories.Service });
        }

        Storage.SaveAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Task.FromResult(Result<string>.Success($"service-requests/2026/10/{Guid.NewGuid():N}")));
        Internal = AccessTestData.CurrentUser(
            AccessTestData.AddMember(Db, AccessTestData.AddOrganization(Db, OrganizationTypes.Internal), profile: null),
            ServiceRequestPermissions.Process);
    }

    public PaymentsFixture Payments { get; } = new();
    public ChargeRulesFixture Rules => Payments.Rules;
    public MockApplicationDbContext Db => Payments.Db;
    public PaymentsFixture.Actor Owner => Payments.Owner;
    public FakeNotificationPublisher Notifications => Payments.Notifications;
    public IFileStorage Storage { get; } = Substitute.For<IFileStorage>();
    public ICurrentUserService Internal { get; }

    public ServiceCatalogEvaluator Catalog() => new(Db, Rules.TariffResolver(), Rules.ChargeRules());

    public ServiceRequestWorkflow Workflow() => new(Db, Catalog(), Notifications);

    public GetAvailableServicesQueryHandler Available(PaymentsFixture.Actor? actor = null) =>
        new(Db, (actor ?? Owner).Evaluator(Db), Catalog());

    public CreateServiceRequestCommandHandler Create(PaymentsFixture.Actor? actor = null)
    {
        actor ??= Owner;
        return new(Db, actor.CurrentUser, actor.Evaluator(Db), Catalog(), Workflow());
    }

    public SubmitServiceRequestCommandHandler Submit(PaymentsFixture.Actor? actor = null)
    {
        actor ??= Owner;
        return new(Db, actor.CurrentUser, actor.Evaluator(Db), Workflow());
    }

    public CancelServiceRequestCommandHandler Cancel(PaymentsFixture.Actor? actor = null)
    {
        actor ??= Owner;
        return new(Db, actor.CurrentUser, actor.Evaluator(Db), Workflow());
    }

    public UploadServiceRequestAttachmentCommandHandler Upload(PaymentsFixture.Actor? actor = null)
    {
        actor ??= Owner;
        return new(Db, actor.CurrentUser, actor.Evaluator(Db), Storage);
    }

    public ApproveServiceRequestCommandHandler Approve() => new(Db, Internal, Workflow(), ReleaseLetters());

    /// <summary>Sistema de TATC simulado (M2-09) para la carta de liberación (M6-08): sin registro por defecto.</summary>
    public ITatcProvider Tatc { get; } = CreateTatc();

    // Generación documental de pruebas (Ola J) sobre el mismo contexto: PDF, firma, almacenamiento y correo simulados.
    public FakePdfDocumentRenderer Renderer { get; } = new();
    public InMemoryFileStorage DocumentStorage { get; } = new();
    public FakeDocumentSigner Signer { get; } = new();
    public IEmailService Email { get; } = Substitute.For<IEmailService>();
    public DocumentSettings DocumentSettings { get; } = new();

    public ShipmentDocumentService DocumentService() => new(Db, Renderer, Signer, DocumentStorage, Email, DocumentSettings);

    public ReleaseLetterService ReleaseLetters() => new(Db, Tatc, DocumentService());

    private static ITatcProvider CreateTatc()
    {
        var tatc = Substitute.For<ITatcProvider>();
        tatc.GetByBlNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result<BlTatcRecord?>.Success(null));
        return tatc;
    }

    public RejectServiceRequestCommandHandler Reject() => new(Db, Internal, Workflow());

    public CompleteServiceRequestCommandHandler Complete() => new(Db, Internal, Workflow());

    public GetServiceRequestQueueQueryHandler Queue() => new(Db, FeatureSettings.AllEnabled());

    /// <summary>Crea y envía la solicitud en un paso (con datos de facturación propios).</summary>
    public Task<Result<ServiceRequestDetailDto>> RequestAsync(
        string code,
        string blNumber,
        string inputJson,
        PaymentsFixture.Actor? actor = null,
        bool acceptTariff = true,
        decimal? acceptedTotal = null,
        bool submit = true,
        ServiceBillingInput? billing = null) =>
        Create(actor).Handle(
            new CreateServiceRequestCommand(code, blNumber, null, inputJson, billing ?? Billing(), submit, acceptTariff, acceptedTotal),
            CancellationToken.None);

    public static ServiceBillingInput Billing(string taxId = "76.123.456-7") =>
        new(taxId, "Importadora Demo SpA", "Av. Apoquindo 4500, Las Condes", "facturacion@importadorademo.cl", "Importación");

    /// <summary>Agrega al carro el cargo de la solicitud (en CLP), paga con Khipu, confirma y procesa la cola (NF-03).</summary>
    public async Task<Payment> PayAsync(ServiceRequestDetailDto request, string key = "key-srv")
    {
        foreach (var charge in request.Charges)
        {
            var added = await Payments.Add().Handle(
                new AddCartItemCommand(PayableItemTypes.LocalCharge, charge.ChargeId, null, OwnTaxId, "CLP"), CancellationToken.None);
            if (added.IsFailure)
                throw new InvalidOperationException(added.Error.Code);
        }

        var checkout = await Payments.CheckoutCart().Handle(
            new CheckoutCartCommand(request.Country, "CLP", PaymentMethodCodes.Khipu, key), CancellationToken.None);
        if (checkout.IsFailure)
            throw new InvalidOperationException(checkout.Error.Code);

        var payment = Db.PaymentList.Single(p => p.Id == checkout.Value.Payment.Id);
        var confirmed = await new ConfirmPaymentCommandHandler(Db, AccessTestData.CurrentUser(Owner.User))
            .Handle(new ConfirmPaymentCommand(payment.Id), CancellationToken.None);
        if (confirmed.IsFailure)
            throw new InvalidOperationException(confirmed.Error.Code);

        // La liberación encola el aviso de las solicitudes: se procesa en la pasada siguiente, como en el worker.
        while (await ProcessOutboxAsync() > 0)
        {
        }

        return payment;
    }

    public Task<int> ProcessOutboxAsync(DateTime? now = null) =>
        Payments.Processor(new NotifyServiceRequestsStep(Db, Workflow())).ProcessDueAsync(20, now ?? DateTime.UtcNow, CancellationToken.None);

    public BillOfLading ExportBl(string blNumber, DateTime etd, string country = CountryCodes.Chile, string? booking = null)
    {
        var bl = AccessTestData.AddBl(Db, Rules.Organization.Id, blNumber, shipmentType: "Export", country: country, bookingNumber: booking ?? $"BKG-{blNumber}");
        bl.ETD = etd;
        bl.ETA = etd.AddDays(10);
        AccessTestData.AddRole(Db, bl, Rules.Organization, ShipmentRoleCodes.Shipper);
        return bl;
    }

    public ServiceDefinition AddDefinition(
        string code,
        string operations,
        string countries,
        string actionCode,
        IReadOnlyList<ServiceInputField> fields,
        Action<ServiceDefinition>? configure = null)
    {
        var definition = new ServiceDefinition
        {
            Code = code,
            NameEs = code,
            NameEn = code,
            Operations = operations,
            Countries = countries,
            ActionCode = actionCode,
            InputSchemaJson = ServiceInputSchema.Serialize(fields)
        };
        configure?.Invoke(definition);
        Db.ServiceDefinitionList.Add(definition);
        return definition;
    }

    public static ServiceInputField ContainersField(bool required = true) =>
        new("containers", "Contenedores", "Containers", ServiceInputFieldTypes.Containers, required);

    /// <summary>Drop Off SCL (M3-09): importación CL tras el arribo, por contenedor, aceptación de tarifa y aprobación del equipo ED.</summary>
    public ServiceDefinition DropOff() =>
        AddDefinition(ServiceDefinitionCodes.DropOff, ServiceOperations.Import, CountryCodes.Chile, ShipmentActionCodes.RequestDropOff,
            [ContainersField(), new("returnDate", "Fecha", "Date", ServiceInputFieldTypes.Date, true)],
            d =>
            {
                d.AvailabilityWindow = ServiceAvailabilityWindows.AfterArrival;
                d.RequiresContainers = true;
                d.BillingDataRequired = true;
                d.TariffAcceptanceRequired = true;
                d.PricingMode = ServicePricingModes.Tariff;
                d.ChargeConceptCode = ChargeConceptCodes.DropOff;
                d.QuantityMode = ServiceQuantityModes.PerContainer;
                d.ApprovalTeam = ServiceTeams.Ed;
            });

    /// <summary>Transmisión de BL hijo (M3-13): exportación tras el zarpe, dentro (PLAZO) o fuera de plazo (FUERA_PLAZO por horas).</summary>
    public ServiceDefinition BlHouse() =>
        AddDefinition(ServiceDefinitionCodes.BlHouseTransmission, ServiceOperations.Export, "CL,BO", ShipmentActionCodes.PayOnDemandLocalCharges,
            [new("houseBlNumbers", "BL hijo", "House BL", ServiceInputFieldTypes.Text, true)],
            d =>
            {
                d.AvailabilityWindow = ServiceAvailabilityWindows.AfterDeparture;
                d.BillingDataRequired = true;
                d.PricingMode = ServicePricingModes.Tariff;
                d.ChargeConceptCode = ChargeConceptCodes.BlHouseTransmission;
                d.TariffCode = "PLAZO";
                d.LateTariffCode = "FUERA_PLAZO";
                d.Milestone = ServiceMilestones.CustomsDeadline;
                d.DeadlineRuleCode = "BL_EMPTY_OUT";
                d.MilestoneOffsetHours = 72;
                d.TimingRule = ServiceTimingRules.InTimeAndLate;
                d.Taxable = false;
                d.FulfillmentTeam = ServiceTeams.CustomerService;
            });

    public void BlHouseTariffs()
    {
        Rules.AddTariff(ChargeConceptCodes.BlHouseTransmission, 35m, "USD", code: "PLAZO");
        Rules.AddTariff(ChargeConceptCodes.BlHouseTransmission, 0m, "USD", code: "FUERA_PLAZO", tierUnit: TariffTierUnits.Hours,
            tiers: [(0, 24, 60m), (25, 72, 120m), (73, null, 250m)]);
    }

    /// <summary>Gestión de sellos (M3-07): booking de exportación antes del zarpe, por contenedor, prestada por Customer Service.</summary>
    public ServiceDefinition Seals() =>
        AddDefinition(ServiceDefinitionCodes.SealManagement, ServiceOperations.Export, CountryCodes.Chile, ShipmentActionCodes.PayOnDemandLocalCharges,
            [ContainersField(), new("sealNumbers", "Sellos", "Seals", ServiceInputFieldTypes.Text, true)],
            d =>
            {
                d.ReferenceType = ServiceReferenceTypes.Booking;
                d.AvailabilityWindow = ServiceAvailabilityWindows.BeforeDeparture;
                d.RequiresContainers = true;
                d.BillingDataRequired = true;
                d.PricingMode = ServicePricingModes.Tariff;
                d.ChargeConceptCode = ChargeConceptCodes.SealManagement;
                d.QuantityMode = ServiceQuantityModes.PerContainer;
                d.FulfillmentTeam = ServiceTeams.CustomerService;
            });

    /// <summary>XOM (M3-10): Bolivia, por contenedor en BOB, excepción de Nexus XOM y unidades SOC excluidas.</summary>
    public ServiceDefinition Xom() =>
        AddDefinition(ServiceDefinitionCodes.ContainerAdministrationXom, "IMPORT,EXPORT", CountryCodes.Bolivia, ShipmentActionCodes.PayOnDemandLocalCharges,
            [ContainersField()],
            d =>
            {
                d.RequiresContainers = true;
                d.BillingDataRequired = true;
                d.PricingMode = ServicePricingModes.Tariff;
                d.ChargeConceptCode = ChargeConceptCodes.ContainerAdministrationXom;
                d.QuantityMode = ServiceQuantityModes.PerContainer;
                d.Taxable = false;
                d.ExemptionConcept = ChargeConceptCodes.ContainerAdministrationXom;
                d.ExcludeShipperOwnedContainers = true;
            });

    /// <summary>Gate In por devolución (M3-15): cargo del sistema de origen con exenciones de Nexus.</summary>
    public ServiceDefinition GateInReturn() =>
        AddDefinition(ServiceDefinitionCodes.GateInReturn, ServiceOperations.Export, CountryCodes.Chile, ShipmentActionCodes.PayMandatoryLocalCharges,
            [ContainersField(required: false)],
            d =>
            {
                d.ReferenceType = ServiceReferenceTypes.Booking;
                d.AllowMultiplePerBl = false;
                d.PricingMode = ServicePricingModes.SourceCharge;
                d.ChargeConceptCode = ChargeConceptCodes.GateIn;
            });
}
