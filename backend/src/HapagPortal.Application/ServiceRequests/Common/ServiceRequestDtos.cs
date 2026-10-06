namespace HapagPortal.Application.ServiceRequests.Common;

using HapagPortal.Domain.Charges;
using HapagPortal.Domain.ServiceRequests;

/// <summary>Definición de servicio on demand tal como la administra el área interna (M2-03, M2-04).</summary>
public sealed record ServiceDefinitionDto(
    Guid Id,
    string Code,
    string NameEs,
    string NameEn,
    string? DescriptionEs,
    string? DescriptionEn,
    IReadOnlyList<string> Operations,
    IReadOnlyList<string> Countries,
    string ReferenceType,
    IReadOnlyList<string> RequiredBlStatuses,
    string AvailabilityWindow,
    bool RequiresContainers,
    bool AllowMultiplePerBl,
    IReadOnlyList<ServiceInputField> InputSchema,
    bool BillingDataRequired,
    bool TariffAcceptanceRequired,
    string PricingMode,
    string? ChargeConceptCode,
    string? TariffCode,
    string? LateTariffCode,
    string QuantityMode,
    string? MeasureFieldKey,
    string Milestone,
    int MilestoneOffsetHours,
    string? DeadlineRuleCode,
    string TimingRule,
    bool Taxable,
    string? ExemptionConcept,
    bool ExcludeShipperOwnedContainers,
    string ApprovalTeam,
    string FulfillmentTeam,
    bool RequiresOutputDocument,
    string ActionCode,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

/// <summary>Instantánea de la definición para el registro de cambios (NF-15).</summary>
public sealed record ServiceDefinitionSnapshot(
    string Code,
    string NameEs,
    string NameEn,
    string? DescriptionEs,
    string? DescriptionEn,
    string Operations,
    string Countries,
    string ReferenceType,
    string? RequiredBlStatuses,
    string AvailabilityWindow,
    bool RequiresContainers,
    bool AllowMultiplePerBl,
    string InputSchemaJson,
    bool BillingDataRequired,
    bool TariffAcceptanceRequired,
    string PricingMode,
    string? ChargeConceptCode,
    string? TariffCode,
    string? LateTariffCode,
    string QuantityMode,
    string? MeasureFieldKey,
    string Milestone,
    int MilestoneOffsetHours,
    string? DeadlineRuleCode,
    string TimingRule,
    bool Taxable,
    string? ExemptionConcept,
    bool ExcludeShipperOwnedContainers,
    string ApprovalTeam,
    string FulfillmentTeam,
    bool RequiresOutputDocument,
    string ActionCode,
    int DisplayOrder,
    bool IsActive);

public sealed record ServiceDefinitionChangeDto(
    Guid Id,
    Guid DefinitionId,
    string Action,
    DateTime ChangedAt,
    string ChangedBy,
    Guid? ChangedByUserId,
    ServiceDefinitionSnapshot? Previous,
    ServiceDefinitionSnapshot? Current);

/// <summary>Datos de facturación de la solicitud (M3-07 a M3-14).</summary>
public sealed record ServiceBillingDataDto(
    string? TaxId,
    string? Name,
    string? Address,
    string? Email,
    string? Activity);

/// <summary>Línea del cálculo: por contenedor (o una por solicitud) con los tramos aplicados.</summary>
public sealed record ServiceQuoteLineDto(
    string? ContainerNumber,
    string? ContainerType,
    decimal Amount,
    string? TariffCode,
    IReadOnlyList<TariffBreakdownLine> Breakdown);

/// <summary>Cargo del sistema de origen vinculado (modo SourceCharge) con el resultado de las reglas de Nexus.</summary>
public sealed record ServiceSourceChargeDto(
    Guid ChargeId,
    string ConceptCode,
    string? Description,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string Outcome,
    decimal PayableTotal);

/// <summary>
/// Cobro del servicio al momento de la consulta o del envío: tramo vigente (NF-22), cantidad, dentro o fuera
/// de plazo, exención aplicada y monto con impuesto.
/// </summary>
public sealed record ServiceQuoteDto(
    string PricingMode,
    string? ChargeConceptCode,
    bool RequiresPayment,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? Currency,
    decimal TaxRate,
    int Quantity,
    string? TierUnit,
    int? MeasuredUnits,
    string Timing,
    DateTime? MilestoneAt,
    string? MilestoneSource,
    Guid? TariffId,
    string? TariffCode,
    string? TariffSource,
    bool IsExempt,
    string? ExemptionReference,
    IReadOnlyList<string> ExcludedContainers,
    IReadOnlyList<ServiceQuoteLineDto> Lines,
    IReadOnlyList<ServiceSourceChargeDto> SourceCharges,
    string TimeZone,
    DateTime QuotedAt);

/// <summary>Servicio aplicable a un BL o booking para el usuario, con su disponibilidad y cobro estimado.</summary>
public sealed record AvailableServiceDto(
    Guid DefinitionId,
    string Code,
    string NameEs,
    string NameEn,
    string? DescriptionEs,
    string? DescriptionEn,
    string ReferenceType,
    bool Available,
    bool CanRequest,
    IReadOnlyList<string> UnavailableReasons,
    IReadOnlyList<ServiceInputField> InputSchema,
    bool BillingDataRequired,
    bool TariffAcceptanceRequired,
    string ApprovalTeam,
    string FulfillmentTeam,
    ServiceQuoteDto? Quote,
    string? QuoteErrorCode);

public sealed record ShipmentContainerOptionDto(string ContainerNumber, string ContainerType, string Status, bool IsShipperOwned);

/// <summary>Servicios on demand de un embarque (M2-03, M2-04).</summary>
public sealed record AvailableServicesDto(
    Guid BlId,
    string BlNumber,
    string? BookingNumber,
    string Country,
    string Operation,
    string Status,
    DateTime? Etd,
    DateTime? Eta,
    string TimeZone,
    IReadOnlyList<ShipmentContainerOptionDto> Containers,
    IReadOnlyList<AvailableServiceDto> Services,
    DateTime EvaluatedAt);

public sealed record ServiceRequestEventDto(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    DateTime OccurredAt,
    string ActorName,
    string ActorKind,
    string? Notes);

public sealed record ServiceRequestAttachmentDto(
    Guid Id,
    string FieldKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt,
    string UploadedBy);

/// <summary>Cargo local que cobra la solicitud y cómo agregarlo al carro (<c>POST /cart/items</c>).</summary>
public sealed record ServiceRequestChargeDto(
    Guid ChargeId,
    string ItemType,
    string ConceptCode,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string Status,
    bool Generated);

public sealed record ServiceRequestSummaryDto(
    Guid Id,
    string RequestNumber,
    string DefinitionCode,
    string NameEs,
    string NameEn,
    string BlNumber,
    string? BookingNumber,
    string Country,
    string Operation,
    string Status,
    string? AssignedTeam,
    decimal TotalAmount,
    string? Currency,
    bool IsExempt,
    string? OrganizationName,
    string? RequestedByEmail,
    DateTime CreatedAt,
    DateTime StatusChangedAt);

public sealed record ServiceRequestDetailDto(
    Guid Id,
    string RequestNumber,
    Guid DefinitionId,
    string DefinitionCode,
    string NameEs,
    string NameEn,
    Guid OrganizationId,
    string? OrganizationName,
    string? RequestedByEmail,
    Guid? OnBehalfOfOrganizationId,
    Guid BillOfLadingId,
    string BlNumber,
    string? BookingNumber,
    string Country,
    string Operation,
    string TimeZone,
    IReadOnlyList<string> ContainerNumbers,
    IReadOnlyList<ServiceInputField> InputSchema,
    System.Text.Json.JsonElement InputValues,
    ServiceBillingDataDto Billing,
    bool BillingDataRequired,
    bool TariffAcceptanceRequired,
    ServiceQuoteDto? Quote,
    DateTime? TariffAcceptedAt,
    string Status,
    DateTime StatusChangedAt,
    string? AssignedTeam,
    string ApprovalTeam,
    string FulfillmentTeam,
    bool RequiresOutputDocument,
    string? ResolutionNotes,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? PaidAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    IReadOnlyList<ServiceRequestChargeDto> Charges,
    Guid? PaymentId,
    string? PaymentNumber,
    string? ReceiptNumber,
    bool CanEdit,
    bool CanSubmit,
    bool CanCancel,
    IReadOnlyList<ServiceRequestAttachmentDto> Attachments,
    IReadOnlyList<ServiceRequestEventDto> Timeline);
