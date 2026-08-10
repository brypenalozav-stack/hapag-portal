using HapagPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Customs;

/// <summary>
/// Transmisor simulado de Aduana. Reproduce el mecanismo real (acuse Aceptado/Rechazado,
/// folio de referencia) sin construir el XML/SOAP ni firmar: el contrato queda listo para
/// enchufar el canal SIDEMAR/SMS real contra los XSD de aduana.cl. Determinista para tests:
/// un payload que contiene "REJECT" se rechaza; el resto se acepta.
/// </summary>
public sealed class StubCustomsTransmitter(ILogger<StubCustomsTransmitter> logger) : ICustomsTransmitter
{
    public Task<CustomsTransmissionResult> TransmitAsync(
        CustomsTransmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var reject = request.Payload.Contains("REJECT", StringComparison.OrdinalIgnoreCase);

        logger.LogInformation(
            "Transmisión Aduana (stub) - Stage: {Stage}, Kind: {Kind}, Ref: {Ref}, Result: {Result}",
            request.Stage, request.Kind, request.ReferenceKey, reject ? "Rejected" : "Accepted");

        var reference = $"ACU-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";

        var result = reject
            ? new CustomsTransmissionResult(false, "E-VALIDACION", "Manifiesto rechazado por validación (stub).", reference)
            : new CustomsTransmissionResult(true, "OK", "Manifiesto aceptado (stub).", reference);

        return Task.FromResult(result);
    }
}
