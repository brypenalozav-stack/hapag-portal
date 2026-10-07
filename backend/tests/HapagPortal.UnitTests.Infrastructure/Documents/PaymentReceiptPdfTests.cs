namespace HapagPortal.UnitTests.Infrastructure.Documents;

using System.Text;
using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Infrastructure.Documents;
using PdfSharp.Pdf.IO;

/// <summary>
/// Plantilla común mejorada: anchos de columna según el contenido o los pesos indicados, insignia de estado, recuadro
/// destacado, totales, pasos y notas. Con la variable <c>HL_PDF_PREVIEW_DIR</c> las pruebas además guardan la boleta
/// y el comprobante de muestra en esa carpeta para revisarlos a ojo.
/// </summary>
public sealed class PaymentReceiptPdfTests
{
    private const string PreviewVariable = "HL_PDF_PREVIEW_DIR";
    private static readonly DateTime IssuedAt = new(2026, 10, 3, 13, 30, 0, DateTimeKind.Utc);
    private readonly MigraDocPdfRenderer _renderer = new();

    private static Payment Slip(string status = PaymentStatus.PendingVerification) => new()
    {
        PaymentNumber = "PAY-20261003-5C7D9E1F",
        PaymentType = "Cart",
        PaymentMethod = PaymentMethods.Deposit,
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
        ExternalReference = "PAY-20261003-5C7D9E1F",
        PayerName = "Importadora Demo SpA",
        PayerTaxId = "76123456-7"
    };

    private static Payment Receipt() => new()
    {
        PaymentNumber = "PAY-20261005-ABCDEF12",
        PaymentType = "Cart",
        PaymentMethod = "KHIPU",
        PaymentMethodCode = PaymentMethodCodes.Khipu,
        Amount = 230000m,
        TaxAmount = 43700m,
        TotalAmount = 273700m,
        Currency = "CLP",
        Status = PaymentStatus.Confirmed,
        Country = CountryCodes.Chile,
        PaymentDate = IssuedAt,
        ConfirmedAt = IssuedAt.AddMinutes(4),
        ReceiptNumber = "RCP-20261005-12345678",
        ExternalReference = "KHIPU-TRX-9F8E7D6C5B",
        PayerName = "Importadora Demo SpA",
        PayerTaxId = "76123456-7"
    };

    private static PaymentDetail Detail(string concept, string description, decimal amount, string bl = "HLCUSAI260300612", string? billing = "76123456-7") => new()
    {
        ConceptType = concept,
        Description = description,
        Amount = amount,
        TaxAmount = decimal.Round(amount * 0.19m),
        Currency = "CLP",
        BlNumber = bl,
        BillingTaxId = billing
    };

    private static DepositInstructionSettings Bank() => new()
    {
        BankName = "Banco de ejemplo",
        AccountType = "Cuenta corriente",
        AccountNumber = "00-000-00000-0",
        AccountHolder = "Hapag-Lloyd Chile SpA",
        TaxId = "Por definir",
        Email = "finanzas@example.com"
    };

    private static PdfSharp.Pdf.PdfDocument Open(byte[] content)
    {
        using var stream = new MemoryStream(content);
        return PdfReader.Open(stream, PdfDocumentOpenMode.Import);
    }

