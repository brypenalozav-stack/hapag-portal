namespace HapagPortal.UnitTests.Application.Shipments;

using FluentAssertions;
using HapagPortal.Application.ChargeRules.Charges;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Application.Demurrage.State;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.NoDebt;
using HapagPortal.Application.Documents.ReleaseLetter;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.LiberationStatus;
using HapagPortal.Application.Shipments.Tatc;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Consulta de BL y TATC con el estado de liberación (M2-09, CL-IMP-13, BO-IMP-13): pasos de Chile y Bolivia, avance,
/// pasos sin información, ventana del TATC (72 h / 48 h desde Callao al norte de Chile), contenedores SOW y la solicitud
/// del TATC de un BL listo.
/// </summary>
public sealed class ReleaseStatusTests
{
    private const string Bl = "HLCUREL2601001";
    private static readonly Guid BlId = Guid.NewGuid();
    private static readonly DateTime Now = DateTime.UtcNow;

    private readonly MockApplicationDbContext _db = new();
    private readonly StubSender _sender = new();
    private readonly DocumentSettings _settings = new();

    // Estado configurable de cada consulta compuesta; por defecto, un BL de importación de Chile listo para el TATC.
    private string _country = CountryCodes.Chile;
    private string _operation = ShipmentOperations.Import;
    private string[] _actions = [ShipmentActionCodes.ViewShipment, ShipmentActionCodes.ViewReleaseRequirements, ShipmentActionCodes.PayFreight];
    private ShipmentFreightDto? _freight = new(3500m, "USD", "PAID");
    private string? _pod = "CLSAI";
    private string? _finalDestination;
    private DateTime? _eta = Now.AddHours(24);

    private Result<ShipmentChargesDto> _charges = Result<ShipmentChargesDto>.Success(Charges([Charge(ChargeConceptCodes.GateIn, ChargeStatus.Paid, ChargeOutcomes.Paid)]));
    private Result<ShipmentDocumentsDto> _documents = Result<ShipmentDocumentsDto>.Success(Documents([]));
    private Result<DemurrageStatusDto> _demurrage = Result<DemurrageStatusDto>.Success(Demurrage(DemurrageStates.NoDemurrage));
    private Result<ShipmentTatcDto> _tatc = Result<ShipmentTatcDto>.Success(Tatc(TatcStatuses.NotIssued, canRequest: true));
    private Result<NoDebtEligibilityDto> _noDebt = Result<NoDebtEligibilityDto>.Success(NoDebt(eligible: true));
    private Result<ReleaseLetterContextDto> _releaseLetter = Result<ReleaseLetterContextDto>.Success(ReleaseLetter([], [Doc(ShipmentDocumentTypes.ReleaseLetter, "CLB-1")]));

    public ReleaseStatusTests()
    {
        _sender
            .On<GetShipmentDetailQuery, ShipmentDetailDto>(_ => Result<ShipmentDetailDto>.Success(Detail()))
            .On<GetShipmentChargesQuery, ShipmentChargesDto>(_ => _charges)
            .On<GetShipmentDocumentsQuery, ShipmentDocumentsDto>(_ => _documents)
            .On<GetDemurrageStatusQuery, DemurrageStatusDto>(_ => _demurrage)
            .On<GetShipmentTatcQuery, ShipmentTatcDto>(_ => _tatc)
            .On<GetNoDebtEligibilityQuery, NoDebtEligibilityDto>(_ => _noDebt)
            .On<GetReleaseLetterQuery, ReleaseLetterContextDto>(_ => _releaseLetter)
            .OnAsync<GetReleaseStatusQuery, ReleaseStatusDto>((q, ct) => Handler().Handle(q, ct))
            .On<RequestTatcBatchCommand, TatcBatchDto>(c => Result<TatcBatchDto>.Success(
                new TatcBatchDto(Guid.NewGuid(), _country, c.LocationCode, TatcBatchStatus.Completed, "SRC-1", null, 1, 1, 0, "user@test", Now, Now, null)));

        AddBl(portOfLoading: "Shanghai (CNSHA)", freightTerms: "Collect", ("HLXU0000001", false), ("HLXU0000002", false));
    }

    // ---------- Datos ----------

    private GetReleaseStatusQueryHandler Handler() => new(_sender, _db, _settings);

    private Task<Result<ReleaseStatusDto>> Evaluate() => Handler().Handle(new GetReleaseStatusQuery(Bl.ToLowerInvariant()), CancellationToken.None);

