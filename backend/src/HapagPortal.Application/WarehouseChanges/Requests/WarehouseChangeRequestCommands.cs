namespace HapagPortal.Application.WarehouseChanges.Requests;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.WarehouseChanges.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

/// <summary>
/// Cotiza el cambio de almacén de un BL para la organización del usuario: si tiene derecho a cambio
/// gratuito (M3-04) y, si no, las tarifas vigentes del mantenedor (M8-01).
/// </summary>
public sealed record GetWarehouseChangeQuoteQuery(string BlNumber, string? ContainerNumber = null)
    : IQuery<WarehouseChangeQuoteDto>;

/// <summary>
/// Solicitud individual de cambio de almacén. Con derecho a cambio gratuito se completa sin cobro y sin
/// Customer Service (M3-04); si no, queda pendiente de pago con la tarifa vigente (por defecto KTE).
/// </summary>
public sealed record RequestWarehouseChangeCommand(
    string BlNumber,
    string? ContainerNumber,
    string? FromWarehouse,
    string ToWarehouse,
    string? TariffCode = null) : ICommand<WarehouseChangeDetailDto>;

public sealed class GetWarehouseChangeQuoteQueryValidator : AbstractValidator<GetWarehouseChangeQuoteQuery>
{
    public GetWarehouseChangeQuoteQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ContainerNumber).MaximumLength(20);
    }
}

public sealed class RequestWarehouseChangeCommandValidator : AbstractValidator<RequestWarehouseChangeCommand>
{
    public RequestWarehouseChangeCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ContainerNumber).MaximumLength(20);
        RuleFor(x => x.FromWarehouse).MaximumLength(100);
        RuleFor(x => x.ToWarehouse).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TariffCode).MaximumLength(20);
    }
}

public sealed class GetWarehouseChangeQuoteQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    WarehouseChangeService warehouseChangeService)
    : IQueryHandler<GetWarehouseChangeQuoteQuery, WarehouseChangeQuoteDto>
{
    public async Task<Result<WarehouseChangeQuoteDto>> Handle(GetWarehouseChangeQuoteQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: false, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<WarehouseChangeQuoteDto>.Failure(loaded.Error);

        var context = loaded.Value;
        if (!context.Permissions.Can(ShipmentActionCodes.RequestWarehouseChange))
            return Result<WarehouseChangeQuoteDto>.Failure(Error.Forbidden);

        var quote = await warehouseChangeService.QuoteAsync(context.BillOfLading, context.Payer, request.ContainerNumber, cancellationToken);
        if (quote.IsFailure || context.Permissions.CanExecute(ShipmentActionCodes.RequestWarehouseChange))
            return quote;

        return Result<WarehouseChangeQuoteDto>.Success(quote.Value with
        {
            CanRequest = false,
            BlockedReason = ChargeActionBlockReasons.NoPermission
        });
    }
}

public sealed class RequestWarehouseChangeCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    WarehouseChangeService warehouseChangeService)
    : ICommandHandler<RequestWarehouseChangeCommand, WarehouseChangeDetailDto>
{
    public async Task<Result<WarehouseChangeDetailDto>> Handle(RequestWarehouseChangeCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: true, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<WarehouseChangeDetailDto>.Failure(loaded.Error);

        var context = loaded.Value;
        if (!context.Permissions.CanExecute(ShipmentActionCodes.RequestWarehouseChange))
            return Result<WarehouseChangeDetailDto>.Failure(Error.Forbidden);

        var created = await warehouseChangeService.CreateAsync(
            new WarehouseChangeInput(
                context.BillOfLading,
                context.Payer,
                currentUserService.UserId,
                request.ContainerNumber,
                request.FromWarehouse,
                request.ToWarehouse,
                request.TariffCode,
                null),
            cancellationToken);

        if (created.IsFailure)
            return Result<WarehouseChangeDetailDto>.Failure(created.Error);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<WarehouseChangeDetailDto>.Success(
            WarehouseChangeService.ToDetail(created.Value, context.BillOfLading.BLNumber));
    }
}
