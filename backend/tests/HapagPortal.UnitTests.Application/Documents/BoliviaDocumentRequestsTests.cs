namespace HapagPortal.UnitTests.Application.Documents;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.FreightCertificate;
using HapagPortal.Application.Documents.ReleaseLetter;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.ServiceRequests.Queue;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.ServiceRequests;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Documentos de importación de Bolivia de la Ola J: certificado de flete (M6-02) sin pago ni carro, firmado, guardado y
/// enviado; y carta de liberación y desconsolidado (M6-08) con tipo de sociedad, transportista, TATC registrado al enviar y
/// al aprobar, aprobación de Customer Service y emisión al aprobar. Permisos de M1-11 en el servidor.
/// </summary>
public sealed class BoliviaDocumentRequestsTests
{
    private const string BlNumber = "HLCU-BO-001";
    private const string Container1 = "HLXU8899001";
    private const string Container2 = "HLXU8899002";
    private readonly ServiceRequestsFixture _f = new();
    private readonly BillOfLading _bl;

    public BoliviaDocumentRequestsTests()
    {
        _bl = _f.Rules.OwnBl(BlNumber, country: CountryCodes.Bolivia);
        _bl.FreightAmount = 1950m;
        _bl.FreightCurrency = "USD";
        _f.Rules.AddContainer(_bl, Container1, "20DV");
        _f.Rules.AddContainer(_bl, Container2, "40HC");
        FreightDefinition();
        ReleaseDefinition();
    }

    private PaymentsFixture.Actor Owner => _f.Owner;

    private ServiceDefinition FreightDefinition() =>
        _f.AddDefinition(ServiceDefinitionCodes.FreightCertificate, ServiceOperations.Import, CountryCodes.Bolivia, ShipmentActionCodes.GenerateFreightCertificate,
            [
                new(FreightCertificateFields.ConsigneeName, "Consignatario", "Consignee", ServiceInputFieldTypes.Text, true, MaxLength: 200),
                new(FreightCertificateFields.ConsigneeTaxId, "NIT", "Tax ID", ServiceInputFieldTypes.Text, true, MaxLength: 30),
                new(FreightCertificateFields.Purpose, "Finalidad", "Purpose", ServiceInputFieldTypes.Select, true,
                    FreightCertificatePurposes.All.Select(p => new ServiceInputOption(p, p, p)).ToList()),
                new(FreightCertificateFields.Recipient, "Dirigido a", "Addressed to", ServiceInputFieldTypes.Text, MaxLength: 200),
                new(FreightCertificateFields.Notes, "Observaciones", "Remarks", ServiceInputFieldTypes.TextArea, MaxLength: 1000)
            ],
            d => d.ChargeConceptCode = ChargeConceptCodes.FreightCertificate);

    private ServiceDefinition ReleaseDefinition() =>
        _f.AddDefinition(ServiceDefinitionCodes.ReleaseLetter, ServiceOperations.Import, CountryCodes.Bolivia, ShipmentActionCodes.GenerateReleaseLetter,
            [
                ServiceRequestsFixture.ContainersField(),
                new(ReleaseLetterFields.LegalEntityType, "Tipo", "Type", ServiceInputFieldTypes.Select, true,
                    LegalEntityTypes.All.Select(t => new ServiceInputOption(t, t, t)).ToList()),
                new(ReleaseLetterFields.ConsigneeName, "Consignatario", "Consignee", ServiceInputFieldTypes.Text, true),
                new(ReleaseLetterFields.ConsigneeTaxId, "NIT", "Tax ID", ServiceInputFieldTypes.Text, true),
                new(ReleaseLetterFields.ConsigneeAddress, "Domicilio", "Address", ServiceInputFieldTypes.Text),
                new(ReleaseLetterFields.LegalRepresentativeName, "Representante", "Representative", ServiceInputFieldTypes.Text),
                new(ReleaseLetterFields.LegalRepresentativeId, "Documento", "ID", ServiceInputFieldTypes.Text),
                new(ReleaseLetterFields.CarrierName, "Transportista", "Carrier", ServiceInputFieldTypes.Text, true),
                new(ReleaseLetterFields.CarrierTaxId, "NIT", "Tax ID", ServiceInputFieldTypes.Text, true),
                new(ReleaseLetterFields.DriverName, "Conductor", "Driver", ServiceInputFieldTypes.Text),
                new(ReleaseLetterFields.DriverId, "Documento", "ID", ServiceInputFieldTypes.Text),
                new(ReleaseLetterFields.TruckPlate, "Patente", "Plate", ServiceInputFieldTypes.Text),
                new(ReleaseLetterFields.Observations, "Observaciones", "Remarks", ServiceInputFieldTypes.TextArea)
            ],
            d =>
            {
                d.RequiresContainers = true;
                d.ApprovalTeam = ServiceTeams.CustomerService;
                d.ChargeConceptCode = ChargeConceptCodes.ReleaseLetter;
            });

