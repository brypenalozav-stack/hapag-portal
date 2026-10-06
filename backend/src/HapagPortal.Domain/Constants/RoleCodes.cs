namespace HapagPortal.Domain.Constants;

/// <summary>
/// Códigos de rol del sistema. Los cuatro primeros son de la consola interna
/// (imagen del cliente); el resto son los de cara al cliente ya existentes.
/// </summary>
public static class RoleCodes
{
    public const string Administrador = "Administrador";
    public const string Coordinador = "Coordinador";
    public const string Supervisor = "Supervisor";
    public const string ExternalApi = "ExternalApi";

    public const string Client = "Client";
    public const string CustomsAgent = "CustomsAgent";
    public const string AdminBA = "AdminBA";
    public const string SuperAdmin = "SuperAdmin";

    // Perfiles de usuario dentro de una organización (M1-02).
    public const string OrgAdmin = "OrgAdmin";
    public const string OrgOperator = "OrgOperator";
    public const string OrgViewer = "OrgViewer";

    /// <summary>Perfiles que el administrador de una organización puede asignar a sus usuarios.</summary>
    public static readonly string[] OrganizationProfiles = [OrgAdmin, OrgOperator, OrgViewer];

    /// <summary>Roles con visibilidad total (M8-06); solo asignables a usuarios internos.</summary>
    public static readonly string[] InternalAdministrators = [Administrador, SuperAdmin, "Admin"];
}
