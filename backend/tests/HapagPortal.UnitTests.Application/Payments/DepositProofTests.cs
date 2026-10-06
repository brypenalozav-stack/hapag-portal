namespace HapagPortal.UnitTests.Application.Payments;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.DepositProofs;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Comprobante del depósito bancario (M5-06): el cliente lo adjunta a su pago con boleta (lo deja en verificación),
/// Finanzas lo verifica (el pago se confirma por la vía existente: comprobante, liberación y aviso) o lo rechaza con
/// motivo (el pago sigue esperando un comprobante válido). Todo queda trazado y avisado.
/// </summary>
public sealed class DepositProofTests
{
    private static readonly byte[] Pdf = "%PDF-1.4 deposit"u8.ToArray();

    private readonly FinanceFixture _f = new();
    private readonly ICurrentUserService _finance;

    public DepositProofTests()
    {
        _f.Payments.NoConditions(_f.Owner.Organization);
        _finance = AccessTestData.CurrentUser(
            AccessTestData.AddMember(_f.Db, AccessTestData.AddOrganization(_f.Db, OrganizationTypes.Internal), profile: null),
            PaymentPermissions.Finance);
    }

    private async Task<Payment> DepositPaymentAsync(string method = PaymentMethodCodes.Deposit)
    {
        var bl = _f.Rules.OwnBl($"BL-DEP-{Guid.NewGuid():N}"[..14]);
        var charge = _f.Rules.AddCharge(bl, ChargeConceptCodes.Thc, 45000m);
        await _f.Payments.Add().Handle(new AddCartItemCommand(PayableItemTypes.LocalCharge, charge.Id, null, _f.OwnTaxId, "CLP"), CancellationToken.None);
        var checkout = await _f.Payments.CheckoutCart().Handle(
            new CheckoutCartCommand(CountryCodes.Chile, "CLP", method, $"key-{Guid.NewGuid():N}"), CancellationToken.None);
        checkout.IsSuccess.Should().BeTrue(checkout.IsFailure ? checkout.Error.Message : null);
        return _f.Db.PaymentList.Single(p => p.Id == checkout.Value.Payment.Id);
    }

    private UploadDepositProofCommandHandler Upload(PaymentsFixture.Actor? actor = null)
    {
        actor ??= _f.Owner;
        return new(_f.Db, actor.Evaluator(_f.Db), actor.CurrentUser, _f.Storage, _f.Payments.Notifications);
    }

    private static UploadDepositProofCommand Proof(Guid paymentId, string contentType = "application/pdf") =>
        new(paymentId, "comprobante.pdf", contentType, Pdf, "Banco de Chile", "OP-123456", new DateOnly(2026, 10, 3), 53550m, "Sucursal centro");

    [Fact]
    public async Task Upload_ShouldIssueTheSlip_AndQueueTheProofForFinance()
    {
        var payment = await DepositPaymentAsync();

        var uploaded = await Upload().Handle(Proof(payment.Id), CancellationToken.None);
        var second = await Upload().Handle(Proof(payment.Id), CancellationToken.None);
        var queue = await new GetDepositProofQueueQueryHandler(_f.Db).Handle(new GetDepositProofQueueQuery(), CancellationToken.None);

        uploaded.IsSuccess.Should().BeTrue(uploaded.IsFailure ? uploaded.Error.Message : null);
        payment.Status.Should().Be(PaymentStatus.PendingVerification);
        payment.SlipIssuedAt.Should().NotBeNull();
        uploaded.Value.Payment.History.Should().Contain(h => h.ToStatus == PaymentStatus.PendingVerification);
        var proof = uploaded.Value.Proofs.Single();
        proof.Status.Should().Be(DepositProofStatus.Submitted);
        proof.BankReference.Should().Be("OP-123456");
        proof.ContentHash.Should().HaveLength(64);
        _f.Storage.Files.Values.Should().ContainSingle(c => c.SequenceEqual(Pdf));
        uploaded.Value.AwaitingProof.Should().BeFalse();
        uploaded.Value.CanUpload.Should().BeFalse();
        second.Error.Should().Be(DomainErrors.DepositProof.PendingReview);
        queue.Value.Single().PaymentNumber.Should().Be(payment.PaymentNumber);
        _f.Payments.Notifications.Published.Should().Contain(n =>
            n.Type == NotificationTypes.DepositProofSubmitted && n.RoleCode == RoleCodes.Administrador);
    }

