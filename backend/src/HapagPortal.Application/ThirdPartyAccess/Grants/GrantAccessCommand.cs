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
/// Otorga a un tercero acceso a uno o varios BL o bookings en un solo flujo: destinatario, vigencia
/// y permisos (M1-12, M1-14, M1-15, M1-24). Sin <see cref="ActionCodes"/> aplica el nivel base de
/// M1-11 del tercero; nunca más de lo que el otorgante posee sobre cada embarque. Un acceso abierto
/// del mismo otorgante al mismo tercero sobre el mismo BL se actualiza, no se duplica.
/// Con <see cref="IsMandate"/> es un mandato digital (M1-03): alcance y vigencia obligatorios, y
/// queda pendiente hasta que el mandante acepte los términos vigentes.
/// </summary>
public sealed record GrantAccessCommand(
    Guid GranteeOrganizationId,
    IReadOnlyList<string>? BlNumbers = null,
    IReadOnlyList<string>? BookingNumbers = null,
    string ValidityType = AccessValidityTypes.Indefinite,
    DateTime? ValidFrom = null,
    DateTime? ValidTo = null,
    int? DurationDays = null,
    IReadOnlyList<string>? ActionCodes = null,
    bool IsMandate = false,
    bool AcceptTerms = false,
    string? TermsVersion = null) : ICommand<GrantAccessResultDto>;

public sealed class GrantAccessCommandValidator : AbstractValidator<GrantAccessCommand>
{
    public const int MaxReferences = 500;

    public GrantAccessCommandValidator()
    {
        RuleFor(x => x.GranteeOrganizationId).NotEmpty();

        RuleFor(x => x.BlNumbers)
            .Must((command, _) => ReferenceCount(command) is >= 1 and <= MaxReferences)
            .OverridePropertyName("References")
            .WithMessage($"Indicate between 1 and {MaxReferences} BL or booking numbers.");

        RuleForEach(x => x.BlNumbers).NotEmpty().MaximumLength(50);
        RuleForEach(x => x.BookingNumbers).NotEmpty().MaximumLength(50);

        RuleFor(x => x.ValidityType)
            .Must(t => AccessValidityTypes.All.Contains(t))
            .WithMessage("ValidityType must be Indefinite, Duration or UntilDate.");

        RuleFor(x => x.DurationDays)
            .NotNull().InclusiveBetween(1, GrantRules.MaxDurationDays)
            .When(x => x.ValidityType == AccessValidityTypes.Duration);

        RuleFor(x => x.ValidTo)
            .NotNull()
            .When(x => x.ValidityType == AccessValidityTypes.UntilDate);

        RuleFor(x => x.ValidFrom)
            .Must(f => f!.Value.ToUniversalTime() >= DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("ValidFrom cannot be in the past.")
            .When(x => x.ValidFrom is not null);

        RuleFor(x => x.ActionCodes)
            .Must(c => c!.Count <= 100)
            .When(x => x.ActionCodes is not null);

        // M1-03: el mandato define alcance y vigencia.
        RuleFor(x => x.ActionCodes)
            .NotEmpty()
            .WithMessage("A mandate requires an explicit scope (action codes).")
            .When(x => x.IsMandate);

        RuleFor(x => x.ValidityType)
            .NotEqual(AccessValidityTypes.Indefinite)
            .WithMessage("A mandate requires a validity period.")
            .When(x => x.IsMandate);
    }

    private static int ReferenceCount(GrantAccessCommand command) =>
        (command.BlNumbers?.Count ?? 0) + (command.BookingNumbers?.Count ?? 0);
}

public sealed class GrantAccessCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<GrantAccessCommand, GrantAccessResultDto>
{
    public async Task<Result<GrantAccessResultDto>> Handle(GrantAccessCommand request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(
            dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return Result<GrantAccessResultDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var matrix = context.Matrix;
        var now = DateTime.UtcNow;

        var grantee = await dbContext.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.GranteeOrganizationId, cancellationToken);
        var granteeError = GrantRules.CheckGrantee(grantee, request.GranteeOrganizationId, context.OrganizationId);
        if (granteeError is not null)
            return Result<GrantAccessResultDto>.Failure(granteeError);

        var normalized = GrantRules.NormalizeExplicit(matrix, request.ActionCodes);
        if (normalized.IsFailure)
            return Result<GrantAccessResultDto>.Failure(normalized.Error);
        var explicitActions = normalized.Value;

        var notGrantable = GrantRules.CheckGrantable(matrix, grantee!.OrganizationType, ShipmentRoleCodes.ThirdParty, explicitActions);
        if (notGrantable is not null)
            return Result<GrantAccessResultDto>.Failure(notGrantable);

        var validity = GrantRules.ResolveValidity(request.ValidityType, request.ValidFrom, request.ValidTo, request.DurationDays, now);
        if (validity.IsFailure)
            return Result<GrantAccessResultDto>.Failure(validity.Error);

        if (request.IsMandate && request.AcceptTerms && request.TermsVersion != MandateTerms.CurrentVersion)
            return Result<GrantAccessResultDto>.Failure(DomainErrors.AccessGrant.TermsVersionMismatch(MandateTerms.CurrentVersion));

        // Solo se resuelven embarques accesibles por el otorgante; lo ajeno no se distingue de lo inexistente.
        var blNumbers = (request.BlNumbers ?? []).Select(n => n.Trim()).Where(n => n.Length > 0).Distinct().ToList();
        var bookingNumbers = (request.BookingNumbers ?? []).Select(n => n.Trim()).Where(n => n.Length > 0).Distinct().ToList();

        var bills = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), context.Scope)
            .Where(b => blNumbers.Contains(b.BLNumber) || (b.BookingNumber != null && bookingNumbers.Contains(b.BookingNumber)))
            .ToListAsync(cancellationToken);