    private BillOfLading AddBl(string? portOfLoading, string? freightTerms, params (string Number, bool Sow)[] containers)
    {
        _db.BillsOfLadingList.Clear();
        var bl = new BillOfLading
        {
            Id = BlId,
            BLNumber = Bl,
            ShipmentType = "Import",
            FreightCurrency = "USD",
            Status = "Arrived",
            Country = _country,
            PortOfLoading = portOfLoading,
            FreightTerms = freightTerms
        };
        foreach (var (number, sow) in containers)
            bl.Containers.Add(new BLContainer { ContainerNumber = number, ContainerType = "40HC", Status = "Discharged", IsShipperOwned = sow, BillOfLadingId = BlId });
        _db.BillsOfLadingList.Add(bl);
        return bl;
    }

    private ShipmentDetailDto Detail() => new(
        BlId, Bl, "BKG-1", _operation, "Arrived", _country, "Hamburg Express", "025E", "Shanghai (CNSHA)", "San Antonio", "Santiago",
        Now.AddDays(-30), _eta, "Shipper Co", "Importadora Demo SpA", ["Consignee"], "Role", _actions, CanOperate: true,
        CanSelfAssociate: false, RequiresAssociationForPayment: false, _freight,
        [new BLContainerDto(Guid.NewGuid(), "HLXU0000001", "40HC", null, null, "Discharged")], null, null, [],
        PortOfDischargeCode: _pod, FinalDestinationCode: _finalDestination);

    private static RuledChargeDto Charge(string concept, string status, string outcome, decimal payable = 0m, string currency = "CLP",
        string action = ChargeActions.None, string category = ChargeCategories.LocalCharge) =>
        new(Guid.NewGuid(), concept, concept, null, category, 100m, 19m, 119m, currency, status, outcome, payable, 0m, payable, action, null, null, null);

    private static ShipmentChargesDto Charges(IReadOnlyList<RuledChargeDto> charges, bool rulesAvailable = true,
        IReadOnlyList<ProcessRequirementDto>? requirements = null) =>
        new(BlId, Bl, CountryCodes.Chile, "Import", "America/Santiago", new PartyDto(null, "Importadora Demo SpA", "76123456-7", null),
            new CommercialConditionsDto(true, "NEXUS", "76123456-7", null, false, null, [], null, null, false, false, false, null),
            [], charges, [], false, false, rulesAvailable, requirements ?? [], true, Now);

    private static ShipmentDocumentDto Doc(string type, string number) => new(
        Guid.NewGuid(), type, number, ShipmentDocumentStatus.Issued, BlId, Bl, null, CountryCodes.Chile, [], Now, ShipmentDocumentOrigins.Request,
        null, null, $"{number}.pdf", "application/pdf", 100, null, "VC-1", false, null, null, null, null, null, [], null, null, null, null, Now.AddYears(10));

    private static ShipmentDocumentsDto Documents(IReadOnlyList<ShipmentDocumentDto> documents, ResponsibilityLetterStateDto? letter = null) =>
        new(BlId, Bl, null, CountryCodes.Chile, "America/Santiago", documents, [],
            new DocumentActionsDto(false, false, CanIssueResponsibilityLetter: true, false, false),
            letter ?? new ResponsibilityLetterStateDto(false, ProcessRequirementStatus.Missing, false));

    private static AdvanceDemurrageDto NoAdvance() =>
        new(false, AdvanceDemurrageStatus.NotRequired, null, null, null, null, null, false, null, ChargeActions.None, null);

    private static DemurrageStatusDto Demurrage(string state, IReadOnlyList<DemurrageLineDto>? lines = null, AdvanceDemurrageDto? advance = null,
        bool actionAllowed = true) =>
        new(BlId, Bl, CountryCodes.Chile, "America/Santiago", state, ChargeActions.Pay, actionAllowed, null, false, null, [], lines ?? [], null, [], null,
            advance ?? NoAdvance(), [], Now);

    private static DemurrageLineDto Line(string container, string status, decimal total = 300m) =>
        new(Guid.NewGuid(), container, 5, 3, 100m, total, "USD", Now.AddDays(-10), Now, status, false, null, null, null, null);

