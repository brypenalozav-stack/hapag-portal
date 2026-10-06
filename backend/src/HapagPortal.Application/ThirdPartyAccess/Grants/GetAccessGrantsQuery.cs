namespace HapagPortal.Application.ThirdPartyAccess.Grants;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Tabla de la vista única de accesos (M1-24): accesos otorgados y recibidos por la organización, con
/// destinatario, vigencia y permisos. Filtros por dirección (Given/Received), estado, BL o booking y
/// contraparte.
/// </summary>
public sealed record GetAccessGrantsQuery(
    string? Direction = null,
    string? Status = null,
    string? Reference = null,
    Guid? CounterpartId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<AccessGrantDto>>;

/// <summary>Un acceso otorgado o recibido por la organización.</summary>
public sealed record GetAccessGrantQuery(Guid Id) : IQuery<AccessGrantDto>;

public sealed class GetAccessGrantsQueryValidator : AbstractValidator<GetAccessGrantsQuery>
{
    public GetAccessGrantsQueryValidator()
    {
        RuleFor(x => x.Direction)
            .Must(d => AccessGrantDirections.All.Contains(d))
            .WithMessage("Direction must be Given or Received.")
            .When(x => !string.IsNullOrWhiteSpace(x.Direction));

        RuleFor(x => x.Status)
            .Must(s => AccessGrantStatus.All.Contains(s))
            .WithMessage("Status must be PendingAcceptance, Active, Expired, Revoked or Reconciled.")
            .When(x => !string.IsNullOrWhiteSpace(x.Status));

        RuleFor(x => x.Reference).MaximumLength(50);
    }
}

public sealed class GetAccessGrantsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetAccessGrantsQuery, PagedResult<AccessGrantDto>>
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    public async Task<Result<PagedResult<AccessGrantDto>>> Handle(GetAccessGrantsQuery request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<PagedResult<AccessGrantDto>>.Failure(loaded.Error);

        var context = loaded.Value;
        var organizationId = context.OrganizationId;
        var now = DateTime.UtcNow;
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > MaxPageSize ? DefaultPageSize : request.PageSize;

        var query = dbContext.AccessGrants.AsNoTracking()
            .Where(g => g.GrantorClientId == organizationId || g.GranteeClientId == organizationId);

        if (request.Direction == AccessGrantDirections.Given)
            query = query.Where(g => g.GrantorClientId == organizationId);
        else if (request.Direction == AccessGrantDirections.Received)
            query = query.Where(g => g.GranteeClientId == organizationId);

        // Estado visible: un acceso activo con la vigencia cumplida figura vencido aunque falte registrarlo.
        query = request.Status switch
        {
            AccessGrantStatus.Active => query.Where(g => g.Status == AccessGrantStatus.Active && (g.ValidTo == null || g.ValidTo > now)),
            AccessGrantStatus.Expired => query.Where(g => g.Status == AccessGrantStatus.Expired
                || (g.Status == AccessGrantStatus.Active && g.ValidTo != null && g.ValidTo <= now)),
            null or "" => query,
            _ => query.Where(g => g.Status == request.Status)
        };

        if (request.CounterpartId is not null)
        {
            var counterpart = request.CounterpartId.Value;
            query = query.Where(g => g.GrantorClientId == counterpart || g.GranteeClientId == counterpart);
        }

        if (!string.IsNullOrWhiteSpace(request.Reference))
        {
            var term = request.Reference.Trim().ToLower();
            var bills = dbContext.BillsOfLading;
            query = query.Where(g =>
                (g.BookingNumber != null && g.BookingNumber.ToLower().Contains(term)) ||
                bills.Any(b => b.Id == g.BillOfLadingId && b.BLNumber.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);

        var grants = await query
            .OrderByDescending(g => g.CreatedAt)
            .ThenBy(g => g.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await AccessGrantMapper.ToDtosAsync(dbContext, context.Matrix, grants, organizationId, now, cancellationToken);

        return Result<PagedResult<AccessGrantDto>>.Success(new PagedResult<AccessGrantDto>(items, total, page, pageSize));
    }
}

public sealed class GetAccessGrantQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetAccessGrantQuery, AccessGrantDto>
{
    public async Task<Result<AccessGrantDto>> Handle(GetAccessGrantQuery request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<AccessGrantDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var grant = await dbContext.AccessGrants.AsNoTracking().FirstOrDefaultAsync(
            g => g.Id == request.Id && (g.GrantorClientId == context.OrganizationId || g.GranteeClientId == context.OrganizationId),
            cancellationToken);

        if (grant is null)
            return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.NotFound(request.Id));

        var dto = await AccessGrantMapper.ToDtosAsync(
            dbContext, context.Matrix, [grant], context.OrganizationId, DateTime.UtcNow, cancellationToken);
        return Result<AccessGrantDto>.Success(dto[0]);
    }
}
