namespace HapagPortal.Application.Organizations.JoinRequests;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Notifications.Common;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Application.Organizations.Users;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Aprueba una solicitud de vinculación asignando un perfil (M1-08, M1-02) y notifica al solicitante.</summary>
public sealed record ApproveJoinRequestCommand(Guid UserId, string Profile) : ICommand<OrganizationUserDto>;

/// <summary>Rechaza una solicitud de vinculación y notifica al solicitante (M1-08).</summary>
public sealed record RejectJoinRequestCommand(Guid UserId, string? Reason) : ICommand;

public sealed class ApproveJoinRequestCommandValidator : AbstractValidator<ApproveJoinRequestCommand>
{
    public ApproveJoinRequestCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Profile)
            .Must(p => RoleCodes.OrganizationProfiles.Contains(p))
            .WithMessage("Profile must be 'OrgAdmin', 'OrgOperator' or 'OrgViewer'.");
    }
}

public sealed class RejectJoinRequestCommandValidator : AbstractValidator<RejectJoinRequestCommand>
{
    public RejectJoinRequestCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class ApproveJoinRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<ApproveJoinRequestCommand, OrganizationUserDto>
{
    public async Task<Result<OrganizationUserDto>> Handle(
        ApproveJoinRequestCommand request,
        CancellationToken cancellationToken)
    {
        var pending = await JoinRequestLoader.LoadAsync(dbContext, currentUserService, request.UserId, cancellationToken);
        if (pending.IsFailure)
            return Result<OrganizationUserDto>.Failure(pending.Error);

        var (applicant, organization) = pending.Value;

        applicant.MembershipStatus = MembershipStatus.Active;
        applicant.MembershipDecidedAt = DateTime.UtcNow;
        applicant.MembershipDecidedBy = currentUserService.Email;

        await OrganizationProfileAssigner.AssignAsync(dbContext, applicant.Id, request.Profile, cancellationToken);
        await NotificationInbox.ResolveActionAsync(
            dbContext, NotificationActionTypes.ApproveJoinRequest, applicant.Id.ToString(), DateTime.UtcNow, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationPublisher.PublishAsync(
            new NotificationRequest(
                NotificationTypes.JoinRequestApproved,
                "Solicitud de vinculación aprobada",
                $"Su solicitud para operar en nombre de {organization.Name} fue aprobada. Ya puede ingresar al portal.",
                UserId: applicant.Id,
                Email: applicant.Email,
                Link: new NotificationLink(NotificationEntityTypes.Organization, organization.Id.ToString(), organization.Name)),
            cancellationToken);

        return Result<OrganizationUserDto>.Success(OrganizationUserMapper.ToDto(applicant, request.Profile));
    }
}

public sealed class RejectJoinRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    INotificationPublisher notificationPublisher)
    : ICommandHandler<RejectJoinRequestCommand>
{
    public async Task<Result> Handle(RejectJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var pending = await JoinRequestLoader.LoadAsync(dbContext, currentUserService, request.UserId, cancellationToken);
        if (pending.IsFailure)
            return Result.Failure(pending.Error);

        var (applicant, organization) = pending.Value;

        applicant.MembershipStatus = MembershipStatus.Rejected;
        applicant.MembershipDecidedAt = DateTime.UtcNow;
        applicant.MembershipDecidedBy = currentUserService.Email;

        await NotificationInbox.ResolveActionAsync(
            dbContext, NotificationActionTypes.ApproveJoinRequest, applicant.Id.ToString(), DateTime.UtcNow, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : $" Motivo: {request.Reason}";

        await notificationPublisher.PublishAsync(
            new NotificationRequest(
                NotificationTypes.JoinRequestRejected,
                "Solicitud de vinculación rechazada",
                $"Su solicitud para operar en nombre de {organization.Name} fue rechazada.{reason}",
                UserId: applicant.Id,
                Email: applicant.Email,
                Link: new NotificationLink(NotificationEntityTypes.Organization, organization.Id.ToString(), organization.Name)),
            cancellationToken);

        return Result.Success();
    }
}

/// <summary>Carga una solicitud pendiente de la organización del usuario actual; ajena o resuelta = NotFound.</summary>
public static class JoinRequestLoader
{
    public static async Task<Result<OrganizationMembership>> LoadAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        Guid applicantId,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(
            dbContext, currentUserService, requireApproved: true, cancellationToken);

        if (membership.IsFailure)
            return membership;

        var organization = membership.Value.Organization;

        User? applicant = await dbContext.Users.FirstOrDefaultAsync(
            u => u.Id == applicantId
                && u.ClientId == organization.Id
                && u.MembershipStatus == MembershipStatus.Pending,
            cancellationToken);

        return applicant is null
            ? Result<OrganizationMembership>.Failure(DomainErrors.Organization.JoinRequestNotFound(applicantId))
            : Result<OrganizationMembership>.Success(new OrganizationMembership(applicant, organization));
    }
}