    private static ShipmentTatcDto Tatc(string? status, bool canRequest, IReadOnlyList<ContainerTatcDto>? containers = null) =>
        new(Bl, null, CountryCodes.Chile, "Arrived", Now.AddHours(24), true, status, containers ?? [], Now, Now, null, canRequest);

    private static NoDebtEligibilityDto NoDebt(bool eligible, IReadOnlyList<NoDebtBlockerDto>? blockers = null) =>
        new(BlId, Bl, CountryCodes.Bolivia, true, eligible, CanRequest: eligible, blockers ?? [], Now);

    private static ServiceRequestSummaryDto Request(string status) =>
        new(Guid.NewGuid(), "SRV-1", "RELEASE_LETTER", "Carta", "Letter", Bl, null, CountryCodes.Bolivia, ShipmentOperations.Import, status,
            null, 0m, null, false, null, null, Now, Now);

    private static ReleaseLetterContextDto ReleaseLetter(IReadOnlyList<ServiceRequestSummaryDto> requests, IReadOnlyList<ShipmentDocumentDto> documents) =>
        new(BlId, Bl, null, CountryCodes.Bolivia, true, CanRequest: true, RequiresIssuedTatc: false, [], new ReleaseLetterConsigneeDto(null, null), [],
            new ReleaseLetterTatcDto(true, null, null, Now, []), [], requests, documents);

    private void Bolivia()
    {
        _country = CountryCodes.Bolivia;
        _pod = "BOLPB";
        _db.BillsOfLadingList[0].Country = CountryCodes.Bolivia;
        _documents = Result<ShipmentDocumentsDto>.Success(Documents([Doc(ShipmentDocumentTypes.NoDebtCertificate, "CLD-1")]));
    }

    private static ReleaseStepDto StepOf(ReleaseStatusDto status, string code) => status.Steps.Single(s => s.Code == code);

    // ---------- Chile ----------

    [Fact]
    public async Task ChileImport_WithEverythingCleared_ShouldBeReleasedAndAllowRequestingTheTatc()
    {
        var result = await Evaluate();

        result.IsSuccess.Should().BeTrue();
        var status = result.Value;
        status.Applicable.Should().BeTrue();
        status.Steps.Select(s => s.Code).Should().Equal(
            ReleaseStepCodes.Freight, ReleaseStepCodes.LocalCharges, ReleaseStepCodes.ResponsibilityLetter, ReleaseStepCodes.Demurrage);
        StepOf(status, ReleaseStepCodes.Freight).Status.Should().Be(ReleaseStepStatuses.Done);
        StepOf(status, ReleaseStepCodes.LocalCharges).Status.Should().Be(ReleaseStepStatuses.Done);
        StepOf(status, ReleaseStepCodes.ResponsibilityLetter).Status.Should().Be(ReleaseStepStatuses.NotRequired);
        StepOf(status, ReleaseStepCodes.ResponsibilityLetter).Reason.Should().Be(ReleaseStepReasons.NotFreightForwarder);
        var demurrage = StepOf(status, ReleaseStepCodes.Demurrage);
        demurrage.Status.Should().Be(ReleaseStepStatuses.Done);
        demurrage.Reason.Should().Be(ReleaseStepReasons.NoDemurrage);

        status.Released.Should().BeTrue();
        status.CompletedSteps.Should().Be(4);
        status.TotalSteps.Should().Be(4);
        status.Tatc.Unlocked.Should().BeTrue();
        status.Tatc.WindowHours.Should().Be(72);
        status.Tatc.WindowOpen.Should().BeTrue();
        status.Tatc.CanRequest.Should().BeTrue();
        status.Notices.Should().Equal(ReleaseNotices.TatcWindow72h);
        status.Containers.Select(c => c.ContainerNumber).Should().Equal("HLXU0000001", "HLXU0000002");

        _sender.SentOf<GetShipmentDetailQuery>().Single().BlNumber.Should().Be(Bl);
        _sender.SentOf<GetNoDebtEligibilityQuery>().Should().BeEmpty();
        _sender.SentOf<GetReleaseLetterQuery>().Should().BeEmpty();
    }

    [Fact]
    public async Task CanRequest_ShouldBeFalse_WhenTatcAlreadyIssuedOrSourceDoesNotAllowGeneration()
    {
        _tatc = Result<ShipmentTatcDto>.Success(Tatc(TatcStatuses.Issued, canRequest: true));
        (await Evaluate()).Value.Tatc.CanRequest.Should().BeFalse();

        _tatc = Result<ShipmentTatcDto>.Success(Tatc(TatcStatuses.NotIssued, canRequest: false));
        (await Evaluate()).Value.Tatc.CanRequest.Should().BeFalse();
    }

