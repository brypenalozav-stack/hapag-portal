namespace HapagPortal.Application.BillsOfLading.Import;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.Validation;
using Microsoft.EntityFrameworkCore;

public sealed class ImportBillsOfLadingCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<ImportBillsOfLadingCommand, ImportResult>
{
    public async Task<Result<ImportResult>> Handle(
        ImportBillsOfLadingCommand request,
        CancellationToken cancellationToken)
    {
        var clientIds = await dbContext.Clients.AsNoTracking()
            .Select(c => c.Id).ToListAsync(cancellationToken);
        var existingClients = clientIds.ToHashSet();

        var errors = new List<ImportRowError>();
        var created = 0;

        for (var i = 0; i < request.Rows.Count; i++)
        {
            var row = request.Rows[i];
            var messages = Validate(row, existingClients);

            if (messages.Count > 0)
            {
                errors.Add(new ImportRowError(i, row.BLNumber, messages));
                continue;
            }

            var bl = new BillOfLading
            {
                BLNumber = row.BLNumber,
                ShipmentType = row.ShipmentType,
                Country = row.Country,
                PortOfLoading = row.PortOfLoading,
                PortOfDischarge = row.PortOfDischarge,
                FreightAmount = 0m,
                FreightCurrency = row.FreightCurrency,
                Status = "Active",
                ClientId = row.ClientId
            };
            dbContext.BillsOfLading.Add(bl);

            if (!string.IsNullOrWhiteSpace(row.ConsigneeName))
            {
                dbContext.BLParties.Add(new BLParty
                {
                    BillOfLadingId = bl.Id,
                    Role = "Consignee",
                    Name = row.ConsigneeName!,
                    TaxId = row.ConsigneeTaxId,
                    TaxIdType = row.Country == CountryCodes.Chile ? "RUT" : "NIT",
                    CountryCode = row.Country
                });
            }

            dbContext.BLCargoItems.Add(new BLCargoItem
            {
                BillOfLadingId = bl.Id,
                HsCode = row.HsCode,
                GrossWeight = row.GrossWeight
            });

            if (!string.IsNullOrWhiteSpace(row.ContainerNumber))
            {
                dbContext.BLContainers.Add(new BLContainer
                {
                    BillOfLadingId = bl.Id,
                    ContainerNumber = row.ContainerNumber!,
                    ContainerType = row.ContainerIsoType ?? "GP",
                    IsoTypeCode = row.ContainerIsoType,
                    GrossWeight = row.GrossWeight,
                    Status = "Active"
                });
            }

            created++;
        }

        if (created > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ImportResult>.Success(new ImportResult(created, errors.Count, errors));
    }

    private static List<string> Validate(ImportBillRow row, HashSet<Guid> existingClients)
    {
        var messages = new List<string>();

        if (string.IsNullOrWhiteSpace(row.BLNumber))
            messages.Add("El número de BL es obligatorio.");

        if (row.Country is not (CountryCodes.Chile or CountryCodes.Bolivia))
            messages.Add("País inválido (debe ser CL o BO).");

        if (!existingClients.Contains(row.ClientId))
            messages.Add("El cliente indicado no existe.");

        if (row.Country == CountryCodes.Chile && !string.IsNullOrWhiteSpace(row.ConsigneeTaxId)
            && !RutValidator.IsValid(row.ConsigneeTaxId))
            messages.Add("El RUT del consignatario es inválido.");

        if (string.IsNullOrWhiteSpace(row.HsCode) || row.HsCode!.Length < 6)
            messages.Add("El código HS es obligatorio (mínimo 6 dígitos).");

        foreach (var port in new[] { row.PortOfLoading, row.PortOfDischarge })
        {
            if (!string.IsNullOrWhiteSpace(port) && port!.Length != 5)
                messages.Add($"El puerto '{port}' no tiene formato UN/LOCODE (5 caracteres).");
        }

        return messages;
    }
}
