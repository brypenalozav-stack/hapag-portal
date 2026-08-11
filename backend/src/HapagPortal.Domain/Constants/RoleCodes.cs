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
}
