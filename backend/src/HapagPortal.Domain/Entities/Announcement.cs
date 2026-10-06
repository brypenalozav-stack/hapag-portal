using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Comunicado masivo a los clientes del portal (M1-26), administrado desde el área interna (M8-05). Se segmenta por
/// país de operación y por operación (importación, exportación o ambas) y se muestra solo publicado y dentro de su
/// vigencia, con su fecha de publicación. Los cambios quedan en el registro de mantenedores (NF-15).
/// </summary>
public sealed class Announcement : BaseAuditableEntity
{
    public required string TitleEs { get; set; }
    public required string TitleEn { get; set; }
    public required string BodyEs { get; set; }
    public required string BodyEn { get; set; }

    /// <summary>Países de operación a los que va dirigido, separados por coma (CL, BO).</summary>
    public required string Countries { get; set; }

    /// <summary><c>AnnouncementOperations</c>: Import, Export o Both.</summary>
    public required string Operation { get; set; }

    /// <summary><c>AnnouncementSeverities</c>: Info o Important.</summary>
    public required string Severity { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    /// <summary><c>AnnouncementStatus</c>: Draft, Published o Unpublished.</summary>
    public required string Status { get; set; }

    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? UnpublishedAt { get; set; }
    public string? UnpublishedBy { get; set; }

    /// <summary>Al publicar se avisa en la bandeja (M1-25) a los usuarios del segmento.</summary>
    public bool NotifyOnPublish { get; set; }
    public DateTime? NotifiedAt { get; set; }
}
