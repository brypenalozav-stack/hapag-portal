using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Transición de estado de un pago con fecha y hora (NF-02), append-only. <see cref="ChangedBy"/> es el
/// usuario (correo), el proveedor (<c>KHIPU_WEBHOOK</c>) o <c>SYSTEM</c> (NF-14).
/// </summary>
public sealed class PaymentStatusChange : GuidEntity
{
    public Guid PaymentId { get; set; }
    public string? FromStatus { get; set; }
    public required string ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public required string ChangedBy { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? Reason { get; set; }

    public Payment Payment { get; set; } = null!;
}
