namespace HapagPortal.Domain.Constants;

public static class UserTypes
{
    public const string Client = "Client";
    public const string Agent = "Agent";
    public const string CustomsAgent = "CustomsAgent";

    /// <summary>
    /// Usuario técnico de un cliente del canal Web Service (M3-17): representa a la organización en las solicitudes
    /// del canal y no inicia sesión en el portal (sin contraseña utilizable ni recuperación).
    /// </summary>
    public const string Technical = "Technical";
}
