namespace HapagPortal.UnitTests.Application.Reinvoicing;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Invoices;
using HapagPortal.Application.Reinvoicing;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.ServiceRequests.Requests;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.ServiceRequests;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Refacturación IAO con pérdida de IVA (M3-11): desde una factura emitida, con los nuevos datos de facturación y la
/// aprobación de la nueva razón social adjunta; la refacturación y la pérdida de IVA se pagan juntas; la nueva
/// factura no se emite sin la aceptación del cobro por la nueva razón social (enlace de un solo uso) y, emitida,
/// reemplaza a la original en M7-01.
/// </summary>
public sealed class ReinvoicingTests
{
    private const string NewTaxId = "77888999-1";

    private readonly FinanceFixture _f = new();
    private readonly CustomerInvoice _invoice;

    public ReinvoicingTests()
    {
        _f.Payments.NoConditions(_f.Owner.Organization);
        _f.Rules.AddTariff(ChargeConceptCodes.Reinvoicing, 25000m, "CLP");
        _f.Services.AddDefinition(ServiceDefinitionCodes.IaoReinvoicing, "IMPORT,EXPORT", CountryCodes.Chile, ShipmentActionCodes.PayOnDemandLocalCharges,
            [
                new ServiceInputField(ReinvoicingFields.Approval, "Aprobación", "Approval", ServiceInputFieldTypes.File, true),
                new ServiceInputField(ReinvoicingFields.Reason, "Motivo", "Reason", ServiceInputFieldTypes.TextArea, MaxLength: 1000)
            ],
            d =>
            {
                d.BillingDataRequired = true;
                d.TariffAcceptanceRequired = true;
                d.PricingMode = ServicePricingModes.Tariff;
                d.ChargeConceptCode = ChargeConceptCodes.Reinvoicing;
            });

        var bl = _f.Rules.OwnBl("BL-IAO");
        _invoice = _f.AddInvoice(_f.Owner.Organization, bl, 120000m, 22800m, new DateOnly(2027, 1, 15), sii: "100198");
    }

    private static ServiceBillingInput NewBilling(string taxId = "77.888.999-1") =>
        new(taxId, "Comercial Austral SpA", "Av. Libertad 1405, Viña del Mar", "facturacion@comercialaustral.cl", "Comercio");

    private Task<Result<ReinvoicingDetailDto>> CreateAsync(Guid? invoiceId = null, ServiceBillingInput? billing = null) =>
        new CreateReinvoicingCommandHandler(_f.Db, _f.Owner.CurrentUser, _f.Owner.Evaluator(_f.Db), _f.Reinvoicing())
            .Handle(new CreateReinvoicingCommand(invoiceId ?? _invoice.Id, billing ?? NewBilling(), null, "Venta de la mercancía"), CancellationToken.None);

    private Task<Result<ServiceRequestAttachmentDto>> AttachApprovalAsync(Guid id) =>
        _f.Services.Upload().Handle(
            new UploadServiceRequestAttachmentCommand(id, ReinvoicingFields.Approval, "aprobacion.pdf", "application/pdf", "%PDF-1.4"u8.ToArray()),
            CancellationToken.None);

    private Task<Result<ReinvoicingDetailDto>> SubmitAsync(Guid id, bool accept = true, decimal? total = null) =>
        new SubmitReinvoicingCommandHandler(_f.Db, _f.Owner.CurrentUser, _f.Owner.Evaluator(_f.Db), _f.Reinvoicing(), _f.Services.Workflow())
            .Handle(new SubmitReinvoicingCommand(id, accept, total), CancellationToken.None);

    private Task<Result<ReinvoicingAcceptanceViewDto>> RespondAsync(string token, bool accept, string? taxId = "12.345.678-5") =>
        new RespondReinvoicingAcceptanceCommandHandler(_f.Db, _f.Reinvoicing())
            .Handle(new RespondReinvoicingAcceptanceCommand(token, accept, "Ana Austral", taxId, accept ? null : "No corresponde", "10.0.0.8"),
                CancellationToken.None);

    private string TokenFromEmail()
    {
        var body = _f.Emails.Last(e => e.To == "facturacion@comercialaustral.cl").Body;
        var start = body.IndexOf(ReinvoicingService.AcceptancePath, StringComparison.Ordinal) + ReinvoicingService.AcceptancePath.Length;
        return body[start..].Split('\n')[0].Trim();
    }

    private async Task<ReinvoicingDetailDto> SubmittedAsync()
    {
        var created = await CreateAsync();
        created.IsSuccess.Should().BeTrue(created.IsFailure ? created.Error.Message : null);
        (await AttachApprovalAsync(created.Value.Request.Id)).IsSuccess.Should().BeTrue();
        var submitted = await SubmitAsync(created.Value.Request.Id, total: 52550m);
        submitted.IsSuccess.Should().BeTrue(submitted.IsFailure ? submitted.Error.Message : null);
        return submitted.Value;
    }

