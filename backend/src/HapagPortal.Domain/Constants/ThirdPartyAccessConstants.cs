namespace HapagPortal.Domain.Constants;

/// <summary>
/// Estado de un acceso otorgado a un tercero (M1-12 a M1-22). Solo <see cref="Active"/> habilita,
/// y siempre dentro de su vigencia. <see cref="PendingAcceptance"/> es un mandato sin la aceptación
/// de términos (M1-03); <see cref="Reconciled"/> es un acceso por booking reemplazado por el rol
/// oficial del BL (M1-20).
/// </summary>
public static class AccessGrantStatus
{
    public const string PendingAcceptance = "PendingAcceptance";
    public const string Active = "Active";
    public const string Expired = "Expired";
    public const string Revoked = "Revoked";
    public const string Reconciled = "Reconciled";

    public static readonly string[] All = [PendingAcceptance, Active, Expired, Revoked, Reconciled];

    /// <summary>Estados que todavía pueden habilitar o llegar a habilitar el acceso.</summary>
    public static readonly string[] Open = [PendingAcceptance, Active];
}

/// <summary>Vía por la que se creó el acceso (M1-12, M1-13, M1-20).</summary>
public static class AccessGrantTypes
{
    public const string Individual = "Individual";
    public const string Bulk = "Bulk";
    public const string Default = "Default";
    public const string EarlyBooking = "EarlyBooking";
}

/// <summary>Tipo de vigencia elegido al crear o editar un acceso (M1-14).</summary>
public static class AccessValidityTypes
{
    public const string Indefinite = "Indefinite";
    public const string Duration = "Duration";
    public const string UntilDate = "UntilDate";

    public static readonly string[] All = [Indefinite, Duration, UntilDate];
}

/// <summary>Motivo de término de un acceso o de una ampliación (M1-22, M1-23).</summary>
public static class AccessEndReasons
{
    public const string Manual = "Manual";
    public const string Expired = "Expired";
    public const string Cascade = "Cascade";
    public const string Reconciled = "Reconciled";
}

public static class VisibilityWideningStatus
{
    public const string Active = "Active";
    public const string Revoked = "Revoked";
}

/// <summary>Eventos del registro de auditoría de accesos (M1-23), append-only.</summary>
public static class AccessAuditEvents
{
    public const string GrantCreated = "GrantCreated";
    public const string GrantPermissionsChanged = "GrantPermissionsChanged";
    public const string GrantValidityChanged = "GrantValidityChanged";
    public const string GrantRevoked = "GrantRevoked";
    public const string GrantExpired = "GrantExpired";
    public const string GrantRevokedByCascade = "GrantRevokedByCascade";
    public const string MandateTermsAccepted = "MandateTermsAccepted";
    public const string BookingAccessReconciled = "BookingAccessReconciled";
    public const string BookingAccessLinked = "BookingAccessLinked";
    public const string OpenAccessEnabled = "OpenAccessEnabled";
    public const string OpenAccessDisabled = "OpenAccessDisabled";
    public const string OpenAccessPermissionsChanged = "OpenAccessPermissionsChanged";
    public const string SelfAssociated = "SelfAssociated";
    public const string WideningCreated = "WideningCreated";
    public const string WideningRevoked = "WideningRevoked";
    public const string WideningRevokedByCascade = "WideningRevokedByCascade";
    public const string DefaultGranteeAdded = "DefaultGranteeAdded";
    public const string DefaultGranteeUpdated = "DefaultGranteeUpdated";
    public const string DefaultGranteeRemoved = "DefaultGranteeRemoved";
}

/// <summary>
/// Términos y condiciones del mandato digital (M1-03, NF-06). El mandato no queda activo hasta que
/// el mandante acepta la versión vigente; la versión aceptada queda registrada en el acceso.
/// </summary>
public static class MandateTerms
{
    public const string CurrentVersion = "MANDATO-2026-10";
    public const string Title = "Términos y condiciones del mandato digital";

    public const string Summary =
        "El mandante autoriza al mandatario a operar en su nombre, dentro del portal, únicamente sobre "
        + "los embarques y las acciones indicadas y solo durante la vigencia definida. El mandato no "
        + "transfiere la titularidad del embarque, puede revocarse en cualquier momento y cada operación "
        + "ejecutada bajo él queda registrada con el usuario y la organización mandataria.";
}

/// <summary>
/// Reglas de recepción del acceso anticipado por booking (M1-20, excepción de M1-11): lo recibe el
/// futuro shipper (o consignee); el tercero solo si es un Freight Forwarder.
/// </summary>
public static class EarlyBookingAccess
{
    public static readonly string[] IntendedRoles =
        [ShipmentRoleCodes.Shipper, ShipmentRoleCodes.Consignee, ShipmentRoleCodes.ThirdParty];

    public static bool CanReceive(string intendedRole, string granteeOrganizationType) =>
        intendedRole switch
        {
            ShipmentRoleCodes.Shipper or ShipmentRoleCodes.Consignee => true,
            ShipmentRoleCodes.ThirdParty => granteeOrganizationType == OrganizationTypes.FreightForwarder,
            _ => false
        };
}

/// <summary>Lista de códigos de acción persistida como texto separado por comas.</summary>
public static class ActionCodeList
{
    public static IReadOnlyList<string> Parse(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToList();

    /// <summary>Nulo conserva el significado "sin permisos explícitos" (nivel base de M1-11).</summary>
    public static IReadOnlyList<string>? ParseNullable(string? value) =>
        value is null ? null : Parse(value);

    public static string Format(IEnumerable<string> codes) =>
        string.Join(',', codes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.Ordinal).OrderBy(c => c, StringComparer.Ordinal));
}
