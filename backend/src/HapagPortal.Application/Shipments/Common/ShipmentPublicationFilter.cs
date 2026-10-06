namespace HapagPortal.Application.Shipments.Common;

using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Shipments;

/// <summary>
/// Condición de publicación por DIFU (M2-01) como consulta, equivalente a <c>ShipmentPublication.Evaluate</c>:
/// se publica el BL sin destino final, con destino final igual al puerto de descarga, con DIFU asociado al
/// destino final o sin una regla activa del país para ese destino (y su puerto de descarga, si la regla lo
/// fija). Los códigos se guardan normalizados en mayúsculas, por lo que la comparación es exacta.
/// </summary>
public static class ShipmentPublicationFilter
{
    public static IQueryable<BillOfLading> Published(
        IQueryable<BillOfLading> source,
        IQueryable<ShipmentPublicationRule> rules) =>
        source.Where(b =>
            b.FinalDestinationCode == null
            || b.FinalDestinationCode == b.PortOfDischargeCode
            || (b.DifuCode != null && b.DifuLocationCode == b.FinalDestinationCode)
            || !rules.Any(r =>
                r.IsActive
                && r.Country == b.Country
                && r.FinalDestinationCode == b.FinalDestinationCode
                && (r.DischargePortCode == null || r.DischargePortCode == b.PortOfDischargeCode)));

    public static IQueryable<BillOfLading> Unpublished(
        IQueryable<BillOfLading> source,
        IQueryable<ShipmentPublicationRule> rules) =>
        source.Where(b =>
            b.FinalDestinationCode != null
            && (b.PortOfDischargeCode == null || b.FinalDestinationCode != b.PortOfDischargeCode)
            && (b.DifuCode == null || b.DifuLocationCode == null || b.DifuLocationCode != b.FinalDestinationCode)
            && rules.Any(r =>
                r.IsActive
                && r.Country == b.Country
                && r.FinalDestinationCode == b.FinalDestinationCode
                && (r.DischargePortCode == null || r.DischargePortCode == b.PortOfDischargeCode)));
}

/// <summary>Vista de la decisión de publicación para el administrador interno (M2-01).</summary>
public static class ShipmentPublicationView
{
    public static ShipmentPublicationDto From(BillOfLading billOfLading, PublicationDecision decision) => new(
        decision.Published,
        decision.ReasonCode,
        decision.RuleId,
        billOfLading.FinalDestinationCode,
        billOfLading.PortOfDischargeCode,
        billOfLading.DifuCode,
        billOfLading.DifuLocationCode);
}
