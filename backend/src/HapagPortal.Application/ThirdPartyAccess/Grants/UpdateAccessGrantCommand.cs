namespace HapagPortal.Application.ThirdPartyAccess.Grants;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Edita en el lugar la vigencia y/o los permisos de un acceso otorgado por la propia organización
/// (M1-14, M1-15, M1-24). <see cref="ValidityType"/> nulo conserva la vigencia; <see cref="ActionCodes"/>
/// nulo conserva los permisos, salvo <see cref="ResetToBaseLevel"/>, que vuelve al nivel base de M1-11.
/// Los derivados se ajustan para no exceder el acceso editado (M1-22). Todo queda en M1-23.
/// </summary>
public sealed record UpdateAccessGrantCommand(
    Guid Id,
    string? ValidityType = null,
    DateTime? ValidFrom = null,
    DateTime? ValidTo = null,
    int? DurationDays = null,
    IReadOnlyList<string>? ActionCodes = null,
    bool ResetToBaseLevel = false) : ICommand<AccessGrantDto>;

public sealed class UpdateAccessGrantCommandValidator : AbstractValidator<UpdateAccessGrantCommand>
{
    public UpdateAccessGrantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.ValidityType)
            .Must(t => AccessValidityTypes.All.Contains(t))
            .WithMessage("ValidityType must be Indefinite, Duration or UntilDate.")
            .When(x => x.ValidityType is not null);

        RuleFor(x => x.DurationDays)
            .NotNull().InclusiveBetween(1, GrantRules.MaxDurationDays)
            .When(x => x.ValidityType == AccessValidityTypes.Duration);

        RuleFor(x => x.ValidTo)
            .NotNull()
            .When(x => x.ValidityType == AccessValidityTypes.UntilDate);

        RuleFor(x => x.ActionCodes)
            .Null()
            .WithMessage("Use either ActionCodes or ResetToBaseLevel.")
            .When(x => x.ResetToBaseLevel);

        RuleFor(x => x)
            .Must(x => x.ValidityType is not null || x.ActionCodes is not null || x.ResetToBaseLevel)
            .OverridePropertyName("Changes")
            .WithMessage("Indicate a new validity or new permissions.");
    }
}

public sealed class UpdateAccessGrantCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<UpdateAccessGrantCommand, AccessGrantDto>
{
    public async Task<Result<AccessGrantDto>> Handle(UpdateAccessGrantCommand request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<AccessGrantDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var matrix = context.Matrix;
        var now = DateTime.UtcNow;

        var grant = await dbContext.AccessGrants.FirstOrDefaultAsync(
            g => g.Id == request.Id && g.GrantorClientId == context.OrganizationId, cancellationToken);
        if (grant is null)
            return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.NotFound(request.Id));

