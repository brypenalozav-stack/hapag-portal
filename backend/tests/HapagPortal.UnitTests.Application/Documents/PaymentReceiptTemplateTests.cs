namespace HapagPortal.UnitTests.Application.Documents;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

/// <summary>
/// Boleta de depósito y comprobante de pago (M7-02): el documento imprime textos en español y no códigos crudos
/// (estado, medio, concepto), no repite datos y, en la boleta vigente, explica cómo pagar y qué pasa después.
/// </summary>
public sealed class PaymentReceiptTemplateTests
{
    private static readonly DateTime IssuedAt = new(2026, 10, 3, 13, 30, 0, DateTimeKind.Utc);

    private static Payment Slip(string status = PaymentStatus.PendingVerification) => new()
    {
        PaymentNumber = "PAY-20261003-5C7D9E1F",
        PaymentType = "Cart",
        PaymentMethod = PaymentMethodCodes.Deposit,
        PaymentMethodCode = PaymentMethodCodes.Deposit,
        Amount = 45000m,
        TaxAmount = 8550m,
        TotalAmount = 53550m,
        Currency = "CLP",
        Status = status,
        Country = CountryCodes.Chile,
        PaymentDate = IssuedAt,
        SlipNumber = "BDP-20261003-5C7D9E1F",
        SlipIssuedAt = IssuedAt,
        ExternalReference = "PAY-20261003-5C7D9E1F"
    };

    private static PaymentDetail Detail(string? billing = "76123456-7") => new()
    {
        ConceptType = ChargeConceptCodes.BlFee,
        Description = "BL Documentation Fee (import)",
        Amount = 45000m,
        TaxAmount = 8550m,
        Currency = "CLP",
        BlNumber = "HLCUSAI260300612",
        BillingTaxId = billing
    };

    /// <summary>Todo el texto visible del modelo, para buscar palabras o códigos.</summary>
    private static string Text(PdfDocumentModel model)
    {
        var parts = new List<string?>
        {
            model.Title, model.Subtitle, model.StatusLabel, model.Highlight?.Label, model.Highlight?.Value, model.Highlight?.Caption,
            model.Footer, model.SignatureNote
        };
        parts.AddRange(model.References.SelectMany(f => new[] { f.Label, f.Value }));
        foreach (var section in model.Sections)
        {
            parts.Add(section.Heading);
            parts.Add(section.Note);
            parts.AddRange((section.Fields ?? []).SelectMany(f => new[] { f.Label, f.Value }));
            parts.AddRange((section.Totals ?? []).SelectMany(f => new[] { f.Label, f.Value }));
            parts.AddRange(section.Paragraphs ?? []);
            parts.AddRange(section.Steps ?? []);
            if (section.Table is { } table)
            {
                parts.AddRange(table.Headers);
                parts.AddRange(table.Rows.SelectMany(r => r));
            }
        }

        return string.Join(" | ", parts.Where(p => p is not null));
    }

    [Fact]
    public void DepositSlip_ShouldUseSpanishNames_AndNeverRawCodes()
    {
        var payment = Slip();

        var model = ShipmentDocumentTemplates.PaymentReceipt(
            payment, [Detail()], "Hapag-Lloyd Chile SpA", payment.SlipNumber!, "Importadora Demo SpA", "76123456-7");
        var text = Text(model);

        model.Title.Should().Be("Boleta de depósito");
        model.StatusLabel.Should().Be("Pendiente de verificación");
        model.StatusTone.Should().Be(PdfTones.Warning);
        text.Should().Contain("Pendiente de verificación").And.Contain("Depósito bancario").And.Contain("Emisión de BL");
        text.Should().NotContain("PendingVerification").And.NotContain("DEPOSIT").And.NotContain("BL_FEE");
    }

    [Fact]
    public void DepositSlip_ShouldHighlightTheAmount_AndExplainHowToPay()
    {
        var payment = Slip();
        var bank = new DepositInstructionSettings
        {
            BankName = "Banco de ejemplo", AccountType = "Cuenta corriente", AccountNumber = "00-000-00000-0",
            AccountHolder = "Hapag-Lloyd Chile SpA", TaxId = DepositInstructionSettings.Placeholder, Email = "finanzas@example.com"
        };

        var model = ShipmentDocumentTemplates.PaymentReceipt(
            payment, [Detail()], "Hapag-Lloyd Chile SpA", payment.SlipNumber!, "Importadora Demo SpA", "76123456-7",
            paymentMethodName: "Depósito bancario (boleta)", depositInstructions: bank);

        model.Highlight!.Label.Should().Be("Monto a depositar");
        model.Highlight.Value.Should().Be("CLP 53.550");
        var howTo = model.Sections.Single(s => s.Heading == "Cómo pagar");
        howTo.Fields!.Select(f => f.Label).Should().Contain(["Monto a depositar", "Referencia del depósito", "Banco", "N° de cuenta", "Enviar comprobante a"]);
        howTo.Fields!.Should().NotContain(f => f.Value == DepositInstructionSettings.Placeholder, "placeholders are not printed");
        howTo.Note.Should().BeNull();
        model.Sections.Single(s => s.Heading == "Qué pasa después").Steps!.Should().Contain(s => s.Contains("Historial de pagos"));
        model.References.Should().Contain(f => f.Label == "Medio de pago" && f.Value == "Depósito bancario (boleta)");
    }

