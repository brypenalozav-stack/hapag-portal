namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Estado de la carta de responsabilidad de una organización sobre un BL (M4-04). Punto de extensión
/// para Ola E (M6-06), que genera y almacena la carta; hasta entonces la carta figura como faltante
/// (<c>ProcessRequirementStatus.Missing</c>) y el proceso de un FFWW queda bloqueado.
/// </summary>
public interface IResponsibilityLetterStatus
{
    Task<string> GetStatusAsync(Guid billOfLadingId, Guid organizationId, CancellationToken cancellationToken = default);
}
