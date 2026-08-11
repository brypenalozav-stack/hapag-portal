namespace HapagPortal.UnitTests.Application.Audit;

using FluentAssertions;
using HapagPortal.Application.Audit.Search;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

public sealed class SearchAuditQueryHandlerTests
{
    private readonly MockApplicationDbContext _db = new();

    public SearchAuditQueryHandlerTests()
    {
        _db.AuditLogList.AddRange(
            new AuditLog { Id = Guid.NewGuid(), EntityName = "BillOfLading", EntityId = "1", Action = "Create", UserId = "u1", Timestamp = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AuditLog { Id = Guid.NewGuid(), EntityName = "BillOfLading", EntityId = "1", Action = "Update", UserId = "u2", Timestamp = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc) },
            new AuditLog { Id = Guid.NewGuid(), EntityName = "User", EntityId = "9", Action = "Create", UserId = "u1", Timestamp = new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc) });
    }

    [Fact]
    public async Task FiltersByEntityName()
    {
        var handler = new SearchAuditQueryHandler(_db);

        var result = await handler.Handle(
            new SearchAuditQuery("BillOfLading", null, null, null, null), CancellationToken.None);

        result.Value.Total.Should().Be(2);
        result.Value.Items.Should().OnlyContain(i => i.EntityName == "BillOfLading");
    }

    [Fact]
    public async Task FiltersByUserAndOrdersByTimestampDescending()
    {
        var handler = new SearchAuditQueryHandler(_db);

        var result = await handler.Handle(
            new SearchAuditQuery(null, "u1", null, null, null), CancellationToken.None);

        result.Value.Total.Should().Be(2);
        result.Value.Items[0].Timestamp.Should().BeAfter(result.Value.Items[1].Timestamp);
    }

    [Fact]
    public async Task FiltersByDateRange()
    {
        var handler = new SearchAuditQueryHandler(_db);

        var result = await handler.Handle(
            new SearchAuditQuery(null, null, null,
                new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc), null), CancellationToken.None);

        result.Value.Total.Should().Be(2);
    }

    [Fact]
    public async Task Paginates()
    {
        var handler = new SearchAuditQueryHandler(_db);

        var result = await handler.Handle(
            new SearchAuditQuery(null, null, null, null, null, Page: 1, PageSize: 2), CancellationToken.None);

        result.Value.Items.Should().HaveCount(2);
        result.Value.Total.Should().Be(3);
    }
}
