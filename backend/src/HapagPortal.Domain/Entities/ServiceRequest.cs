using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Solicitud genérica de un servicio on demand sobre un BL o booking: contenedores, datos ingresados
/// (JSON validado contra el formulario de la definición), datos de facturación, tarifa calculada y aceptada
/// al enviarla (tramo vigente a esa fecha, NF-22), estado y línea de tiempo con fecha y actor. El cobro es
/// un cargo local del BL (<see cref="ServiceRequestCharge"/>) que se paga por el carro; la liberación del pago
/// hace avanzar la solicitud.
/// </summary>
public sealed class ServiceRequest : BaseAuditableEntity
{
    public required string RequestNumber { get; set; }
    public Guid DefinitionId { get; set; }
    public required string DefinitionCode { get; set; }

    public Guid OrganizationId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public string? RequestedByEmail { get; set; }

    /// <summary>NF-14: mandante y acceso otorgado bajo el que se solicitó.</summary>
    public Guid? OnBehalfOfClientId { get; set; }
    public Guid? AccessGrantId { get; set; }

    public Guid BillOfLadingId { get; set; }
    public required string BlNumber { get; set; }
    public string? BookingNumber { get; set; }
    public required string Country { get; set; }
    public required string Operation { get; set; }

    /// <summary>Contenedores seleccionados, separados por coma.</summary>
    public string? ContainerNumbers { get; set; }

    /// <summary>Datos ingresados (objeto JSON con las claves del formulario).</summary>
    public string InputValuesJson { get; set; } = "{}";

    // Datos de facturación (M3-07 a M3-14).
    public string? BillingTaxId { get; set; }
    public string? BillingName { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingEmail { get; set; }
    public string? BillingActivity { get; set; }

    // Tarifa calculada al enviar la solicitud.
    public string? ChargeConceptCode { get; set; }
    public Guid? TariffId { get; set; }
    public string? TariffCode { get; set; }
    public string? TariffSource { get; set; }
    public string? TierUnit { get; set; }
    public int? MeasuredUnits { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Timing { get; set; }
    public DateTime? MilestoneAt { get; set; }
    public string? MilestoneSource { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Currency { get; set; }

    /// <summary>Detalle del cálculo (JSON): líneas por contenedor y tramos aplicados.</summary>
    public string? PricingDetailJson { get; set; }
    public DateTime? QuotedAt { get; set; }
    public DateTime? TariffAcceptedAt { get; set; }

    /// <summary>Excepción que dejó el servicio sin cobro (exención de Nexus o unidades excluidas).</summary>
    public bool IsExempt { get; set; }
    public string? ExemptionReference { get; set; }

    public required string Status { get; set; }
    public DateTime StatusChangedAt { get; set; }
    public string? AssignedTeam { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? PaymentId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? ResolutionNotes { get; set; }

    /// <summary>Último número de hito de la línea de tiempo (orden estable entre hitos del mismo instante).</summary>
    public int TimelineSequence { get; set; }

    public ServiceDefinition Definition { get; set; } = null!;
    public ICollection<ServiceRequestEvent> Events { get; set; } = [];
    public ICollection<ServiceRequestAttachment> Attachments { get; set; } = [];
    public ICollection<ServiceRequestCharge> Charges { get; set; } = [];
}
