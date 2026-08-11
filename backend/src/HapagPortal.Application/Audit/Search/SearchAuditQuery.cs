namespace HapagPortal.Application.Audit.Search;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record AuditLogDto(
    Guid Id,
    string EntityName,
    string EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    string? UserId,
    DateTime Timestamp);

public sealed record SearchAuditQuery(
    string? EntityName,
    string? UserId,
    string? Action,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<AuditLogDto>>;

public sealed class SearchAuditQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<SearchAuditQuery, PagedResult<AuditLogDto>>
{
    public async Task<Result<PagedResult<AuditLogDto>>> Handle(
        SearchAuditQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var query = dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
            query = query.Where(a => a.EntityName == request.EntityName);
        if (!string.IsNullOrWhiteSpace(request.UserId))
            query = query.Where(a => a.UserId == request.UserId);
        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);
        if (request.From is DateTime from)
            query = query.Where(a => a.Timestamp >= from);
        if (request.To is DateTime to)
            query = query.Where(a => a.Timestamp <= to);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.EntityName, a.EntityId, a.Action,
                a.OldValues, a.NewValues, a.UserId, a.Timestamp))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<AuditLogDto>>.Success(
            new PagedResult<AuditLogDto>(items, total, page, pageSize));
    }
}
