namespace HapagPortal.Domain.Constants;

/// <summary>
/// Tipo de organización declarado en el registro (M1-07). Determina la columna de la matriz
/// base de M1-11 para agencias de aduanas y transportistas; <see cref="Internal"/> es Hapag-Lloyd.
/// </summary>
public static class OrganizationTypes
{
    public const string Customer = "Customer";
    public const string FreightForwarder = "FreightForwarder";
    public const string CustomsAgency = "CustomsAgency";
    public const string Carrier = "Carrier";
    public const string Internal = "Internal";

    /// <summary>Tipos que una organización puede declarar al registrarse.</summary>
    public static readonly string[] Registrable = [Customer, FreightForwarder, CustomsAgency, Carrier];

    /// <summary>Tipo de organización equivalente al <c>ClientType</c> heredado.</summary>
    public static string FromLegacyClientType(string? clientType) => clientType switch
    {
        "CustomsAgent" or "Agent" => CustomsAgency,
        "Internal" => Internal,
        _ => Customer
    };

    /// <summary><c>ClientType</c> heredado que se mantiene por compatibilidad de contratos.</summary>
    public static string ToLegacyClientType(string organizationType) => organizationType switch
    {
        CustomsAgency => "CustomsAgent",
        Internal => "Internal",
        _ => "Client"
    };
}

/// <summary>
/// Estado del registro de la organización (M1-07 / M8-04). Solo <see cref="Approved"/> opera.
/// PendingValidation -> PendingArCheck -> Approved; cualquiera de los dos primeros puede pasar a Rejected.
/// </summary>
public static class OrganizationStatus
{
    public const string PendingValidation = "PendingValidation";
    public const string PendingArCheck = "PendingArCheck";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly string[] All = [PendingValidation, PendingArCheck, Approved, Rejected];
}

/// <summary>Estado de la vinculación de un usuario a su organización (M1-08).</summary>
public static class MembershipStatus
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Rejected = "Rejected";
}

/// <summary>Tipos de documentación de respaldo adjunta al registro (M1-07).</summary>
public static class OrganizationDocumentTypes
{
    public const string RegistrationLetter = "RegistrationLetter";
    public const string CreditAuthorization = "CreditAuthorization";
    public const string TaxCertificate = "TaxCertificate";
    public const string Other = "Other";

    public static readonly string[] All = [RegistrationLetter, CreditAuthorization, TaxCertificate, Other];
}
