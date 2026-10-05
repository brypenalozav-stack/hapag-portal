namespace HapagPortal.Application.Users.Update;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Users.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Phone,
    string? RoleCode) : ICommand<UserListItemDto>;

public sealed class UpdateUserCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<UpdateUserCommand, UserListItemDto>
{
    public async Task<Result<UserListItemDto>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user is null)
            return Result<UserListItemDto>.Failure(DomainErrors.User.NotFound(request.Id));

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;

        if (!string.IsNullOrWhiteSpace(request.RoleCode))
        {
            var role = await dbContext.Roles
                .FirstOrDefaultAsync(r => r.Code == request.RoleCode, cancellationToken);

            if (role is null)
                return Result<UserListItemDto>.Failure(
                    new Error("Role.NotFound", $"Role '{request.RoleCode}' was not found."));

            // M8-06: el perfil con visibilidad total solo se asigna a usuarios internos de Hapag-Lloyd.
            if (RoleCodes.InternalAdministrators.Contains(role.Code) && user.ClientId is not null)
            {
                var isInternal = await dbContext.Clients.AnyAsync(
                    c => c.Id == user.ClientId.Value && c.OrganizationType == OrganizationTypes.Internal,
                    cancellationToken);

                if (!isInternal)
                    return Result<UserListItemDto>.Failure(
                        new Error("User.AdminRoleInternalOnly", "The administrator role can only be assigned to internal users."));
            }

            var userRoles = await dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync(cancellationToken);

            foreach (var ur in userRoles)
                dbContext.UserRoles.Remove(ur);

            dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleName = role.Code,
                RoleId = role.Id
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var roles = await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleName)
            .ToListAsync(cancellationToken);

        return Result<UserListItemDto>.Success(new UserListItemDto(
            user.Id, user.DisplayId, $"{user.FirstName} {user.LastName}",
            user.Email, user.Phone, user.IsActive, roles));
    }
}
