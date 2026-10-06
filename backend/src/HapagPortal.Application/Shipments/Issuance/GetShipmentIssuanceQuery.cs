namespace HapagPortal.Application.Shipments.Issuance;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.Shipments;

/// <summary>
/// Consulta del estado de emisión del BL (M2-02, CL-IMP-14, BO-IMP-14): SWB, EBL (incluido Wave BL) y BL con
/// emisión autorizada en destino, tal como lo registra el origen. Requiere <c>bl-issuance.view</c> (M1-11).
/// </summary>
public sealed record GetShipmentIssuanceQuery(string BlNumber) : IQuery<ShipmentIssuanceDto>;

public sealed class GetShipmentIssuanceQueryValidator : AbstractValidator<GetShipmentIssuanceQuery>
{
    public GetShipmentIssuanceQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

/// <summary>
/// Lee el estado de emisión del origen (CT-FIS) en el momento de la consulta. Si el origen no responde, lo
/// informa (NF-11) con el último estado conocido aparte, sin presentarlo como vigente.
/// </summary>
public sealed class ShipmentIssuanceReader(IShipmentSource shipmentSource)
{
    public const string SourceSystem = "FIS";

    public async Task<ShipmentIssuanceDto> ReadAsync(BillOfLading billOfLading, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var operation = ShipmentOperations.Normalize(billOfLading.ShipmentType);
        var result = await shipmentSource.GetByBlNumberAsync(billOfLading.BLNumber, cancellationToken);

        if (result.IsFailure)
        {
            return new ShipmentIssuanceDto(
                billOfLading.BLNumber, billOfLading.Country, operation, Available: false, SourceSystem,
                null, null, null, null, null, null, now, result.Error.Code, Summary(billOfLading));
        }

        var record = result.Value;
        return new ShipmentIssuanceDto(
            billOfLading.BLNumber,
            billOfLading.Country,
            operation,
            Available: true,
            SourceSystem,
            BlIssuanceMapper.MapDocumentType(record?.DocumentType),
            record?.EblPlatform,
            BlIssuanceMapper.MapStatus(record?.IssuanceStatus),
            record?.IssuanceStatus,
            record?.IssuanceStatusAt,
            record?.IssuancePlace,
            now,
            ErrorCode: null,
            LastKnown: null);
    }

    /// <summary>Último estado conocido guardado con el BL (listado); nulo si nunca se informó.</summary>
    public static ShipmentIssuanceSummaryDto? Summary(BillOfLading billOfLading) =>
        billOfLading.TransportDocumentType is null && billOfLading.IssuanceStatus is null
            ? null
            : new ShipmentIssuanceSummaryDto(
                billOfLading.TransportDocumentType,
                billOfLading.EblPlatform,
                billOfLading.IssuanceStatus,
                billOfLading.IssuanceStatusAt);
}

public sealed class GetShipmentIssuanceQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ShipmentIssuanceReader issuanceReader)
    : IQueryHandler<GetShipmentIssuanceQuery, ShipmentIssuanceDto>
{
    public async Task<Result<ShipmentIssuanceDto>> Handle(GetShipmentIssuanceQuery request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: false, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<ShipmentIssuanceDto>.Failure(loaded.Error);

        var (bl, permissions, _, _) = loaded.Value;
        if (!permissions.Can(ShipmentActionCodes.ViewBlIssuance))
            return Result<ShipmentIssuanceDto>.Failure(Error.Forbidden);

        return Result<ShipmentIssuanceDto>.Success(await issuanceReader.ReadAsync(bl, cancellationToken));
    }
}
