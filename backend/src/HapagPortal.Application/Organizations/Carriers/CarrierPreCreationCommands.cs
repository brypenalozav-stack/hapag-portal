namespace HapagPortal.Application.Organizations.Carriers;

using FluentValidation;
using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.ThirdPartyAccess.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>BL o booking asignado a un transportista pre-creado y el estado de su acceso (pendiente hasta su activación).</summary>
public sealed record CarrierAssignmentDto(Guid GrantId, string? BlNumber, string? BookingNumber, string Status, DateTime CreatedAt);

/// <summary>Transportista pre-creado por la organización (M1-09).</summary>
public sealed record CarrierPreRegistrationDto(
    Guid Id,
    OrganizationRefDto Carrier,
    string LegalName,
    string TaxId,
    string Email,
    string Country,
    string Status,
    int? DurationDays,
    DateTime CreatedAt,
    string RequestedBy,
    DateTime? InvitationSentAt,
    DateTime? ActivatedAt,
    IReadOnlyList<CarrierAssignmentDto> Assignments);

/// <summary>
/// Resultado de la pre-creación: <see cref="Created"/> = se creó el perfil (si ya existía pre-creado, se reutiliza sin
/// duplicarlo), <see cref="Assigned"/> = accesos pendientes nuevos y <see cref="Skipped"/> las referencias no asignadas.
/// </summary>
public sealed record CarrierPreCreationResultDto(
    CarrierPreRegistrationDto PreRegistration,
    bool Created,
    bool InvitationSent,
    int Assigned,
    IReadOnlyList<GrantSkippedDto> Skipped);

/// <summary>
/// El cliente pre-crea el perfil de un transportista sin cuenta (M1-09) y le asigna BL y bookings. La organización
/// queda <c>PreCreated</c> con su usuario invitado (define su contraseña con un código de un solo uso); los accesos
/// quedan pendientes de activación y se activan en su primer ingreso.
/// </summary>
public sealed record PreCreateCarrierCommand(
    string LegalName,
    string TaxId,
    string Email,
    string? Country = null,
    string? ContactFirstName = null,
    string? ContactLastName = null,
    IReadOnlyList<string>? BlNumbers = null,
    IReadOnlyList<string>? BookingNumbers = null,
    int? DurationDays = null) : ICommand<CarrierPreCreationResultDto>;

/// <summary>Asigna más BL o bookings a un transportista pre-creado que todavía no ingresa.</summary>
public sealed record AssignCarrierReferencesCommand(
    Guid PreRegistrationId,
    IReadOnlyList<string>? BlNumbers = null,
    IReadOnlyList<string>? BookingNumbers = null) : ICommand<CarrierPreCreationResultDto>;

public sealed record GetCarrierPreRegistrationsQuery : IQuery<IReadOnlyList<CarrierPreRegistrationDto>>;

/// <summary>
/// Reenvía el código de invitación a un transportista pre-creado que intenta registrarse (anónimo). Responde igual exista
/// o no la cuenta, para no revelar qué correos están registrados.
/// </summary>
public sealed record ResendCarrierInvitationCommand(string Email) : ICommand;

public sealed class PreCreateCarrierCommandValidator : AbstractValidator<PreCreateCarrierCommand>
{
    public PreCreateCarrierCommandValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c!.Trim().ToUpperInvariant()))
            .When(x => !string.IsNullOrWhiteSpace(x.Country))
            .WithMessage("Country must be 'CL' or 'BO'.");
        RuleFor(x => x.ContactFirstName).MaximumLength(100);
        RuleFor(x => x.ContactLastName).MaximumLength(100);
        RuleFor(x => x.DurationDays).InclusiveBetween(1, GrantRules.MaxDurationDays).When(x => x.DurationDays is not null);
        RuleFor(x => x)
            .Must(x => (x.BlNumbers?.Count ?? 0) + (x.BookingNumbers?.Count ?? 0) <= CarrierAssignments.MaxReferences)
            .WithName("References")
            .WithMessage($"Indicate at most {CarrierAssignments.MaxReferences} BL or booking numbers.");
        RuleForEach(x => x.BlNumbers).NotEmpty().MaximumLength(50);
        RuleForEach(x => x.BookingNumbers).NotEmpty().MaximumLength(50);
    }
}

