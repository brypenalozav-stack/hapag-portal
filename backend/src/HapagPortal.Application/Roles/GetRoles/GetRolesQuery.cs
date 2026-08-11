namespace HapagPortal.Application.Roles.GetRoles;

using HapagPortal.Application.Common.Messaging;

public sealed record RoleDto(Guid Id, string Code, string Name, bool IsSystem);

public sealed record GetRolesQuery : IQuery<List<RoleDto>>;
