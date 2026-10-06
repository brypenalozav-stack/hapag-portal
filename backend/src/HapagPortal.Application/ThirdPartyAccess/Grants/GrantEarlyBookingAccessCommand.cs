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
/// El customer otorga acceso a un futuro shipper (o consignee) con solo el número de booking, antes de
/// que exista el BL con los roles formales (M1-20). Un Freight Forwarder también puede recibirlo como
/// tercero (excepción de M1-11). El acceso se vincula y reconcilia con el rol oficial cuando el BL llega.
/// </summary>
public sealed record GrantEarlyBookingAccessCommand(
    string BookingNumber,
    Guid GranteeOrganizationId,
    string IntendedRole = ShipmentRoleCodes.Shipper,
    string ValidityType = AccessValidityTypes.Indefinite,
    DateTime? ValidFrom = null,
    DateTime? ValidTo = null,
    int? DurationDays = null,
    IReadOnlyList<string>? ActionCodes = null) : ICommand<AccessGrantDto>;

public sealed class GrantEarlyBookingAccessCommandValidator : AbstractValidator<GrantEarlyBookingAccessCommand>
{
    public GrantEarlyBookingAccessCommandValidator()
    {
        RuleFor(x => x.BookingNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.GranteeOrganizationId).NotEmpty();
        RuleFor(x => x.IntendedRole)
            .Must(r => EarlyBookingAccess.IntendedRoles.Contains(r))
            .WithMessage("IntendedRole must be Shipper, Consignee or ThirdParty.");
        RuleFor(x => x.ValidityType)
            .Must(t => AccessValidityTypes.All.Contains(t))
            .WithMessage("ValidityType must be Indefinite, Duration or UntilDate.");
        RuleFor(x => x.DurationDays)
            .NotNull().InclusiveBetween(1, GrantRules.MaxDurationDays)
            .When(x => x.ValidityType == AccessValidityTypes.Duration);
        RuleFor(x => x.ValidTo)
            .NotNull()
            .When(x => x.ValidityType == AccessValidityTypes.UntilDate);
    }
}

public sealed class GrantEarlyBookingAccessCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<GrantEarlyBookingAccessCommand, AccessGrantDto>
{
    public async Task<Result<AccessGrantDto>> Handle(GrantEarlyBookingAccessCommand request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<AccessGrantDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var matrix = context.Matrix;
        var now = DateTime.UtcNow;
        var bookingNumber = request.BookingNumber.Trim();

        var grantee = await dbContext.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.GranteeOrganizationId, cancellationToken);
        var granteeError = GrantRules.CheckGrantee(grantee, request.GranteeOrganizationId, context.OrganizationId);
        if (granteeError is not null)
            return Result<AccessGrantDto>.Failure(granteeError);

        if (!EarlyBookingAccess.CanReceive(request.IntendedRole, grantee!.OrganizationType))
            return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.EarlyBookingRecipient);

        // Con el BL ya en el portal, el otorgante debe ser su customer; si todavía no existe, debe poder
        // actuar como customer (M1-11) y el acceso se vincula solo si al llegar el BL es su customer.
        var bl = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), context.Scope)
            .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber, cancellationToken);

        List<string> ceiling;
        if (bl is not null)
        {
            var permissions = await accessEvaluator.EvaluateAsync(context.Scope, bl, cancellationToken);
            if (!permissions.CanExecute(ShipmentActionCodes.GrantEarlyBookingAccess))
                return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.EarlyBookingNotAllowed);
            ceiling = GrantRules.CeilingOf(matrix, permissions);
        }
        else
        {
            if (!matrix.OrganizationCan(context.OrganizationType, ShipmentActionCodes.GrantEarlyBookingAccess, [ShipmentRoleCodes.Customer]))
                return Result<AccessGrantDto>.Failure(DomainErrors.AccessGrant.EarlyBookingNotAllowed);

            var shipmentScoped = matrix.ShipmentScopedActions.Select(a => a.Code).ToHashSet(StringComparer.Ordinal);
            ceiling = matrix.AllowedForRoles(context.OrganizationType, [ShipmentRoleCodes.Customer])
                .Where(shipmentScoped.Contains)
                .ToList();
        }

        var normalized = GrantRules.NormalizeExplicit(matrix, request.ActionCodes);
        if (normalized.IsFailure)
            return Result<AccessGrantDto>.Failure(normalized.Error);

        var error = GrantRules.CheckGrantable(matrix, grantee.OrganizationType, request.IntendedRole, normalized.Value)
            ?? GrantRules.CheckGrantorLevel(normalized.Value, ceiling);
        if (error is not null)
            return Result<AccessGrantDto>.Failure(error);

        var validity = GrantRules.ResolveValidity(request.ValidityType, request.ValidFrom, request.ValidTo, request.DurationDays, now);
        if (validity.IsFailure)
            return Result<AccessGrantDto>.Failure(validity.Error);

        var window = validity.Value;
        var grant = await dbContext.AccessGrants.FirstOrDefaultAsync(
            g => g.GrantType == AccessGrantTypes.EarlyBooking
                && g.BookingNumber == bookingNumber
                && g.GrantorClientId == context.OrganizationId
                && g.GranteeClientId == grantee.Id
                && AccessGrantStatus.Open.Contains(g.Status),
            cancellationToken);

        var isNew = grant is null;
        grant ??= new AccessGrant
        {
            GrantorClientId = context.OrganizationId,
            GrantorRole = ShipmentRoleCodes.Customer,
            GranteeClientId = grantee.Id,
            BookingNumber = bookingNumber,
            GrantType = AccessGrantTypes.EarlyBooking,
            ValidityType = window.Type,
            Status = AccessGrantStatus.Active,
            GrantedByUserId = context.Actor.UserId
        };

        grant.BillOfLadingId = bl?.Id;
        grant.IntendedRole = request.IntendedRole;
        grant.ActionCodes = normalized.Value is null ? null : ActionCodeList.Format(normalized.Value);
        grant.CeilingActionCodes = ActionCodeList.Format(ceiling);
        grant.ValidityType = window.Type;
        grant.ValidFrom = window.From;
        grant.ValidTo = window.To;
        grant.DurationDays = window.DurationDays;

        if (isNew)
            dbContext.AccessGrants.Add(grant);

        AccessAudit.ForGrant(dbContext, isNew ? AccessAuditEvents.GrantCreated : AccessAuditEvents.GrantPermissionsChanged,
            context.Actor, now, grant, bl?.BLNumber, new
            {
                grantType = AccessGrantTypes.EarlyBooking,
                bookingNumber,
                intendedRole = request.IntendedRole,
                actionCodes = normalized.Value,
                validityType = window.Type,
                validFrom = window.From,
                validTo = window.To
            });

        if (isNew && bl is not null)
            AccessAudit.ForGrant(dbContext, AccessAuditEvents.BookingAccessLinked, context.Actor, now, grant, bl.BLNumber);

        await dbContext.SaveChangesAsync(cancellationToken);

        await AccessNotifier.NotifyOrganizationsAsync(
            dbContext,
            notificationPublisher,
            [grantee.Id],
            NotificationTypes.AccessGranted,
            "Acceso anticipado por booking",
            $"{context.Membership.Organization.Name} le otorgó acceso al booking {bookingNumber} como {request.IntendedRole}.",
            cancellationToken,
            AccessNotifier.ForGrant(grant, bl?.BLNumber));

        var dto = await AccessGrantMapper.ToDtosAsync(dbContext, matrix, [grant], context.OrganizationId, now, cancellationToken);
        return Result<AccessGrantDto>.Success(dto[0]);
    }
}
