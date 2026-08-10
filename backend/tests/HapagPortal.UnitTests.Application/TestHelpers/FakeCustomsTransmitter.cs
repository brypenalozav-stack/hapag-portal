namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Interfaces;

/// <summary>Transmisor de Aduana controlable para tests: decide aceptar/rechazar y cuenta llamadas.</summary>
public sealed class FakeCustomsTransmitter(bool accept = true) : ICustomsTransmitter
{
    public bool Accept { get; set; } = accept;
    public int Calls { get; private set; }

    public Task<CustomsTransmissionResult> TransmitAsync(
        CustomsTransmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        var result = Accept
            ? new CustomsTransmissionResult(true, "OK", "Aceptado", "ACU-TEST")
            : new CustomsTransmissionResult(false, "E-VAL", "Rechazado", "ACU-TEST");
        return Task.FromResult(result);
    }
}
