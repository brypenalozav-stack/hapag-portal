namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Estado de la carta de responsabilidad de una organización sobre un BL (M4-04): <c>Fulfilled</c> si
/// existe una carta vigente emitida para ella en el repositorio documental (M6-06); si no, <c>Missing</c>
/// y el proceso de un FFWW queda bloqueado.
/// </summary>
public interface IResponsibilityLetterStatus
{
    Task<string> GetStatusAsync(Guid billOfLadingId, Guid organizationId, CancellationToken cancellationToken = default);
}