    [Fact]
    public void DepositSlip_WithoutBankData_ShouldPointToThePortal()
    {
        var payment = Slip();
        var placeholders = new DepositInstructionSettings { BankName = "Por definir", AccountNumber = "Por definir" };

        var model = ShipmentDocumentTemplates.PaymentReceipt(
            payment, [Detail()], "Hapag-Lloyd Chile SpA", payment.SlipNumber!, "Importadora Demo SpA", "76123456-7",
            depositInstructions: placeholders);

        var howTo = model.Sections.Single(s => s.Heading == "Cómo pagar");
        howTo.Fields!.Select(f => f.Label).Should().Equal("Monto a depositar", "Referencia del depósito");
        howTo.Note.Should().Contain("Los datos bancarios");
        Text(model).Should().NotContain("Por definir");
    }

    [Fact]
    public void Receipt_ShouldNotRepeatValues()
    {
        var payment = Slip();

        var model = ShipmentDocumentTemplates.PaymentReceipt(
            payment, [Detail()], "Hapag-Lloyd Chile SpA", payment.SlipNumber!, "Importadora Demo SpA", "76.123.456-7");

        model.References.Should().NotContain(f => f.Label == "Referencia", "the reference equals the payment number");
        var table = model.Sections.Single(s => s.Table is not null).Table!;
        table.Headers.Should().Equal("BL", "Concepto", "Neto", "IVA", "Total");
        table.ColumnWidths.Should().HaveCount(5);
        table.Rows.Single()[1].Should().Be("Emisión de BL\nBL Documentation Fee (import)");
    }

    [Fact]
    public void Receipt_ShouldShowTheBillingColumn_OnlyWhenItDiffersFromThePayer()
    {
        var payment = Slip();

        var model = ShipmentDocumentTemplates.PaymentReceipt(
            payment, [Detail(), Detail("96555444-3")], "Hapag-Lloyd Chile SpA", payment.SlipNumber!, "Agencia SpA", "76123456-7");

        model.Sections.Single(s => s.Table is not null).Table!.Headers.Should().Contain("RUT facturación");
    }

    [Fact]
    public void ConfirmedReceipt_ShouldSayPaid_AndUseTheCatalogName()
    {
        var payment = Slip(PaymentStatus.Confirmed);
        payment.ReceiptNumber = "RCP-20261004-11112222";
        payment.ConfirmedAt = IssuedAt.AddDays(1);

        var model = ShipmentDocumentTemplates.PaymentReceipt(
            payment, [Detail()], "Hapag-Lloyd Chile SpA", payment.ReceiptNumber, "Importadora Demo SpA", "76123456-7",
            new Dictionary<string, string> { [ChargeConceptCodes.BlFee] = "Emisión de BL (catálogo)" });
        var text = Text(model);

        model.Title.Should().Be("Comprobante de pago");
        model.StatusLabel.Should().Be("Pagado");
        model.StatusTone.Should().Be(PdfTones.Success);
        model.Highlight!.Label.Should().Be("Total pagado");
        text.Should().Contain("Emisión de BL (catálogo)").And.NotContain("Confirmed");
        model.Sections.Should().NotContain(s => s.Heading == "Cómo pagar");
        model.Sections.SelectMany(s => s.Totals ?? []).Last().Label.Should().Be("Total pagado");
    }

    [Fact]
    public void CancelledSlip_ShouldWarnNotToDeposit()
    {
        var payment = Slip(PaymentStatus.Cancelled);

        var model = ShipmentDocumentTemplates.PaymentReceipt(
            payment, [Detail()], "Hapag-Lloyd Chile SpA", payment.SlipNumber!, "Importadora Demo SpA", "76123456-7");

        model.StatusLabel.Should().Be("Anulado");
        model.Sections.Should().NotContain(s => s.Heading == "Cómo pagar");
        model.Sections.Should().Contain(s => s.Note != null && s.Note.Contains("no realice el depósito"));
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, "Pendiente")]
    [InlineData(PaymentStatus.Processing, "En proceso")]
    [InlineData(PaymentStatus.PendingVerification, "Pendiente de verificación")]
    [InlineData(PaymentStatus.Confirmed, "Pagado")]
    [InlineData(PaymentStatus.Failed, "Rechazado")]
    [InlineData(PaymentStatus.Cancelled, "Anulado")]
    public void EveryStatus_ShouldHaveASpanishLabel(string status, string label) =>
        PaymentDisplayNames.Status(status).Label.Should().Be(label);

    [Theory]
    [InlineData(PaymentMethodCodes.Deposit, "Depósito bancario")]
    [InlineData(PaymentMethodCodes.Khipu, "Khipu")]
    [InlineData(PaymentMethodCodes.BankButtonBancoChile, "Botón de pago Banco de Chile")]
    [InlineData("SOME_NEW_METHOD", "Some new method")]
    public void PaymentMethods_ShouldHaveAReadableName(string code, string name) =>
        PaymentDisplayNames.Method(code).Should().Be(name);
}
