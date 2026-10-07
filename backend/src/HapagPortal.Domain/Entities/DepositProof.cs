using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Comprobante del depósito bancario que el cliente adjunta a un pago con boleta (M5-06): archivo guardado por
/// el puerto de almacenamiento (CT-STORAGE), datos del abono informados por el cliente y la revisión de
/// Finanzas (verificación, que confirma el pago, o rechazo con motivo). Cada envío es una fila: el historial
/// completo queda trazado (NF-14).
/// </summary>
public sealed class DepositProof : GuidEntity
{
    public Guid PaymentId { get; set; }

    /// <summary><c>DepositProofStatus</c>: Submitted, Verified o Rejected.</summary>
    public required string Status { get; set; }

    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }

    /// <summary>Clave del archivo; nula en los datos de demostración (se genera un PDF de muestra).</summary>
    public string? StorageKey { get; set; }
    public string? ContentHash { get; set; }

    /// <summary>Banco, número de operación, fecha y monto del abono informados por el cliente (CT-DEP).</summary>
    public string? BankName { get; set; }
    public string? BankReference { get; set; }
    public DateOnly? DepositDate { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? Notes { get; set; }

    public DateTime UploadedAt { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string UploadedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public string? RejectionReason { get; set; }

    public Payment Payment { get; set; } = null!;
}
