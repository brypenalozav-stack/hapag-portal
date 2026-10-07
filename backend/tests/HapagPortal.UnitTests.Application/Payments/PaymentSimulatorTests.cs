namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.Payments.Lifecycle;
using HapagPortal.Application.Payments.Simulator;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Simulador de pago en modo de prueba (POST payments/simulator/{referencia}): solo con la pasarela simulada y para
/// pagos de la propia organización; el resultado elegido se aplica por el mismo camino que la verificación (idempotente).
/// </summary>
public sealed class PaymentSimulatorTests
{
    private readonly PaymentsFixture _f = new();
    private readonly FakeSimulatorStore _store = new();
    private readonly FakeSimulatedProvider _simulated;

    public PaymentSimulatorTests()
    {
        _simulated = new FakeSimulatedProvider(_store);
        _f.Providers.Register(PaymentProviderKeys.Khipu, _simulated);
    }

    private Payment AddPayment(string reference = "EXT-1", string providerKey = PaymentProviderKeys.Khipu)
    {
        var payment = new Payment
        {
            PaymentNumber = "PAY-001",
            PaymentType = PaymentOrigins.Cart,
            PaymentMethod = PaymentMethodCodes.Khipu,
            PaymentMethodCode = PaymentMethodCodes.Khipu,
            ProviderKey = providerKey,
            ProviderReference = $"DUMMY-KHIPU-{reference}",
            Amount = 1190m,
            TotalAmount = 1190m,
            Currency = "CLP",
            Status = PaymentStatus.Processing,
            Country = "CL",
            ClientId = _f.Owner.Organization.Id,
            ExternalReference = reference,
            PaymentDate = DateTime.UtcNow.AddMinutes(-1),
        };
        _f.Db.PaymentList.Add(payment);
        return payment;
    }

    private SimulatePaymentCommandHandler Handler(PaymentsFixture.Actor? actor = null)
    {
        actor ??= _f.Owner;
        return new(_f.Db, actor.Evaluator(_f.Db), actor.CurrentUser, _f.Providers, _store);
    }

    private Task<Result<PaymentStatusDto>> Simulate(string outcome, string reference = "EXT-1", PaymentsFixture.Actor? actor = null) =>
        Handler(actor).Handle(new SimulatePaymentCommand(reference, outcome), CancellationToken.None);

    [Fact]
    public async Task Approved_ShouldConfirmOnce_EvenIfRepeated()
    {
        var payment = AddPayment();

        var first = await Simulate("approved");
        var receipt = payment.ReceiptNumber;
        var second = await Simulate("approved");

        first.Value.Payment.Status.Should().Be(PaymentStatus.Confirmed);
        second.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.ReceiptNumber.Should().NotBeNull().And.Be(receipt);
        payment.ProviderTransactionId.Should().Be("DUMMY-TXN-EXT-1");
        payment.ConfirmedBy.Should().Be("KHIPU_SIMULATOR");
        _f.Db.PaymentStatusChangeList.Count(c => c.ToStatus == PaymentStatus.Confirmed).Should().Be(1);
        _f.Db.PaymentOutboxMessageList.Count(m => m.JobType == PaymentOutboxJobTypes.Release).Should().Be(1);
        _simulated.VerifyCalls.Should().Be(1, "un pago confirmado no se vuelve a consultar");
    }

    [Theory]
    [InlineData("rejected")]
    [InlineData("cancelled")]
    public async Task RejectedOrCancelled_ShouldFailThePayment(string outcome)
    {
        var payment = AddPayment();

        var result = await Simulate(outcome);

        result.Value.Payment.Status.Should().Be(PaymentStatus.Failed);
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.ReceiptNumber.Should().BeNull();
        _store.Outcome("EXT-1").Should().Be(outcome);
    }

    [Fact]
    public async Task Pending_ShouldStayProcessing()
    {
        var payment = AddPayment();

        var result = await Simulate("pending");

        result.Value.Payment.Status.Should().Be(PaymentStatus.Processing);
        payment.Status.Should().Be(PaymentStatus.Processing);
        _f.Db.PaymentStatusChangeList.Should().BeEmpty();
    }

    [Fact]
    public async Task ProviderNotInTestMode_ShouldBeNotFound_WithoutChanges()
    {
        var real = new FakePaymentProvider(verifiesNotifications: true);
        _f.Providers.Register(PaymentProviderKeys.Khipu, real);
        var payment = AddPayment();

        var result = await Simulate("approved");

        result.Error.Code.Should().Be("Payment.NotFound");
        payment.Status.Should().Be(PaymentStatus.Processing);
        real.VerifyCalls.Should().Be(0);
        _store.Outcome("EXT-1").Should().BeNull();
    }