    private RequestFreightCertificateCommandHandler Freight(PaymentsFixture.Actor actor) =>
        new(_f.Db, actor.Evaluator(_f.Db), actor.CurrentUser, _f.Workflow(), _f.DocumentService());

    private GetFreightCertificateQueryHandler FreightContext(PaymentsFixture.Actor actor) =>
        new(_f.Db, actor.Evaluator(_f.Db), _f.DocumentSettings);

    private RequestReleaseLetterCommandHandler Release(PaymentsFixture.Actor actor) =>
        new(_f.Db, actor.Evaluator(_f.Db), actor.CurrentUser, _f.Workflow(), _f.ReleaseLetters());

    private static RequestFreightCertificateCommand FreightCommand(string blNumber = BlNumber) =>
        new(blNumber, "Comercial Altiplano SRL", "1023456017", FreightCertificatePurposes.Customs, "Aduana Nacional de Bolivia", null);

    private static RequestReleaseLetterCommand ReleaseCommand(
        IReadOnlyList<string>? containers = null,
        Guid? carrierId = null,
        string? carrierName = "Transportes Illimani SRL",
        string? carrierTaxId = "4455667018") =>
        new(BlNumber, containers ?? [Container1], LegalEntityTypes.Company, "Comercial Altiplano SRL", "1023456017",
            "Av. Arce 2631, La Paz", "Marcela Quispe", "4876512 LP", carrierId, carrierName, carrierTaxId,
            "Juan Mamani", "6123987 LP", "2345-KTR", null);

    private void TatcReturns(params (string Container, string SourceStatus, string? Number)[] containers) =>
        _f.Tatc.GetByBlNumberAsync(BlNumber, Arg.Any<CancellationToken>()).Returns(Result<BlTatcRecord?>.Success(new BlTatcRecord(
            BlNumber, CountryCodes.Bolivia, DateTime.UtcNow,
            containers.Select(c => new ContainerTatcRecord(c.Container, c.Number, c.SourceStatus, null, "ALM-ARI-01", [])).ToList())));

    // ── Certificado de flete (M6-02) ──────────────────────────────

    [Fact]
    public async Task FreightCertificate_ShouldBeIssuedSignedStoredAndEmailed_WithoutPaymentOrCart()
    {
        var result = await Freight(Owner).Handle(FreightCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : null);
        var document = result.Value.Document;
        document.DocumentType.Should().Be(ShipmentDocumentTypes.FreightCertificate);
        document.DocumentNumber.Should().StartWith(DocumentPrefixes.FreightCertificate);
        document.Signed.Should().BeTrue();
        document.SignatureProvider.Should().Be("TestSigner");
        _f.Signer.Requests.Should().ContainSingle(r => r.DocumentType == SignatureDocumentTypes.FreightCertificate);
        _f.DocumentStorage.Files.Should().ContainSingle();
        result.Value.SentTo.Should().Equal(Owner.Organization.Email);
        await _f.Email.Received(1).SendEmailAsync(
            Owner.Organization.Email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<EmailAttachment>>(), Arg.Any<CancellationToken>());

        // Sin pago ni carro: la solicitud se completa sin cargos y nada llega al carro.
        result.Value.Request.Status.Should().Be(ServiceRequestStatus.Completed);
        result.Value.Request.Charges.Should().BeEmpty();
        result.Value.Request.Quote.Should().BeNull();
        _f.Db.LocalChargeList.Should().NotContain(c => c.ChargeType == ChargeConceptCodes.FreightCertificate);
        _f.Db.CartItemList.Should().BeEmpty();
        result.Value.Request.Timeline.Select(e => e.ToStatus).Should().Equal(
            ServiceRequestStatus.Draft, ServiceRequestStatus.Submitted, ServiceRequestStatus.Completed);

        _f.Renderer.Rendered.Single().Sections.Should().Contain(s => s.Heading == "Flete"
            && s.Fields!.Any(f => f.Label == "Monto del flete" && f.Value == "1.950,00 USD"));
        _f.Db.ShipmentDocumentList.Single().GenerationKey.Should().Be(DocumentServiceRequests.GenerationKey(result.Value.Request.Id));
    }

