namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;

/// <summary>
/// Pasarela de pago controlable para tests: fija si es Real (verifica notificaciones), el resultado de la lectura de la
/// notificación y de la consulta del estado, y cuenta las llamadas.
/// </summary>
public sealed class FakePaymentProvider(bool verifiesNotifications = false) : IPaymentProvider
{
    public string ProviderCode { get; init; } = "Khipu";
    public bool VerifiesNotifications { get; set; } = verifiesNotifications;

    public Result<PaymentVerification> Verification { get; set; } =
        Result<PaymentVerification>.Success(new PaymentVerification("EXT-1", PaymentStatus.Confirmed, 1190m, "CLP", "TXN-TEST"));

    public Result<PaymentNotificationInfo> Notification { get; set; } =
        Result<PaymentNotificationInfo>.Success(new PaymentNotificationInfo("PRV-1", "EXT-1"));

    public Result CancelResult { get; set; } = Result.Success();

    public int VerifyCalls { get; private set; }
    public int CancelCalls { get; private set; }
    public List<PaymentNotification> Notifications { get; } = [];

    public Task<Result<PaymentInitiation>> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<PaymentInitiation>.Success(
            new PaymentInitiation("FAKE-REF", request.ExternalReference, PaymentStatus.Pending, request.ReturnUrl)));

    public Task<Result<PaymentVerification>> GetStatusAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default)
    {
        VerifyCalls++;
        return Task.FromResult(Verification);
    }

    public Task<Result> CancelAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default)
    {
        CancelCalls++;
        return Task.FromResult(CancelResult);
    }

    public Task<Result<PaymentNotificationInfo>> ReadNotificationAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        Notifications.Add(notification);
        return Task.FromResult(Notification);
    }
}
