using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Archivo de una solicitud guardado por el puerto de almacenamiento (CT-STORAGE): respuesta a un campo
/// <c>file</c> del formulario o documento de salida que adjunta el equipo interno (<c>_output</c>).
/// </summary>
public sealed class ServiceRequestAttachment : GuidEntity
{
    public Guid ServiceRequestId { get; set; }
    public required string FieldKey { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string StorageKey { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string UploadedBy { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
}
