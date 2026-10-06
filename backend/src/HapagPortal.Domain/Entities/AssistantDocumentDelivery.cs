using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Entrega de un documento por el asistente (M10-04, NF-14): el documento del repositorio (M6-09), recibo o factura
/// que el asistente ofreció al usuario en una respuesta, con el usuario, su organización y el mandante si el permiso
/// venía de un acceso otorgado, y cuándo lo descargó por el enlace de la entrega. El enlace vuelve a validar los
/// permisos al descargar. Solo cambian las marcas de descarga.
/// </summary>
public sealed class AssistantDocumentDelivery : GuidEntity
{
    public Guid SessionId { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OnBehalfOfOrganizationId { get; set; }

    public Guid BillOfLadingId { get; set; }
    public required string BlNumber { get; set; }

    /// <summary><c>AssistantDeliveryKinds</c>: documento del embarque, recibo de pago o factura.</summary>
    public required string DocumentKind { get; set; }

    /// <summary>Tipo del documento del embarque (<c>ShipmentDocumentTypes</c>); nulo para recibos y facturas.</summary>
    public string? DocumentType { get; set; }
    public Guid DocumentId { get; set; }
    public required string DocumentNumber { get; set; }

    public DateTime DeliveredAt { get; set; }
    public DateTime? DownloadedAt { get; set; }
    public int DownloadCount { get; set; }
}