public sealed class AssignCarrierReferencesCommandValidator : AbstractValidator<AssignCarrierReferencesCommand>
{
    public AssignCarrierReferencesCommandValidator()
    {
        RuleFor(x => x.PreRegistrationId).NotEmpty();
        RuleFor(x => x)
            .Must(x => (x.BlNumbers?.Count ?? 0) + (x.BookingNumbers?.Count ?? 0) is >= 1 and <= CarrierAssignments.MaxReferences)
            .WithName("References")
            .WithMessage($"Indicate between 1 and {CarrierAssignments.MaxReferences} BL or booking numbers.");
        RuleForEach(x => x.BlNumbers).NotEmpty().MaximumLength(50);
        RuleForEach(x => x.BookingNumbers).NotEmpty().MaximumLength(50);
    }
}

public sealed class ResendCarrierInvitationCommandValidator : AbstractValidator<ResendCarrierInvitationCommand>
{
    public ResendCarrierInvitationCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
    }
}

/// <summary>
/// Asignación de BL y bookings a un transportista pre-creado (M1-09, M1-12): mismas reglas que un otorgamiento
/// (embarque accesible por la organización, acción de otorgar, nivel base del transportista bajo el techo de lo que
/// posee), pero el acceso queda <c>PendingActivation</c> hasta que el transportista ingresa por primera vez.
/// </summary>
public static class CarrierAssignments
{
    public const int MaxReferences = 500;
    public const int InvitationValidityHours = 168;

    public static async Task<(List<AccessGrant> Created, List<GrantSkippedDto> Skipped)> AssignAsync(
        IApplicationDbContext dbContext,
        IShipmentAccessEvaluator accessEvaluator,
        AccessManagementContext context,
        Client carrier,
        int? durationDays,
        IReadOnlyList<string>? blNumbers,
        IReadOnlyList<string>? bookingNumbers,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var bls = (blNumbers ?? []).Select(n => n.Trim()).Where(n => n.Length > 0).Distinct().ToList();
        var bookings = (bookingNumbers ?? []).Select(n => n.Trim()).Where(n => n.Length > 0).Distinct().ToList();
        var created = new List<AccessGrant>();
        var skipped = new List<GrantSkippedDto>();
        if (bls.Count + bookings.Count == 0)
            return (created, skipped);

        var bills = await accessEvaluator.FilterAccessible(dbContext.BillsOfLading.AsNoTracking(), context.Scope)
            .Where(b => bls.Contains(b.BLNumber) || (b.BookingNumber != null && bookings.Contains(b.BookingNumber)))
            .ToListAsync(cancellationToken);

        var targets = new List<(string Reference, BillOfLading Bl)>();
        foreach (var number in bls)
        {
            var bl = bills.FirstOrDefault(b => b.BLNumber == number);
            if (bl is null)
                skipped.Add(Skip(number, DomainErrors.BillOfLading.NotFoundByNumber(number)));
            else
                targets.Add((number, bl));
        }

        foreach (var booking in bookings)
        {
            var matches = bills.Where(b => b.BookingNumber == booking).ToList();
            if (matches.Count == 0)
                skipped.Add(Skip(booking, DomainErrors.BillOfLading.NotFoundByNumber(booking)));
            targets.AddRange(matches.Select(b => (booking, b)));
        }

        var existing = await dbContext.AccessGrants.AsNoTracking()
            .Where(g => g.GrantorClientId == context.OrganizationId
                && g.GranteeClientId == carrier.Id
                && g.BillOfLadingId != null
                && AccessGrantStatus.Open.Contains(g.Status))
            .Select(g => g.BillOfLadingId!.Value)
            .ToListAsync(cancellationToken);

        var grantType = bls.Count + bookings.Count == 1 ? AccessGrantTypes.Individual : AccessGrantTypes.Bulk;
        var matrix = context.Matrix;

        foreach (var (reference, bl) in targets.DistinctBy(t => t.Bl.Id))
        {
            if (existing.Contains(bl.Id))
            {
                skipped.Add(new GrantSkippedDto(reference, "CarrierPreCreation.AlreadyAssigned", $"{bl.BLNumber} is already assigned to the carrier."));
                continue;
            }

            var permissions = await accessEvaluator.EvaluateAsync(context.Scope, bl, cancellationToken);
            if (!permissions.CanExecute(ShipmentActionCodes.GrantAccess))
            {
                skipped.Add(Skip(reference, DomainErrors.AccessGrant.NotAllowed));
                continue;
            }

            var ceiling = GrantRules.CeilingOf(matrix, permissions);
            var granted = matrix.AllowedByGrant(carrier.OrganizationType, ShipmentRoleCodes.ThirdParty, null, ceiling);
            var origin = GrantRules.OriginOf(permissions, granted);

            var grant = new AccessGrant
            {
                GrantorClientId = context.OrganizationId,
                GrantorRole = GrantRules.GrantorRoleOf(permissions),
                GranteeClientId = carrier.Id,
                BillOfLadingId = bl.Id,
                BookingNumber = bl.BookingNumber,
                GrantType = grantType,
                CeilingActionCodes = ActionCodeList.Format(ceiling),
                ValidityType = durationDays is null ? AccessValidityTypes.Indefinite : AccessValidityTypes.Duration,
                ValidFrom = now,
                DurationDays = durationDays,
                Status = AccessGrantStatus.PendingActivation,
                ParentGrantId = origin?.GrantId,
                GrantedByUserId = context.Actor.UserId
            };
            dbContext.AccessGrants.Add(grant);
            AccessAudit.ForGrant(dbContext, AccessAuditEvents.GrantCreated, context.Actor, now, grant, bl.BLNumber, new
            {
                grantType,
                preCreatedCarrier = true,
                status = AccessGrantStatus.PendingActivation,
                durationDays
            });
            created.Add(grant);
        }

        return (created, skipped);
    }

