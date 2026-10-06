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
/// Plantillas de documentos (M6) generadas con PDFsharp/MigraDoc: cada tipo produce un PDF válido (encabezado
/// <c>%PDF-</c>, se abre con el lector de PDFsharp y tiene páginas), las tablas largas paginan y el texto
/// con acentos se genera con la fuente incrustada.
/// </summary>
public sealed class MigraDocPdfRendererTests
{
    private static readonly DateTime IssuedAt = new(2026, 10, 5, 15, 30, 0, DateTimeKind.Utc);
    private readonly MigraDocPdfRenderer _renderer = new();

    private static ShipmentDocumentData Data(int containers = 2, string country = CountryCodes.Chile)
    {
        var bl = new BillOfLading
        {
            BLNumber = "HLCUVAL250100123",
            BookingNumber = "HLCUBKG2501001",
            ShipmentType = "Import",
            Vessel = "Hamburg Express",
            Voyage = "025E",
            PortOfLoading = "Shanghai (CNSHA)",
            PortOfDischarge = "San Antonio (CLSAI)",
            PlaceOfDelivery = "Santiago, Chile",
            Consignee = "Importadora Demo SpA",
            Shipper = "Shanghai Electronics Co. Ltd",
            FreightAmount = 3500m,
            FreightCurrency = "USD",
            FreightTerms = "Collect",
            Status = "Arrived",
            Country = country
        };

        var units = Enumerable.Range(1, containers)
            .Select(i => new BLContainer { ContainerNumber = $"HLXU{i:0000000}", ContainerType = "40HC", SealNumber = $"SL-{i}", Weight = 24500m, Status = "Discharged" })
            .ToList();

        return new ShipmentDocumentData(
            bl,
            [new BLParty { Role = "Consignee", Name = "Importadora Demo SpA", TaxId = "76.123.456-7" }],
            units,
            [new BLCargoItem { HsCode = "8471", Description = "Equipos electrónicos y accesorios", PackageCount = 120, GrossWeight = 18000m, Volume = 54m }],
            [new LocalCharge { ChargeType = ChargeConceptCodes.Thc, Description = "Terminal Handling Charge", Amount = 185000m, TaxAmount = 35150m, TotalAmount = 220150m, Currency = "CLP", Status = ChargeStatus.Pending }]);
    }

    private static DocumentHeader Header(string number) =>
        new(number, IssuedAt, ShipmentDocumentTemplates.VerificationCode(number, "HLCUVAL250100123", IssuedAt), "Hapag-Lloyd Chile SpA");

    public static TheoryData<string> DocumentTypes() => new(ShipmentDocumentTypes.All);