    [Fact]
    public async Task CanRequest_ShouldBeFalse_WhenTheWindowIsNotOpenYet()
    {
        _eta = Now.AddHours(100);

        var status = (await Evaluate()).Value;

        status.Released.Should().BeTrue();
        status.Tatc.WindowOpen.Should().BeFalse();
        status.Tatc.AvailableFrom.Should().Be(_eta.Value.AddHours(-72));
        status.Tatc.CanRequest.Should().BeFalse();
    }

    [Fact]
    public async Task PendingRequirements_ShouldBlockTheReleaseWithTheirActionsAndAmounts()
    {
        _freight = new ShipmentFreightDto(3500m, "USD", "PENDING");
        _charges = Result<ShipmentChargesDto>.Success(Charges(
        [
            Charge(ChargeConceptCodes.GateIn, ChargeStatus.Pending, ChargeOutcomes.Payable, payable: 119m, action: ChargeActions.AddToCart),
            Charge(ChargeConceptCodes.Eds, ChargeStatus.Exempt, ChargeOutcomes.Exempt)
        ]));
        _documents = Result<ShipmentDocumentsDto>.Success(Documents([], new ResponsibilityLetterStateDto(true, ProcessRequirementStatus.Missing, true)));
        _demurrage = Result<DemurrageStatusDto>.Success(Demurrage(DemurrageStates.CalculatedUnpaid,
            [Line("HLXU0000001", DemurrageChargeStatus.Pending, 300m), Line("HLXU0000002", DemurrageChargeStatus.Paid, 200m)]));

        var status = (await Evaluate()).Value;

        var freight = StepOf(status, ReleaseStepCodes.Freight);
        freight.Status.Should().Be(ReleaseStepStatuses.Pending);
        freight.Action.Should().Be(ReleaseStepActions.PayFreight);
        freight.ActionAllowed.Should().BeTrue();
        freight.PendingAmounts.Should().Equal(new ReleaseAmountDto("USD", 3500m));

        var local = StepOf(status, ReleaseStepCodes.LocalCharges);
        local.Status.Should().Be(ReleaseStepStatuses.Pending);
        local.Action.Should().Be(ReleaseStepActions.PayCharges);
        local.ActionAllowed.Should().BeTrue();
        local.PendingAmounts.Should().Equal(new ReleaseAmountDto("CLP", 119m));
        local.Items.Should().HaveCount(2);
        local.Items.Single(i => i.Code == ChargeConceptCodes.Eds).Satisfied.Should().BeTrue();

        var letter = StepOf(status, ReleaseStepCodes.ResponsibilityLetter);
        letter.Status.Should().Be(ReleaseStepStatuses.Pending);
        letter.Action.Should().Be(ReleaseStepActions.IssueResponsibilityLetter);

        var demurrage = StepOf(status, ReleaseStepCodes.Demurrage);
        demurrage.Status.Should().Be(ReleaseStepStatuses.Pending);
        demurrage.Reason.Should().Be(ReleaseStepReasons.CalculatedUnpaid);
        demurrage.Action.Should().Be(ReleaseStepActions.PayDemurrage);
        demurrage.PendingAmounts.Should().Equal(new ReleaseAmountDto("USD", 300m));

        status.Released.Should().BeFalse();
        status.CompletedSteps.Should().Be(0);
        status.Tatc.Unlocked.Should().BeFalse();
        status.Tatc.CanRequest.Should().BeFalse();
        var container = status.Containers.Single(c => c.ContainerNumber == "HLXU0000001");
        container.DemurrageStatus.Should().Be(DemurrageChargeStatus.Pending);
        container.DemurrageAmount.Should().Be(300m);
    }

    [Fact]
    public async Task PrepaidFreight_ShouldNotBeRequired()
    {
        _freight = new ShipmentFreightDto(3500m, "USD", "PENDING");
        AddBl("Shanghai (CNSHA)", "Prepaid", ("HLXU0000001", false));

        var freight = StepOf((await Evaluate()).Value, ReleaseStepCodes.Freight);

        freight.Status.Should().Be(ReleaseStepStatuses.NotRequired);
        freight.Reason.Should().Be(ReleaseStepReasons.Prepaid);
    }

