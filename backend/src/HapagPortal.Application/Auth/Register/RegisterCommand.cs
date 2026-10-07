namespace HapagPortal.Application.Auth.Register;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

/// <summary>
/// Registro autónomo de una organización nueva (M1-07). <c>OrganizationType</c> (Customer,
/// FreightForwarder, CustomsAgency, Carrier) prevalece; si no llega se deriva de <c>ClientType</c>.
/// La organización queda pendiente de la validación interna de M8-04 y su primer usuario es OrgAdmin.
/// </summary>
public sealed record RegisterCommand(
    string Name,
    string TaxId,
    string Country,
    string Email,
    string Password,
    string ClientType,
    string? Phone,
    string? AgentCode,
    string? OrganizationType = null,
    string? ContactFirstName = null,
    string? ContactLastName = null) : ICommand<ClientResponseDto>;
