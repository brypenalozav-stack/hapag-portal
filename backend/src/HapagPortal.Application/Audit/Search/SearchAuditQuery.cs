namespace HapagPortal.Application.Audit.Search;

using FluentValidation;
using HapagPortal.Application.Common.Helpers;
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
    int PageSize = 20,
    string? Sort = null,
    string? Direction = null) : IQuery<PagedResult<AuditLogDto>>
{
    /// <summary>Columnas por las que se puede ordenar el registro (<c>sort</c>, con <c>direction</c> asc|desc).</summary>
    public static readonly SortMap<Domain.Entities.AuditLog> Sorts = new SortMap<Domain.Entities.AuditLog>()
        .Add("timestamp", a => a.Timestamp)
        .Add("entityName", a => a.EntityName)
        .Add("action", a => a.Action)
        .Add("userId", a => a.UserId);
}

public sealed class SearchAuditQueryValidator : AbstractValidator<SearchAuditQuery>
{
    public SearchAuditQueryValidator()
    {
        RuleFor(x => x.Sort)
            .Must(SearchAuditQuery.Sorts.IsValid)
            .WithMessage($"Sort must be one of: {string.Join(", ", SearchAuditQuery.Sorts.Names)}.");

        RuleFor(x => x.Direction)
            .Must(SortMap<Domain.Entities.AuditLog>.IsValidDirection)
            .WithMessage("Direction must be 'asc' or 'desc'.");
    }
}

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

        var items = await SearchAuditQuery.Sorts
            .Apply(
                query,
                request.Sort,
                request.Direction,
                q => q.OrderByDescending(a => a.Timestamp),
                q => q.ThenByDescending(a => a.Timestamp))
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
