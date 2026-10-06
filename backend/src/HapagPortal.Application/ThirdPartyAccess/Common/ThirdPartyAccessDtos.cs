namespace HapagPortal.Application.ThirdPartyAccess.Common;

/// <summary>Organización referida en un acceso (otorgante, tercero o actor).</summary>
public sealed record OrganizationRefDto(Guid Id, string Name, string OrganizationType, string Country);

/// <summary>Organización que puede recibir un acceso (buscador del flujo de otorgamiento, M1-24).</summary>
public sealed record GranteeOrganizationDto(Guid Id, string Name, string TaxId, string OrganizationType, string Country);

/// <summary>
/// Fila de la vista única de accesos (M1-24): destinatario, vigencia y permisos, editables en el lugar.
/// <see cref="ActionCodes"/> nulo = sin permisos explícitos (nivel base de M1-11);
/// <see cref="EffectiveActionCodes"/> es lo que el acceso habilita hoy, bajo el techo del otorgante.
/// <see cref="Status"/> refleja la vigencia aunque el vencimiento aún no se haya registrado.
/// </summary>
public sealed record AccessGrantDto(
    Guid Id,
    string Direction,
    OrganizationRefDto Grantor,
    OrganizationRefDto Grantee,
    string GrantorRole,
    Guid? BillOfLadingId,
    string? BlNumber,
    string? BookingNumber,
    string GrantType,
    string? IntendedRole,
    bool HasExplicitPermissions,
    IReadOnlyList<string>? ActionCodes,
    IReadOnlyList<string> EffectiveActionCodes,
    string ValidityType,
    DateTime ValidFrom,
    DateTime? ValidTo,
    int? DurationDays,
    string Status,
    bool IsEffective,
    bool IsMandate,
    string? TermsVersion,
    DateTime? TermsAcceptedAt,
    Guid? ParentGrantId,
    Guid? DefaultGranteeId,
    DateTime CreatedAt,
    DateTime? EndedAt,
    string? EndReason,
    bool CanEdit);

/// <summary>Referencia (BL o booking) que no recibió el acceso en un otorgamiento masivo, y por qué.</summary>
public sealed record GrantSkippedDto(string Reference, string Code, string Message);

/// <summary>
/// Resultado del otorgamiento individual o masivo (M1-12): <see cref="Updated"/> es la cantidad de
/// registros actualizados (creados + modificados), que el portal confirma al usuario.
/// </summary>
public sealed record GrantAccessResultDto(
    int Requested,
    int Updated,
    int Created,
    int Modified,
    IReadOnlyList<GrantSkippedDto> Skipped,
    IReadOnlyList<AccessGrantDto> Grants);

/// <summary>Resultado de una revocación, con lo revocado en cadena (M1-22).</summary>
public sealed record RevokeAccessResultDto(int Revoked, int CascadeRevoked, int WideningsRevoked);

/// <summary>Acción que puede incluirse en un acceso, para el selector de permisos del flujo único.</summary>
public sealed record GrantableActionDto(
    string Code,
    string Name,
    string Category,
    string Kind,
    bool GrantorHas,
    bool Grantable,
    bool IncludedInBaseLevel);

/// <summary>Tercero configurado por defecto (M1-13).</summary>
public sealed record DefaultGranteeDto(
    Guid Id,
    OrganizationRefDto Grantee,
    IReadOnlyList<string>? ActionCodes,
    int? DurationDays,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

/// <summary>Configuración de acceso abierto por número de BL (M1-17) y su único conjunto de permisos.</summary>
public sealed record OpenAccessSettingDto(
    bool IsEnabled,
    IReadOnlyList<string>? ActionCodes,
    IReadOnlyList<string> EffectiveActionCodes,
    bool CanManage,
    DateTime? ChangedAt);

/// <summary>Autoasociación a un BL consultado con acceso abierto (M1-18).</summary>
public sealed record ShipmentAssociationDto(Guid BillOfLadingId, string BlNumber, DateTime AssociatedAt);

/// <summary>Ampliación de un dato del BL hacia otro rol (M1-16).</summary>
public sealed record VisibilityWideningDto(
    Guid Id,
    Guid BillOfLadingId,
    string BlNumber,
    OrganizationRefDto Grantor,
    string GrantorRole,
    string TargetRole,
    string ActionCode,
    Guid? OriginGrantId,
    string Status,
    DateTime CreatedAt,
    DateTime? EndedAt,
    string? EndReason,
    bool CanRevoke);

/// <summary>Entrada de la auditoría de accesos (M1-23).</summary>
public sealed record AccessAuditEntryDto(
    Guid Id,
    DateTime OccurredAt,
    string EventType,
    Guid? BillOfLadingId,
    string? BlNumber,
    string? BookingNumber,
    Guid? AccessGrantId,
    Guid? VisibilityWideningId,
    OrganizationRefDto? Grantor,
    OrganizationRefDto? Grantee,
    Guid? ActorUserId,
    string? ActorEmail,
    OrganizationRefDto? ActorOrganization,
    string? Details);

/// <summary>Términos vigentes del mandato digital (M1-03).</summary>
public sealed record MandateTermsDto(string Version, string Title, string Summary);

public static class AccessGrantDirections
{
    public const string Given = "Given";
    public const string Received = "Received";

    public static readonly string[] All = [Given, Received];
}
