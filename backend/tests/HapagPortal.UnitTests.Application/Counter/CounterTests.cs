namespace HapagPortal.UnitTests.Application.Counter;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Counter;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Counter Bolivia/Ultramar (M8-09): registro por BL de canje, HBL y desconsolidado con el país, propagado a Nexus por
/// el puerto <see cref="ICounterRecorder"/>, con copia local, reintento y registro de cambios (NF-15).
/// </summary>
public sealed class CounterTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly ICounterRecorder _recorder = Substitute.For<ICounterRecorder>();
    private readonly ICurrentUserService _admin = Substitute.For<ICurrentUserService>();
    private readonly BillOfLading _bl;

    public CounterTests()
    {
        _admin.UserId.Returns(Guid.NewGuid());
        _admin.Email.Returns("counter@hapag-lloyd.cl");
        _bl = AccessTestData.AddBl(_db, Guid.NewGuid(), "HLCUARI0001", country: CountryCodes.Bolivia);
        _recorder.RecordAsync(Arg.Any<CounterRecordRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<CounterSourceRecord>.Success(Source(call.Arg<CounterRecordRequest>(), "CNT-1")));
    }

    private static CounterSourceRecord Source(CounterRecordRequest r, string reference) =>
        new(r.BlNumber, r.Country, r.ExchangeDate, r.HblReceived, r.HblReceivedAt, r.Deconsolidated, r.DeconsolidatedAt, reference, DateTime.UtcNow, r.RecordedBy);

    private UpsertCounterRecordCommandHandler Upsert() => new(_db, _admin, new CounterSynchronizer(_db, _recorder));

    private static UpsertCounterRecordCommand Command(string bl = "HLCUARI0001", bool hbl = true, bool deconsolidated = false, DateOnly? exchange = null) =>
        new(bl, CountryCodes.Bolivia, exchange ?? new DateOnly(2026, 10, 1), hbl, null, deconsolidated, null, "nota");

    [Fact]
    public async Task Upsert_ShouldStoreLocally_PropagateToNexus_AndLogTheChange()
    {
        var result = await Upsert().Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var record = _db.CounterRecordList.Should().ContainSingle().Subject;
        record.BillOfLadingId.Should().Be(_bl.Id);
        record.Country.Should().Be(CountryCodes.Bolivia);
        record.HblReceivedAt.Should().NotBeNull();                // sin fecha explícita: hoy
        record.DeconsolidatedAt.Should().BeNull();
        record.SyncStatus.Should().Be(CounterSyncStatus.Synced);
        record.SourceReference.Should().Be("CNT-1");
        record.RecordedBy.Should().Be("counter@hapag-lloyd.cl");

        var change = _db.MaintainerChangeLogList.Should().ContainSingle(c => c.Maintainer == MaintainerNames.CounterRecord).Subject;
        await _recorder.Received(1).RecordAsync(
            Arg.Is<CounterRecordRequest>(r => r.BlNumber == "HLCUARI0001" && r.HblReceived && r.Country == CountryCodes.Bolivia),
            $"counter-{change.Id:N}",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upsert_WhenNexusFails_ShouldKeepTheRecordAsFailed_AndSyncShouldRetry()
    {
        _recorder.RecordAsync(Arg.Any<CounterRecordRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<CounterSourceRecord>.Failure(DomainErrors.Integration.Unavailable("Nexus")));

        var failed = await Upsert().Handle(Command(), CancellationToken.None);

        failed.IsSuccess.Should().BeTrue();
        failed.Value.SyncStatus.Should().Be(CounterSyncStatus.Failed);
        failed.Value.SyncError.Should().Be("Integration.Unavailable");

        _recorder.RecordAsync(Arg.Any<CounterRecordRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<CounterSourceRecord>.Success(Source(call.Arg<CounterRecordRequest>(), "CNT-2")));
        var retried = await new SyncCounterRecordCommandHandler(_db, new CounterSynchronizer(_db, _recorder))
            .Handle(new SyncCounterRecordCommand("HLCUARI0001"), CancellationToken.None);

        retried.Value.SyncStatus.Should().Be(CounterSyncStatus.Synced);
        retried.Value.SourceReference.Should().Be("CNT-2");

        var again = await new SyncCounterRecordCommandHandler(_db, new CounterSynchronizer(_db, _recorder))
            .Handle(new SyncCounterRecordCommand("HLCUARI0001"), CancellationToken.None);
        again.Error.Code.Should().Be("Counter.AlreadySynced");
    }

    [Fact]
    public async Task Modifications_ShouldKeepOneRecordPerBl_WithHistory()
    {
        await Upsert().Handle(Command(hbl: false), CancellationToken.None);
        await Upsert().Handle(Command(hbl: true, deconsolidated: true), CancellationToken.None);

        _db.CounterRecordList.Should().ContainSingle().Which.Deconsolidated.Should().BeTrue();
        var history = await new GetCounterHistoryQueryHandler(_db).Handle(new GetCounterHistoryQuery("HLCUARI0001"), CancellationToken.None);

        history.Value.Select(h => h.Action).Should().BeEquivalentTo([MaintainerActions.Created, MaintainerActions.Updated]);
        var update = history.Value.Single(h => h.Action == MaintainerActions.Updated);
        update.Previous!.HblReceived.Should().BeFalse();
        update.Current!.Deconsolidated.Should().BeTrue();
    }

    [Fact]
    public async Task Upsert_InvalidInput_ShouldFail()
    {
        var unknown = await Upsert().Handle(Command(bl: "NOPE"), CancellationToken.None);
        var future = await Upsert().Handle(Command(exchange: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5))), CancellationToken.None);

        unknown.Error.Code.Should().Be("BillOfLading.NotFound");
        future.Error.Code.Should().Be("Counter.Invalid");
        new UpsertCounterRecordCommandValidator().Validate(Command() with { Country = "PE" }).IsValid.Should().BeFalse();
        new UpsertCounterRecordCommandValidator().Validate(Command(hbl: false) with { HblReceivedAt = new DateOnly(2026, 10, 1) })
            .IsValid.Should().BeFalse();
        _db.CounterRecordList.Should().BeEmpty();
    }

    [Fact]
    public async Task Detail_ShouldShowTheLocalRecordAndTheSourceState()
    {
        await Upsert().Handle(Command(), CancellationToken.None);
        _recorder.GetAsync("HLCUARI0001", Arg.Any<CancellationToken>())
            .Returns(Result<CounterSourceRecord?>.Failure(DomainErrors.Integration.Timeout("Nexus")));

        var detail = await new GetCounterRecordQueryHandler(_db, _recorder).Handle(new GetCounterRecordQuery("HLCUARI0001"), CancellationToken.None);
        var list = await new GetCounterRecordsQueryHandler(_db).Handle(new GetCounterRecordsQuery(BlNumber: "ari0"), CancellationToken.None);

        detail.Value.Record.Should().NotBeNull();
        detail.Value.Shipment.BlNumber.Should().Be("HLCUARI0001");
        detail.Value.SourceAvailable.Should().BeFalse();
        detail.Value.SourceErrorCode.Should().Be("Integration.Timeout");
        list.Value.Items.Should().ContainSingle();
    }
}