    public static async Task<CarrierPreRegistrationDto> ToDtoAsync(
        IApplicationDbContext dbContext,
        CarrierPreRegistration registration,
        CancellationToken cancellationToken)
    {
        var carrier = await dbContext.Clients.AsNoTracking().FirstAsync(c => c.Id == registration.CarrierOrganizationId, cancellationToken);
        var grants = await (
                from g in dbContext.AccessGrants.AsNoTracking()
                join b in dbContext.BillsOfLading.AsNoTracking() on g.BillOfLadingId equals b.Id
                where g.GrantorClientId == registration.RequestedByOrganizationId && g.GranteeClientId == registration.CarrierOrganizationId
                select new { Grant = g, b.BLNumber })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        return new CarrierPreRegistrationDto(
            registration.Id,
            AccessGrantMapper.ToRef(carrier),
            registration.LegalName,
            registration.TaxId,
            registration.Email,
            registration.Country,
            registration.Status,
            registration.DurationDays,
            registration.CreatedAt,
            registration.RequestedBy,
            registration.InvitationSentAt,
            registration.ActivatedAt,
            grants.OrderBy(g => g.Grant.CreatedAt)
                .Select(g => new CarrierAssignmentDto(g.Grant.Id, g.BLNumber, g.Grant.BookingNumber,
                    AccessGrantMapper.DisplayStatus(g.Grant, now), g.Grant.CreatedAt))
                .ToList());
    }

    /// <summary>Invitación con código de un solo uso para definir la contraseña (mismo flujo de M1-10).</summary>
    public static async Task SendInvitationAsync(
        IEmailService emailService,
        User user,
        string carrierName,
        string requestedBy,
        CancellationToken cancellationToken) =>
        await emailService.SendEmailAsync(
            user.Email,
            "Su cuenta de transportista en el portal Hapag-Lloyd",
            $"{requestedBy} creó el perfil de {carrierName} en el portal Hapag-Lloyd y le asignó embarques. " +
            $"Defina su contraseña con este código de un solo uso, válido por {InvitationValidityHours / 24} días: {user.PasswordResetToken}. " +
            "Al ingresar por primera vez verá los BL y bookings asignados.",
            cancellationToken);

