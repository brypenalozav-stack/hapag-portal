namespace HapagPortal.Application.Admin.Organizations;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Paso 1 del flujo interno de M8-04: validación del cliente (identificación tributaria, razón
/// social, documentación). PendingValidation -> PendingArCheck.
/// </summary>
public sealed record ValidateOrganizationCommand(Guid Id, string? Notes) : ICommand;

/// <summary>
/// Paso 2 de M8-04: punto de control con AR y asignación del Match Code. PendingArCheck -> Approved;
/// desde aquí la organización opera. Opcionalmente fija los países de operación (M1-04).
/// </summary>
public sealed record CompleteOrganizationArCheckCommand(
    Guid Id,
    string MatchCode,
    string? ArReference,
    IReadOnlyList<string>? OperatingCountries,
    string? Notes) : ICommand;

/// <summary>Rechazo del registro en cualquiera de los pasos pendientes.</summary>
public sealed record RejectOrganizationCommand(Guid Id, string Reason) : ICommand;

public sealed class ValidateOrganizationCommandValidator : AbstractValidator<ValidateOrganizationCommand>
{
    public ValidateOrganizationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class CompleteOrganizationArCheckCommandValidator : AbstractValidator<CompleteOrganizationArCheckCommand>
{
    public CompleteOrganizationArCheckCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.MatchCode)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9-]+$").WithMessage("Match Code must be alphanumeric.");
        RuleFor(x => x.ArReference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleForEach(x => x.OperatingCountries)
            .Must(c => CountryCodes.ValidCountries.Contains(c))
            .WithMessage("Operating countries must be 'CL' or 'BO'.");
    }
}

public sealed class RejectOrganizationCommandValidator : AbstractValidator<RejectOrganizationCommand>
{
    public RejectOrganizationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class ValidateOrganizationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<ValidateOrganizationCommand>
{
    public async Task<Result> Handle(ValidateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (organization is null)
            return Result.Failure(DomainErrors.Organization.NotFound(request.Id));

        if (organization.RegistrationStatus != OrganizationStatus.PendingValidation)
            return Result.Failure(DomainErrors.Organization.InvalidStatus(organization.RegistrationStatus));

        organization.RegistrationStatus = OrganizationStatus.PendingArCheck;
        organization.ValidatedAt = DateTime.UtcNow;
        organization.ValidatedBy = currentUserService.Email;
        organization.ReviewNotes = request.Notes ?? organization.ReviewNotes;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class CompleteOrganizationArCheckCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<CompleteOrganizationArCheckCommand>
{
    public async Task<Result> Handle(CompleteOrganizationArCheckCommand request, CancellationToken cancellationToken)
    {
        var organization = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (organization is null)
            return Result.Failure(DomainErrors.Organization.NotFound(request.Id));

        // El Match Code se asigna solo tras la validación del cliente (M8-04).
        if (organization.RegistrationStatus != OrganizationStatus.PendingArCheck)
            return Result.Failure(DomainErrors.Organization.InvalidStatus(organization.RegistrationStatus));

        var matchCode = request.MatchCode.Trim().ToUpperInvariant();

        var matchCodeTaken = await dbContext.Clients
            .AnyAsync(c => c.MatchCode == matchCode && c.Id != organization.Id, cancellationToken);

        if (matchCodeTaken)
            return Result.Failure(DomainErrors.Organization.MatchCodeExists(matchCode));

        var now = DateTime.UtcNow;

        organization.MatchCode = matchCode;
        organization.ArReference = request.ArReference;
        organization.ArCheckedAt = now;
        organization.ArCheckedBy = currentUserService.Email;
        organization.ApprovedAt = now;
        organization.RegistrationStatus = OrganizationStatus.Approved;
        organization.ReviewNotes = request.Notes ?? organization.ReviewNotes;

        if (request.OperatingCountries is { Count: > 0 })
            organization.OperatingCountries = string.Join(",", request.OperatingCountries.Distinct());

        await dbContext.SaveChangesAsync(cancellationToken);

        await OrganizationNotifier.NotifyAdminsAsync(
            dbContext,
            notificationPublisher,
            organization,
            NotificationTypes.OrganizationApproved,
            "Registro de organización aprobado",
            $"El registro de {organization.Name} fue aprobado (Match Code {matchCode}). Ya puede operar en el portal.",
            cancellationToken,
            link: new NotificationLink(NotificationEntityTypes.Organization, organization.Id.ToString(), organization.Name));

        return Result.Success();
    }
}

public sealed class RejectOrganizationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<RejectOrganizationCommand>
{
    public async Task<Result> Handle(RejectOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (organization is null)
            return Result.Failure(DomainErrors.Organization.NotFound(request.Id));

        if (organization.RegistrationStatus is not (OrganizationStatus.PendingValidation or OrganizationStatus.PendingArCheck))
            return Result.Failure(DomainErrors.Organization.InvalidStatus(organization.RegistrationStatus));

        organization.RegistrationStatus = OrganizationStatus.Rejected;
        organization.RejectedAt = DateTime.UtcNow;
        organization.ReviewNotes = request.Reason;
        organization.ValidatedBy ??= currentUserService.Email;

        await dbContext.SaveChangesAsync(cancellationToken);

        await OrganizationNotifier.NotifyAdminsAsync(
            dbContext,
            notificationPublisher,
            organization,
            NotificationTypes.OrganizationRejected,
            "Registro de organización rechazado",
            $"El registro de {organization.Name} fue rechazado. Motivo: {request.Reason}",
            cancellationToken,
            link: new NotificationLink(NotificationEntityTypes.Organization, organization.Id.ToString(), organization.Name));

        return Result.Success();
    }
}
