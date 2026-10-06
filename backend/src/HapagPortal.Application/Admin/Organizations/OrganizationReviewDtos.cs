namespace HapagPortal.Application.Admin.Organizations;

using HapagPortal.Application.Organizations.Common;

/// <summary>Fila de la bandeja interna de organizaciones (M8-04, M8-06).</summary>
public sealed record OrganizationReviewItemDto(
    Guid Id,
    string Name,
    string TaxId,
    string TaxIdType,
    string Country,
    string OrganizationType,
    string Status,
    string? MatchCode,
    string Email,
    string? Phone,
    int UserCount,
    DateTime CreatedAt);

/// <summary>Detalle para validar el cliente, registrar el control con AR y asignar el Match Code (M8-04).</summary>
public sealed record OrganizationReviewDetailDto(
    Guid Id,
    string Name,
    string TaxId,
    string TaxIdType,
    string Country,
    string OrganizationType,
    string Status,
    string? MatchCode,
    IReadOnlyList<string> OperatingCountries,
    string Email,
    string? Phone,
    string? Address,
    string? City,
    DateTime CreatedAt,
    DateTime? ValidatedAt,
    string? ValidatedBy,
    DateTime? ArCheckedAt,
    string? ArCheckedBy,
    string? ArReference,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    string? ReviewNotes,
    IReadOnlyList<OrganizationUserDto> Users,
    IReadOnlyList<OrganizationDocumentDto> Documents);