    [Fact]
    public async Task PaymentOfAnotherClient_ShouldBeNotFound_WithoutChanges()
    {
        var payment = AddPayment();
        var other = _f.NewActor();

        var result = await Simulate("approved", actor: other);

        result.Error.Code.Should().Be("Payment.NotFound");
        payment.Status.Should().Be(PaymentStatus.Processing);
        _store.Outcome("EXT-1").Should().BeNull();
    }

    [Fact]
    public async Task UnknownReference_ShouldBeNotFound()
    {
        AddPayment();

        var result = await Simulate("approved", reference: "EXT-OTRA");

        result.Error.Code.Should().Be("Payment.NotFound");
    }

    [Fact]
    public async Task VerifyOnReturn_WithTheSimulatedGateway_ShouldApplyTheChosenOutcome()
    {
        var payment = AddPayment();
        _store.Record("EXT-1", PaymentSimulatorOutcomes.Approved);

        var result = await new VerifyPaymentCommandHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser, _f.Providers)
            .Handle(new VerifyPaymentCommand(payment.Id), CancellationToken.None);

        result.Value.Payment.Status.Should().Be(PaymentStatus.Confirmed);
        _f.Db.PaymentStatusChangeList.Last().ChangedBy.Should().Be("PAYER_RETURN_CHECK");
    }

    [Fact]
    public async Task VerifyOnReturn_WithoutOutcome_ShouldStayProcessing()
    {
        var payment = AddPayment();

        var result = await new VerifyPaymentCommandHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser, _f.Providers)
            .Handle(new VerifyPaymentCommand(payment.Id), CancellationToken.None);

        result.Value.Payment.Status.Should().Be(PaymentStatus.Processing);
        _simulated.VerifyCalls.Should().Be(1);
    }

    [Theory]
    [InlineData("approved", true)]
    [InlineData("Rejected", true)]
    [InlineData("pending", true)]
    [InlineData("cancelled", true)]
    [InlineData("paid", false)]
    [InlineData("", false)]
    public void Validator_ShouldAcceptOnlyTheSimulatorOutcomes(string outcome, bool valid)
    {
        new SimulatePaymentCommandValidator().Validate(new SimulatePaymentCommand("EXT-1", outcome)).IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData(null, PaymentStatus.Processing)]
    [InlineData("pending", PaymentStatus.Processing)]
    [InlineData("approved", PaymentStatus.Confirmed)]
    [InlineData("rejected", PaymentStatus.Failed)]
    [InlineData("cancelled", PaymentStatus.Failed)]
    public void StatusFor_ShouldMapEachOutcome(string? outcome, string expected)
    {
        PaymentSimulatorOutcomes.StatusFor(outcome).Should().Be(expected);
    }

    private sealed class FakeSimulatorStore : IPaymentSimulatorStore
    {
        private readonly Dictionary<string, string> _outcomes = new(StringComparer.Ordinal);

        public void Record(string externalReference, string outcome) => _outcomes[externalReference] = outcome.Trim().ToLowerInvariant();

        public string? Outcome(string externalReference) => _outcomes.GetValueOrDefault(externalReference);
    }

    /// <summary>Pasarela simulada como la Dummy: informa el resultado del simulador con el monto del pago del portal.</summary>
    private sealed class FakeSimulatedProvider(IPaymentSimulatorStore store) : ISimulatedPaymentProvider
    {
        public int VerifyCalls { get; private set; }

        public string ProviderCode => PaymentProviderKeys.Khipu;

        public bool VerifiesNotifications => false;

        public Task<Result<PaymentInitiation>> InitiateAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<PaymentVerification>> GetStatusAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default)
        {
            VerifyCalls++;
            var status = PaymentSimulatorOutcomes.StatusFor(store.Outcome(request.ExternalReference));
            return Task.FromResult(Result<PaymentVerification>.Success(new PaymentVerification(
                request.ExternalReference, status, request.ExpectedAmount ?? 0m, request.ExpectedCurrency ?? string.Empty,
                status == PaymentStatus.Confirmed ? $"DUMMY-TXN-{request.ExternalReference}" : null)));
        }

        public Task<Result> CancelAsync(PaymentStatusRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());

        public Task<Result<PaymentNotificationInfo>> ReadNotificationAsync(PaymentNotification notification, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<PaymentNotificationInfo>.Failure(Error.Unauthorized));
    }
}
