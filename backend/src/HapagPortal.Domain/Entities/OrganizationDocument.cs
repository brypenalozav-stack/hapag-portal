using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>Documentación de respaldo adjunta al registro de la organización (M1-07).</summary>
public sealed class OrganizationDocument : BaseAuditableEntity
{
    public Guid ClientId { get; set; }
    public required string DocumentType { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string StorageKey { get; set; }
    public Guid? UploadedByUserId { get; set; }

    public Client Client { get; set; } = null!;
}
