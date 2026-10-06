using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Mantenedor local heredado de clientes con crédito. No es fuente de verdad (M8-02): la condición de
/// crédito vigente se lee de Nexus por Match Code mediante <c>ICreditConditionReader</c> y ninguna regla
/// de cobro consulta esta tabla. Se conserva solo como referencia histórica hasta retirar su pantalla.
/// </summary>
public sealed class CreditClient : BaseAuditableEntity
{
    public required string Country { get; set; }
    public decimal CreditLimit { get; set; }
    public required string CreditStatus { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;
}