    [Fact]
    public async Task FreightCertificateContext_ShouldReportTheFreeModeAndTheIssuedCertificates()
    {
        await Freight(Owner).Handle(FreightCommand(), CancellationToken.None);

        var context = await FreightContext(Owner).Handle(new GetFreightCertificateQuery(BlNumber), CancellationToken.None);

        context.Value.Applicable.Should().BeTrue();
        context.Value.CanRequest.Should().BeTrue();
        context.Value.PaymentMode.Should().Be(FreightCertificateModes.Free);
        context.Value.RequiresPayment.Should().BeFalse();
        context.Value.Freight.Should().Be(new FreightInfoDto(1950m, "USD", null));
        context.Value.Requests.Should().ContainSingle(r => r.Status == ServiceRequestStatus.Completed);
        context.Value.Documents.Should().ContainSingle(d => d.DocumentType == ShipmentDocumentTypes.FreightCertificate);
    }

    [Fact]
    public async Task FreightCertificate_ShouldApplyOnlyToBoliviaImports()
    {
        _f.Rules.OwnBl("HLCU-CL-001");

        var result = await Freight(Owner).Handle(FreightCommand("HLCU-CL-001"), CancellationToken.None);

        result.Error.Should().Be(DomainErrors.FreightCertificate.NotApplicable);
        _f.Db.ServiceRequestList.Should().BeEmpty();
    }

    [Fact]
    public async Task FreightCertificate_ShouldFollowTheMatrix_CustomerAndViewerCannot_GrantedCarrierCan()
    {
        // Customer sin rol de consignee: X en M1-11.
        _f.Rules.OwnBl("HLCU-BO-CUST", country: CountryCodes.Bolivia, consignee: false);
        (await Freight(Owner).Handle(FreightCommand("HLCU-BO-CUST"), CancellationToken.None)).Error.Should().Be(Error.Forbidden);

        // Perfil de consulta: ve el embarque pero no solicita.
        var viewerUser = AccessTestData.AddMember(_f.Db, Owner.Organization, profile: RoleCodes.OrgViewer);
        var viewer = new PaymentsFixture.Actor(Owner.Organization, viewerUser, AccessTestData.CurrentUser(viewerUser));
        (await Freight(viewer).Handle(FreightCommand(), CancellationToken.None)).Error.Should().Be(Error.Forbidden);

        // Transportista: X(o); sin el permiso expreso no puede, con él sí, y queda el mandante (NF-14).
        var carrier = _f.Payments.NewActor(OrganizationTypes.Carrier);
        var grant = ThirdPartyTestData.AddGrant(_f.Db, Owner.Organization, carrier.Organization, _bl, grantorRole: ShipmentRoleCodes.Consignee);
        (await Freight(carrier).Handle(FreightCommand(), CancellationToken.None)).Error.Should().Be(Error.Forbidden);

        grant.ActionCodes = $"{ShipmentActionCodes.ViewShipment},{ShipmentActionCodes.GenerateFreightCertificate}";
        var granted = await Freight(carrier).Handle(FreightCommand(), CancellationToken.None);

        granted.IsSuccess.Should().BeTrue(granted.IsFailure ? granted.Error.Message : null);
        granted.Value.Document.IssuedForOrganizationId.Should().Be(carrier.Organization.Id);
        _f.Db.ShipmentDocumentList.Single().OnBehalfOfOrganizationId.Should().Be(Owner.Organization.Id);
    }

    // ── Carta de liberación y desconsolidado (M6-08) ──────────────

