namespace HapagPortal.UnitTests.Application.ServiceRequests;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Payments.PostProcessing;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.ServiceRequests.PostPayment;
using HapagPortal.Application.ServiceRequests.Queue;
using HapagPortal.Application.ServiceRequests.Requests;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Modelo estándar de servicios on demand (M2-03, M2-04) y sus servicios de Fase 2: condiciones de disponibilidad
/// (exportación tras el zarpe, importación tras el arribo, XOM solo Bolivia), tarifa por tramos al momento de la
/// solicitud (NF-22), aprobación del equipo ED (M3-09), avance por la liberación del pago (NF-03), matriz M1-11,
/// excepciones XOM (M3-10) y Gate In por devolución con exención de Nexus (M3-15).
/// </summary>
public sealed class ServiceRequestWorkflowTests
{
    private const string OwnTaxId = ServiceRequestsFixture.OwnTaxId;
    private static readonly DateTime Departure = new(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc);

    private readonly ServiceRequestsFixture _f = new();

    private BillOfLading ImportBl(string number, params (string Number, string Type)[] containers)
    {
        var bl = _f.Rules.OwnBl(number, eta: DateTime.UtcNow.AddDays(-5));
        foreach (var (containerNumber, type) in containers)
            _f.Rules.AddContainer(bl, containerNumber, type);
        return bl;
    }

