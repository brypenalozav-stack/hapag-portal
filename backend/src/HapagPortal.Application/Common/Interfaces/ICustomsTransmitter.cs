namespace HapagPortal.Application.Common.Interfaces;

/// <summary>Datos a transmitir a Aduana en una etapa concreta.</summary>
public sealed record CustomsTransmissionRequest(
    string Stage,        // Header | BillOfLading
    string Kind,         // Original | Amendment
    string ReferenceKey, // IMO+voyage o número de BL, para trazabilidad
    string Payload);     // cuerpo (XML) — mock por ahora

/// <summary>Acuse del servidor de Aduana ante una transmisión.</summary>
public sealed record CustomsTransmissionResult(
    bool Accepted,
    string ResponseCode,
    string ResponseMessage,
    string? Reference);

/// <summary>
/// Abstracción del canal de transmisión a Aduana (SIDEMAR/SMS vía XML/SOAP en producción;
/// stub simulado en esta fase). Ver Fase 3 del plan.
/// </summary>
public interface ICustomsTransmitter
{
    Task<CustomsTransmissionResult> TransmitAsync(
        CustomsTransmissionRequest request,
        CancellationToken cancellationToken = default);
}