    [Fact]
    public async Task ReleaseLetter_ShouldRecordTheTatcAndWaitForCustomerServiceApproval()
    {
        TatcReturns((Container1, TatcSourceStatuses.NotIssued, null), (Container2, TatcSourceStatuses.Issued, "TATC-ARI-1"));

        var result = await Release(Owner).Handle(ReleaseCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : null);
        result.Value.Request.Status.Should().Be(ServiceRequestStatus.PendingApproval);
        result.Value.Request.AssignedTeam.Should().Be(ServiceTeams.CustomerService);
        result.Value.Request.ContainerNumbers.Should().Equal(Container1);
        result.Value.LegalEntityType.Should().Be(LegalEntityTypes.Company);
        result.Value.TatcAtSubmission.Available.Should().BeTrue();
        result.Value.TatcAtSubmission.Status.Should().Be(TatcStatuses.NotIssued);
        result.Value.TatcAtSubmission.Containers.Should().ContainSingle(c => c.ContainerNumber == Container1 && c.Status == TatcStatuses.NotIssued);
        result.Value.TatcAtApproval.Should().BeNull();
        result.Value.Document.Should().BeNull();
        result.Value.Request.Timeline.Last().Notes.Should().Contain("TATC NotIssued");
        _f.Db.ShipmentDocumentList.Should().BeEmpty();
        _f.Db.LocalChargeList.Should().NotContain(c => c.ChargeType == ChargeConceptCodes.ReleaseLetter);
    }

    [Fact]
    public async Task ReleaseLetter_ApprovedByCustomerService_ShouldIssueThePdfForTheSelectedUnits()
    {
        TatcReturns((Container1, TatcSourceStatuses.Issued, "TATC-ARI-1"), (Container2, TatcSourceStatuses.NotIssued, null));
        var submitted = await Release(Owner).Handle(ReleaseCommand(), CancellationToken.None);

        var approved = await _f.Approve().Handle(new ApproveServiceRequestCommand(submitted.Value.Request.Id, "Datos verificados."), CancellationToken.None);

        approved.IsSuccess.Should().BeTrue(approved.IsFailure ? approved.Error.Message : null);
        approved.Value.Status.Should().Be(ServiceRequestStatus.Completed);
        approved.Value.ResolutionNotes.Should().StartWith("Datos verificados.").And.Contain(DocumentPrefixes.ReleaseLetter);

        var document = _f.Db.ShipmentDocumentList.Single();
        document.DocumentType.Should().Be(ShipmentDocumentTypes.ReleaseLetter);
        document.ContainerNumbers.Should().Be(Container1);
        document.IssuedForOrganizationId.Should().Be(Owner.Organization.Id);
        document.SignatureId.Should().BeNull();
        _f.DocumentStorage.Files.Should().ContainSingle();
        await _f.Email.Received(1).SendEmailAsync(
            Owner.Organization.Email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<EmailAttachment>>(), Arg.Any<CancellationToken>());

        var letter = _f.Db.ReleaseLetterRequestList.Single();
        letter.DocumentId.Should().Be(document.Id);
        letter.TatcStatusAtApproval.Should().Be(TatcStatuses.Issued);
        var model = _f.Renderer.Rendered.Single();
        model.Sections.Should().Contain(s => s.Heading == "TATC de las unidades" && s.Table!.Rows.Single()[0] == Container1);
        model.Sections.Should().Contain(s => s.Heading == "Consignatario" && s.Fields!.Any(f => f.Label == "Representante legal" && f.Value == "Marcela Quispe"));

        var detail = await new GetReleaseLetterRequestQueryHandler(_f.Db, Owner.Evaluator(_f.Db), _f.ReleaseLetters())
            .Handle(new GetReleaseLetterRequestQuery(submitted.Value.Request.Id), CancellationToken.None);
        detail.Value.Document!.Id.Should().Be(document.Id);
        detail.Value.TatcAtApproval!.Containers.Should().ContainSingle(c => c.TatcNumber == "TATC-ARI-1");
    }

    [Fact]
    public async Task ReleaseLetter_WhenTheTatcRuleIsOn_ApprovalNeedsTheTatcIssued()
    {
        _f.DocumentSettings.ReleaseLetterRequiresIssuedTatc = true;
        TatcReturns((Container1, TatcSourceStatuses.NotIssued, null));
        var submitted = await Release(Owner).Handle(ReleaseCommand(), CancellationToken.None);

        var blocked = await _f.Approve().Handle(new ApproveServiceRequestCommand(submitted.Value.Request.Id, null), CancellationToken.None);

        blocked.Error.Code.Should().Be("ReleaseLetter.TatcNotIssued");
        _f.Db.ServiceRequestList.Single().Status.Should().Be(ServiceRequestStatus.PendingApproval);
        _f.Db.ShipmentDocumentList.Should().BeEmpty();

        TatcReturns((Container1, TatcSourceStatuses.Issued, "TATC-ARI-9"));
        var approved = await _f.Approve().Handle(new ApproveServiceRequestCommand(submitted.Value.Request.Id, null), CancellationToken.None);

        approved.Value.Status.Should().Be(ServiceRequestStatus.Completed);
    }

