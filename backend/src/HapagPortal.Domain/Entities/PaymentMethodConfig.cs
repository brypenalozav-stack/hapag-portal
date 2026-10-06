using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Medio de pago configurable por país (M5-03): monedas que acepta y proveedor que lo procesa
/// (<c>IPaymentProvider</c> registrado con <see cref="ProviderKey"/>). El depósito bancario no tiene
/// proveedor: lo confirma Finanzas. Incorporar un medio = adaptador + fila en este mantenedor.
/// </summary>
public sealed class PaymentMethodConfig : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Country { get; set; }
    public required string Kind { get; set; }
    public string? ProviderKey { get; set; }

    /// <summary>Monedas aceptadas, separadas por coma (ISO 4217).</summary>
    public string Currencies { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int DisplayOrder { get; set; }
}
