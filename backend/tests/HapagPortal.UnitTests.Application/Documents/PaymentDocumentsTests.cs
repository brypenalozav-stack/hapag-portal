namespace HapagPortal.UnitTests.Application.Documents;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Commands.Confirm;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Documentos emitidos por la cola posterior al pago (NF-03): al liberarse el pago, el certificado de
/// transbordo firmado y enviado a UMAR y al cliente (M6-01), el cupón de Gate Out con las unidades del BL
/// (M6-03) y el comprobante del flete Collect (M6-04). Una sola vez por ítem pagado, aunque se reintente.
/// </summary>
public sealed class PaymentDocumentsTests
{
    private const string OwnTaxId = "76123456-7";
    private readonly DocumentsFixture _f = new();
    private readonly BillOfLading _bl;

    public PaymentDocumentsTests()
    {
        _bl = _f.Payments.Rules.OwnBl("BL-POST");
        _f.Payments.Rules.AddContainer(_bl, "HLXU1111111", "40HC");
        _f.Payments.Rules.AddContainer(_bl, "HLXU2222222", "20DV");
    }

    private async Task<Payment> PayAsync(string currency, string method, params (string ItemType, Guid SourceId)[] items)
    {
        foreach (var (itemType, sourceId) in items)
        {
            var added = await _f.Payments.Add().Handle(
                new AddCartItemCommand(itemType, sourceId, null, OwnTaxId, null), CancellationToken.None);
            added.IsSuccess.Should().BeTrue(added.IsFailure ? added.Error.Message : null);
        }

        var checkout = await _f.Payments.CheckoutCart().Handle(
            new CheckoutCartCommand(CountryCodes.Chile, currency, method, $"key-{Guid.NewGuid():N}"), CancellationToken.None);
        checkout.IsSuccess.Should().BeTrue();

        var payment = _f.Db.PaymentList.Single(p => p.Id == checkout.Value.Payment.Id);
        var confirmed = await new ConfirmPaymentCommandHandler(_f.Db, AccessTestData.CurrentUser(_f.Payments.Owner.User))
            .Handle(new ConfirmPaymentCommand(payment.Id), CancellationToken.None);
        confirmed.IsSuccess.Should().BeTrue();
        return payment;
    }

