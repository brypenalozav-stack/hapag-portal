namespace HapagPortal.UnitTests.Application.Shipments;

using FluentAssertions;
using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.Tatc;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>Comprobante PDF de los TATC emitidos de un BL, con código QR (M2-09).</summary>
public sealed class TatcVoucherTests
{
    private const string Bl = "HLCUTAT2601001";
    private static readonly DateTime IssuedAt = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly StubSender _sender = new();
    private readonly FakePdfDocumentRenderer _renderer = new();
    private IReadOnlyList<ContainerTatcDto> _containers = [];

    public TatcVoucherTests()
    {
        _sender
            .On<GetShipmentTatcQuery, ShipmentTatcDto>(_ => Result<ShipmentTatcDto>.Success(
                new ShipmentTatcDto(Bl, null, CountryCodes.Chile, "Arrived", IssuedAt, true, TatcStatuses.PartiallyIssued, _containers,
                    IssuedAt, IssuedAt, null, false)))
            .On<GetShipmentDetailQuery, ShipmentDetailDto>(_ => Result<ShipmentDetailDto>.Success(Detail()));
    }

    private static ShipmentDetailDto Detail() => new(
        Guid.NewGuid(), Bl, null, ShipmentOperations.Import, "Arrived", CountryCodes.Chile, "Hamburg Express", "025E", "Shanghai (CNSHA)",
        "San Antonio", null, null, IssuedAt, null, "Importadora Demo SpA", ["Consignee"], "Role", [ShipmentActionCodes.DownloadTatc], true, false, false,
        null,
        [
            new BLContainerDto(Guid.NewGuid(), "HLXU0000001", "40HC", null, null, "Discharged"),
            new BLContainerDto(Guid.NewGuid(), "HLXU0000002", "20DV", null, null, "Discharged")
        ],
        null, null, [], PortOfDischargeCode: "CLSAI");

    private static ContainerTatcDto Container(string number, string status, string? tatcNumber) =>
        new(number, tatcNumber, status, status.ToUpperInvariant(), status == TatcStatuses.Issued ? IssuedAt : null, "WH-1", []);

    private Task<Result<DocumentFileDto>> Voucher(string? container = null) =>
        new GetTatcVoucherQueryHandler(_sender, _renderer, new DocumentSettings())
            .Handle(new GetTatcVoucherQuery(Bl, container), CancellationToken.None);

    [Fact]
    public async Task WithoutIssuedTatc_ShouldFailNotFound()
    {
        _containers = [Container("HLXU0000001", TatcStatuses.NotIssued, null), Container("HLXU0000002", TatcStatuses.PreTatc, "PRE-1")];

        var result = await Voucher();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Tatc.VoucherNotFound);
        result.Error.Code.Should().Be("TatcVoucher.NotFound");
        _renderer.Rendered.Should().BeEmpty();
    }

    [Fact]
    public async Task WithIssuedTatc_ShouldReturnThePdfOfAllIssuedContainers()
    {
        _containers =
        [
            Container("HLXU0000001", TatcStatuses.Issued, "TATC-1"),
            Container("HLXU0000002", TatcStatuses.Issued, "TATC-2"),
            Container("HLXU0000003", TatcStatuses.NotIssued, null)
        ];

        var result = await Voucher();

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.FileName.Should().Be($"comprobante-tatc-{Bl}.pdf");
        result.Value.Content.Should().NotBeEmpty();

        var model = _renderer.Rendered.Should().ContainSingle().Subject;
        model.DocumentNumber.Should().Be($"TATC-{Bl}");
        model.QrPayload.Should().Be($"TATC TATC-1,TATC-2 | BL {Bl}");
        var rows = model.Sections[0].Table!.Rows;
        rows.Should().HaveCount(2);
        rows[0].Should().Equal("HLXU0000001", "40HC", "WH-1", "TATC-1");
        rows[1].Should().Equal("HLXU0000002", "20DV", "WH-1", "TATC-2");
    }

    [Fact]
    public async Task WithContainerFilter_ShouldIncludeOnlyThatContainer()
    {
        _containers = [Container("HLXU0000001", TatcStatuses.Issued, "TATC-1"), Container("HLXU0000002", TatcStatuses.Issued, "TATC-2")];

        var result = await Voucher(" hlxu0000002 ");

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be($"comprobante-tatc-{Bl}-HLXU0000002.pdf");
        var model = _renderer.Rendered.Single();
        model.DocumentNumber.Should().Be("TATC-2");
        model.Sections[0].Table!.Rows.Should().ContainSingle();
    }

    [Fact]
    public async Task ContainerWithoutIssuedTatc_ShouldFailNotFound()
    {
        _containers = [Container("HLXU0000001", TatcStatuses.Issued, "TATC-1"), Container("HLXU0000002", TatcStatuses.NotIssued, null)];

        var result = await Voucher("HLXU0000002");

        result.Error.Should().Be(DomainErrors.Tatc.VoucherNotFound);
    }

    [Fact]
    public async Task TatcQueryFailure_ShouldPropagate()
    {
        _sender.On<GetShipmentTatcQuery, ShipmentTatcDto>(Result<ShipmentTatcDto>.Failure(Error.Forbidden));

        var result = await Voucher();

        result.Error.Should().Be(Error.Forbidden);
        _sender.SentOf<GetShipmentDetailQuery>().Should().BeEmpty();
    }
}
