using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

namespace HapagPortal.Domain.Shipments;

/// <summary>Decisión de publicación de un BL (M2-01) con su motivo y la regla aplicada.</summary>
public sealed record PublicationDecision(bool Published, string ReasonCode, Guid? RuleId);

/// <summary>
/// Reglas de publicación por DIFU de destino final (M2-01). Un BL cuyo destino final es distinto del puerto
/// de descarga y coincide con una regla activa del país se publica solo si el origen informa un DIFU
/// asociado a esa localidad. Sin regla aplicable, o con destino final igual al puerto de descarga, se publica.
/// Los códigos UN/LOCODE se guardan normalizados (<see cref="NormalizeCode"/>). La misma condición se
/// traduce a SQL en <c>ShipmentPublicationFilter</c> para filtrar listados.
/// </summary>
public static class ShipmentPublication
{
    public static readonly PublicationDecision PublishedWithoutRule = new(true, ShipmentPublicationReasons.NoRule, null);

    public static PublicationDecision Evaluate(BillOfLading billOfLading, IEnumerable<ShipmentPublicationRule> rules)
    {
        var destination = NormalizeCode(billOfLading.FinalDestinationCode);
        if (destination is null)
            return PublishedWithoutRule;

        var dischargePort = NormalizeCode(billOfLading.PortOfDischargeCode);
        if (destination == dischargePort)
            return new PublicationDecision(true, ShipmentPublicationReasons.SameAsDischarge, null);

        var rule = rules
            .Where(r => r.IsActive
                && r.Country == billOfLading.Country
                && NormalizeCode(r.FinalDestinationCode) == destination
                && (r.DischargePortCode is null || NormalizeCode(r.DischargePortCode) == dischargePort))
            .OrderByDescending(r => r.DischargePortCode is not null)
            .FirstOrDefault();

        if (rule is null)
            return PublishedWithoutRule;

        if (NormalizeCode(billOfLading.DifuCode) is null)
            return new PublicationDecision(false, ShipmentPublicationReasons.DifuMissing, rule.Id);

        return NormalizeCode(billOfLading.DifuLocationCode) == destination
            ? new PublicationDecision(true, ShipmentPublicationReasons.DifuAssociated, rule.Id)
            : new PublicationDecision(false, ShipmentPublicationReasons.DifuOtherLocation, rule.Id);
    }

    /// <summary>Código en mayúsculas y sin espacios alrededor; vacío = nulo.</summary>
    public static string? NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
}
