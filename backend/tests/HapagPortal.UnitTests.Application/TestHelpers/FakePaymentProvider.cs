namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

/// <summary>
/// Proveedor de pago controlable para tests: fija si verifica notificaciones (Dummy = false, Real = true),
/// el resultado de la verificación y cuenta las llamadas.
/// </summary>
public sealed class FakePaymentProvider(bool verifiesNotifications = false) : IPaymentProvider
{
    public string ProviderCode { get; init; } = "Khipu";
    public bool VerifiesNotifications { get; set; } = verifiesNotifications;
    public Result<PaymentVerification> Verification { get; set; } =
        Result<PaymentVerification>.Success(new PaymentVerification("EXT-1", PaymentStatus.Confirmed, 1190m, "CLP", "TXN-TEST"));
    public int VerifyCalls { get; private set; }

    public Task<Result<PaymentInitiation>> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<PaymentInitiation>.Success(
            new PaymentInitiation("FAKE-REF", request.ExternalReference, PaymentStatus.Pending, request.ReturnUrl)));

    public Task<Result<PaymentVerification>> VerifyNotificationAsync(
        string notificationToken,
        string externalReference,
        CancellationToken cancellationToken = default)
    {
        VerifyCalls++;
        return Task.FromResult(Verification);
    }
}