    [Fact]
    public async Task ResponsibilityLetter_ShouldBeRequiredFromChargeRequirements_WhenDocumentsHaveNoLetterState()
    {
        _charges = Result<ShipmentChargesDto>.Success(Charges([],
            requirements: [new ProcessRequirementDto(ProcessRequirements.ResponsibilityLetter, ProcessRequirementStatus.Missing, true, "NEXUS")]));
        var documents = Documents([]) with { ResponsibilityLetter = null };
        _documents = Result<ShipmentDocumentsDto>.Success(documents);

        StepOf((await Evaluate()).Value, ReleaseStepCodes.ResponsibilityLetter).Status.Should().Be(ReleaseStepStatuses.Pending);

        _documents = Result<ShipmentDocumentsDto>.Success(documents with { Documents = [Doc(ShipmentDocumentTypes.ResponsibilityLetter, "CRE-1")] });

        StepOf((await Evaluate()).Value, ReleaseStepCodes.ResponsibilityLetter).Status.Should().Be(ReleaseStepStatuses.Done);
    }

    [Fact]
    public async Task Demurrage_NotCalculated_ShouldAskToCalculate()
    {
        _demurrage = Result<DemurrageStatusDto>.Success(Demurrage(DemurrageStates.NotCalculated));

        var step = StepOf((await Evaluate()).Value, ReleaseStepCodes.Demurrage);

        step.Status.Should().Be(ReleaseStepStatuses.Pending);
        step.Reason.Should().Be(ReleaseStepReasons.NotCalculated);
        step.Action.Should().Be(ReleaseStepActions.CalculateDemurrage);
    }

    [Fact]
    public async Task NotifiedTatcRequest_ShouldAddTheRequestedNotice()
    {
        var batch = new TatcBatch { RequestedByEmail = "u@test", Country = CountryCodes.Chile, LocationCode = "CLSAI", Status = TatcBatchStatus.Completed, CreatedAt = Now.AddMinutes(-5) };
        _db.TatcBatchItemList.Add(new TatcBatchItem { BlNumber = Bl, BillOfLadingId = BlId, Status = TatcBatchItemStatus.Accepted, Batch = batch, BatchId = batch.Id });

        var status = (await Evaluate()).Value;

        status.Notices.Should().Contain(ReleaseNotices.TatcRequested);
        status.Tatc.LastRequestStatus.Should().Be(TatcBatchItemStatus.Accepted);
        status.Tatc.LastRequestedAt.Should().Be(batch.CreatedAt);
    }

    // ---------- Bolivia ----------

    [Fact]
    public async Task BoliviaImport_ShouldAddNoDebtAndReleaseLetter_AndAdvanceDemurrageOnlyWhenRequired()
    {
        Bolivia();

        var status = (await Evaluate()).Value;

        status.Steps.Select(s => s.Code).Should().Equal(
            ReleaseStepCodes.Freight, ReleaseStepCodes.LocalCharges, ReleaseStepCodes.ResponsibilityLetter, ReleaseStepCodes.Demurrage,
            ReleaseStepCodes.NoDebtCertificate, ReleaseStepCodes.ReleaseLetter);
        StepOf(status, ReleaseStepCodes.NoDebtCertificate).Status.Should().Be(ReleaseStepStatuses.Done);
        StepOf(status, ReleaseStepCodes.ReleaseLetter).Status.Should().Be(ReleaseStepStatuses.Done);
        status.Released.Should().BeTrue();

        var advance = new AdvanceDemurrageDto(true, AdvanceDemurrageStatus.Pending, Guid.NewGuid(), "Regla", 500m, "USD", Guid.NewGuid(), true, "ADVANCE",
            ChargeActions.Pay, null);
        _demurrage = Result<DemurrageStatusDto>.Success(Demurrage(DemurrageStates.NoDemurrage, advance: advance));

        status = (await Evaluate()).Value;

        var step = StepOf(status, ReleaseStepCodes.AdvanceDemurrage);
        step.Status.Should().Be(ReleaseStepStatuses.Pending);
        step.Action.Should().Be(ReleaseStepActions.PayAdvanceDemurrage);
        step.ActionAllowed.Should().BeTrue();
        step.PendingAmounts.Should().Equal(new ReleaseAmountDto("USD", 500m));
        status.Released.Should().BeFalse();

        _demurrage = Result<DemurrageStatusDto>.Success(Demurrage(DemurrageStates.NoDemurrage, advance: advance with { Status = AdvanceDemurrageStatus.Paid }));
        StepOf((await Evaluate()).Value, ReleaseStepCodes.AdvanceDemurrage).Status.Should().Be(ReleaseStepStatuses.Done);
    }

