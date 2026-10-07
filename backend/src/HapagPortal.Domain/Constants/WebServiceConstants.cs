namespace HapagPortal.Domain.Constants;

/// <summary>
/// Alcances que el área interna habilita a un cliente del canal Web Service (M3-17): los procesos que puede gestionar
/// por el canal, en la primera definición la carta de responsabilidad y el cambio de almacén.
/// </summary>
public static class ApiClientScopes
{
    public const string ResponsibilityLetter = "responsibility-letter";
    public const string WarehouseChange = "warehouse-change";

    public static readonly string[] All = [ResponsibilityLetter, WarehouseChange];
}

/// <summary>Estado de un cliente del canal Web Service: activo o revocado (todas sus claves dejan de servir).</summary>
public static class ApiClientStatus
{
    public const string Active = "Active";
    public const string Revoked = "Revoked";
}

/// <summary>Operaciones del canal Web Service registradas en su bitácora (NF-14).</summary>
public static class ApiClientOperations
{
    public const string ResponsibilityLetter = "ResponsibilityLetter";
    public const string ResponsibilityLetterTerms = "ResponsibilityLetterTerms";
    public const string WarehouseChange = "WarehouseChange";
    public const string WarehouseChangeBatch = "WarehouseChangeBatch";
    public const string ListRequests = "ListRequests";
    public const string GetRequest = "GetRequest";
    public const string ClientInfo = "ClientInfo";

    /// <summary>Operaciones que crean una solicitud: exigen clave de idempotencia y se listan como solicitudes.</summary>
    public static readonly string[] Submissions = [ResponsibilityLetter, WarehouseChange, WarehouseChangeBatch];
}

/// <summary>Resultado de una solicitud recibida por el canal Web Service.</summary>
public static class ApiClientRequestOutcomes
{
    /// <summary>La solicitud se está procesando (reservada la clave de idempotencia).</summary>
    public const string Processing = "Processing";

    /// <summary>Aceptada: se creó la solicitud o el documento en el portal.</summary>
    public const string Accepted = "Accepted";

    /// <summary>Rechazada por validación, permisos o reglas del portal (respuesta 4xx).</summary>
    public const string Rejected = "Rejected";

    /// <summary>Falla inesperada del portal (respuesta 5xx); se puede reintentar con la misma clave de idempotencia.</summary>
    public const string Failed = "Failed";
}

/// <summary>Tipo de la entidad del portal creada por una solicitud del canal.</summary>
public static class ApiClientTargetTypes
{
    public const string ShipmentDocument = "ShipmentDocument";
    public const string WarehouseChange = "WarehouseChange";
    public const string WarehouseChangeBatch = "WarehouseChangeBatch";
}

/// <summary>Claims del principal autenticado con una clave del canal Web Service.</summary>
public static class ApiClientClaims
{
    public const string ClientId = "ws_client";
    public const string KeyId = "ws_key";
    public const string Scope = "ws_scope";
    public const string Channel = "channel";
}

/// <summary>Permiso interno para administrar los clientes del canal Web Service y sus claves (NF-09).</summary>
public static class ApiClientPermissions
{
    public const string Manage = "api-clients.manage";
}