        var skipped = new List<GrantSkippedDto>();
        var targets = new List<(string Reference, BillOfLading Bl)>();

        foreach (var number in blNumbers)
        {
            var bl = bills.FirstOrDefault(b => b.BLNumber == number);
            if (bl is null)
                skipped.Add(Skip(number, DomainErrors.BillOfLading.NotFoundByNumber(number)));
            else
                targets.Add((number, bl));
        }

        foreach (var booking in bookingNumbers)
        {
            var matches = bills.Where(b => b.BookingNumber == booking).ToList();
            if (matches.Count == 0)
                skipped.Add(Skip(booking, DomainErrors.BillOfLading.NotFoundByNumber(booking)));
            targets.AddRange(matches.Select(b => (booking, b)));
        }

        targets = targets.DistinctBy(t => t.Bl.Id).ToList();

        var existing = await dbContext.AccessGrants
            .Where(g => g.GrantorClientId == context.OrganizationId
                && g.GranteeClientId == grantee.Id
                && g.BillOfLadingId != null
                && g.GrantType != AccessGrantTypes.EarlyBooking
                && AccessGrantStatus.Open.Contains(g.Status))
            .ToListAsync(cancellationToken);

        var grantType = blNumbers.Count + bookingNumbers.Count == 1 ? AccessGrantTypes.Individual : AccessGrantTypes.Bulk;
        var termsAccepted = request.IsMandate && request.AcceptTerms;
        var applied = new List<AccessGrant>();
        var created = 0;
        var modified = 0;

        foreach (var (reference, bl) in targets)
        {
            var permissions = await accessEvaluator.EvaluateAsync(context.Scope, bl, cancellationToken);
            if (!permissions.CanExecute(ShipmentActionCodes.GrantAccess))
            {
                skipped.Add(Skip(reference, DomainErrors.AccessGrant.NotAllowed));
                continue;
            }

            var ceiling = GrantRules.CeilingOf(matrix, permissions);
            var exceeds = GrantRules.CheckGrantorLevel(explicitActions, ceiling);
            if (exceeds is not null)
            {
                skipped.Add(Skip(reference, exceeds));
                continue;
            }

            var granted = explicitActions
                ?? matrix.AllowedByGrant(grantee.OrganizationType, ShipmentRoleCodes.ThirdParty, null, ceiling);
            var origin = GrantRules.OriginOf(permissions, granted);
            var window = validity.Value.ClampTo(origin?.ValidTo);
            var status = request.IsMandate && !termsAccepted ? AccessGrantStatus.PendingAcceptance : AccessGrantStatus.Active;

            var grant = existing.FirstOrDefault(g => g.BillOfLadingId == bl.Id);
            if (grant is null)
            {
                grant = new AccessGrant
                {
                    GrantorClientId = context.OrganizationId,
                    GrantorRole = GrantRules.GrantorRoleOf(permissions),
                    GranteeClientId = grantee.Id,
                    BillOfLadingId = bl.Id,
                    BookingNumber = bl.BookingNumber,
                    GrantType = grantType,
                    ValidityType = window.Type,
                    Status = status,
                    GrantedByUserId = context.Actor.UserId
                };
                Apply(grant, explicitActions, ceiling, window, origin, request, termsAccepted, context.Actor, now);
                dbContext.AccessGrants.Add(grant);

                AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantCreated, context.Actor, now, grant, bl.BLNumber, new
                {
                    grantType,
                    actionCodes = explicitActions,
                    validityType = window.Type,
                    validFrom = window.From,
                    validTo = window.To,
                    isMandate = request.IsMandate,
                    termsVersion = grant.TermsVersion,
                    parentGrantId = grant.ParentGrantId
                });

                if (termsAccepted)
                    AccessAudit.ForGrant(dbContext, AccessAuditEvents.MandateTermsAccepted, context.Actor, now, grant, bl.BLNumber,
                        new { termsVersion = grant.TermsVersion });

                created++;
            }
            else
            {
                var previous = new
                {
                    actionCodes = ActionCodeList.ParseNullable(grant.ActionCodes),
                    validityType = grant.ValidityType,
                    validFrom = grant.ValidFrom,
                    validTo = grant.ValidTo
                };

                Apply(grant, explicitActions, ceiling, window, origin, request, termsAccepted, context.Actor, now);
                grant.Status = request.IsMandate && grant.TermsAcceptedAt is null
                    ? AccessGrantStatus.PendingAcceptance
                    : AccessGrantStatus.Active;

                if (!SameCodes(previous.actionCodes, explicitActions))
                    AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantPermissionsChanged, context.Actor, now, grant, bl.BLNumber,
                        new { previous = previous.actionCodes, current = explicitActions });

