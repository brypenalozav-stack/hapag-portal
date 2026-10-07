namespace HapagPortal.Application.ThirdPartyAccess.Grants;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Organizaciones que pueden recibir un acceso (agencias de aduanas, transportistas, clientes y
/// Freight Forwarders aprobados), por nombre o identificador tributario. Excluye la propia.
/// </summary>
public sealed record SearchGranteeOrganizationsQuery(string? Search = null, string? OrganizationType = null)
    : IQuery<IReadOnlyList<GranteeOrganizationDto>>;

/// <summary>
/// Acciones del flujo único de otorgamiento (M1-15, M1-24): cuáles posee el otorgante sobre el BL,
/// cuáles puede recibir el tercero y cuáles incluye el nivel base de M1-11 si no se eligen permisos.
/// Sin BL, el otorgante se evalúa con el nivel de sus roles de cliente (otorgamiento por defecto).
/// </summary>
public sealed record GetGrantableActionsQuery(
    string? BlNumber = null,
    Guid? GranteeOrganizationId = null,
    string? GranteeOrganizationType = null,
    string? ReceiverRole = null) : IQuery<IReadOnlyList<GrantableActionDto>>;

public sealed class SearchGranteeOrganizationsQueryValidator : AbstractValidator<SearchGranteeOrganizationsQuery>
{
    public SearchGranteeOrganizationsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.OrganizationType)
            .Must(t => OrganizationTypes.Registrable.Contains(t))
            .WithMessage("OrganizationType must be Customer, FreightForwarder, CustomsAgency or Carrier.")
            .When(x => !string.IsNullOrWhiteSpace(x.OrganizationType));
    }
}

public sealed class GetGrantableActionsQueryValidator : AbstractValidator<GetGrantableActionsQuery>
{
    public GetGrantableActionsQueryValidator()
    {
        RuleFor(x => x.BlNumber).MaximumLength(50);
        RuleFor(x => x.GranteeOrganizationType)
            .Must(t => OrganizationTypes.Registrable.Contains(t))
            .When(x => !string.IsNullOrWhiteSpace(x.GranteeOrganizationType));
        RuleFor(x => x.ReceiverRole)
            .Must(r => EarlyBookingAccess.IntendedRoles.Contains(r))
            .WithMessage("ReceiverRole must be Shipper, Consignee or ThirdParty.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReceiverRole));
    }
}

public sealed class SearchGranteeOrganizationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<SearchGranteeOrganizationsQuery, IReadOnlyList<GranteeOrganizationDto>>
{
    private const int MaxResults = 20;

    public async Task<Result<IReadOnlyList<GranteeOrganizationDto>>> Handle(
        SearchGranteeOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<IReadOnlyList<GranteeOrganizationDto>>.Failure(loaded.Error);

        var organizationId = loaded.Value.OrganizationId;
        var query = dbContext.Clients.AsNoTracking()
            .Where(c => c.Id != organizationId
                && c.IsActive
                && c.RegistrationStatus == OrganizationStatus.Approved
                && c.OrganizationType != OrganizationTypes.Internal);

        if (!string.IsNullOrWhiteSpace(request.OrganizationType))
            query = query.Where(c => c.OrganizationType == request.OrganizationType);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) || c.TaxId.ToLower().Contains(term));
        }

        var items = await query
            .OrderBy(c => c.Name)
            .Take(MaxResults)
            .Select(c => new GranteeOrganizationDto(c.Id, c.Name, c.TaxId, c.OrganizationType, c.Country))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<GranteeOrganizationDto>>.Success(items);
    }
}

public sealed class GetGrantableActionsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetGrantableActionsQuery, IReadOnlyList<GrantableActionDto>>
{
    public async Task<Result<IReadOnlyList<GrantableActionDto>>> Handle(
        GetGrantableActionsQuery request,
        CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<IReadOnlyList<GrantableActionDto>>.Failure(loaded.Error);

        var context = loaded.Value;
        var matrix = context.Matrix;

        IReadOnlyCollection<string> grantorActions;
        if (!string.IsNullOrWhiteSpace(request.BlNumber))
        {
            var blNumber = request.BlNumber.Trim();
            var bl = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), context.Scope)
                .FirstOrDefaultAsync(b => b.BLNumber == blNumber, cancellationToken);
            if (bl is null)
                return Result<IReadOnlyList<GrantableActionDto>>.Failure(DomainErrors.BillOfLading.NotFoundByNumber(blNumber));

            var permissions = await accessEvaluator.EvaluateAsync(context.Scope, bl, cancellationToken);
            grantorActions = GrantRules.CeilingOf(matrix, permissions);
        }
        else
        {
            grantorActions = matrix.AllowedForRoles(context.OrganizationType, ShipmentRoleCodes.ShipmentParties);
        }

        var granteeType = request.GranteeOrganizationType;
        if (request.GranteeOrganizationId is not null)
        {
            granteeType = await dbContext.Clients.AsNoTracking()
                .Where(c => c.Id == request.GranteeOrganizationId.Value)
                .Select(c => c.OrganizationType)
                .FirstOrDefaultAsync(cancellationToken);
            if (granteeType is null)
                return Result<IReadOnlyList<GrantableActionDto>>.Failure(
                    DomainErrors.AccessGrant.GranteeNotFound(request.GranteeOrganizationId.Value));
        }

        var receiverRole = string.IsNullOrWhiteSpace(request.ReceiverRole) ? ShipmentRoleCodes.ThirdParty : request.ReceiverRole;
        var grantable = matrix.GrantableTo(granteeType ?? OrganizationTypes.Customer, receiverRole);
        var baseLevel = matrix.AllowedByGrant(granteeType ?? OrganizationTypes.Customer, receiverRole, null, grantorActions);

        var items = matrix.ShipmentScopedActions
            .Select(a => new GrantableActionDto(
                a.Code,
                a.Name,
                a.Category,
                a.Kind,
                grantorActions.Contains(a.Code),
                grantable.Contains(a.Code) && grantorActions.Contains(a.Code),
                baseLevel.Contains(a.Code)))
            .ToList();

        return Result<IReadOnlyList<GrantableActionDto>>.Success(items);
    }
}