    private static GrantSkippedDto Skip(string reference, Error error) => new(reference, error.Code, error.Message);
}

public sealed class PreCreateCarrierCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    IPasswordHasher passwordHasher,
    IEmailService emailService)
    : ICommandHandler<PreCreateCarrierCommand, CarrierPreCreationResultDto>
{
    public async Task<Result<CarrierPreCreationResultDto>> Handle(PreCreateCarrierCommand request, CancellationToken cancellationToken)
    {
        var loaded = await CarrierContext.LoadAsync(dbContext, currentUserService, accessEvaluator, cancellationToken);
        if (loaded.IsFailure)
            return Result<CarrierPreCreationResultDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var country = string.IsNullOrWhiteSpace(request.Country) ? context.Membership.Organization.Country : request.Country.Trim().ToUpperInvariant();
        var email = EmailNormalizer.Normalize(request.Email);
        var taxId = request.TaxId.Trim();

        // Sin duplicados: misma identificación tributaria (normalizada) en el país o mismo correo.
        var sameCountry = await dbContext.Clients.AsNoTracking().Where(c => c.Country == country).ToListAsync(cancellationToken);
        var byTaxId = sameCountry.FirstOrDefault(c => TaxIdNormalizer.AreEqual(c.TaxId, taxId));
        var userByEmail = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        var byEmail = userByEmail?.ClientId is { } emailOrgId
            ? await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == emailOrgId, cancellationToken)
            : null;

        var match = byTaxId ?? byEmail;
        if (match is not null && match.Id == context.OrganizationId)
            return Result<CarrierPreCreationResultDto>.Failure(DomainErrors.AccessGrant.SelfGrant);
        if (match is not null && match.OrganizationType != OrganizationTypes.Carrier)
            return Result<CarrierPreCreationResultDto>.Failure(DomainErrors.CarrierPreCreation.NotACarrier);
        if (match is not null && match.RegistrationStatus != OrganizationStatus.PreCreated)
            return Result<CarrierPreCreationResultDto>.Failure(DomainErrors.CarrierPreCreation.AlreadyRegistered(match.Name));
        if (match is null && userByEmail is not null)
            return Result<CarrierPreCreationResultDto>.Failure(DomainErrors.CarrierPreCreation.EmailInUse);
        if (match is null && await dbContext.Clients.AnyAsync(c => c.Email == email, cancellationToken))
            return Result<CarrierPreCreationResultDto>.Failure(DomainErrors.CarrierPreCreation.EmailInUse);

        var now = DateTime.UtcNow;
        var carrier = match;
        User? invited = null;
        var created = carrier is null;

        if (carrier is null)
        {
            carrier = new Client
            {
                Name = request.LegalName.Trim(),
                TaxId = taxId,
                TaxIdType = CountryCodes.GetTaxIdType(country),
                Country = country,
                Email = email,
                ClientType = OrganizationTypes.ToLegacyClientType(OrganizationTypes.Carrier),
                OrganizationType = OrganizationTypes.Carrier,
                RegistrationStatus = OrganizationStatus.PreCreated,
                OperatingCountries = country,
                ReviewNotes = $"Pre-creado por {context.Membership.Organization.Name} (M1-09).",
                IsActive = true
            };
            dbContext.Clients.Add(carrier);

            invited = new User
            {
                ClientId = carrier.Id,
                Email = email,
                Username = email,
                PasswordHash = passwordHasher.Hash(Guid.NewGuid().ToString("N") + "Aa1!"),
                UserType = UserTypes.Client,
                Country = country,
                FirstName = string.IsNullOrWhiteSpace(request.ContactFirstName) ? null : request.ContactFirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(request.ContactLastName) ? null : request.ContactLastName.Trim(),
                IsActive = true,
                MembershipStatus = MembershipStatus.Active,
                MembershipDecidedAt = now,
                MembershipDecidedBy = context.Actor.Email,
                PasswordResetToken = Guid.NewGuid().ToString(),
                PasswordResetTokenExpiry = now.AddHours(CarrierAssignments.InvitationValidityHours)
            };
            dbContext.Users.Add(invited);
            dbContext.UserRoles.Add(new UserRole { UserId = invited.Id, RoleName = UserTypes.Client });
            dbContext.UserRoles.Add(new UserRole { UserId = invited.Id, RoleName = RoleCodes.OrgAdmin });
        }

        var carrierUserId = invited?.Id
            ?? userByEmail?.Id
            ?? await dbContext.Users.Where(u => u.ClientId == carrier.Id).OrderBy(u => u.CreatedAt).Select(u => u.Id).FirstAsync(cancellationToken);

        var registration = await dbContext.CarrierPreRegistrations.FirstOrDefaultAsync(r =>
            r.CarrierOrganizationId == carrier.Id && r.RequestedByOrganizationId == context.OrganizationId, cancellationToken);
        if (registration is null)
        {
            registration = new CarrierPreRegistration
            {
                CarrierOrganizationId = carrier.Id,
                CarrierUserId = carrierUserId,
                RequestedByOrganizationId = context.OrganizationId,
                RequestedByUserId = context.Actor.UserId,
                RequestedBy = context.Actor.Email ?? "system",
                LegalName = request.LegalName.Trim(),
                TaxId = taxId,
                Email = email,
                Country = country,
                DurationDays = request.DurationDays,
                Status = CarrierPreRegistrationStatus.Pending,
                CreatedAt = now
            };
            dbContext.CarrierPreRegistrations.Add(registration);

            AccessAudit.ForOrganization(dbContext, AccessAuditEvents.CarrierPreCreated, context.Actor, now, context.OrganizationId, carrier.Id,
                new { preRegistrationId = registration.Id, created, legalName = registration.LegalName, taxId, email, country });
        }
        else if (request.DurationDays is not null)
        {
            registration.DurationDays = request.DurationDays;
        }

        var (grants, skipped) = await CarrierAssignments.AssignAsync(
            dbContext, accessEvaluator, context, carrier, registration.DurationDays, request.BlNumbers, request.BookingNumbers, now, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var invitationSent = false;
        if (invited is not null)
        {
            await CarrierAssignments.SendInvitationAsync(emailService, invited, carrier.Name, context.Membership.Organization.Name, cancellationToken);
            registration.InvitationSentAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            invitationSent = true;
        }

        return Result<CarrierPreCreationResultDto>.Success(new CarrierPreCreationResultDto(
            await CarrierAssignments.ToDtoAsync(dbContext, registration, cancellationToken), created, invitationSent, grants.Count, skipped));
    }
}

