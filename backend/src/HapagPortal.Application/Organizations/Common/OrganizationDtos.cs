namespace HapagPortal.Application.Organizations.Common;

using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

/// <summary>Organización del usuario y su situación (M1-07, M1-08, M1-02, M1-04).</summary>
public sealed record OrganizationSummaryDto(
    Guid Id,
    string Name,
    string TaxId,
    string TaxIdType,
    string Country,
    string OrganizationType,
    string Status,
    string? MatchCode,
    IReadOnlyList<string> OperatingCountries,
    string MembershipStatus,
    string? Profile,
    bool CanOperate);

/// <summary>Usuario de la propia organización (M1-02).</summary>
public sealed record OrganizationUserDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string FullName,
    string? Phone,
    string? Profile,
    bool IsActive,
    string MembershipStatus,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

/// <summary>Solicitud pendiente de vinculación a la organización (M1-08).</summary>
public sealed record JoinRequestDto(
    Guid UserId,
    string Email,
    string FullName,
    string? Phone,
    DateTime RequestedAt,
    Guid OrganizationId,
    string OrganizationName);

/// <summary>País de operación del usuario (M1-04). Si la organización opera en uno solo, es fijo.</summary>
public sealed record OperatingCountryDto(
    string Country,
    IReadOnlyList<string> AvailableCountries,
    bool CanChange);

/// <summary>Documento de respaldo adjunto al registro (M1-07).</summary>
public sealed record OrganizationDocumentDto(
    Guid Id,
    string DocumentType,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);

public static class OrganizationMapper
{
    /// <summary>Perfil de organización (OrgAdmin/OrgOperator/OrgViewer) entre los roles del usuario.</summary>
    public static string? ProfileOf(IEnumerable<string> roleCodes)
    {
        var roles = roleCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return RoleCodes.OrganizationProfiles.FirstOrDefault(roles.Contains);
    }

    public static IReadOnlyList<string> OperatingCountriesOf(Client client) =>
        CountryCodes.ParseOperatingCountries(client.OperatingCountries, client.Country);

    public static OrganizationSummaryDto ToSummary(
        Client client,
        User user,
        IEnumerable<string> roleCodes,
        bool hasOperatePermission)
    {
        var canOperate = hasOperatePermission
            && client.IsActive
            && client.RegistrationStatus == OrganizationStatus.Approved
            && user.MembershipStatus == MembershipStatus.Active;

        return new OrganizationSummaryDto(
            client.Id,
            client.Name,
            client.TaxId,
            client.TaxIdType,
            client.Country,
            client.OrganizationType,
            client.RegistrationStatus,
            client.MatchCode,
            OperatingCountriesOf(client),
            user.MembershipStatus,
            ProfileOf(roleCodes),
            canOperate);
    }

    public static string FullNameOf(User user)
    {
        var name = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrWhiteSpace(name) ? user.Email : name;
    }

    public static OrganizationDocumentDto ToDto(OrganizationDocument document) =>
        new(document.Id, document.DocumentType, document.FileName, document.ContentType, document.SizeBytes, document.CreatedAt);
}
