using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Shipments;

/// <summary>
/// Estados del TATC (M2-09): traducción del código del sistema de TATC (CT-TATC) por contenedor y estado
/// agregado del BL. Agregado: sin contenedores = <see cref="TatcStatuses.NotRegistered"/>; todos emitidos =
/// <see cref="TatcStatuses.Issued"/>; alguno desconocido = <see cref="TatcStatuses.Unknown"/>; alguno
/// emitido = <see cref="TatcStatuses.PartiallyIssued"/>; alguno con pre-TATC = <see cref="TatcStatuses.PreTatc"/>;
/// todos anulados = <see cref="TatcStatuses.Cancelled"/>; si no, <see cref="TatcStatuses.NotIssued"/>.
/// </summary>
public static class TatcStatusMapper
{
    public static string MapContainer(string? sourceStatus) => sourceStatus?.Trim().ToUpperInvariant() switch
    {
        TatcSourceStatuses.NotIssued => TatcStatuses.NotIssued,
        TatcSourceStatuses.PreTatc => TatcStatuses.PreTatc,
        TatcSourceStatuses.Issued => TatcStatuses.Issued,
        TatcSourceStatuses.Cancelled => TatcStatuses.Cancelled,
        _ => TatcStatuses.Unknown,
    };

    public static string Aggregate(IReadOnlyCollection<string> containerStatuses)
    {
        if (containerStatuses.Count == 0)
            return TatcStatuses.NotRegistered;

        if (containerStatuses.All(s => s == TatcStatuses.Issued))
            return TatcStatuses.Issued;

        if (containerStatuses.Any(s => s == TatcStatuses.Unknown))
            return TatcStatuses.Unknown;

        if (containerStatuses.Any(s => s == TatcStatuses.Issued))
            return TatcStatuses.PartiallyIssued;

        if (containerStatuses.Any(s => s == TatcStatuses.PreTatc))
            return TatcStatuses.PreTatc;

        return containerStatuses.All(s => s == TatcStatuses.Cancelled) ? TatcStatuses.Cancelled : TatcStatuses.NotIssued;
    }
}