    [Fact]
    public async Task ReleaseLetter_ShouldRequireACarrier_AndOnlyAcceptCarriersLinkedByTheOrganization()
    {
        var missing = await Release(Owner).Handle(ReleaseCommand(carrierName: null, carrierTaxId: null), CancellationToken.None);
        missing.Error.Should().Be(DomainErrors.ReleaseLetter.CarrierRequired);

        var stranger = AccessTestData.AddOrganization(_f.Db, OrganizationTypes.Carrier);
        var unknown = await Release(Owner).Handle(ReleaseCommand(carrierId: stranger.Id), CancellationToken.None);
        unknown.Error.Should().Be(DomainErrors.ReleaseLetter.CarrierNotFound);

        var carrier = AccessTestData.AddOrganization(_f.Db, OrganizationTypes.Carrier, name: "Transportes Linked");
        ThirdPartyTestData.AddGrant(_f.Db, Owner.Organization, carrier, _bl, grantorRole: ShipmentRoleCodes.Consignee);
        var linked = await Release(Owner).Handle(ReleaseCommand(carrierId: carrier.Id, carrierName: null, carrierTaxId: null), CancellationToken.None);

        linked.IsSuccess.Should().BeTrue(linked.IsFailure ? linked.Error.Message : null);
        linked.Value.CarrierOrganizationId.Should().Be(carrier.Id);
        linked.Value.Request.InputValues.GetProperty(ReleaseLetterFields.CarrierName).GetString().Should().Be("Transportes Linked");
    }

    [Fact]
    public async Task ReleaseLetter_ShouldNotAcceptAUnitAlreadyInAPendingLetter()
    {
        (await Release(Owner).Handle(ReleaseCommand([Container1]), CancellationToken.None)).IsSuccess.Should().BeTrue();

        var overlapping = await Release(Owner).Handle(ReleaseCommand([Container1, Container2]), CancellationToken.None);
        var other = await Release(Owner).Handle(ReleaseCommand([Container2]), CancellationToken.None);

        overlapping.Error.Should().Be(DomainErrors.ReleaseLetter.AlreadyRequested);
        other.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseLetter_ShouldFollowTheMatrix_AndRejectUnitsOfAnotherBl()
    {
        _f.Rules.OwnBl("HLCU-BO-CUST", country: CountryCodes.Bolivia, consignee: false);
        var customer = await Release(Owner).Handle(ReleaseCommand() with { BlNumber = "HLCU-BO-CUST" }, CancellationToken.None);
        customer.Error.Should().Be(Error.Forbidden);

        var foreignUnit = await Release(Owner).Handle(ReleaseCommand(["HLXU0000000"]), CancellationToken.None);
        foreignUnit.Should().BeAssignableTo<IValidationResult>()
            .Which.Errors.Should().ContainSingle(e => e.Code == $"InputValues.{ReleaseLetterFields.Containers}");
    }

    [Fact]
    public void ReleaseLetter_CompanyRequiresAddressAndLegalRepresentative_NaturalPersonDoesNot()
    {
        var validator = new RequestReleaseLetterCommandValidator();

        var company = validator.Validate(ReleaseCommand() with { ConsigneeAddress = null, LegalRepresentativeName = null, LegalRepresentativeId = null });
        var person = validator.Validate(ReleaseCommand() with
        {
            LegalEntityType = LegalEntityTypes.NaturalPerson, ConsigneeAddress = null, LegalRepresentativeName = null, LegalRepresentativeId = null
        });

        company.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(
            [nameof(RequestReleaseLetterCommand.ConsigneeAddress), nameof(RequestReleaseLetterCommand.LegalRepresentativeName),
             nameof(RequestReleaseLetterCommand.LegalRepresentativeId)]);
        person.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Repository_ShouldOfferTheBoliviaRequestsOnlyToWhomTheMatrixAllows()
    {
        var list = await new GetShipmentDocumentsQueryHandler(_f.Db, Owner.Evaluator(_f.Db), _f.Rules.ChargeRules(), new ResponsibilityLetterStatus(_f.Db))
            .Handle(new GetShipmentDocumentsQuery(BlNumber), CancellationToken.None);

        list.Value.Actions.CanRequestFreightCertificate.Should().BeTrue();
        list.Value.Actions.CanRequestReleaseLetter.Should().BeTrue();
        list.Value.Actions.CanRequestTransshipmentCertificate.Should().BeFalse();
    }
}
