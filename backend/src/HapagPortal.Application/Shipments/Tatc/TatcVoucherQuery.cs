namespace HapagPortal.Application.Shipments.Tatc;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using MediatR;

/// <summary>
/// Comprobante PDF de los TATC emitidos de un BL (todos, o solo el contenedor indicado), con código QR (M2-09).
/// Exige la descarga de TATC (<c>tatc.download</c>, validada por la consulta del TATC); un contenedor sin TATC
/// emitido no tiene comprobante. Se genera al momento con el estado vigente del sistema de TATC.
/// </summary>
public sealed record GetTatcVoucherQuery(string BlNumber, string? ContainerNumber = null) : IQuery<DocumentFileDto>;

public sealed class GetTatcVoucherQueryValidator : AbstractValidator<GetTatcVoucherQuery>
{
    public GetTatcVoucherQueryValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ContainerNumber).MaximumLength(20);
    }
}

public sealed class GetTatcVoucherQueryHandler(ISender sender, IPdfDocumentRenderer renderer, DocumentSettings settings)
    : IQueryHandler<GetTatcVoucherQuery, DocumentFileDto>
{
    public async Task<Result<DocumentFileDto>> Handle(GetTatcVoucherQuery request, CancellationToken cancellationToken)
    {
        var tatc = await sender.Send(new GetShipmentTatcQuery(request.BlNumber), cancellationToken);
        if (tatc.IsFailure)
            return Result<DocumentFileDto>.Failure(tatc.Error);

        var detail = await sender.Send(new GetShipmentDetailQuery(request.BlNumber), cancellationToken);
        if (detail.IsFailure)
            return Result<DocumentFileDto>.Failure(detail.Error);

        var container = request.ContainerNumber?.Trim();
        var issued = tatc.Value.Containers
            .Where(c => c.Status == TatcStatuses.Issued && !string.IsNullOrWhiteSpace(c.TatcNumber))
            .Where(c => string.IsNullOrEmpty(container) || string.Equals(c.ContainerNumber, container, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (issued.Count == 0)
            return Result<DocumentFileDto>.Failure(DomainErrors.Tatc.VoucherNotFound);

        var d = detail.Value;
        var types = d.Containers.ToDictionary(c => c.ContainerNumber, c => c.ContainerType, StringComparer.OrdinalIgnoreCase);
        var lines = issued
            .Select(c => new ShipmentDocumentTemplates.TatcVoucherLine(
                c.ContainerNumber, types.GetValueOrDefault(c.ContainerNumber, "-"), c.WarehouseCode, c.TatcNumber!))
            .ToList();

        var number = issued.Count == 1 ? issued[0].TatcNumber! : $"TATC-{d.BlNumber}";
        var pdf = renderer.Render(ShipmentDocumentTemplates.TatcVoucher(
            settings.IssuerFor(d.Country), d.Country, number, issued.Max(c => c.IssuedAt) ?? DateTime.UtcNow,
            d.BlNumber, d.PortOfDischarge, d.Vessel, d.Voyage, d.Consignee, lines));

        var suffix = string.IsNullOrEmpty(container) ? d.BlNumber : $"{d.BlNumber}-{issued[0].ContainerNumber}";
        return Result<DocumentFileDto>.Success(new DocumentFileDto(pdf, "application/pdf", $"comprobante-tatc-{suffix}.pdf"));
    }
}