    [Fact]
    public async Task Bolivia_NoDebt_NotEligible_ShouldBePendingAndBlockedWithItsBlockers()
    {
        Bolivia();
        _documents = Result<ShipmentDocumentsDto>.Success(Documents([]));
        _noDebt = Result<NoDebtEligibilityDto>.Success(NoDebt(false,
            [new NoDebtBlockerDto("PENDING_CHARGES", ["GATE_IN"], [new NoDebtAmountDto("BOB", 700m)])]));

        var step = StepOf((await Evaluate()).Value, ReleaseStepCodes.NoDebtCertificate);

        step.Status.Should().Be(ReleaseStepStatuses.Pending);
        step.Reason.Should().Be(ReleaseStepReasons.Blocked);
        step.Items.Should().ContainSingle().Which.Code.Should().Be("PENDING_CHARGES");
        step.Items[0].Amount.Should().Be(700m);
        step.PendingAmounts.Should().Equal(new ReleaseAmountDto("BOB", 700m));
    }

    [Fact]
    public async Task Bolivia_NoDebt_Eligible_ShouldOfferTheRequest()
    {
        Bolivia();
        _documents = Result<ShipmentDocumentsDto>.Success(Documents([]));

        var step = StepOf((await Evaluate()).Value, ReleaseStepCodes.NoDebtCertificate);

        step.Status.Should().Be(ReleaseStepStatuses.Pending);
        step.Reason.Should().BeNull();
        step.Action.Should().Be(ReleaseStepActions.RequestNoDebtCertificate);
        step.ActionAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Bolivia_ReleaseLetter_ShouldFollowItsRequests()
    {
        Bolivia();

        _releaseLetter = Result<ReleaseLetterContextDto>.Success(ReleaseLetter([], []));
        var step = StepOf((await Evaluate()).Value, ReleaseStepCodes.ReleaseLetter);
        step.Status.Should().Be(ReleaseStepStatuses.Pending);
        step.Action.Should().Be(ReleaseStepActions.RequestReleaseLetter);
        step.ActionAllowed.Should().BeTrue();

        _releaseLetter = Result<ReleaseLetterContextDto>.Success(ReleaseLetter([Request(ServiceRequestStatus.Rejected)], []));
        StepOf((await Evaluate()).Value, ReleaseStepCodes.ReleaseLetter).Reason.Should().Be(ReleaseStepReasons.Rejected);

        _releaseLetter = Result<ReleaseLetterContextDto>.Success(ReleaseLetter([Request(ServiceRequestStatus.Submitted)], []));
        var status = (await Evaluate()).Value;
        step = StepOf(status, ReleaseStepCodes.ReleaseLetter);
        step.Status.Should().Be(ReleaseStepStatuses.InProgress);
        step.Reason.Should().Be(ReleaseStepReasons.AwaitingApproval);
        status.Released.Should().BeFalse();

        _releaseLetter = Result<ReleaseLetterContextDto>.Success(ReleaseLetter([Request(ServiceRequestStatus.Approved)], []));
        StepOf((await Evaluate()).Value, ReleaseStepCodes.ReleaseLetter).Status.Should().Be(ReleaseStepStatuses.Done);
    }

    [Fact]
    public async Task Bolivia_ReleaseLetterAwaitingTatc_ShouldNotBlockTheTatc()
    {
        Bolivia();
        _settings.ReleaseLetterRequiresIssuedTatc = true;
        _releaseLetter = Result<ReleaseLetterContextDto>.Success(ReleaseLetter([Request(ServiceRequestStatus.Submitted)], []));

        var status = (await Evaluate()).Value;

        var step = StepOf(status, ReleaseStepCodes.ReleaseLetter);
        step.Status.Should().Be(ReleaseStepStatuses.InProgress);
        step.Reason.Should().Be(ReleaseStepReasons.AwaitingTatc);
        status.Released.Should().BeTrue();
        status.CompletedSteps.Should().Be(status.TotalSteps - 1);
        status.Tatc.Unlocked.Should().BeTrue();
        status.Tatc.CanRequest.Should().BeTrue();
    }

    // ---------- Aplicabilidad, permisos y fuentes ----------

    [Fact]
    public async Task ExportBl_ShouldNotBeApplicable()
    {
        _operation = ShipmentOperations.Export;

        var status = (await Evaluate()).Value;

        status.Applicable.Should().BeFalse();
        status.Steps.Should().BeEmpty();
        status.Released.Should().BeFalse();
        status.Tatc.CanRequest.Should().BeFalse();
        _sender.SentOf<GetShipmentChargesQuery>().Should().BeEmpty();
    }

    [Fact]
    public async Task DetailFailure_ShouldPropagate()
    {
        var notFound = Error.NotFound("BillOfLading");
        _sender.On<GetShipmentDetailQuery, ShipmentDetailDto>(Result<ShipmentDetailDto>.Failure(notFound));

        var result = await Evaluate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(notFound);
    }

    [Fact]
    public async Task WithoutReleaseRequirementsPermission_ShouldBeForbidden()
    {
        _actions = [ShipmentActionCodes.ViewShipment];

        var result = await Evaluate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.Forbidden);
    }