    private Task PayAsync(ReinvoicingDetailDto detail) =>
        _f.PayByCartAsync(_f.Owner, _f.OwnTaxId, detail.Request.Charges.Select(c => (PayableItemTypes.LocalCharge, c.ChargeId)).ToArray());

    [Fact]
    public async Task Quote_ShouldChargeTheFeeAndTheVatLossTogether()
    {
        var quote = await new GetReinvoicingQuoteQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Reinvoicing())
            .Handle(new GetReinvoicingQuoteQuery(_invoice.Id), CancellationToken.None);

        quote.Value.Eligible.Should().BeTrue();
        quote.Value.Fee!.TotalAmount.Should().Be(29750m);
        quote.Value.VatLoss!.Amount.Should().Be(22800m);
        quote.Value.TotalAmount.Should().Be(52550m);
        quote.Value.Currency.Should().Be("CLP");
    }

    [Fact]
    public async Task Submit_ShouldRequireTheApprovalAndTheTariff_AndAskTheNewEntityToAccept()
    {
        var created = await CreateAsync();
        var id = created.Value.Request.Id;

        var withoutApproval = await SubmitAsync(id);
        await AttachApprovalAsync(id);
        var notAccepted = await SubmitAsync(id, accept: false);
        var changed = await SubmitAsync(id, total: 1m);
        var submitted = await SubmitAsync(id, total: 52550m);

        created.Value.Request.Status.Should().Be(ServiceRequestStatus.Draft);
        withoutApproval.Error.Should().Be(DomainErrors.Reinvoicing.ApprovalRequired);
        notAccepted.Error.Should().Be(DomainErrors.ServiceRequest.TariffNotAccepted);
        changed.Error.Code.Should().Be("ServiceRequest.TariffChanged");

        var detail = submitted.Value;
        detail.Request.Status.Should().Be(ServiceRequestStatus.PendingPayment);
        detail.Request.Quote!.TotalAmount.Should().Be(52550m);
        detail.Request.Billing.TaxId.Should().Be(NewTaxId);
        detail.Request.Charges.Select(c => (c.ConceptCode, c.TotalAmount)).Should().BeEquivalentTo(
            [(ChargeConceptCodes.Reinvoicing, 29750m), (ChargeConceptCodes.VatLoss, 22800m)]);
        detail.Reissue.ApprovalAttached.Should().BeTrue();
        detail.Reissue.AcceptanceStatus.Should().Be(ReinvoicingAcceptanceStatus.Pending);
        detail.Reissue.VatLossAmount.Should().Be(22800m);
        _f.Emails.Should().ContainSingle(e => e.To == "facturacion@comercialaustral.cl" && e.Body.Contains(ReinvoicingService.AcceptancePath));
        _f.Db.InvoiceReissueList.Single().AcceptanceTokenHash.Should().Be(ReinvoicingService.Hash(TokenFromEmail()));
    }

    [Fact]
    public async Task Issuance_ShouldWaitForTheAcceptance_AndSupersedeTheOriginalInvoice()
    {
        var detail = await SubmittedAsync();
        await PayAsync(detail);
        var request = _f.Db.ServiceRequestList.Single();

        request.Status.Should().Be(ServiceRequestStatus.InProgress, "paid but not accepted by the new legal entity");
        _f.Issued.Should().BeEmpty();
        await _f.InvoiceProvider.DidNotReceiveWithAnyArgs().IssueAsync(default!, default);

        var view = await new GetReinvoicingAcceptanceQueryHandler(_f.Db)
            .Handle(new GetReinvoicingAcceptanceQuery(TokenFromEmail()), CancellationToken.None);
        var accepted = await RespondAsync(TokenFromEmail(), accept: true);
        await _f.ProcessOutboxAsync();
        var replay = await RespondAsync(TokenFromEmail(), accept: true);

        view.Value.NewTaxId.Should().Be(NewTaxId);
        view.Value.TotalAmount.Should().Be(52550m);
        accepted.Value.AcceptanceStatus.Should().Be(ReinvoicingAcceptanceStatus.Accepted);
        replay.Error.Should().Be(DomainErrors.Reinvoicing.AcceptanceClosed);

        var issued = _f.Issued.Single();
        issued.Receiver.TaxId.Should().Be(NewTaxId);
        issued.ExternalReference.Should().Be(request.RequestNumber);
        issued.Totals.TotalAmount.Should().Be(142800m);

        var reissue = _f.Db.InvoiceReissueList.Single();
        reissue.AcceptedByTaxId.Should().Be("12345678-5");
        reissue.AcceptedFromAddress.Should().Be("10.0.0.8");
        var replacement = _f.Db.CustomerInvoiceList.Single(i => i.Id == reissue.NewInvoiceId);
        replacement.TaxId.Should().Be(NewTaxId);
        replacement.LegalName.Should().Be("Comercial Austral SpA");
        replacement.SupersedesInvoiceId.Should().Be(_invoice.Id);
        replacement.IsPayable.Should().BeTrue();
        _invoice.Status.Should().Be(InvoiceStatus.Superseded);
        _invoice.SupersededByInvoiceId.Should().Be(replacement.Id);
        _invoice.IsPayable.Should().BeFalse();
        request.Status.Should().Be(ServiceRequestStatus.Completed);

        // M7-01: ambas facturas visibles y vinculadas; la original ya no es pagable.
        var invoices = await new GetInvoicesQueryHandler(_f.Db, _f.Owner.CurrentUser, _f.Owner.Evaluator(_f.Db))
            .Handle(new GetInvoicesQuery(), CancellationToken.None);
        var original = invoices.Value.Items.Single(i => i.Id == _invoice.Id);
        original.Status.Should().Be(InvoiceStatus.Superseded);
        original.SupersededByInvoiceId.Should().Be(replacement.Id);
        original.IsPayable.Should().BeFalse();
        invoices.Value.Items.Single(i => i.Id == replacement.Id).SupersedesInvoiceId.Should().Be(_invoice.Id);
    }

    [Fact]
    public async Task AcceptanceBeforePayment_ShouldIssueWhenThePaymentIsReleased()
    {
        var detail = await SubmittedAsync();

        (await RespondAsync(TokenFromEmail(), accept: true)).IsSuccess.Should().BeTrue();
        _f.Db.PaymentOutboxMessageList.Should().BeEmpty("nothing is issued before the payment");
        await PayAsync(detail);

        _f.Issued.Should().ContainSingle();
        _f.Db.ServiceRequestList.Single().Status.Should().Be(ServiceRequestStatus.Completed);
    }

    [Fact]
    public async Task DeclinedCharge_ShouldNeverIssueTheInvoice()
    {
        var detail = await SubmittedAsync();

        var missingTaxId = await RespondAsync(TokenFromEmail(), accept: false, taxId: null);
        var declined = await RespondAsync(TokenFromEmail(), accept: false);
        await PayAsync(detail);

        missingTaxId.Error.Should().Be(DomainErrors.Reinvoicing.AcceptorTaxIdRequired);
        declined.Value.AcceptanceStatus.Should().Be(ReinvoicingAcceptanceStatus.Declined);
        _f.Issued.Should().BeEmpty();
        _invoice.Status.Should().Be(InvoiceStatus.Pending);
        _f.Db.ServiceRequestList.Single().Status.Should().Be(ServiceRequestStatus.InProgress);
        _f.Payments.Notifications.Published.Should().Contain(n => n.Type == NotificationTypes.ReinvoicingDeclined);
    }

    [Fact]
    public async Task Requests_ShouldBeValidated()
    {
        var creditNote = _f.Payments.AddInvoice(_f.Owner.Organization, _f.Db.BillsOfLadingList.Single(), 1000m, sii: "100199",
            type: InvoiceDocumentTypes.CreditNote, status: InvoiceStatus.Paid);
        var noFolio = _f.Payments.AddInvoice(_f.Owner.Organization, _f.Db.BillsOfLadingList.Single(), 1000m, sii: null);

        var sameTaxId = await CreateAsync(billing: NewBilling("76.123.456-7"));
        var noteResult = await CreateAsync(creditNote.Id);
        var noFolioResult = await CreateAsync(noFolio.Id);
        var first = await CreateAsync();
        var duplicate = await CreateAsync();
        var generic = await _f.Services.Create().Handle(
            new CreateServiceRequestCommand(ServiceDefinitionCodes.IaoReinvoicing, "BL-IAO", null, null, NewBilling(), false), CancellationToken.None);
        var invalidToken = await RespondAsync("not-a-token", accept: true);
        var stranger = _f.Payments.NewActor();
        var foreign = await new CreateReinvoicingCommandHandler(_f.Db, stranger.CurrentUser, stranger.Evaluator(_f.Db), _f.Reinvoicing())
            .Handle(new CreateReinvoicingCommand(_invoice.Id, NewBilling(), null, null), CancellationToken.None);

        sameTaxId.Error.Should().Be(DomainErrors.Reinvoicing.SameTaxId);
        noteResult.Error.Should().Be(DomainErrors.Reinvoicing.InvoiceNotEligible);
        noFolioResult.Error.Should().Be(DomainErrors.Reinvoicing.InvoiceNotEligible);
        first.IsSuccess.Should().BeTrue();
        duplicate.Error.Should().Be(DomainErrors.Reinvoicing.AlreadyRequested);
        generic.Error.Should().Be(DomainErrors.Reinvoicing.UseDedicatedFlow);
        invalidToken.Error.Should().Be(DomainErrors.Reinvoicing.AcceptanceNotFound);
        foreign.Error.Code.Should().Be("Invoice.NotFound");
    }
}
