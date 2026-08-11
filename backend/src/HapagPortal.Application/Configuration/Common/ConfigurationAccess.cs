namespace HapagPortal.Application.Configuration.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

/// <summary>
/// Reglas de acceso por ámbito: Global lo gestiona el administrador del sistema;
/// Client solo puede tocar sus propias credenciales/parámetros.
/// </summary>
internal static class ConfigurationAccess
{
    public static Error? Check(ICurrentUserService user, string scope, Guid? clientId)
    {
        return scope switch
        {
            ConfigurationScopes.Global =>
                user.HasPermission("config.global.manage") ? null : Error.Forbidden,

            ConfigurationScopes.Client when !user.HasPermission("config.client.manage") =>
                Error.Forbidden,

            // Un cliente solo puede operar sobre su propio ClientId.
            ConfigurationScopes.Client when clientId is null || clientId != user.ClientId =>
                Error.Forbidden,

            ConfigurationScopes.Client => null,

            _ => new Error("Configuration.InvalidScope", "Invalid configuration scope.")
        };
    }
}
