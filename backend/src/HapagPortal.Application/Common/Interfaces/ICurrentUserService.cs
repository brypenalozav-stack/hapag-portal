namespace HapagPortal.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? ClientId { get; }
    string? Email { get; }
    string? Country { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permission);
    bool IsAuthenticated { get; }

    /// <summary>
    /// Sesión de «Vista como cliente» del token (M8-08): <see cref="UserId"/> es el usuario cliente visto y este es el
    /// identificador de la sesión. Nulo fuera de una impersonación.
    /// </summary>
    Guid? ImpersonationSessionId => null;

    /// <summary>Usuario interno que inició la impersonación: las acciones se auditan con su identidad (M8-08).</summary>
    Guid? ImpersonatorUserId => null;

    bool IsImpersonating => ImpersonationSessionId is not null;

    /// <summary>
    /// Cliente del canal Web Service (M3-17) que autenticó la solicitud con su clave; <see cref="UserId"/> es entonces su
    /// usuario técnico. Nulo para los usuarios del portal.
    /// </summary>
    Guid? ApiClientId => null;

    /// <summary>IP del navegador del usuario (tras el proxy). La exigen algunas pasarelas de pago al crear el cobro.</summary>
    string? IpAddress => null;

    /// <summary>User-Agent del navegador del usuario, para las pasarelas de pago que lo exigen.</summary>
    string? UserAgent => null;
}