    [Fact]
    public async Task ReleasedPayment_ShouldIssueTheCertificateAndTheCouponOnce()
    {
        var transshipment = _f.Payments.Rules.AddCharge(_bl, ChargeConceptCodes.TransshipmentCertificate, 35000m);
        var gateOut = _f.Payments.Rules.AddCharge(_bl, ChargeConceptCodes.GateOut, 60000m);
        var payment = await PayAsync("CLP", PaymentMethodCodes.Khipu,
            (PayableItemTypes.LocalCharge, transshipment.Id), (PayableItemTypes.LocalCharge, gateOut.Id));
        var processor = _f.Processor();

        // 1) Liberación y aviso; la liberación encola la emisión documental una sola vez.
        await processor.ProcessDueAsync(10, DateTime.UtcNow, CancellationToken.None);
        _f.Db.PaymentOutboxMessageList.Should().ContainSingle(m => m.JobType == PaymentOutboxJobTypes.Documents && m.Status == PaymentOutboxStatus.Pending);
        _f.Db.ShipmentDocumentList.Should().BeEmpty();

        // 2) Emisión.
        await processor.ProcessDueAsync(10, DateTime.UtcNow, CancellationToken.None);

        _f.Db.PaymentOutboxMessageList.Should().OnlyContain(m => m.Status == PaymentOutboxStatus.Succeeded);
        var certificate = _f.Db.ShipmentDocumentList.Single(d => d.DocumentType == ShipmentDocumentTypes.TransshipmentCertificate);
        var coupon = _f.Db.ShipmentDocumentList.Single(d => d.DocumentType == ShipmentDocumentTypes.GateOutCoupon);

        certificate.PaymentId.Should().Be(payment.Id);
        certificate.Origin.Should().Be(ShipmentDocumentOrigins.Payment);
        certificate.IssuedForOrganizationId.Should().Be(_f.Payments.Owner.Organization.Id);
        certificate.SignatureId.Should().Be("TEST-SIG-1");
        certificate.SignatureProvider.Should().Be("TestSigner");
        _f.Signer.Requests.Should().ContainSingle(r => r.DocumentType == SignatureDocumentTypes.TransshipmentCertificate);

        // Importación (CL-IMP-07): a UMAR con copia al cliente.
        certificate.RecipientEmails.Should().Be($"umar@test.cl,{_f.Payments.Owner.Organization.Email}");
        certificate.DeliveredAt.Should().NotBeNull();
        await _f.Email.Received(1).SendEmailAsync(
            "umar@test.cl", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<EmailAttachment>>(), Arg.Any<CancellationToken>());
        await _f.Email.Received(1).SendEmailAsync(
            _f.Payments.Owner.Organization.Email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<EmailAttachment>>(), Arg.Any<CancellationToken>());

        coupon.ContainerNumbers.Should().Be("HLXU1111111,HLXU2222222");
        coupon.SignatureId.Should().BeNull();
        coupon.RecipientEmails.Should().BeNull();
        coupon.GenerationKey.Should().StartWith($"{ShipmentDocumentTypes.GateOutCoupon}:");

        _f.Payments.Notifications.Published.Count(n => n.Type == NotificationTypes.DocumentIssued).Should().Be(2);
        _f.Db.ShipmentDocumentEventList.Count(e => e.EventType == ShipmentDocumentEventTypes.Issued && e.Channel == DocumentChannels.System)
            .Should().Be(2);

        // 3) Reintentos de la liberación y de la emisión: no duplican documentos ni envíos.
        foreach (var message in _f.Db.PaymentOutboxMessageList)
        {
            message.Status = PaymentOutboxStatus.Pending;
            message.NextAttemptAt = DateTime.UtcNow.AddMinutes(-1);
        }

        await processor.ProcessDueAsync(10, DateTime.UtcNow, CancellationToken.None);

        _f.Db.PaymentOutboxMessageList.Count(m => m.JobType == PaymentOutboxJobTypes.Documents).Should().Be(1);
        _f.Db.ShipmentDocumentList.Should().HaveCount(2);
        _f.Email.ReceivedCalls().Should().HaveCount(2);
    }

    [Fact]
    public async Task CollectFreight_ShouldIssueTheCollectReceipt()
    {
        _bl.FreightTerms = "Collect";
        var payment = await PayAsync("USD", PaymentMethodCodes.BankButtonBancoChile, (PayableItemTypes.Freight, _bl.Id));
        var processor = _f.Processor();

        await processor.ProcessDueAsync(10, DateTime.UtcNow, CancellationToken.None);
        await processor.ProcessDueAsync(10, DateTime.UtcNow, CancellationToken.None);

        var receipt = _f.Db.ShipmentDocumentList.Single();
        receipt.DocumentType.Should().Be(ShipmentDocumentTypes.CollectReceipt);
        receipt.PaymentId.Should().Be(payment.Id);
        _f.Renderer.Rendered.Single().Sections.Single(s => s.Heading == "Pago del flete").Fields
            .Should().Contain(f => f.Label == "Monto pagado" && f.Value == "1.500,00 USD");
    }

    [Fact]
    public async Task PaymentWithoutDocumentItems_ShouldNotEnqueueTheDocumentsStep()
    {
        var thc = _f.Payments.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 185000m);
        await PayAsync("CLP", PaymentMethodCodes.Khipu, (PayableItemTypes.LocalCharge, thc.Id));

        await _f.Processor().ProcessDueAsync(10, DateTime.UtcNow, CancellationToken.None);

        _f.Db.PaymentOutboxMessageList.Should().NotContain(m => m.JobType == PaymentOutboxJobTypes.Documents);
        _f.Db.ShipmentDocumentList.Should().BeEmpty();
    }
}
