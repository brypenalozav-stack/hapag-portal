namespace HapagPortal.Application.Documents.Transshipment;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Solicitud del certificado de transbordo (M6-01, CL-EXP-08, CL-IMP-07): crea, con la tarifa del mantenedor
/// (M8-01), el cargo del servicio listo para el carro (M5-01). Es idempotente: un cargo pendiente o pagado
/// del BL se devuelve tal cual. Confirmado el pago, la cola posterior (NF-03) genera el certificado firmado,
/// lo publica en el repositorio y lo envía al cliente o a UMAR.
/// </summary>
public sealed record RequestTransshipmentCertificateCommand(string BlNumber) : ICommand<TransshipmentRequestDto>;

public sealed class RequestTransshipmentCertificateCommandValidator : AbstractValidator<RequestTransshipmentCertificateCommand>
{
    public RequestTransshipmentCertificateCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class RequestTransshipmentCertificateCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ITariffResolver tariffResolver)
    : ICommandHandler<RequestTransshipmentCertificateCommand, TransshipmentRequestDto>
{
    public async Task<Result<TransshipmentRequestDto>> Handle(RequestTransshipmentCertificateCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: true, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<TransshipmentRequestDto>.Failure(loaded.Error);

        var (bl, permissions, _, _) = loaded.Value;

        if (bl.Country != CountryCodes.Chile)
            return Result<TransshipmentRequestDto>.Failure(
                DomainErrors.ShipmentDocument.NotAvailable(ShipmentDocumentTypes.TransshipmentCertificate, bl.Country));
        if (!permissions.CanExecute(ShipmentActionCodes.GenerateTransshipmentCertificate))
            return Result<TransshipmentRequestDto>.Failure(Error.Forbidden);

        var charge = await dbContext.LocalCharges.AsNoTracking()
            .Where(c => c.BillOfLadingId == bl.Id
                && c.ChargeType == ChargeConceptCodes.TransshipmentCertificate
                && (c.Status == ChargeStatus.Pending || c.Status == ChargeStatus.Paid))
            .OrderByDescending(c => c.Status == ChargeStatus.Pending)
            .FirstOrDefaultAsync(cancellationToken);

        if (charge is null)
        {
            var today = BusinessCalendar.LocalDate(bl.Country, DateTime.UtcNow);
            var tariff = (await tariffResolver.GetInForceAsync(
                    new TariffLookup(bl.Country, ChargeConceptCodes.TransshipmentCertificate, today), cancellationToken))
                .FirstOrDefault();
            if (tariff is null)
                return Result<TransshipmentRequestDto>.Failure(
                    DomainErrors.Tariff.NotInForce(ChargeConceptCodes.TransshipmentCertificate, bl.Country));

            var amount = TariffCalculator.Compute(tariff.Amount, tariff.TierMode, tariff.Tiers, 1).Amount;
            var taxRate = await dbContext.TaxConfigurations.AsNoTracking()
                .Where(t => t.Country == bl.Country && t.IsActive)
                .Select(t => (decimal?)t.TaxRate)
                .FirstOrDefaultAsync(cancellationToken) ?? 0m;
            var tax = MoneyRounding.Round(amount * taxRate / 100m, tariff.Currency);

            charge = new LocalCharge
            {
                BillOfLadingId = bl.Id,
                ChargeType = ChargeConceptCodes.TransshipmentCertificate,
                Description = "Certificado de transbordo",
                Amount = amount,
                Currency = tariff.Currency,
                Status = ChargeStatus.Pending,
                IsTaxable = taxRate > 0m,
                TaxRate = taxRate,
                TaxAmount = tax,
                TotalAmount = amount + tax
            };
            dbContext.LocalCharges.Add(charge);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var chargeId = charge.Id;
        var document = await dbContext.ShipmentDocuments.AsNoTracking()
            .Where(d => d.BillOfLadingId == bl.Id && d.DocumentType == ShipmentDocumentTypes.TransshipmentCertificate)
            .OrderByDescending(d => d.IssuedAt)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<TransshipmentRequestDto>.Success(new TransshipmentRequestDto(
            chargeId,
            bl.Id,
            bl.BLNumber,
            charge.ChargeType,
            charge.Amount,
            charge.TaxAmount,
            charge.TotalAmount,
            charge.Currency,
            charge.Status,
            charge.Status == ChargeStatus.Paid ? document : null));
    }
}
