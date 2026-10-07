namespace HapagPortal.Application.ChargeRules.Charges;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

/// <summary>
/// Recargos locales de un BL con las reglas de Nexus aplicadas para la organización del usuario:
/// exenciones (M4-01, M3-01 Gate Out), exclusión del IPO por crédito (M4-03), carta FFWW (M4-04) y
/// montos en moneda local con el tipo de cambio de Nexus (M5-05). Los cargos de la pestaña de
/// demurrage (MHD, demoras anticipadas) se consultan en <c>GetDemurrageStatusQuery</c>.
/// </summary>
public sealed record GetShipmentChargesQuery(string BlNumber) : IQuery<ShipmentChargesDto>;

public sealed class GetShipmentChargesQueryValidator : AbstractValidator<GetShipmentChargesQuery>
{
    public GetShipmentChargesQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class GetShipmentChargesQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IChargeRulesService chargeRulesService)
    : IQueryHandler<GetShipmentChargesQuery, ShipmentChargesDto>
{
    public async Task<Result<ShipmentChargesDto>> Handle(GetShipmentChargesQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: false, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<ShipmentChargesDto>.Failure(loaded.Error);

        var context = loaded.Value;

        if (!context.Permissions.Can(ShipmentActionCodes.PayMandatoryLocalCharges) &&
            !context.Permissions.Can(ShipmentActionCodes.PayOnDemandLocalCharges))
        {
            return Result<ShipmentChargesDto>.Failure(Error.Forbidden);
        }

        var evaluation = await chargeRulesService.EvaluateAsync(
            context.BillOfLading, context.Payer, context.Permissions, cancellationToken);

        return Result<ShipmentChargesDto>.Success(evaluation.Result);
    }
}
