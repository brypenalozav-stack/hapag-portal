namespace HapagPortal.UnitTests.Application.Shipments;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.Issuance;
using HapagPortal.Application.Shipments.Search;
using HapagPortal.Application.Shipments.Tatc;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Ola F en el listado y detalle de embarques: publicación por DIFU de destino final (M2-01), estado de
/// emisión del BL desde el origen (M2-02) y consulta y generación masiva del TATC (M2-09).
/// </summary>
public sealed class PublicationIssuanceAndTatcTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly Client _org;
    private readonly ICurrentUserService _currentUser;
    private readonly IShipmentAccessEvaluator _evaluator;
    private readonly ITatcProvider _tatc = Substitute.For<ITatcProvider>();

    public PublicationIssuanceAndTatcTests()
    {
        var client = AccessTestData.ClientContext(_db);
        _org = client.Organization;
        _currentUser = client.CurrentUser;
        _evaluator = client.Evaluator;

        _db.ShipmentPublicationRuleList.Add(new ShipmentPublicationRule
        {
            Country = CountryCodes.Chile,
            FinalDestinationCode = "CLANF",
            DischargePortCode = "CLSAI",
            CreatedBy = "test"
        });
    }

    private BillOfLading Consignee(string blNumber, string? destination = null, string? difu = null, string type = "Import", string discharge = "CLSAI")
    {
        var bl = AccessTestData.AddBl(_db, Guid.NewGuid(), blNumber, type);
        bl.PortOfDischargeCode = discharge;
        bl.FinalDestinationCode = destination;
        bl.DifuCode = difu;
        bl.DifuLocationCode = difu is null ? null : destination;
        AccessTestData.AddRole(_db, bl, _org, ShipmentRoleCodes.Consignee);
        return bl;
    }

    private GetShipmentDetailQueryHandler Detail(IShipmentAccessEvaluator evaluator, IShipmentSource? source = null) =>
        new(_db, evaluator, Substitute.For<IChargeRulesService>(), TestShipmentSources.IssuanceReader(source));

    [Fact]
    public async Task Search_ShouldHideBlsWithoutDifuForTheirFinalDestination()
    {
        Consignee("BL-SAI", destination: "CLSAI");
        Consignee("BL-ANF-NODIFU", destination: "CLANF");
        Consignee("BL-ANF-DIFU", destination: "CLANF", difu: "ANF-01");

        var result = await new SearchShipmentsQueryHandler(_db, _evaluator).Handle(new SearchShipmentsQuery(), CancellationToken.None);

        result.Value.Items.Select(i => i.BlNumber).Should().BeEquivalentTo(["BL-SAI", "BL-ANF-DIFU"]);
        result.Value.Items.Should().OnlyContain(i => i.Publication == null);
    }

    [Fact]
    public async Task Detail_UnpublishedBl_ShouldNotExistForTheClient()
    {
        Consignee("BL-ANF-NODIFU", destination: "CLANF");

        var result = await Detail(_evaluator).Handle(new GetShipmentDetailQuery("BL-ANF-NODIFU"), CancellationToken.None);

        result.Error.Code.Should().Be("BillOfLading.NotFound");
    }

    [Fact]
    public async Task Admin_ShouldSeeUnpublishedBlsWithTheReason()
    {
        var hidden = Consignee("BL-ANF-NODIFU", destination: "CLANF");
        Consignee("BL-SAI", destination: "CLSAI");
        var admin = AccessTestData.AdminContext(_db);

        var all = await new SearchShipmentsQueryHandler(_db, admin.Evaluator).Handle(new SearchShipmentsQuery(), CancellationToken.None);
        var unpublished = await new SearchShipmentsQueryHandler(_db, admin.Evaluator)
            .Handle(new SearchShipmentsQuery(Published: false), CancellationToken.None);
        var detail = await Detail(admin.Evaluator).Handle(new GetShipmentDetailQuery(hidden.BLNumber), CancellationToken.None);

        all.Value.Items.Should().HaveCount(2);
        unpublished.Value.Items.Should().ContainSingle(i => i.BlNumber == "BL-ANF-NODIFU");
        unpublished.Value.Items[0].Publication!.Published.Should().BeFalse();
        unpublished.Value.Items[0].Publication!.ReasonCode.Should().Be(ShipmentPublicationReasons.DifuMissing);
        detail.Value.Publication!.ReasonCode.Should().Be(ShipmentPublicationReasons.DifuMissing);
    }

    [Fact]
    public async Task Issuance_ShouldMapTheSourceStatusAndKeepTheSourceCode()
    {
        Consignee("BL-SWB");
        var source = TestShipmentSources.With(new ShipmentRecord(
            "BL-SWB", null, "IMPORT", null, null, null, null, null, null, null, null, null,
            DocumentType: "SWB", IssuanceStatus: "TELEX_RELEASED", IssuanceStatusAt: new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc)));

        var result = await new GetShipmentIssuanceQueryHandler(_db, _evaluator, TestShipmentSources.IssuanceReader(source))
            .Handle(new GetShipmentIssuanceQuery("BL-SWB"), CancellationToken.None);

        result.Value.Available.Should().BeTrue();
        result.Value.DocumentType.Should().Be(TransportDocumentTypes.Swb);
        result.Value.Status.Should().Be(BlIssuanceStatuses.TelexReleased);
        result.Value.SourceStatus.Should().Be("TELEX_RELEASED");
        result.Value.LastKnown.Should().BeNull();
    }

    [Fact]
    public async Task Issuance_SourceDown_ShouldSayItWithTheLastKnownStatusApart()
    {
        var bl = Consignee("BL-EBL");
        bl.TransportDocumentType = TransportDocumentTypes.Ebl;
        bl.EblPlatform = EblPlatforms.Wave;
        bl.IssuanceStatus = BlIssuanceStatuses.Issued;
        var source = Substitute.For<IShipmentSource>();
        source.GetByBlNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ShipmentRecord?>.Failure(DomainErrors.Integration.Unavailable("Fis")));

        var detail = await Detail(_evaluator, source).Handle(new GetShipmentDetailQuery("BL-EBL"), CancellationToken.None);
        var list = await new SearchShipmentsQueryHandler(_db, _evaluator).Handle(new SearchShipmentsQuery(), CancellationToken.None);

        detail.Value.Issuance!.Available.Should().BeFalse();
        detail.Value.Issuance.Status.Should().BeNull();
        detail.Value.Issuance.ErrorCode.Should().Be("Integration.Unavailable");
        detail.Value.Issuance.LastKnown!.Status.Should().Be(BlIssuanceStatuses.Issued);
        list.Value.Items.Single().Issuance!.EblPlatform.Should().Be(EblPlatforms.Wave);
    }

    private static BlTatcRecord Tatc(string blNumber, params (string Container, string Status)[] containers) => new(
        blNumber, CountryCodes.Chile, new DateTime(2026, 10, 5, 13, 0, 0, DateTimeKind.Utc),
        containers.Select(c => new ContainerTatcRecord(c.Container, null, c.Status, null, "ALM-1",
            c.Status == TatcSourceStatuses.NotIssued ? [TatcPendingReasons.PaymentPending] : [])).ToList());

    [Fact]
    public async Task Tatc_ShouldShowTheCurrentStateByContainer()
    {
        Consignee("BL-TATC");
        _tatc.GetByBlNumberAsync("BL-TATC", Arg.Any<CancellationToken>())
            .Returns(Result<BlTatcRecord?>.Success(Tatc("BL-TATC", ("C1", "ISSUED"), ("C2", "NOT_ISSUED"))));

        var result = await new GetShipmentTatcQueryHandler(_db, _evaluator, _tatc)
            .Handle(new GetShipmentTatcQuery("BL-TATC"), CancellationToken.None);

        result.Value.Available.Should().BeTrue();
        result.Value.Status.Should().Be(TatcStatuses.PartiallyIssued);
        result.Value.Containers.Single(c => c.ContainerNumber == "C2").PendingReasons.Should().Equal(TatcPendingReasons.PaymentPending);
        result.Value.CanRequestGeneration.Should().BeTrue();
    }

    [Fact]
    public async Task Tatc_SourceDown_ShouldNotPresentPartialData()
    {
        Consignee("BL-TATC");
        _tatc.GetByBlNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<BlTatcRecord?>.Failure(DomainErrors.Integration.Timeout("Tatc")));

        var result = await new GetShipmentTatcQueryHandler(_db, _evaluator, _tatc)
            .Handle(new GetShipmentTatcQuery("BL-TATC"), CancellationToken.None);

        result.Value.Available.Should().BeFalse();
        result.Value.ErrorCode.Should().Be("Integration.Timeout");
        result.Value.Containers.Should().BeEmpty();
        result.Value.Status.Should().BeNull();
    }

    [Fact]
    public async Task Tatc_CustomerOnlyOrExport_ShouldBeRejected()
    {
        AccessTestData.AddBl(_db, _org.Id, "BL-CUSTOMER");            // titular = Customer: tatc.download X
        Consignee("BL-EXPORT", type: "Export");

        var handler = new GetShipmentTatcQueryHandler(_db, _evaluator, _tatc);

        (await handler.Handle(new GetShipmentTatcQuery("BL-CUSTOMER"), CancellationToken.None)).Error.Should().Be(Error.Forbidden);
        (await handler.Handle(new GetShipmentTatcQuery("BL-EXPORT"), CancellationToken.None)).Error.Should().Be(DomainErrors.Tatc.NotApplicable);
    }

    [Fact]
    public async Task TatcBatch_ShouldValidateEachLineAndSendOneRequest()
    {
        Consignee("BL-A");
        Consignee("BL-B");
        Consignee("BL-VAP", discharge: "CLVAP");
        AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, "BL-FOREIGN", "Import");
        _tatc.RequestGenerationAsync(Arg.Any<TatcGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<TatcGenerationReceipt>.Success(new TatcGenerationReceipt("REQ-1",
            [
                new TatcGenerationItem("BL-A", true, null),
                new TatcGenerationItem("BL-B", false, TatcBatchReasons.AlreadyIssued),
            ])));

        var result = await new RequestTatcBatchCommandHandler(_db, _evaluator, _currentUser, _tatc).Handle(
            new RequestTatcBatchCommand("clsai", ["bl-a", "BL-B", "BL-A", "BL-VAP", "BL-FOREIGN"]), CancellationToken.None);

        result.Value.Status.Should().Be(TatcBatchStatus.CompletedWithErrors);
        result.Value.SourceRequestId.Should().Be("REQ-1");
        result.Value.AcceptedItems.Should().Be(1);
        result.Value.Items!.Select(i => (i.BlNumber, i.Status, i.ReasonCode)).Should().Equal(
            ("BL-A", TatcBatchItemStatus.Accepted, (string?)null),
            ("BL-B", TatcBatchItemStatus.Rejected, TatcBatchReasons.AlreadyIssued),
            ("BL-A", TatcBatchItemStatus.Failed, TatcBatchReasons.Duplicate),
            ("BL-VAP", TatcBatchItemStatus.Failed, TatcBatchReasons.OtherLocation),
            ("BL-FOREIGN", TatcBatchItemStatus.Failed, TatcBatchReasons.NotFound));
        await _tatc.Received(1).RequestGenerationAsync(
            Arg.Is<TatcGenerationRequest>(r => r.LocationCode == "CLSAI" && r.BlNumbers.SequenceEqual(new[] { "BL-A", "BL-B" })),
            Arg.Any<CancellationToken>());
        _db.TatcBatchList.Should().ContainSingle(b => b.ClientId == _org.Id && b.RequestedByEmail == _currentUser.Email);
    }

    [Fact]
    public async Task TatcBatch_SourceDown_ShouldFailWithoutAcceptingLines()
    {
        Consignee("BL-A");
        _tatc.RequestGenerationAsync(Arg.Any<TatcGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<TatcGenerationReceipt>.Failure(DomainErrors.Integration.Unavailable("Tatc")));

        var result = await new RequestTatcBatchCommandHandler(_db, _evaluator, _currentUser, _tatc)
            .Handle(new RequestTatcBatchCommand("CLSAI", ["BL-A"]), CancellationToken.None);

        result.Value.Status.Should().Be(TatcBatchStatus.Failed);
        result.Value.ErrorCode.Should().Be("Integration.Unavailable");
        result.Value.Items!.Single().ReasonCode.Should().Be(TatcBatchReasons.SourceUnavailable);
    }
}
