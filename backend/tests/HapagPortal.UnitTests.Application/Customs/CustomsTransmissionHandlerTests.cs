namespace HapagPortal.UnitTests.Application.Customs;

using FluentAssertions;
using HapagPortal.Application.Customs.Amend;
using HapagPortal.Application.Customs.Transmit;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class CustomsTransmissionHandlerTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly FakeCustomsTransmitter _transmitter = new();

    private CustomsManifest NewManifest()
    {
        var m = new CustomsManifest
        {
            Id = Guid.NewGuid(),
            VesselImo = "9321483",
            Voyage = "V123",
            Port = "CLVAP",
            Direction = CustomsDirections.Ingreso
        };
        _db.CustomsManifestList.Add(m);
        return m;
    }

    private void AcceptedHeader(Guid manifestId) =>
        _db.CustomsTransmissionList.Add(new CustomsTransmission
        {
            Id = Guid.NewGuid(),
            ManifestId = manifestId,
            Stage = CustomsStages.Header,
            Kind = CustomsTransmissionKind.Original,
            Status = CustomsTransmissionStatus.Accepted
        });

    private BillOfLading NewCompleteBL(Guid? parentId = null)
    {
        var bl = new BillOfLading
        {
            Id = Guid.NewGuid(),
            BLNumber = "HLCUBL0001",
            ShipmentType = "Import",
            FreightCurrency = "USD",
            Status = "Active",
            Country = "CL",
            ParentBLId = parentId,
            PortOfLoading = "CNSHA",
            PortOfDischarge = "CLVAP",
            Parties = [new BLParty { Role = "Consignee", Name = "ACME" }],
            CargoItems = [new BLCargoItem { HsCode = "870323" }]
        };
        _db.BillsOfLadingList.Add(bl);
        return bl;
    }

    [Fact]
    public async Task TransmitHeader_Accepted_SetsAcceptedAndRecordsEvent()
    {
        var m = NewManifest();
        var handler = new TransmitManifestHeaderCommandHandler(_db, _transmitter);

        var result = await handler.Handle(new TransmitManifestHeaderCommand(m.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CustomsTransmissionStatus.Accepted);
        result.Value.AttemptCount.Should().Be(1);
        _db.CustomsTransmissionEventList.Should().ContainSingle();
    }

    [Fact]
    public async Task TransmitHeader_WhenAlreadyAccepted_Fails()
    {
        var m = NewManifest();
        AcceptedHeader(m.Id);
        var handler = new TransmitManifestHeaderCommandHandler(_db, _transmitter);

        var result = await handler.Handle(new TransmitManifestHeaderCommand(m.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customs.AlreadyAccepted");
        _transmitter.Calls.Should().Be(0);
    }

    [Fact]
    public async Task TransmitBL_WhenHeaderNotAccepted_Fails()
    {
        var m = NewManifest();
        var bl = NewCompleteBL();
        var handler = new TransmitBLCommandHandler(_db, _transmitter);

        var result = await handler.Handle(new TransmitBLCommand(m.Id, bl.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customs.HeaderNotAccepted");
    }

    [Fact]
    public async Task TransmitBL_WhenIncomplete_Fails()
    {
        var m = NewManifest();
        AcceptedHeader(m.Id);
        var bl = new BillOfLading
        {
            Id = Guid.NewGuid(),
            BLNumber = "HLCUBL0002",
            ShipmentType = "Import",
            FreightCurrency = "USD",
            Status = "Active",
            Country = "CL",
            Parties = [],
            CargoItems = []
        };
        _db.BillsOfLadingList.Add(bl);
        var handler = new TransmitBLCommandHandler(_db, _transmitter);

        var result = await handler.Handle(new TransmitBLCommand(m.Id, bl.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customs.IncompleteBL");
    }

    [Fact]
    public async Task TransmitBL_HouseWithoutAcceptedParent_Fails()
    {
        var m = NewManifest();
        AcceptedHeader(m.Id);
        var parent = NewCompleteBL();
        var house = NewCompleteBL(parent.Id);
        var handler = new TransmitBLCommandHandler(_db, _transmitter);

        var result = await handler.Handle(new TransmitBLCommand(m.Id, house.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customs.ParentNotTransmitted");
    }

    [Fact]
    public async Task TransmitBL_HappyPath_Accepted()
    {
        var m = NewManifest();
        AcceptedHeader(m.Id);
        var bl = NewCompleteBL();
        var handler = new TransmitBLCommandHandler(_db, _transmitter);

        var result = await handler.Handle(new TransmitBLCommand(m.Id, bl.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CustomsTransmissionStatus.Accepted);
        result.Value.BLNumber.Should().Be("HLCUBL0001");
    }

    [Fact]
    public async Task Retry_OnRejected_IncrementsAttemptAndCanAccept()
    {
        var m = NewManifest();
        var rejected = new CustomsTransmission
        {
            Id = Guid.NewGuid(),
            ManifestId = m.Id,
            Stage = CustomsStages.Header,
            Kind = CustomsTransmissionKind.Original,
            Status = CustomsTransmissionStatus.Rejected,
            AttemptCount = 1
        };
        _db.CustomsTransmissionList.Add(rejected);
        var handler = new RetryTransmissionCommandHandler(_db, _transmitter);

        var result = await handler.Handle(new RetryTransmissionCommand(rejected.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CustomsTransmissionStatus.Accepted);
        result.Value.AttemptCount.Should().Be(2);
    }

    [Fact]
    public async Task Retry_OnAccepted_Fails()
    {
        var m = NewManifest();
        var accepted = new CustomsTransmission
        {
            Id = Guid.NewGuid(),
            ManifestId = m.Id,
            Stage = CustomsStages.Header,
            Kind = CustomsTransmissionKind.Original,
            Status = CustomsTransmissionStatus.Accepted
        };
        _db.CustomsTransmissionList.Add(accepted);
        var handler = new RetryTransmissionCommandHandler(_db, _transmitter);

        var result = await handler.Handle(new RetryTransmissionCommand(accepted.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customs.AlreadyAccepted");
        _transmitter.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Amendment_CreatesAmendmentTransmission()
    {
        var m = NewManifest();
        var handler = new SubmitManifestAmendmentCommandHandler(_db, _transmitter);

        var result = await handler.Handle(
            new SubmitManifestAmendmentCommand(m.Id, null, "Corrección de peso"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(CustomsTransmissionKind.Amendment);
        _db.CustomsTransmissionList.Should().ContainSingle(t => t.Kind == CustomsTransmissionKind.Amendment);
    }
}
