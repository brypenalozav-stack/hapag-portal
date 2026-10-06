using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Paso posterior a la confirmación de un pago (liberación, aviso) en una cola recuperable (NF-03): si
/// falla, el pago sigue confirmado y el paso se reintenta con espera creciente hasta quedar detenido
/// (<c>Stuck</c>) para resolución interna.
/// </summary>
public sealed class PaymentOutboxMessage : GuidEntity
{
    public Guid PaymentId { get; set; }
    public required string JobType { get; set; }
    public required string Status { get; set; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Payment Payment { get; set; } = null!;
}