    private async Task<AvailableServicesDto> AvailableAsync(string blNumber, bool includeUnavailable = true, PaymentsFixture.Actor? actor = null)
    {
        var result = await _f.Available(actor).Handle(new GetAvailableServicesQuery(blNumber, null, includeUnavailable), CancellationToken.None);
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Code : null);
        return result.Value;
    }

    [Fact]
    public async Task Availability_ShouldFollowExportDeparture_ImportArrival_AndBoliviaOnlyForXom()
    {
        _f.BlHouse();
        _f.Seals();
        _f.DropOff();
        _f.Xom();
        _f.Rules.AddTariff(ChargeConceptCodes.DropOff, 180000m, "CLP");
        var sailed = _f.ExportBl("EXP-SAILED", DateTime.UtcNow.AddDays(-2));
        _f.Rules.AddContainer(sailed, "HLXU1000001", "20DV");
        var booked = _f.ExportBl("EXP-BOOKED", DateTime.UtcNow.AddDays(5));
        _f.Rules.AddContainer(booked, "HLXU1000002", "20DV");
        ImportBl("IMP-CL", ("HLXU1000003", "40HC"));
        var bolivia = _f.Rules.OwnBl("IMP-BO", CountryCodes.Bolivia, eta: DateTime.UtcNow.AddDays(-3));
        _f.Rules.AddContainer(bolivia, "HLXU1000004", "20DV");

        var exportSailed = await AvailableAsync("EXP-SAILED");
        exportSailed.Services.Single(s => s.Code == ServiceDefinitionCodes.BlHouseTransmission).Available.Should().BeTrue();
        exportSailed.Services.Single(s => s.Code == ServiceDefinitionCodes.SealManagement).UnavailableReasons
            .Should().Equal(ServiceUnavailableReasons.AlreadyDeparted);
        exportSailed.Services.Select(s => s.Code).Should().NotContain([ServiceDefinitionCodes.DropOff, ServiceDefinitionCodes.ContainerAdministrationXom]);

        var exportBooked = await AvailableAsync("EXP-BOOKED");
        exportBooked.Services.Single(s => s.Code == ServiceDefinitionCodes.BlHouseTransmission).UnavailableReasons
            .Should().Equal(ServiceUnavailableReasons.NotDeparted);
        (await AvailableAsync("EXP-BOOKED", includeUnavailable: false)).Services.Select(s => s.Code)
            .Should().Equal(ServiceDefinitionCodes.SealManagement);

        var importCl = await AvailableAsync("IMP-CL", includeUnavailable: false);
        var dropOff = importCl.Services.Should().ContainSingle().Subject;
        dropOff.Code.Should().Be(ServiceDefinitionCodes.DropOff);
        dropOff.CanRequest.Should().BeTrue();
        dropOff.Quote!.TotalAmount.Should().Be(214200m);

        var importBo = await AvailableAsync("IMP-BO");
        importBo.Services.Select(s => s.Code).Should().Equal(ServiceDefinitionCodes.ContainerAdministrationXom);
        importBo.TimeZone.Should().Be("America/La_Paz");
    }

    [Fact]
    public async Task TieredTariff_ShouldUseTheTierInForceAtRequestTime_WithTheHourBoundaryInUtc()
    {
        var definition = _f.BlHouse();
        _f.BlHouseTariffs();
        var bl = _f.ExportBl("EXP-HOUSE", Departure);
        var deadline = Departure.AddHours(72);

        async Task<ServiceQuoteDto> QuoteAt(DateTime now)
        {
            var computed = await _f.Catalog().QuoteAsync(
                new ServiceQuoteInput(definition, bl, _f.Rules.Organization, ShipmentPermissionSet.None, [], new Dictionary<string, decimal>(), now),
                CancellationToken.None);
            computed.IsSuccess.Should().BeTrue();
            return computed.Value.Quote;
        }

        var inTime = await QuoteAt(deadline.AddHours(-1));
        inTime.Timing.Should().Be(ServiceTimings.InTime);
        inTime.TariffCode.Should().Be("PLAZO");
        inTime.TotalAmount.Should().Be(35m);
        inTime.MilestoneSource.Should().Be(ServiceCatalogEvaluator.MilestoneSourceEtd);

        var lastMinuteOfFirstTier = await QuoteAt(deadline.AddHours(24).AddMinutes(59));
        lastMinuteOfFirstTier.Timing.Should().Be(ServiceTimings.Late);
        lastMinuteOfFirstTier.TariffCode.Should().Be("FUERA_PLAZO");
        lastMinuteOfFirstTier.MeasuredUnits.Should().Be(24);
        lastMinuteOfFirstTier.TotalAmount.Should().Be(60m);

        var secondTier = await QuoteAt(deadline.AddHours(25));
        secondTier.MeasuredUnits.Should().Be(25);
        secondTier.TotalAmount.Should().Be(120m);
        secondTier.Lines.Single().Breakdown.Single().FromUnit.Should().Be(25);

        // Con el plazo aduanero del BL registrado (DeadlineInstance), el hito es ese plazo y no el zarpe + 72 h.
        var rule = new DeadlineRule { Code = "BL_EMPTY_OUT", Name = "B/L (salida)", BaseEvent = DeadlineBaseEvents.DepartureEstimated, OffsetHours = 72, Severity = DeadlineSeverity.Low, Certainty = DeadlineCertainty.Uncertain };
        _f.Db.DeadlineRuleList.Add(rule);
        _f.Db.DeadlineInstanceList.Add(new DeadlineInstance { RuleId = rule.Id, BillOfLadingId = bl.Id, BaseEventAt = Departure, DueAt = deadline.AddHours(48), Status = DeadlineStatus.OnTrack });

        var withCustomsDeadline = await QuoteAt(deadline.AddHours(25));
        withCustomsDeadline.Timing.Should().Be(ServiceTimings.InTime);
        withCustomsDeadline.MilestoneSource.Should().Be(ServiceCatalogEvaluator.MilestoneSourceCustomsDeadline);
        withCustomsDeadline.TotalAmount.Should().Be(35m);
    }

    [Fact]
    public async Task SubmittedRequest_ShouldKeepTheTierQuotedAtSubmission_AndGenerateTheCharge()
    {
        _f.BlHouse();
        _f.BlHouseTariffs();
        _f.ExportBl("EXP-HOUSE", DateTime.UtcNow.AddHours(-72 - 30));

        var result = await _f.RequestAsync(ServiceDefinitionCodes.BlHouseTransmission, "EXP-HOUSE", "{\"houseBlNumbers\":\"HB-1\"}");

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Code : null);
        var request = result.Value;
        request.Status.Should().Be(ServiceRequestStatus.PendingPayment);
        request.Quote!.Timing.Should().Be(ServiceTimings.Late);
        request.Quote.MeasuredUnits.Should().Be(30);
        request.Quote.TotalAmount.Should().Be(120m);
        var charge = request.Charges.Should().ContainSingle().Subject;
        charge.Generated.Should().BeTrue();
        charge.Currency.Should().Be("USD");
        charge.TotalAmount.Should().Be(120m);
        _f.Db.LocalChargeList.Single(c => c.Id == charge.ChargeId).ChargeType.Should().Be(ChargeConceptCodes.BlHouseTransmission);
        request.Timeline.Select(e => e.ToStatus).Should().Equal(
            ServiceRequestStatus.Draft, ServiceRequestStatus.Submitted, ServiceRequestStatus.PendingPayment);
        _f.Notifications.Published.Should().ContainSingle(n => n.Type == NotificationTypes.ServiceRequestPendingPayment && n.UserId == _f.Owner.User.Id);
    }

    [Fact]
    public async Task DropOff_ShouldRequireBillingAndTariffAcceptance_ThenGoToTheEdTeam_AndBeApprovedIntoPayment()
    {
        _f.DropOff();
        _f.Rules.AddTariff(ChargeConceptCodes.DropOff, 180000m, "CLP");
        ImportBl("IMP-DO", ("HLXU2000001", "40HC"), ("HLXU2000002", "20DV"));
        const string input = "{\"containers\":[\"HLXU2000001\"],\"returnDate\":\"2026-10-09\"}";

        (await _f.RequestAsync(ServiceDefinitionCodes.DropOff, "IMP-DO", input, acceptTariff: false)).Error.Code
            .Should().Be("ServiceRequest.TariffNotAccepted");
        (await _f.RequestAsync(ServiceDefinitionCodes.DropOff, "IMP-DO", input, acceptedTotal: 100m)).Error.Code
            .Should().Be("ServiceRequest.TariffChanged");
        (await _f.RequestAsync(ServiceDefinitionCodes.DropOff, "IMP-DO", input, billing: new ServiceBillingInput(OwnTaxId, null, null, null, null))).Error.Code
            .Should().Be("ServiceRequest.BillingDataRequired");
        (await _f.RequestAsync(ServiceDefinitionCodes.DropOff, "IMP-DO", input, billing: ServiceRequestsFixture.Billing("77.888.999-0"))).Error.Code
            .Should().Be("ServiceRequest.BillingTaxIdNotAllowed");
        _f.Db.ServiceRequestList.Clear();
        _f.Db.ServiceRequestEventList.Clear();

        var submitted = await _f.RequestAsync(ServiceDefinitionCodes.DropOff, "IMP-DO", input, acceptedTotal: 214200m);

        submitted.IsSuccess.Should().BeTrue(submitted.IsFailure ? submitted.Error.Code : null);
        submitted.Value.Status.Should().Be(ServiceRequestStatus.PendingApproval);
        submitted.Value.AssignedTeam.Should().Be(ServiceTeams.Ed);
        submitted.Value.TariffAcceptedAt.Should().NotBeNull();
        submitted.Value.Charges.Should().BeEmpty();
        _f.Notifications.Published.Should().ContainSingle(n => n.Type == NotificationTypes.ServiceRequestPendingApproval && n.UserId == _f.Owner.User.Id);

        var queue = await _f.Queue().Handle(new GetServiceRequestQueueQuery(Team: ServiceTeams.Ed), CancellationToken.None);
        queue.Value.Items.Should().ContainSingle(i => i.Id == submitted.Value.Id);

        var approved = await _f.Approve().Handle(new ApproveServiceRequestCommand(submitted.Value.Id, "Depósito confirmado"), CancellationToken.None);

        approved.IsSuccess.Should().BeTrue();
        approved.Value.Status.Should().Be(ServiceRequestStatus.PendingPayment);
        approved.Value.ApprovedAt.Should().NotBeNull();
        var charge = approved.Value.Charges.Should().ContainSingle().Subject;
        (charge.Amount, charge.TaxAmount, charge.TotalAmount).Should().Be((180000m, 34200m, 214200m));
        approved.Value.Timeline.Select(e => (e.ToStatus, e.ActorKind)).Should().Equal(
            (ServiceRequestStatus.Draft, ServiceRequestActorKinds.Client),
            (ServiceRequestStatus.Submitted, ServiceRequestActorKinds.Client),
            (ServiceRequestStatus.PendingApproval, ServiceRequestActorKinds.System),
            (ServiceRequestStatus.Approved, ServiceRequestActorKinds.Internal),
            (ServiceRequestStatus.PendingPayment, ServiceRequestActorKinds.System));
        _f.Notifications.Published.Should().Contain(n => n.Type == NotificationTypes.ServiceRequestPendingPayment);

        // El cargo generado se paga por el carro como cualquier recargo (M5-01).
        var added = await _f.Payments.Add().Handle(
            new AddCartItemCommand(PayableItemTypes.LocalCharge, charge.ChargeId, null, OwnTaxId, null), CancellationToken.None);
        added.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DropOff_RejectedByTheEdTeam_ShouldShowTheReasonToTheClient_WithoutCharge()
    {
        _f.DropOff();
        _f.Rules.AddTariff(ChargeConceptCodes.DropOff, 180000m, "CLP");
        ImportBl("IMP-DO", ("HLXU2000001", "40HC"));
        var submitted = await _f.RequestAsync(ServiceDefinitionCodes.DropOff, "IMP-DO", "{\"containers\":[\"HLXU2000001\"],\"returnDate\":\"2026-10-09\"}");

        var rejected = await _f.Reject().Handle(new RejectServiceRequestCommand(submitted.Value.Id, "Sin cupo en el depósito"), CancellationToken.None);
        var seen = await new GetServiceRequestQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db))
            .Handle(new GetServiceRequestQuery(submitted.Value.Id), CancellationToken.None);

        rejected.Value.Status.Should().Be(ServiceRequestStatus.Rejected);
        seen.Value.Status.Should().Be(ServiceRequestStatus.Rejected);
        seen.Value.ResolutionNotes.Should().Be("Sin cupo en el depósito");
        seen.Value.Charges.Should().BeEmpty();
        seen.Value.CanCancel.Should().BeFalse();
        _f.Db.LocalChargeList.Should().BeEmpty();
        _f.Notifications.Published.Should().Contain(n => n.Type == NotificationTypes.ServiceRequestRejected && n.Body.Contains("Sin cupo"));

        // Ya resuelta: no se puede aprobar.
        (await _f.Approve().Handle(new ApproveServiceRequestCommand(submitted.Value.Id, null), CancellationToken.None)).Error.Code
            .Should().Be("ServiceRequest.InvalidTransition");
    }

    [Fact]
    public async Task PaymentRelease_ShouldAdvanceTheRequestOnce_AndTheTeamCompletesIt()
    {
        _f.Seals();
        _f.Rules.AddTariff(ChargeConceptCodes.SealManagement, 12000m, "CLP");
        var bl = _f.ExportBl("EXP-SEALS", DateTime.UtcNow.AddDays(10));
        _f.Rules.AddContainer(bl, "HLXU3000001", "40RF");
        _f.Rules.AddContainer(bl, "HLXU3000002", "20DV");

        var requested = await _f.RequestAsync(ServiceDefinitionCodes.SealManagement, "EXP-SEALS",
            "{\"containers\":[\"HLXU3000001\",\"HLXU3000002\"],\"sealNumbers\":\"S-1, S-2\"}");
        requested.Value.Status.Should().Be(ServiceRequestStatus.PendingPayment);
        requested.Value.Quote!.Quantity.Should().Be(2);
        requested.Value.Charges.Single().TotalAmount.Should().Be(28560m);

        var payment = await _f.PayAsync(requested.Value);

        var request = _f.Db.ServiceRequestList.Single();
        request.Status.Should().Be(ServiceRequestStatus.InProgress);
        request.AssignedTeam.Should().Be(ServiceTeams.CustomerService);
        request.PaymentId.Should().Be(payment.Id);
        request.PaidAt.Should().NotBeNull();
        _f.Db.ServiceRequestEventList.Where(e => e.ServiceRequestId == request.Id).Select(e => e.ToStatus).Should().Equal(
            ServiceRequestStatus.Draft, ServiceRequestStatus.Submitted, ServiceRequestStatus.PendingPayment,
            ServiceRequestStatus.Paid, ServiceRequestStatus.InProgress);
        _f.Db.ServiceRequestEventList.Single(e => e.ToStatus == ServiceRequestStatus.Paid).ActorName.Should().Be($"PAYMENT {payment.PaymentNumber}");
        _f.Db.PaymentOutboxMessageList.Should().Contain(m => m.JobType == PaymentOutboxJobTypes.ServiceRequests && m.Status == PaymentOutboxStatus.Succeeded);
        _f.Notifications.Published.Should().ContainSingle(n => n.Type == NotificationTypes.ServiceRequestInProgress);

        // Idempotencia: repetir la liberación no repite hitos ni avisos.
        var details = _f.Db.PaymentDetailList.Where(d => d.PaymentId == payment.Id).ToList();
        await new ReleasePaymentItemsStep(_f.Db).ExecuteAsync(payment, details, DateTime.UtcNow, CancellationToken.None);
        (await ServiceRequestPaymentRelease.AdvanceAsync(_f.Db, [requested.Value.Charges.Single().ChargeId], payment, DateTime.UtcNow, CancellationToken.None))
            .Should().BeEmpty();
        await new NotifyServiceRequestsStep(_f.Db, _f.Workflow()).ExecuteAsync(payment, details, DateTime.UtcNow, CancellationToken.None);
        _f.Db.ServiceRequestEventList.Should().HaveCount(5);
        _f.Db.PaymentOutboxMessageList.Count(m => m.JobType == PaymentOutboxJobTypes.ServiceRequests).Should().Be(1);

        var completed = await _f.Complete().Handle(new CompleteServiceRequestCommand(request.Id, "Sellos registrados"), CancellationToken.None);
        completed.Value.Status.Should().Be(ServiceRequestStatus.Completed);
        completed.Value.PaymentNumber.Should().Be(payment.PaymentNumber);
        completed.Value.ReceiptNumber.Should().Be(payment.ReceiptNumber);
        _f.Notifications.Published.Should().Contain(n => n.Type == NotificationTypes.ServiceRequestCompleted);
    }

    [Fact]
    public async Task AccessMatrix_ShouldHideDropOffFromShippers_AndForbidViewersFromRequesting()
    {
        _f.DropOff();
        _f.Rules.AddTariff(ChargeConceptCodes.DropOff, 180000m, "CLP");
        var bl = ImportBl("IMP-DO", ("HLXU2000001", "40HC"));
        var shipper = _f.Payments.NewActor();
        AccessTestData.AddRole(_f.Db, bl, shipper.Organization, ShipmentRoleCodes.Shipper);
        var viewerUser = AccessTestData.AddMember(_f.Db, _f.Rules.Organization, profile: RoleCodes.OrgViewer);
        var viewer = new PaymentsFixture.Actor(_f.Rules.Organization, viewerUser, AccessTestData.CurrentUser(viewerUser));
        const string input = "{\"containers\":[\"HLXU2000001\"],\"returnDate\":\"2026-10-09\"}";

        (await AvailableAsync("IMP-DO", actor: shipper)).Services.Should().BeEmpty();
        (await _f.RequestAsync(ServiceDefinitionCodes.DropOff, "IMP-DO", input, actor: shipper)).Error.Code
            .Should().Be("ServiceDefinition.NotFound");

        var asViewer = await AvailableAsync("IMP-DO", actor: viewer);
        var dropOff = asViewer.Services.Should().ContainSingle().Subject;
        dropOff.CanRequest.Should().BeFalse();
        dropOff.UnavailableReasons.Should().Contain(ServiceUnavailableReasons.NoPermission);
        (await _f.RequestAsync(ServiceDefinitionCodes.DropOff, "IMP-DO", input, actor: viewer)).Error
            .Should().Be(Error.Forbidden);
        _f.Db.ServiceRequestList.Should().BeEmpty();
    }

    [Fact]
    public async Task Xom_ShouldChargePerUnitInBolivianos_ExcludingShipperOwnedUnits()
    {
        _f.Xom();
        _f.Rules.AddTariff(ChargeConceptCodes.ContainerAdministrationXom, 350m, "BOB", country: CountryCodes.Bolivia);
        _f.Rules.AddTariff(ChargeConceptCodes.ContainerAdministrationXom, 520m, "BOB", country: CountryCodes.Bolivia, containerType: "40HC");
        var bl = _f.Rules.OwnBl("IMP-XOM", CountryCodes.Bolivia, eta: DateTime.UtcNow.AddDays(-2));
        _f.Rules.AddContainer(bl, "HLXU4000001", "20DV");
        _f.Rules.AddContainer(bl, "HLXU4000002", "40HC");
        _f.Rules.AddContainer(bl, "HLXU4000003", "20DV").IsShipperOwned = true;

        var result = await _f.RequestAsync(ServiceDefinitionCodes.ContainerAdministrationXom, "IMP-XOM",
            "{\"containers\":[\"HLXU4000001\",\"HLXU4000002\",\"HLXU4000003\"]}");

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Code : null);
        var quote = result.Value.Quote!;
        quote.Currency.Should().Be("BOB");
        quote.Quantity.Should().Be(2);
        quote.TotalAmount.Should().Be(870m);
        quote.TaxAmount.Should().Be(0m);
        quote.ExcludedContainers.Should().Equal("HLXU4000003");
        result.Value.Status.Should().Be(ServiceRequestStatus.PendingPayment);
        result.Value.Charges.Single().Currency.Should().Be("BOB");
    }

    [Fact]
    public async Task Xom_WithANexusException_ShouldNotGenerateACharge()
    {
        _f.Xom();
        _f.Rules.AddTariff(ChargeConceptCodes.ContainerAdministrationXom, 350m, "BOB", country: CountryCodes.Bolivia);
        var bl = _f.Rules.OwnBl("IMP-XOM", CountryCodes.Bolivia, eta: DateTime.UtcNow.AddDays(-2));
        _f.Rules.AddContainer(bl, "HLXU4000001", "20DV");
        _f.Rules.Exempt(OwnTaxId, new ExemptionInfo(ChargeConceptCodes.ContainerAdministrationXom, null, null, ChargeRulesFixture.Validity, null));

        var result = await _f.RequestAsync(ServiceDefinitionCodes.ContainerAdministrationXom, "IMP-XOM", "{\"containers\":[\"HLXU4000001\"]}");

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Code : null);
        result.Value.Status.Should().Be(ServiceRequestStatus.Completed);
        result.Value.Quote!.IsExempt.Should().BeTrue();
        result.Value.Quote.ExemptionReference.Should().StartWith($"NEXUS:XOM:{OwnTaxId}");
        result.Value.Charges.Should().BeEmpty();
        _f.Db.LocalChargeList.Should().BeEmpty();
        _f.Notifications.Published.Should().ContainSingle(n => n.Type == NotificationTypes.ServiceRequestCompleted);
    }

    [Fact]
    public async Task GateInReturn_ShouldLinkTheSourceCharge_AndBlockASecondRequest()
    {
        _f.GateInReturn();
        var bl = _f.ExportBl("EXP-GATE", DateTime.UtcNow.AddDays(3));
        _f.Rules.AddContainer(bl, "HLXU5000001", "40RF");
        var gateIn = _f.Rules.AddCharge(bl, ChargeConceptCodes.GateIn, 95000m);

        var result = await _f.RequestAsync(ServiceDefinitionCodes.GateInReturn, "EXP-GATE", "{}");

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Code : null);
        result.Value.Status.Should().Be(ServiceRequestStatus.PendingPayment);
        var linked = result.Value.Charges.Should().ContainSingle().Subject;
        linked.ChargeId.Should().Be(gateIn.Id);
        linked.Generated.Should().BeFalse();
        result.Value.Quote!.TotalAmount.Should().Be(113050m);
        _f.Db.LocalChargeList.Should().ContainSingle();

        var second = await _f.RequestAsync(ServiceDefinitionCodes.GateInReturn, "EXP-GATE", "{}");
        second.Error.Code.Should().Be("ServiceRequest.NotAvailable");

        // Pagado por el carro, la liberación completa la solicitud.
        await _f.PayAsync(result.Value);
        _f.Db.ServiceRequestList.Single(r => r.Id == result.Value.Id).Status.Should().Be(ServiceRequestStatus.Completed);
        gateIn.Status.Should().Be(ChargeStatus.Paid);
    }

    [Fact]
    public async Task GateInReturn_WithANexusExemption_ShouldNotGenerateACharge_AndRecordTheExemption()
    {
        _f.GateInReturn();
        var bl = _f.ExportBl("EXP-GATE", DateTime.UtcNow.AddDays(3));
        _f.Rules.AddContainer(bl, "HLXU5000001", "40RF");
        var gateIn = _f.Rules.AddCharge(bl, ChargeConceptCodes.GateIn, 95000m);
        _f.Rules.Exempt(OwnTaxId, new ExemptionInfo(ChargeConceptCodes.GateIn, null, null, ChargeRulesFixture.Validity, null));

        var result = await _f.RequestAsync(ServiceDefinitionCodes.GateInReturn, "EXP-GATE", "{}");

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Code : null);
        result.Value.Status.Should().Be(ServiceRequestStatus.Completed);
        result.Value.Quote!.IsExempt.Should().BeTrue();
        result.Value.Quote.SourceCharges.Single().Outcome.Should().Be(ChargeOutcomes.Exempt);
        gateIn.Status.Should().Be(ChargeStatus.Exempt);
        _f.Db.AppliedExemptionList.Should().ContainSingle(a => a.LocalChargeId == gateIn.Id && a.Source == RuleSources.Nexus);
        result.Value.Timeline.Last().Notes.Should().StartWith("Sin cobro");
    }

    [Fact]
    public async Task Cancel_ShouldRemoveTheGeneratedChargeAndItsCartItem()
    {
        _f.Seals();
        _f.Rules.AddTariff(ChargeConceptCodes.SealManagement, 12000m, "CLP");
        var bl = _f.ExportBl("EXP-SEALS", DateTime.UtcNow.AddDays(10));
        _f.Rules.AddContainer(bl, "HLXU3000001", "40RF");
        var requested = await _f.RequestAsync(ServiceDefinitionCodes.SealManagement, "EXP-SEALS",
            "{\"containers\":[\"HLXU3000001\"],\"sealNumbers\":\"S-1\"}");
        var chargeId = requested.Value.Charges.Single().ChargeId;
        (await _f.Payments.Add().Handle(new AddCartItemCommand(PayableItemTypes.LocalCharge, chargeId, null, OwnTaxId, null), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        var cancelled = await _f.Cancel().Handle(new CancelServiceRequestCommand(requested.Value.Id, "Ya no se requiere"), CancellationToken.None);

        cancelled.Value.Status.Should().Be(ServiceRequestStatus.Cancelled);
        _f.Db.LocalChargeList.Should().NotContain(c => c.Id == chargeId);
        _f.Db.CartItemList.Should().BeEmpty();
        _f.Notifications.Published.Should().Contain(n => n.Type == NotificationTypes.ServiceRequestCancelled);
    }

    [Fact]
    public async Task Draft_WithAFileField_ShouldRequireTheAttachmentBeforeSubmitting()
    {
        _f.AddDefinition(ServiceDefinitionCodes.BlCorrection, "IMPORT,EXPORT", "CL,BO", ShipmentActionCodes.PayOnDemandLocalCharges,
            [
                new("description", "Detalle", "Details", ServiceInputFieldTypes.TextArea, true),
                new("supportingDocument", "Respaldo", "Support", ServiceInputFieldTypes.File, true),
            ],
            d =>
            {
                d.BillingDataRequired = true;
                d.PricingMode = ServicePricingModes.Tariff;
                d.ChargeConceptCode = ChargeConceptCodes.BlCorrection;
                d.FulfillmentTeam = ServiceTeams.CustomerService;
            });
        _f.Rules.AddTariff(ChargeConceptCodes.BlCorrection, 45000m, "CLP");
        ImportBl("IMP-COR");

        var draft = await _f.RequestAsync(ServiceDefinitionCodes.BlCorrection, "IMP-COR", "{\"description\":\"Corregir consignatario\"}", submit: false);
        draft.Value.Status.Should().Be(ServiceRequestStatus.Draft);
        draft.Value.CanSubmit.Should().BeTrue();

        var missing = await _f.Submit().Handle(new SubmitServiceRequestCommand(draft.Value.Id), CancellationToken.None);
        missing.Should().BeAssignableTo<IValidationResult>();
        ((IValidationResult)missing).Errors.Should().ContainSingle(e => e.Code == "InputValues.supportingDocument");

        var unknownField = await _f.Upload().Handle(
            new UploadServiceRequestAttachmentCommand(draft.Value.Id, "description", "a.pdf", "application/pdf", [1, 2, 3]), CancellationToken.None);
        unknownField.Error.Code.Should().Be("ServiceRequest.UnknownFileField");

        var uploaded = await _f.Upload().Handle(
            new UploadServiceRequestAttachmentCommand(draft.Value.Id, "supportingDocument", "respaldo.pdf", "application/pdf", [1, 2, 3]), CancellationToken.None);
        uploaded.IsSuccess.Should().BeTrue();

        var submitted = await _f.Submit().Handle(new SubmitServiceRequestCommand(draft.Value.Id), CancellationToken.None);
        submitted.IsSuccess.Should().BeTrue(submitted.IsFailure ? submitted.Error.Code : null);
        submitted.Value.Status.Should().Be(ServiceRequestStatus.PendingPayment);
        submitted.Value.Attachments.Should().ContainSingle(a => a.FieldKey == "supportingDocument");
        submitted.Value.Charges.Single().TotalAmount.Should().Be(53550m);
    }

    [Fact]
    public async Task OtherOrganizations_ShouldNotSeeTheRequest()
    {
        _f.DropOff();
        _f.Rules.AddTariff(ChargeConceptCodes.DropOff, 180000m, "CLP");
        ImportBl("IMP-DO", ("HLXU2000001", "40HC"));
        var submitted = await _f.RequestAsync(ServiceDefinitionCodes.DropOff, "IMP-DO", "{\"containers\":[\"HLXU2000001\"],\"returnDate\":\"2026-10-09\"}");
        var other = _f.Payments.NewActor();

        var seen = await new GetServiceRequestQueryHandler(_f.Db, other.Evaluator(_f.Db))
            .Handle(new GetServiceRequestQuery(submitted.Value.Id), CancellationToken.None);
        var mine = await new GetMyServiceRequestsQueryHandler(_f.Db, other.Evaluator(_f.Db), FeatureSettings.AllEnabled())
            .Handle(new GetMyServiceRequestsQuery(), CancellationToken.None);
        var cancel = await _f.Cancel(other).Handle(new CancelServiceRequestCommand(submitted.Value.Id, null), CancellationToken.None);

        seen.Error.Code.Should().Be("ServiceRequest.NotFound");
        mine.Value.Items.Should().BeEmpty();
        cancel.Error.Code.Should().Be("ServiceRequest.NotFound");
    }
}
