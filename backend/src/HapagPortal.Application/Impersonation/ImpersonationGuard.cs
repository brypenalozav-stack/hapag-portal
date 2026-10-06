namespace HapagPortal.Application.Impersonation;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Resultado del control de una solicitud hecha con un token de impersonación.</summary>
public sealed record ImpersonationDecision(bool Allowed, Error? Error, bool SessionEnded)
{
    public static readonly ImpersonationDecision Allow = new(true, null, false);
}

/// <summary>
/// Control en el servidor de cada solicitud de una «Vista como cliente» (M8-08), antes de llegar al controlador: la
/// sesión debe seguir activa y sin vencer (vence sola), y las escrituras se bloquean salvo las permitidas
/// (<see cref="ImpersonationSettings.AllowedActions"/>, vacío = solo consulta). Cada solicitud, permitida o bloqueada,
/// queda en <c>AuditLogs</c> con la identidad del actor interno. Debe usarse con su propio contexto de datos (un ámbito
/// aparte), para no confirmar cambios pendientes de la solicitud.
/// </summary>
public sealed class ImpersonationGuard(IApplicationDbContext dbContext, ImpersonationSettings settings)
{
    public async Task<ImpersonationDecision> CheckAsync(Guid sessionId, string method, string path, CancellationToken cancellationToken)
    {
        var session = await dbContext.ImpersonationSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
            return new ImpersonationDecision(false, DomainErrors.Impersonation.Ended, true);

        var now = DateTime.UtcNow;
        if (session.Status == ImpersonationStatus.Active && session.ExpiresAt <= now)
        {
            ImpersonationSessions.End(dbContext, session, ImpersonationEndReasons.Expired, now);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (session.Status != ImpersonationStatus.Active)
            return new ImpersonationDecision(false, DomainErrors.Impersonation.Ended, true);

        return ImpersonationPolicy.IsAllowed(method, path, settings.AllowedActions)
            ? ImpersonationDecision.Allow
            : new ImpersonationDecision(false, DomainErrors.Impersonation.ReadOnly, false);
    }

    /// <summary>Registra la solicitud (método, ruta y resultado) bajo la identidad del actor de la sesión.</summary>
    public async Task RecordAsync(
        Guid sessionId,
        string method,
        string path,
        int statusCode,
        bool blocked,
        string? sourceAddress,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.ImpersonationSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
            return;

        if (blocked)
            session.BlockedCount++;
        else
            session.RequestCount++;
        session.SourceAddress ??= sourceAddress;

        ImpersonationSessions.Audit(dbContext, session, blocked ? ImpersonationAuditActions.BlockedWrite : ImpersonationAuditActions.Request,
            DateTime.UtcNow, new { method, path, statusCode });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