    [Fact]
    public async Task FailedSources_ShouldMarkTheirStepsUnavailable()
    {
        _charges = Result<ShipmentChargesDto>.Failure(Error.Forbidden);
        _demurrage = Result<DemurrageStatusDto>.Failure(new Error("Demurrage.SourceUnavailable", "down"));
        _documents = Result<ShipmentDocumentsDto>.Failure(new Error("Documents.Down", "down"));
        _tatc = Result<ShipmentTatcDto>.Failure(new Error("Tatc.SourceUnavailable", "down"));
        _freight = null;

        var status = (await Evaluate()).Value;

        StepOf(status, ReleaseStepCodes.Freight).Should().Match<ReleaseStepDto>(s =>
            s.Status == ReleaseStepStatuses.Unavailable && s.Reason == ReleaseStepReasons.NoPermission);
        StepOf(status, ReleaseStepCodes.LocalCharges).Should().Match<ReleaseStepDto>(s =>
            s.Status == ReleaseStepStatuses.Unavailable && s.Reason == ReleaseStepReasons.NoPermission);
        StepOf(status, ReleaseStepCodes.ResponsibilityLetter).Should().Match<ReleaseStepDto>(s =>
            s.Status == ReleaseStepStatuses.Unavailable && s.Reason == ReleaseStepReasons.SourceUnavailable);
        StepOf(status, ReleaseStepCodes.Demurrage).Should().Match<ReleaseStepDto>(s =>
            s.Status == ReleaseStepStatuses.Unavailable && s.Reason == ReleaseStepReasons.SourceUnavailable);
        status.Released.Should().BeFalse();
        status.Tatc.ErrorCode.Should().Be("Tatc.SourceUnavailable");
        status.Tatc.CanRequest.Should().BeFalse();
    }

    [Fact]
    public async Task ChargeRulesUnavailable_ShouldMarkLocalChargesUnavailable()
    {
        _charges = Result<ShipmentChargesDto>.Success(Charges([], rulesAvailable: false));

        var step = StepOf((await Evaluate()).Value, ReleaseStepCodes.LocalCharges);

        step.Status.Should().Be(ReleaseStepStatuses.Unavailable);
        step.Reason.Should().Be(ReleaseStepReasons.SourceUnavailable);
    }

    // ---------- Ventana y SOW ----------

    [Theory]
    [InlineData("Callao (PECLL)", "CLIQQ", 48)]
    [InlineData("CALLAO", "clmjs", 48)]
    [InlineData("PECLL", "CLANF", 48)]
    [InlineData("Callao (PECLL)", "CLSAI", 72)]
    [InlineData("Shanghai (CNSHA)", "CLIQQ", 72)]
    [InlineData(null, "CLIQQ", 72)]
    [InlineData("Callao (PECLL)", null, 72)]
    public void WindowHours_ShouldBe48hOnlyFromCallaoToNorthernChile(string? portOfLoading, string? dischargeCode, int expected)
    {
        GetReleaseStatusQueryHandler.WindowHours(portOfLoading, dischargeCode).Should().Be(expected);
    }