                if (previous.validityType != window.Type || previous.validFrom != window.From || previous.validTo != window.To)
                    AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantValidityChanged, context.Actor, now, grant, bl.BLNumber,
                        new { previous, current = new { validityType = window.Type, validFrom = window.From, validTo = window.To } });

                if (termsAccepted)
                    AccessAudit.ForGrant(dbContext, AccessAuditEvents.MandateTermsAccepted, context.Actor, now, grant, bl.BLNumber,
                        new { termsVersion = grant.TermsVersion });

                await AccessGrantCascade.ClampDescendantsAsync(
                    dbContext, grant, explicitActions ?? ceiling, context.Actor, now, cancellationToken);

                modified++;
            }

            applied.Add(grant);
        }

        if (applied.Count == 0 && blNumbers.Count + bookingNumbers.Count == 1 && skipped.Count > 0)
        {
            var only = skipped[0];
            return Result<GrantAccessResultDto>.Failure(new Error(only.Code, only.Message));
        }

        if (applied.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            var references = targets.Where(t => applied.Any(g => g.BillOfLadingId == t.Bl.Id)).Select(t => t.Bl.BLNumber).ToList();
            await AccessNotifier.NotifyOrganizationsAsync(
                dbContext,
                notificationPublisher,
                [grantee.Id],
                NotificationTypes.AccessGranted,
                request.IsMandate ? "Mandato digital recibido" : "Acceso a embarques otorgado",
                $"{context.Membership.Organization.Name} le otorgó acceso a {AccessNotifier.Describe(references)}.",
                cancellationToken);
        }

        var dtos = await AccessGrantMapper.ToDtosAsync(dbContext, matrix, applied, context.OrganizationId, now, cancellationToken);

        return Result<GrantAccessResultDto>.Success(new GrantAccessResultDto(
            blNumbers.Count + bookingNumbers.Count,
            applied.Count,
            created,
            modified,
            skipped,
            dtos));
    }

    private static void Apply(
        AccessGrant grant,
        IReadOnlyList<string>? explicitActions,
        IReadOnlyList<string> ceiling,
        ValidityWindow window,
        ShipmentGrantAccess? origin,
        GrantAccessCommand request,
        bool termsAccepted,
        AccessActor actor,
        DateTime now)
    {
        grant.ActionCodes = explicitActions is null ? null : ActionCodeList.Format(explicitActions);
        grant.CeilingActionCodes = ActionCodeList.Format(ceiling);
        grant.ValidityType = window.Type;
        grant.ValidFrom = window.From;
        grant.ValidTo = window.To;
        grant.DurationDays = window.DurationDays;
        grant.ParentGrantId = origin?.GrantId;
        grant.IsMandate = request.IsMandate;

        if (termsAccepted)
        {
            grant.TermsVersion = MandateTerms.CurrentVersion;
            grant.TermsAcceptedAt = now;
            grant.TermsAcceptedByUserId = actor.UserId;
            grant.Status = AccessGrantStatus.Active;
        }
    }

    private static bool SameCodes(IReadOnlyList<string>? left, IReadOnlyList<string>? right) =>
        left is null ? right is null : right is not null && left.OrderBy(c => c).SequenceEqual(right.OrderBy(c => c));

    private static GrantSkippedDto Skip(string reference, Error error) => new(reference, error.Code, error.Message);
}
