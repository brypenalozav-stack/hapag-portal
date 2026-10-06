using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Moneda de pago habilitada o deshabilitada para un concepto en un país (M5-04). Sin filas para el par
/// concepto × país rige la moneda del cargo y la local; las reglas de Finanzas de M5-08 se aplican siempre.
/// </summary>
public sealed class PaymentCurrencyRule : BaseAuditableEntity
{
    public required string Country { get; set; }
    public required string ConceptCode { get; set; }
    public required string Currency { get; set; }
    public bool IsEnabled { get; set; } = true;
}
