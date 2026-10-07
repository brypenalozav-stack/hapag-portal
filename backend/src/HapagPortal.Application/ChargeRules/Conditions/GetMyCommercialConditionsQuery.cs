namespace HapagPortal.Application.ChargeRules.Conditions;

using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Results;

/// <summary>
/// Condición de crédito (M8-02) y de FFWW autorizado (M8-03) de la organización del usuario, leídas de
/// Nexus por RUT y Match Code. Nexus es la fuente: el portal no mantiene un listado paralelo.
/// </summary>
public sealed record GetMyCommercialConditionsQuery : IQuery<CommercialConditionsDto>;

public sealed class GetMyCommercialConditionsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IChargeRulesService chargeRulesService)
    : IQueryHandler<GetMyCommercialConditionsQuery, CommercialConditionsDto>
{
    public async Task<Result<CommercialConditionsDto>> Handle(GetMyCommercialConditionsQuery request, CancellationToken cancellationToken)
    {
        var membership = await CurrentOrganization.LoadAsync(dbContext, currentUserService, requireApproved: true, cancellationToken);
        if (membership.IsFailure)
            return Result<CommercialConditionsDto>.Failure(membership.Error);

        var conditions = await chargeRulesService.GetConditionsAsync(membership.Value.Organization, cancellationToken);
        return Result<CommercialConditionsDto>.Success(conditions);
    }
}
