namespace HapagPortal.UnitTests.Infrastructure.Documents;

using System.Text;
using FluentAssertions;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Documents;
using PdfSharp.Pdf.IO;

/// <summary>Comprobante de TATC (M2-09): el PDF se genera con y sin código QR (QRCoder).</summary>
public sealed class TatcVoucherPdfTests
{
    private static readonly DateTime IssuedAt = new(2026, 10, 5, 15, 30, 0, DateTimeKind.Utc);
    private readonly MigraDocPdfRenderer _renderer = new();

    private static HapagPortal.Application.Common.Interfaces.PdfDocumentModel Voucher() =>
        ShipmentDocumentTemplates.TatcVoucher(
            "Hapag-Lloyd Chile SpA", CountryCodes.Chile, "TATC-HLCU1", IssuedAt, "HLCUVAL250100123", "San Antonio", "Hamburg Express", "025E",
            "Importadora Demo SpA",
            [
                new ShipmentDocumentTemplates.TatcVoucherLine("HLXU0000001", "40HC", "WH-1", "TATC-1"),
                new ShipmentDocumentTemplates.TatcVoucherLine("HLXU0000002", "20DV", null, "TATC-2")
            ]);

    private static void ShouldBeValidPdf(byte[] content)
    {
        Encoding.ASCII.GetString(content, 0, 5).Should().Be("%PDF-");
        using var stream = new MemoryStream(content);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        document.PageCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void WithQrPayload_ShouldRenderAValidPdfWithTheImage()
    {
        var model = Voucher();
        model.QrPayload.Should().NotBeNullOrWhiteSpace();

        var withQr = _renderer.Render(model);
        var withoutQr = _renderer.Render(model with { QrPayload = null });

        ShouldBeValidPdf(withQr);
        withQr.Length.Should().BeGreaterThan(withoutQr.Length);
    }

    [Fact]
    public void WithoutQrPayload_ShouldStillRender()
    {
        ShouldBeValidPdf(_renderer.Render(Voucher() with { QrPayload = null }));
        ShouldBeValidPdf(_renderer.Render(Voucher() with { QrPayload = "   " }));
    }
}
