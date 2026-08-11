namespace HapagPortal.Application.Users.Create;

using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Users.Common;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class CreateUserCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher)
    : ICommandHandler<CreateUserCommand, UserListItemDto>
{
    public async Task<Result<UserListItemDto>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        var emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailExists)
            return Result<UserListItemDto>.Failure(
                new Error("User.EmailExists", $"A user with email '{email}' already exists."));

        var role = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Code == request.RoleCode, cancellationToken);

        if (role is null)
            return Result<UserListItemDto>.Failure(
                new Error("Role.NotFound", $"Role '{request.RoleCode}' was not found."));

        var displayId = request.DisplayId
            ?? (await dbContext.Users.MaxAsync(u => (int?)u.DisplayId, cancellationToken) ?? 0) + 1;

        // Se crea con una contraseña aleatoria; el usuario la establece luego vía correo
        // (token de confirmación). El portal nunca almacena la contraseña en claro.
        var tempPassword = Guid.NewGuid().ToString("N") + "Aa1!";

        var user = new User
        {
            Username = email,
            Email = email,
            PasswordHash = passwordHasher.Hash(tempPassword),
            UserType = role.Code,
            Country = request.Country ?? "CL",
            DisplayId = displayId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            IsActive = true,
            EmailConfirmationToken = Guid.NewGuid().ToString(),
            EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(48)
        };

        dbContext.Users.Add(user);
        dbContext.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleName = role.Code,
            RoleId = role.Id
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UserListItemDto>.Success(new UserListItemDto(
            user.Id,
            user.DisplayId,
            $"{user.FirstName} {user.LastName}",
            user.Email,
            user.Phone,
            user.IsActive,
            [role.Code]));
    }
}