    [Fact]
    public async Task Verify_ShouldConfirmThePaymentThroughTheExistingPath()
    {
        var payment = await DepositPaymentAsync();
        var proofId = (await Upload().Handle(Proof(payment.Id), CancellationToken.None)).Value.Proofs.Single().Id;

        var verified = await new VerifyDepositProofCommandHandler(_f.Db, _finance)
            .Handle(new VerifyDepositProofCommand(proofId, "Abono verificado en cartola"), CancellationToken.None);
        var again = await new VerifyDepositProofCommandHandler(_f.Db, _finance)
            .Handle(new VerifyDepositProofCommand(proofId, null), CancellationToken.None);
        var queue = await new GetDepositProofQueueQueryHandler(_f.Db).Handle(new GetDepositProofQueueQuery(), CancellationToken.None);

        verified.IsSuccess.Should().BeTrue(verified.IsFailure ? verified.Error.Message : null);
        verified.Value.Proof.Status.Should().Be(DepositProofStatus.Verified);
        verified.Value.Proof.ReviewedBy.Should().Be(_finance.Email);
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.ReceiptNumber.Should().StartWith(DocumentPrefixes.Receipt);
        payment.ProviderTransactionId.Should().Be("OP-123456");
        payment.DepositProofUrl.Should().Contain(proofId.ToString());
        _f.Db.PaymentOutboxMessageList.Should().Contain(m => m.PaymentId == payment.Id && m.JobType == PaymentOutboxJobTypes.Release);
        again.Error.Should().Be(DomainErrors.DepositProof.NotPendingReview);
        queue.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Reject_ShouldKeepThePaymentAwaitingANewProof()
    {
        var payment = await DepositPaymentAsync();
        var proofId = (await Upload().Handle(Proof(payment.Id), CancellationToken.None)).Value.Proofs.Single().Id;

        var rejected = await new RejectDepositProofCommandHandler(_f.Db, _finance, _f.Payments.Notifications)
            .Handle(new RejectDepositProofCommand(proofId, "El monto no coincide con la boleta"), CancellationToken.None);
        var view = await new GetDepositProofsQueryHandler(_f.Db, _f.Owner.Evaluator(_f.Db), _f.Owner.CurrentUser)
            .Handle(new GetDepositProofsQuery(payment.Id), CancellationToken.None);
        var resubmitted = await Upload().Handle(Proof(payment.Id), CancellationToken.None);

        rejected.Value.Proof.Status.Should().Be(DepositProofStatus.Rejected);
        rejected.Value.Proof.RejectionReason.Should().Be("El monto no coincide con la boleta");
        payment.Status.Should().Be(PaymentStatus.PendingVerification);
        view.Value.AwaitingProof.Should().BeTrue();
        view.Value.CanUpload.Should().BeTrue();
        _f.Payments.Notifications.Published.Should().Contain(n =>
            n.Type == NotificationTypes.DepositProofRejected && n.UserId == _f.Owner.User.Id && n.Body.Contains("El monto no coincide"));
        resubmitted.IsSuccess.Should().BeTrue();
        resubmitted.Value.Proofs.Select(p => p.Status).Should().Equal(DepositProofStatus.Rejected, DepositProofStatus.Submitted);
    }

    [Fact]
    public async Task Upload_ShouldOnlyAcceptOwnDepositPayments()
    {
        var online = await DepositPaymentAsync(PaymentMethodCodes.Khipu);
        var deposit = await DepositPaymentAsync();
        var stranger = _f.Payments.NewActor();
        var viewer = _f.Payments.NewActor(profile: RoleCodes.OrgViewer);

        var onlineResult = await Upload().Handle(Proof(online.Id), CancellationToken.None);
        var strangerResult = await Upload(stranger).Handle(Proof(deposit.Id), CancellationToken.None);
        var viewerResult = await Upload(viewer).Handle(Proof(deposit.Id), CancellationToken.None);
        var invalid = new UploadDepositProofCommandValidator().Validate(Proof(deposit.Id, "text/plain"));

        onlineResult.Error.Should().Be(DomainErrors.DepositProof.PaymentNotAwaitingProof);
        strangerResult.Error.Code.Should().Be("Payment.NotFound");
        viewerResult.Error.Should().Be(Error.Forbidden);
        invalid.IsValid.Should().BeFalse();
    }
}
