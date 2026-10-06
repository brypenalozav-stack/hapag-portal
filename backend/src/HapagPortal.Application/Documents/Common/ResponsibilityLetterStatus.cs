namespace HapagPortal.Application.Documents.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Carta de responsabilidad en el repositorio documental (M6-06): cumple M4-04 si existe una carta emitida
/// para la organización sobre el BL, no reemplazada ni revocada y vigente. Reemplaza el estado fijo
/// "faltante" de la Ola C, de modo que la carta levanta el bloqueo del FFWW en cargos y carro.
/// </summary>
public sealed class ResponsibilityLetterStatus(IApplicationDbContext dbContext) : IResponsibilityLetterStatus
{
    public async Task<string> GetStatusAsync(Guid billOfLadingId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var valid = await dbContext.ShipmentDocuments.AsNoTracking()
            .AnyAsync(d => d.BillOfLadingId == billOfLadingId
                && d.IssuedForOrganizationId == organizationId
                && d.DocumentType == ShipmentDocumentTypes.ResponsibilityLetter
                && d.Status == ShipmentDocumentStatus.Issued
                && (d.ValidUntil == null || d.ValidUntil > now), cancellationToken);

        return valid ? ProcessRequirementStatus.Fulfilled : ProcessRequirementStatus.Missing;
    }
}