    private static void Preview(string fileName, byte[] content)
    {
        var folder = Environment.GetEnvironmentVariable(PreviewVariable);
        if (string.IsNullOrWhiteSpace(folder))
            return;

        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, fileName), content);
    }

    [Fact]
    public void DepositSlip_ShouldRenderOnOnePage_WithStatusHighlightAndInstructions()
    {
        var payment = Slip();
        var model = ShipmentDocumentTemplates.PaymentReceipt(
            payment,
            [Detail(ChargeConceptCodes.BlFee, "BL Documentation Fee (import) - emisión del conocimiento de embarque", 45000m)],
            "Hapag-Lloyd Chile SpA", payment.SlipNumber!, payment.PayerName!, payment.PayerTaxId,
            new Dictionary<string, string> { [ChargeConceptCodes.BlFee] = "Emisión de BL" },
            "Depósito bancario (boleta)",
            Bank());

        var content = _renderer.Render(model);
        Preview("boleta-deposito.pdf", content);

        Encoding.ASCII.GetString(content, 0, 5).Should().Be("%PDF-");
        using var document = Open(content);
        document.PageCount.Should().Be(1);

        var withoutExtras = _renderer.Render(model with { StatusLabel = null, Highlight = null });
        content.Length.Should().BeGreaterThan(withoutExtras.Length);
    }

    [Fact]
    public void DepositSlip_WithoutBankData_ShouldStillRender()
    {
        var payment = Slip();
        var model = ShipmentDocumentTemplates.PaymentReceipt(
            payment, [Detail(ChargeConceptCodes.BlFee, "BL Documentation Fee (import)", 45000m)],
            "Hapag-Lloyd Chile SpA", payment.SlipNumber!, payment.PayerName!, payment.PayerTaxId);

        var content = _renderer.Render(model);
        Preview("boleta-deposito-sin-banco.pdf", content);

        using var document = Open(content);
        document.PageCount.Should().Be(1);
    }

    [Fact]
    public void ConfirmedReceipt_WithSeveralLinesAndBillingParty_ShouldRender()
    {
        var payment = Receipt();
        var model = ShipmentDocumentTemplates.PaymentReceipt(
            payment,
            [
                Detail(ChargeConceptCodes.Thc, "Terminal Handling Charge - 20DV (Valparaíso)", 185000m),
                Detail(ChargeConceptCodes.BlFee, "BL Documentation Fee (import)", 45000m, "HLCUSAI260300613", "96555444-3")
            ],
            "Hapag-Lloyd Chile SpA", payment.ReceiptNumber!, payment.PayerName!, payment.PayerTaxId);

        var content = _renderer.Render(model);
        Preview("comprobante-pago.pdf", content);

        using var document = Open(content);
        document.PageCount.Should().Be(1);
    }

    [Fact]
    public void AutomaticWidths_ShouldGiveLongCodesTheirWidth_AndFillThePage()
    {
        var table = new PdfTable(
            ["BL", "Concepto", "Descripción", "RUT facturación", "Neto", "Impuesto", "Total"],
            [["HLCUSAI2603006125", "BL_FEE", "BL Documentation Fee (import) con una descripción larga que debe ajustarse en varias líneas", "76123456-7", "45.000,00", "8.550,00", "53.550,00"]],
            [4, 5, 6]);

        var widths = MigraDocPdfRenderer.ColumnWidthsCm(table);

        widths.Sum().Should().BeApproximately(17, 0.01);
        widths[0].Should().BeGreaterThan(17.0 / 7, "the BL number must fit without spilling into the next column");
        widths[2].Should().Be(widths.Max(), "free text takes the remaining space");
    }

    [Fact]
    public void ExplicitWidths_ShouldBeScaledToThePage()
    {
        var table = new PdfTable(["A", "B"], [["1", "2"]], ColumnWidths: [1, 3]);

        var widths = MigraDocPdfRenderer.ColumnWidthsCm(table);

        widths[0].Should().BeApproximately(4.25, 0.01);
        widths[1].Should().BeApproximately(12.75, 0.01);
    }

    [Fact]
    public void ModelWithEveryOptionalPart_ShouldRender_AndUnbreakableTextShouldNotFail()
    {
        var model = new PdfDocumentModel(
            "Documento de prueba", "Subtítulo", "Hapag-Lloyd Chile SpA", "Agente de Hapag-Lloyd AG - Operación Chile",
            "DOC-1", IssuedAt, "America/Santiago",
            [new("Uno", "1"), new("Dos", "2"), new("Tres", null)],
            [
                new PdfSection("Tabla",
                    [new("Campo", "Valor\nsecundario")],
                    new PdfTable(
                        ["Código", "Detalle"],
                        [[new string('X', 120), "texto"], ["Y", "a\nb"]]),
                    ["Párrafo"],
                    [new("Neto", "1"), new("Total", "2")],
                    ["Paso uno", "Paso dos"],
                    "Nota", PdfTones.Warning)
            ],
            "AAAA-BBBB", "Firma", "Pie",
            StatusLabel: "Anulado", StatusTone: PdfTones.Danger,
            Highlight: new PdfHighlight("Monto", "CLP 1", "Leyenda", PdfTones.Info));

        var content = _renderer.Render(model);

        using var document = Open(content);
        document.PageCount.Should().BeGreaterThanOrEqualTo(1);
    }
}
