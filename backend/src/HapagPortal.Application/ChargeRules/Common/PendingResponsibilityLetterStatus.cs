namespace HapagPortal.Application.ChargeRules.Common;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;

/// <summary>
/// Estado de la carta de responsabilidad hasta Ola E (M6-06): el portal todavía no genera ni almacena
/// cartas, por lo que para un FFWW la carta siempre figura como faltante y bloquea el proceso (M4-04).
/// Ola E reemplaza este registro por uno que consulte el repositorio documental.
/// </summary>
public sealed class PendingResponsibilityLetterStatus : IResponsibilityLetterStatus
{
    public Task<string> GetStatusAsync(Guid billOfLadingId, Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ProcessRequirementStatus.Missing);
}