    [Fact]
    public async Task FromCallaoToIquique_ShouldUseThe48hWindowAndNotice()
    {
        _pod = "CLIQQ";
        _eta = Now.AddHours(60);
        AddBl("Callao (PECLL)", "Collect", ("HLXU0000001", false));

        var status = (await Evaluate()).Value;

        status.Tatc.WindowHours.Should().Be(48);
        status.Tatc.WindowOpen.Should().BeFalse();
        status.Notices.Should().Equal(ReleaseNotices.TatcWindow48h);
    }

    [Fact]
    public async Task OnlyShipperOwnedContainers_ShouldNotAllowRequestingTheTatc()
    {
        AddBl("Shanghai (CNSHA)", "Collect", ("SOWU0000001", true), ("SOWU0000002", true));

        var status = (await Evaluate()).Value;

        status.Released.Should().BeTrue();
        status.Tatc.CanRequest.Should().BeFalse();
        status.Notices.Should().Contain(ReleaseNotices.ShipperOwned);
        status.Containers.Should().OnlyContain(c => c.IsShipperOwned);
    }

    [Fact]
    public async Task Containers_ShouldMergeTheirTatc()
    {
        _tatc = Result<ShipmentTatcDto>.Success(Tatc(TatcStatuses.PartiallyIssued, canRequest: true,
            [new ContainerTatcDto("hlxu0000001", "TATC-1", TatcStatuses.Issued, TatcSourceStatuses.Issued, Now, "WH-1", [])]));

        var containers = (await Evaluate()).Value.Containers;

        var first = containers.Single(c => c.ContainerNumber == "HLXU0000001");
        first.TatcNumber.Should().Be("TATC-1");
        first.WarehouseCode.Should().Be("WH-1");
        containers.Single(c => c.ContainerNumber == "HLXU0000002").TatcStatus.Should().BeNull();
    }

    [Fact]
    public async Task Containers_WithTwoDemurrageLinesForTheSameContainer_ShouldShowThePendingOne()
    {
        // Un recálculo deja una línea pendiente junto a la pagada del mismo contenedor.
        _demurrage = Result<DemurrageStatusDto>.Success(Demurrage(DemurrageStates.CalculatedUnpaid,
            [Line("HLXU0000001", DemurrageChargeStatus.Paid, 100m), Line("hlxu0000001", DemurrageChargeStatus.Pending, 250m)]));

        var result = await Evaluate();

        result.IsSuccess.Should().BeTrue();
        var container = result.Value.Containers.Single(c => c.ContainerNumber == "HLXU0000001");
        container.DemurrageStatus.Should().Be(DemurrageChargeStatus.Pending);
        container.DemurrageAmount.Should().Be(250m);
    }

    // ---------- Solicitud del TATC ----------

    [Fact]
    public async Task RequestTatc_ShouldFail_WhenTheTatcCannotBeRequested()
    {
        _freight = new ShipmentFreightDto(3500m, "USD", "PENDING");

        var result = await new RequestReleaseTatcCommandHandler(_sender).Handle(new RequestReleaseTatcCommand(Bl), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Release.TatcNotRequestable);
        _sender.SentOf<RequestTatcBatchCommand>().Should().BeEmpty();
    }

    [Fact]
    public async Task RequestTatc_ShouldPropagateStatusFailures()
    {
        _actions = [ShipmentActionCodes.ViewShipment];

        var result = await new RequestReleaseTatcCommandHandler(_sender).Handle(new RequestReleaseTatcCommand(Bl), CancellationToken.None);

        result.Error.Should().Be(Error.Forbidden);
        _sender.SentOf<RequestTatcBatchCommand>().Should().BeEmpty();
    }

    [Fact]
    public async Task RequestTatc_ShouldRequestASingleBlBatchByDischargePort()
    {
        var result = await new RequestReleaseTatcCommandHandler(_sender).Handle(new RequestReleaseTatcCommand(Bl), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var command = _sender.SentOf<RequestTatcBatchCommand>().Single();
        command.LocationCode.Should().Be("CLSAI");
        command.BlNumbers.Should().Equal(Bl);
        result.Value.LocationCode.Should().Be("CLSAI");
    }

    [Fact]
    public async Task RequestTatc_WithoutDischargePortCode_ShouldUseTheFinalDestination()
    {
        _pod = null;
        _finalDestination = "CLANF";

        await new RequestReleaseTatcCommandHandler(_sender).Handle(new RequestReleaseTatcCommand(Bl), CancellationToken.None);

        _sender.SentOf<RequestTatcBatchCommand>().Single().LocationCode.Should().Be("CLANF");
    }
}