    private static PdfDocumentModel ModelFor(string type, ShipmentDocumentData data) => type switch
    {
        ShipmentDocumentTypes.TransshipmentCertificate =>
            ShipmentDocumentTemplates.TransshipmentCertificate(data, Header("CTB-1"), "Importadora Demo SpA", "76123456-7", "PAY-1 / RCP-1"),
        ShipmentDocumentTypes.GateOutCoupon =>
            ShipmentDocumentTemplates.GateOutCoupon(data, Header("CGO-1"), "Importadora Demo SpA", "PAY-1", "Gate Out - 40HC"),
        ShipmentDocumentTypes.CollectReceipt =>
            ShipmentDocumentTemplates.CollectReceipt(data, Header("CCO-1"), "Agencia Marítima del Pacífico Ltda", "96555444-3", "PAY-1", "RCP-1", 3500m, "USD", IssuedAt),
        ShipmentDocumentTypes.BlCopyValued => ShipmentDocumentTemplates.BlCopy(data, Header("CBL-1"), valued: true, "Importadora Demo SpA"),
        ShipmentDocumentTypes.BlCopyNonValued => ShipmentDocumentTemplates.BlCopy(data, Header("CBL-2"), valued: false, "Importadora Demo SpA"),
        ShipmentDocumentTypes.ResponsibilityLetter => ShipmentDocumentTemplates.ResponsibilityLetter(
            data, Header("CRE-1"),
            new ResponsibilityLetterData("Global Forwarding Chile SpA", "76000003-3", "Felipe Forwarder", "12.345.678-5", "Gerente",
                "ffww@globalforwarding.cl", null, "Muebles", "Sin observaciones", ResponsibilityLetterTerms.Version, IssuedAt)),
        ShipmentDocumentTypes.NoDebtCertificate =>
            ShipmentDocumentTemplates.NoDebtCertificate(Data(country: CountryCodes.Bolivia), Header("CLD-1"), "Comercial Altiplano SRL", "1023456017"),
        ShipmentDocumentTypes.GateOutAdvanceReceipt => ShipmentDocumentTemplates.GateOutAdvanceReceipt(
            data, Header("RGO-1"), "Agencia Marítima del Pacífico Ltda", "96555444-3", "PAY-1", "RCP-1", "KHIPU",
            new PaymentDetail
            {
                ConceptType = ChargeConceptCodes.GateOut,
                Description = "Gate Out - 40HC",
                Amount = 60000m,
                TaxAmount = 11400m,
                Currency = "CLP",
                OriginalAmount = 71400m,
                OriginalCurrency = "CLP",
                BillingTaxId = "76123456-7",
                BillingName = "Importadora Demo SpA"
            },
            IssuedAt),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static PdfSharp.Pdf.PdfDocument Open(byte[] content)
    {
        using var stream = new MemoryStream(content);
        return PdfReader.Open(stream, PdfDocumentOpenMode.Import);
    }

    [Theory]
    [MemberData(nameof(DocumentTypes))]
    public void EveryDocumentType_ShouldRenderAValidPdf(string type)
    {
        var content = _renderer.Render(ModelFor(type, Data()));

        Encoding.ASCII.GetString(content, 0, 5).Should().Be("%PDF-");
        Encoding.ASCII.GetString(content[^8..]).Should().Contain("%%EOF");
        using var document = Open(content);
        document.PageCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void LongUnitTables_ShouldSpanSeveralPages()
    {
        var content = _renderer.Render(ModelFor(ShipmentDocumentTypes.GateOutCoupon, Data(containers: 150)));

        using var document = Open(content);
        document.PageCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public void PortalPdfs_PaymentReceiptAndServiceOrder_ShouldBeValid()
    {
        var payment = new Payment
        {
            PaymentNumber = "PAY-20261005-ABCDEF12",
            PaymentType = "Cart",
            PaymentMethod = "KHIPU",
            Amount = 185000m,
            TaxAmount = 35150m,
            TotalAmount = 220150m,
            Currency = "CLP",
            Status = PaymentStatus.Confirmed,
            Country = CountryCodes.Chile,
            PaymentDate = IssuedAt,
            ConfirmedAt = IssuedAt,
            ReceiptNumber = "RCP-20261005-12345678"
        };
        var detail = new PaymentDetail { ConceptType = ChargeConceptCodes.Thc, Description = "THC", Amount = 185000m, TaxAmount = 35150m, Currency = "CLP", BlNumber = "HLCUVAL250100123" };
        var order = new ServiceOrder
        {
            OrderNumber = "ODS-2026-0001",
            OrderType = "WarehouseChange",
            Status = "Completed",
            Country = CountryCodes.Bolivia,
            RequestedAt = IssuedAt,
            Description = "Cambio de almacén"
        };

        var receipt = _renderer.Render(ShipmentDocumentTemplates.PaymentReceipt(payment, [detail], "Hapag-Lloyd Chile SpA", payment.ReceiptNumber, "Importadora Demo SpA", "76123456-7"));
        var serviceOrder = _renderer.Render(ShipmentDocumentTemplates.ServiceOrder(order, null, "Hapag-Lloyd Bolivia S.R.L.", "Comercial Altiplano SRL", "1023456017"));

        using (var document = Open(receipt))
            document.PageCount.Should().Be(1);
        using (var document = Open(serviceOrder))
            document.PageCount.Should().Be(1);
    }

    [Fact]
    public void Renderer_ShouldBeUsableConcurrently()
    {
        var models = ShipmentDocumentTypes.All.Select(t => ModelFor(t, Data())).ToList();

        var results = models.AsParallel().Select(m => _renderer.Render(m)).ToList();

        results.Should().OnlyContain(r => r.Length > 1000);
    }
}