        if (!grant.IsEffectiveAt(now) && grant.Status != AccessGrantStatus.PendingAcceptance)
            return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.NotOpen);

        var grantee = await dbContext.Clients.AsNoTracking().FirstAsync(c => c.Id == grant.GranteeClientId, cancellationToken);
        var receiverRole = grant.IntendedRole ?? ShipmentRoleCodes.ThirdParty;

        // Lo que el otorgante posee hoy sobre el embarque; sin BL (booking), el nivel del customer.
        var bl = grant.BillOfLadingId is null
            ? null
            : await dbContext.BillsOfLading.AsNoTracking().FirstOrDefaultAsync(b => b.Id == grant.BillOfLadingId, cancellationToken);

        ShipmentPermissionSet? grantorPermissions = null;
        List<string> ceiling;
        if (bl is not null)
        {
            grantorPermissions = await accessEvaluator.EvaluateAsync(context.Scope, bl, cancellationToken);
            ceiling = GrantRules.CeilingOf(matrix, grantorPermissions);
        }
        else
        {
            ceiling = ActionCodeList.Parse(grant.CeilingActionCodes).ToList();
        }

        var changesPermissions = request.ActionCodes is not null || request.ResetToBaseLevel;
        var changesValidity = request.ValidityType is not null;

        if (grantorPermissions is not null
            && ((changesPermissions && !grantorPermissions.CanExecute(ShipmentActionCodes.GrantAccess))
                || (changesValidity && !grantorPermissions.CanExecute(ShipmentActionCodes.SetAccessValidity))))
            return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.NotAllowed);

        var explicitActions = ActionCodeList.ParseNullable(grant.ActionCodes);
        if (changesPermissions)
        {
            var normalized = GrantRules.NormalizeExplicit(matrix, request.ResetToBaseLevel ? null : request.ActionCodes);
            if (normalized.IsFailure)
                return Result<AccessGrantDto>.Failure(normalized.Error);

            var error = GrantRules.CheckGrantable(matrix, grantee.OrganizationType, receiverRole, normalized.Value)
                ?? GrantRules.CheckGrantorLevel(normalized.Value, ceiling);
            if (error is not null)
                return Result<AccessGrantDto>.Failure(error);

            if (grant.IsMandate && normalized.Value is null)
                return Result<AccessGrantDto>.Failure(
                    DomainErrors.AccessGrant.InvalidValidity("A mandate requires an explicit scope (action codes)."));

            var previous = explicitActions;
            explicitActions = normalized.Value;
            grant.ActionCodes = explicitActions is null ? null : ActionCodeList.Format(explicitActions);
            grant.CeilingActionCodes = ActionCodeList.Format(ceiling);

            AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantPermissionsChanged, context.Actor, now, grant, bl?.BLNumber,
                new { previous, current = explicitActions });
        }

        if (changesValidity)
        {
            var validity = GrantRules.ResolveValidity(
                request.ValidityType!,
                request.ValidFrom ?? grant.ValidFrom,
                request.ValidTo,
                request.DurationDays,
                now);
            if (validity.IsFailure)
                return Result<AccessGrantDto>.Failure(validity.Error);

            if (grant.IsMandate && validity.Value.To is null)
                return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.InvalidValidity("A mandate requires a validity period."));

            var parentValidTo = grant.ParentGrantId is null
                ? null
                : await dbContext.AccessGrants.AsNoTracking()
                    .Where(g => g.Id == grant.ParentGrantId)
                    .Select(g => g.ValidTo)
                    .FirstOrDefaultAsync(cancellationToken);

            var window = validity.Value.ClampTo(parentValidTo);
            var previous = new { validityType = grant.ValidityType, validFrom = grant.ValidFrom, validTo = grant.ValidTo };

            grant.ValidityType = window.Type;
            grant.ValidFrom = window.From;
            grant.ValidTo = window.To;
            grant.DurationDays = window.DurationDays;

            AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantValidityChanged, context.Actor, now, grant, bl?.BLNumber,
                new { previous, current = new { validityType = window.Type, validFrom = window.From, validTo = window.To } });
        }

        var effective = matrix.AllowedByGrant(grantee.OrganizationType, receiverRole, explicitActions, ceiling);
        var cascade = await AccessGrantCascade.ClampDescendantsAsync(dbContext, grant, effective, context.Actor, now, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await AccessNotifier.NotifyOrganizationsAsync(
            dbContext,
            notificationPublisher,
            [grant.GranteeClientId],
            NotificationTypes.AccessUpdated,
            "Acceso modificado",
            $"{context.Membership.Organization.Name} modificó la vigencia o los permisos de su acceso sobre {AccessNotifier.Describe(grant, bl?.BLNumber)}.",
            cancellationToken,
            AccessNotifier.ForGrant(grant, bl?.BLNumber));
        await AccessNotifier.NotifyCascadeAsync(dbContext, notificationPublisher, cascade, cancellationToken);

        var dto = await AccessGrantMapper.ToDtosAsync(dbContext, matrix, [grant], context.OrganizationId, now, cancellationToken);
        return Result<AccessGrantDto>.Success(dto[0]);
    }
}
