namespace HapagPortal.Application.Demurrage.State;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

/// <summary>
/// Estado del demurrage de un BL y la acción disponible (M3-18), con MHD (M3-02) y demoras anticipadas
/// de Bolivia (M3-16). Requiere el dato de demurrage de importación de la matriz (M1-11).
/// </summary>
public sealed record GetDemurrageStatusQuery(string BlNumber) : IQuery<DemurrageStatusDto>;

public sealed class GetDemurrageStatusQueryValidator : AbstractValidator<GetDemurrageStatusQuery>
{
    public GetDemurrageStatusQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class GetDemurrageStatusQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    DemurrageStatusBuilder statusBuilder)
    : IQueryHandler<GetDemurrageStatusQuery, DemurrageStatusDto>
{
    public async Task<Result<DemurrageStatusDto>> Handle(GetDemurrageStatusQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: false, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<DemurrageStatusDto>.Failure(loaded.Error);

        var context = loaded.Value;

        if (!context.Permissions.Can(ShipmentActionCodes.PayImportDemurrage))
            return Result<DemurrageStatusDto>.Failure(Error.Forbidden);

        var status = await statusBuilder.BuildAsync(context.BillOfLading, context.Permissions, cancellationToken);
        return Result<DemurrageStatusDto>.Success(status);
    }
}
