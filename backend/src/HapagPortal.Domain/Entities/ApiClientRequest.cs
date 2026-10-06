using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Bitácora de cada solicitud recibida por el canal Web Service (M3-17, NF-14): cliente y clave, usuario técnico y
/// organización, operación, clave de idempotencia con la huella de la solicitud, resultado (código HTTP, error) y la
/// entidad del portal creada. La respuesta de una operación de creación se conserva para repetirla ante un reintento
/// con la misma clave de idempotencia. Sus filas del último minuto son la base del límite por cliente. Solo se
/// completa el resultado de la propia fila; no se elimina.
/// </summary>
public sealed class ApiClientRequest : GuidEntity
{
    public Guid ApiClientId { get; set; }
    public Guid? ApiClientKeyId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TechnicalUserId { get; set; }

    /// <summary><c>ApiClientOperations</c>.</summary>
    public required string Operation { get; set; }
    public required string Method { get; set; }
    public required string Path { get; set; }

    public string? IdempotencyKey { get; set; }

    /// <summary>SHA-256 de la operación y su contenido: un reintento con la misma clave debe ser idéntico.</summary>
    public string? RequestHash { get; set; }

    /// <summary><c>ApiClientRequestOutcomes</c>.</summary>
    public required string Outcome { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorCode { get; set; }

    /// <summary>Cuerpo JSON de la respuesta de una operación de creación (para responder igual a un reintento).</summary>
    public string? ResponseJson { get; set; }

    /// <summary>Entidad del portal creada (<c>ApiClientTargetTypes</c>) y su referencia legible.</summary>
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? TargetReference { get; set; }
    public string? BlNumber { get; set; }

    public string? SourceAddress { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMs { get; set; }
}
