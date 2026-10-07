using HapagPortal.Domain.Common;
using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Entities;

public sealed class Client : BaseAuditableEntity
{
    public required string Name { get; set; }
    public required string TaxId { get; set; }
    public required string TaxIdType { get; set; }
    public required string Country { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public required string ClientType { get; set; }
    public string? AgentCode { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailConfirmed { get; set; }

    // Organización (M1-07 / M8-04). Por defecto pendiente: solo opera tras la aprobación interna.
    public string OrganizationType { get; set; } = OrganizationTypes.Customer;
    public string RegistrationStatus { get; set; } = OrganizationStatus.PendingValidation;
    public string? MatchCode { get; set; }
    // Países en los que opera, separados por coma (M1-04). Vacío = solo <see cref="Country"/>.
    public string OperatingCountries { get; set; } = string.Empty;
    public DateTime? ValidatedAt { get; set; }
    public string? ValidatedBy { get; set; }
    public DateTime? ArCheckedAt { get; set; }
    public string? ArCheckedBy { get; set; }
    public string? ArReference { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public ICollection<BillOfLading> BillsOfLading { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<OrganizationDocument> Documents { get; set; } = [];
}
