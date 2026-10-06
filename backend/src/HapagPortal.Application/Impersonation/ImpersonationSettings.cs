namespace HapagPortal.Application.Impersonation;

/// <summary>
/// Configuración de la «Vista como cliente» (M8-08), sección <c>Impersonation</c>. La sesión es de solo consulta: toda
/// escritura se bloquea en el servidor salvo las rutas de <see cref="AllowedActions"/> (vacío por defecto, a la espera
/// de la definición de Seguridad e IT), con el formato <c>"MÉTODO /api/v1/ruta"</c> (prefijo de la ruta).
/// </summary>
public sealed class ImpersonationSettings
{
    public const string SectionName = "Impersonation";

    public const int MinSessionMinutes = 5;
    public const int MaxSessionMinutes = 120;

    /// <summary>Duración de una sesión; vence sola aunque no se cierre.</summary>
    public int SessionMinutes { get; set; } = 30;

    /// <summary>Escrituras permitidas durante la sesión (<c>"POST /api/v1/..."</c>). Vacío = solo consulta.</summary>
    public string[] AllowedActions { get; set; } = [];

    public TimeSpan SessionLength => TimeSpan.FromMinutes(Math.Clamp(SessionMinutes, MinSessionMinutes, MaxSessionMinutes));
}
