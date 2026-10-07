namespace HapagPortal.Application.AccessMatrix;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Nivel de una acción para un rol; con <c>OrganizationType</c> es una excepción por tipo.</summary>
public sealed record AccessMatrixLevelDto(string Role, string? OrganizationType, string Level);

/// <summary>Fila de la matriz base de M1-11 tal como está vigente en la base.</summary>
public sealed record AccessMatrixActionDto(
    string Code,
    string Name,
    string Category,
    string Kind,
    string Scope,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<AccessMatrixLevelDto> Levels);

/// <summary>Matriz base de accesos por rol (M1-11), para administrarla desde el portal interno.</summary>
public sealed record GetAccessMatrixQuery : IQuery<List<AccessMatrixActionDto>>;

/// <summary>
/// Cambia el nivel base de una acción para un rol (y opcionalmente un tipo de organización) sin
/// desarrollo (M1-11). El cambio aplica de inmediato y queda auditado por la entidad.
/// </summary>
public sealed record UpdateAccessMatrixLevelCommand(
    string ActionCode,
    string Role,
    string? OrganizationType,
    string Level) : ICommand<AccessMatrixActionDto>;

public sealed class UpdateAccessMatrixLevelCommandValidator : AbstractValidator<UpdateAccessMatrixLevelCommand>
{
    public UpdateAccessMatrixLevelCommandValidator()
    {
        RuleFor(x => x.ActionCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role)
            .Must(r => ShipmentRoleCodes.MatrixColumns.Contains(r))
            .WithMessage("Role must be one of: " + string.Join(", ", ShipmentRoleCodes.MatrixColumns) + ".");
        RuleFor(x => x.Level)
            .Must(l => AccessLevels.All.Contains(l))
            .WithMessage("Level must be Allowed, Denied or OnGrant.");
        RuleFor(x => x.OrganizationType)
            .Must(t => OrganizationTypes.Registrable.Contains(t))
            .WithMessage("Organization type must be Customer, FreightForwarder, CustomsAgency or Carrier.")
            .When(x => !string.IsNullOrWhiteSpace(x.OrganizationType));
    }
}

public sealed class GetAccessMatrixQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAccessMatrixQuery, List<AccessMatrixActionDto>>
{
    public async Task<Result<List<AccessMatrixActionDto>>> Handle(
        GetAccessMatrixQuery request,
        CancellationToken cancellationToken)
    {
        var actions = await dbContext.ShipmentActions
            .AsNoTracking()
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(cancellationToken);

        var rules = await dbContext.ShipmentAccessRules
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Result<List<AccessMatrixActionDto>>.Success(
            actions.Select(a => AccessMatrixMapper.ToDto(a, rules)).ToList());
    }
}

public sealed class UpdateAccessMatrixLevelCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<UpdateAccessMatrixLevelCommand, AccessMatrixActionDto>
{
    public async Task<Result<AccessMatrixActionDto>> Handle(
        UpdateAccessMatrixLevelCommand request,
        CancellationToken cancellationToken)
    {
        var action = await dbContext.ShipmentActions
            .FirstOrDefaultAsync(a => a.Code == request.ActionCode, cancellationToken);

        if (action is null)
            return Result<AccessMatrixActionDto>.Failure(DomainErrors.ShipmentAccess.ActionNotFound(request.ActionCode));

        var organizationType = string.IsNullOrWhiteSpace(request.OrganizationType) ? null : request.OrganizationType;

        var rule = await dbContext.ShipmentAccessRules.FirstOrDefaultAsync(
            r => r.ShipmentActionId == action.Id
                && r.Role == request.Role
                && r.OrganizationType == organizationType,
            cancellationToken);

        if (rule is null)
        {
            dbContext.ShipmentAccessRules.Add(new ShipmentAccessRule
            {
                ShipmentActionId = action.Id,
                Role = request.Role,
                OrganizationType = organizationType,
                Level = request.Level
            });
        }
        else
        {
            rule.Level = request.Level;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var rules = await dbContext.ShipmentAccessRules
            .AsNoTracking()
            .Where(r => r.ShipmentActionId == action.Id)
            .ToListAsync(cancellationToken);

        return Result<AccessMatrixActionDto>.Success(AccessMatrixMapper.ToDto(action, rules));
    }
}

public static class AccessMatrixMapper
{
    public static AccessMatrixActionDto ToDto(ShipmentAction action, IEnumerable<ShipmentAccessRule> rules) =>
        new(
            action.Code,
            action.Name,
            action.Category,
            action.Kind,
            action.Scope,
            action.DisplayOrder,
            action.IsActive,
            rules
                .Where(r => r.ShipmentActionId == action.Id)
                .OrderBy(r => r.OrganizationType is null ? 0 : 1)
                .ThenBy(r => Array.IndexOf(ShipmentRoleCodes.MatrixColumns, r.Role))
                .Select(r => new AccessMatrixLevelDto(r.Role, r.OrganizationType, r.Level))
                .ToList());
}
