namespace HapagPortal.UnitTests.Application.WarehouseChanges;

using FluentAssertions;
using HapagPortal.Application.Payments.Commands.Confirm;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Application.WarehouseChanges.History;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Historial y trazabilidad del cambio de almacén (M3-06): fecha, estado, embarque, razón social y RUT de quien
/// pagó (pagador del pago confirmado), línea de tiempo con los estados del pago y solicitudes masivas.
/// </summary>
public sealed class WarehouseChangeHistoryTests
{
    private const string OwnTaxId = "76123456-7";
    private readonly PaymentsFixture _f = new();

    private WarehouseChange AddChange(BillOfLading bl, decimal amount, Guid? batchId = null, Client? requester = null)
    {
        var change = new WarehouseChange
        {
            BillOfLadingId = bl.Id,
            FromWarehouse = "STI",
            ToWarehouse = "Bodega Central",
            Amount = amount,
            Currency = "CLP",
            Status = amount > 0m ? WarehouseChangeStatus.PendingPayment : WarehouseChangeStatus.Completed,
            Country = CountryCodes.Chile,
            IsFree = amount == 0m,
            EntitlementSource = amount == 0m ? RuleSources.Portal : null,
            RequestedByClientId = (requester ?? _f.Owner.Organization).Id,
            RequestedByUserId = _f.Owner.User.Id,
            BatchId = batchId,
            CreatedAt = new DateTime(2026, 10, 2, 14, 0, 0, DateTimeKind.Utc)
        };
        _f.Db.WarehouseChangeList.Add(change);
        return change;
    }

    private async Task PayAsync(WarehouseChange change)
    {
        (await _f.Add().Handle(new AddCartItemCommand(PayableItemTypes.WarehouseChange, change.Id, null, OwnTaxId, null), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        var checkout = await _f.CheckoutCart().Handle(
            new CheckoutCartCommand(CountryCodes.Chile, "CLP", PaymentMethodCodes.Khipu, $"key-{change.Id}"), CancellationToken.None);
        await new ConfirmPaymentCommandHandler(_f.Db, AccessTestData.CurrentUser(_f.Owner.User))
            .Handle(new ConfirmPaymentCommand(checkout.Value.Payment.Id), CancellationToken.None);
        await _f.Processor().ProcessDueAsync(10, DateTime.UtcNow, CancellationToken.None);
    }

    [Fact]
    public async Task History_ShouldShowThePayerLegalNameAndTaxId_AndTheTraceFromTheShipment()
    {
        var bl = _f.Rules.OwnBl("BL-WCH");
        var paid = AddChange(bl, 9940m);
        var batch = new WarehouseChangeBatch { ClientId = _f.Owner.Organization.Id, RequestedByUserId = _f.Owner.User.Id, Status = BulkRequestStatus.Completed };
        _f.Db.WarehouseChangeBatchList.Add(batch);
        var free = AddChange(bl, 0m, batch.Id);
        _f.Db.WarehouseChangeBatchItemList.Add(new WarehouseChangeBatchItem
        {
            BatchId = batch.Id,
            LineNumber = 3,
            BlNumber = bl.BLNumber,
            ToWarehouse = "Bodega Central",
            Status = BulkItemStatus.Succeeded,
            WarehouseChangeId = free.Id
        });
        await PayAsync(paid);

        var history = await new GetWarehouseChangeHistoryQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db))
            .Handle(new GetWarehouseChangeHistoryQuery(BlNumber: "BL-WCH"), CancellationToken.None);

        history.IsSuccess.Should().BeTrue();
        history.Value.Total.Should().Be(2);
        var item = history.Value.Items.Single(i => i.Id == paid.Id);
        item.Status.Should().Be(WarehouseChangeStatus.Completed);
        item.BlNumber.Should().Be("BL-WCH");
        item.Payer!.TaxId.Should().Be(OwnTaxId);
        item.Payer.Name.Should().Be(_f.Owner.Organization.Name);
        item.BillingTaxId.Should().Be(OwnTaxId);
        item.PaymentStatus.Should().Be(PaymentStatus.Confirmed);
        item.ReceiptNumber.Should().StartWith(DocumentPrefixes.Receipt);
        item.RequestedByEmail.Should().Be(_f.Owner.User.Email);

        var bulk = history.Value.Items.Single(i => i.Id == free.Id);
        bulk.IsFree.Should().BeTrue();
        bulk.Payer.Should().BeNull();
        bulk.BatchId.Should().Be(batch.Id);
        bulk.BatchLineNumber.Should().Be(3);

        var trace = await new GetWarehouseChangeTraceQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db))
            .Handle(new GetWarehouseChangeTraceQuery(paid.Id), CancellationToken.None);

        trace.Value.Timeline.Select(e => (e.Event, e.Status)).Should().Equal(
            (WarehouseChangeTimelineEvents.Requested, WarehouseChangeStatus.PendingPayment),
            (WarehouseChangeTimelineEvents.PaymentStatusChanged, PaymentStatus.Pending),
            (WarehouseChangeTimelineEvents.PaymentStatusChanged, PaymentStatus.Processing),
            (WarehouseChangeTimelineEvents.PaymentStatusChanged, PaymentStatus.Confirmed),
            (WarehouseChangeTimelineEvents.Completed, WarehouseChangeStatus.Completed));
        trace.Value.Timeline.Skip(1).First().Reference.Should().Be(item.PaymentNumber);
    }

    [Fact]
    public async Task History_ShouldOnlyListTheOrganizationsRequests()
    {
        var bl = _f.Rules.OwnBl("BL-WCH");
        var other = _f.NewActor();
        var foreign = AddChange(bl, 9940m, requester: other.Organization);
        AddChange(bl, 9940m);

        var mine = await new GetWarehouseChangeHistoryQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db))
            .Handle(new GetWarehouseChangeHistoryQuery(), CancellationToken.None);
        var theirs = await new GetWarehouseChangeHistoryQueryHandler(_f.Db, other.Evaluator(_f.Db))
            .Handle(new GetWarehouseChangeHistoryQuery(), CancellationToken.None);
        var trace = await new GetWarehouseChangeTraceQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db))
            .Handle(new GetWarehouseChangeTraceQuery(foreign.Id), CancellationToken.None);

        mine.Value.Items.Should().ContainSingle().Which.Id.Should().NotBe(foreign.Id);
        theirs.Value.Items.Should().ContainSingle().Which.Id.Should().Be(foreign.Id);
        trace.Error.Code.Should().Be("WarehouseChange.NotFound");
    }

    [Fact]
    public async Task History_ShouldFilterByTheLocalDateOfTheRequest()
    {
        var bl = _f.Rules.OwnBl("BL-WCH");
        // 01:30 UTC del 03-10 es el 02-10 a las 22:30 en Santiago (NF-22).
        var change = AddChange(bl, 9940m);
        change.CreatedAt = new DateTime(2026, 10, 3, 1, 30, 0, DateTimeKind.Utc);

        var onSecond = await new GetWarehouseChangeHistoryQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db))
            .Handle(new GetWarehouseChangeHistoryQuery(From: new DateOnly(2026, 10, 2), To: new DateOnly(2026, 10, 2)), CancellationToken.None);
        var onThird = await new GetWarehouseChangeHistoryQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db))
            .Handle(new GetWarehouseChangeHistoryQuery(From: new DateOnly(2026, 10, 3)), CancellationToken.None);

        onSecond.Value.Total.Should().Be(1);
        onThird.Value.Total.Should().Be(0);
    }
}
