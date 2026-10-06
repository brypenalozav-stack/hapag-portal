namespace HapagPortal.Application.ThirdPartyAccess.Common;

using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Results;

/// <summary>
/// Usuario, organización aprobada, alcance de accesos y matriz vigente para administrar accesos a
/// terceros. Todas las operaciones quedan acotadas a la organización del usuario (NF-05).
/// </summary>
public sealed record AccessManagementContext(
    OrganizationMembership Membership,
    AccessScope Scope,
    AccessMatrixSnapshot Matrix)
{
    public Guid OrganizationId => Membership.Organization.Id;

    public string OrganizationType => Membership.Organization.OrganizationType;

    public AccessActor Actor => new(Membership.User.Id, Membership.User.Email, Membership.Organization.Id);

    /// <summary>
    /// Carga el contexto. Con <paramref name="requireOperate"/> exige además un perfil que opere
    /// (M1-02): consultar la vista de accesos no lo requiere, cambiarlos sí.
    /// </summary>
    public static async Task<Result<AccessManagementContext>> LoadAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IShipmentAccessEvaluator accessEvaluator,
        bool requireOperate,
        CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(
            dbContext, currentUserService, requireApproved: true, cancellationToken);

        if (membership.IsFailure)
            return Result<AccessManagementContext>.Failure(membership.Error);

        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (!scope.IsOperational || scope.IsAdmin || (requireOperate && !scope.CanOperate))
            return Result<AccessManagementContext>.Failure(Error.Forbidden);

        var matrix = await accessEvaluator.GetMatrixAsync(cancellationToken);

        return Result<AccessManagementContext>.Success(new AccessManagementContext(membership.Value, scope, matrix));
    }
}