public sealed class AssignCarrierReferencesCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : ICommandHandler<AssignCarrierReferencesCommand, CarrierPreCreationResultDto>
{
    public async Task<Result<CarrierPreCreationResultDto>> Handle(AssignCarrierReferencesCommand request, CancellationToken cancellationToken)
    {
        var loaded = await CarrierContext.LoadAsync(dbContext, currentUserService, accessEvaluator, cancellationToken);
        if (loaded.IsFailure)
            return Result<CarrierPreCreationResultDto>.Failure(loaded.Error);

        var context = loaded.Value;
        var registration = await dbContext.CarrierPreRegistrations.FirstOrDefaultAsync(
            r => r.Id == request.PreRegistrationId && r.RequestedByOrganizationId == context.OrganizationId, cancellationToken);
        if (registration is null)
            return Result<CarrierPreCreationResultDto>.Failure(DomainErrors.CarrierPreCreation.NotFound(request.PreRegistrationId));
        if (registration.Status != CarrierPreRegistrationStatus.Pending)
            return Result<CarrierPreCreationResultDto>.Failure(DomainErrors.CarrierPreCreation.AlreadyActivated);

        var carrier = await dbContext.Clients.AsNoTracking().FirstAsync(c => c.Id == registration.CarrierOrganizationId, cancellationToken);
        var (grants, skipped) = await CarrierAssignments.AssignAsync(
            dbContext, accessEvaluator, context, carrier, registration.DurationDays, request.BlNumbers, request.BookingNumbers,
            DateTime.UtcNow, cancellationToken);

        if (grants.Count == 0 && skipped.Count == 1 && (request.BlNumbers?.Count ?? 0) + (request.BookingNumbers?.Count ?? 0) == 1)
            return Result<CarrierPreCreationResultDto>.Failure(new Error(skipped[0].Code, skipped[0].Message));

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<CarrierPreCreationResultDto>.Success(new CarrierPreCreationResultDto(
            await CarrierAssignments.ToDtoAsync(dbContext, registration, cancellationToken), false, false, grants.Count, skipped));
    }
}

public sealed class GetCarrierPreRegistrationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator)
    : IQueryHandler<GetCarrierPreRegistrationsQuery, IReadOnlyList<CarrierPreRegistrationDto>>
{
    public async Task<Result<IReadOnlyList<CarrierPreRegistrationDto>>> Handle(GetCarrierPreRegistrationsQuery request, CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(dbContext, currentUserService, accessEvaluator, requireOperate: false, cancellationToken);
        if (loaded.IsFailure)
            return Result<IReadOnlyList<CarrierPreRegistrationDto>>.Failure(loaded.Error);

        var organizationId = loaded.Value.OrganizationId;
        var registrations = await dbContext.CarrierPreRegistrations.AsNoTracking()
            .Where(r => r.RequestedByOrganizationId == organizationId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = new List<CarrierPreRegistrationDto>();
        foreach (var registration in registrations)
            items.Add(await CarrierAssignments.ToDtoAsync(dbContext, registration, cancellationToken));

        return Result<IReadOnlyList<CarrierPreRegistrationDto>>.Success(items);
    }
}

public sealed class ResendCarrierInvitationCommandHandler(
    IApplicationDbContext dbContext,
    IEmailService emailService)
    : ICommandHandler<ResendCarrierInvitationCommand>
{
    public async Task<Result> Handle(ResendCarrierInvitationCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        var user = await dbContext.Users.Include(u => u.Client).FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Solo cuentas pre-creadas que todavía no ingresan; la respuesta es la misma en todos los casos.
        if (user?.Client is { RegistrationStatus: OrganizationStatus.PreCreated } carrier && user.IsActive && user.LastLoginAt is null)
        {
            var now = DateTime.UtcNow;
            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiry = now.AddHours(CarrierAssignments.InvitationValidityHours);

            var registrations = await dbContext.CarrierPreRegistrations
                .Where(r => r.CarrierOrganizationId == carrier.Id && r.Status == CarrierPreRegistrationStatus.Pending)
                .ToListAsync(cancellationToken);
            foreach (var registration in registrations)
                registration.InvitationSentAt = now;

            await dbContext.SaveChangesAsync(cancellationToken);
            await CarrierAssignments.SendInvitationAsync(emailService, user, carrier.Name,
                registrations.FirstOrDefault()?.RequestedBy ?? "Un cliente", cancellationToken);
        }

        return Result.Success();
    }
}

/// <summary>Contexto de la organización que pre-crea: pre-crear transportistas es una acción de la organización en M1-11.</summary>
internal static class CarrierContext
{
    public static async Task<Result<AccessManagementContext>> LoadAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IShipmentAccessEvaluator accessEvaluator,
        CancellationToken cancellationToken)
    {
        var loaded = await AccessManagementContext.LoadAsync(dbContext, currentUserService, accessEvaluator, requireOperate: true, cancellationToken);
        if (loaded.IsFailure)
            return loaded;

        return loaded.Value.Matrix.OrganizationCan(loaded.Value.OrganizationType, ShipmentActionCodes.PreCreateCarrier)
            ? loaded
            : Result<AccessManagementContext>.Failure(DomainErrors.CarrierPreCreation.NotAllowed);
    }
}
