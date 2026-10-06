namespace HapagPortal.Application.Documents.Common;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;

/// <summary>Datos del embarque con que se arman los documentos: BL, partes, unidades, mercancía y cargos.</summary>
public sealed record ShipmentDocumentData(
    BillOfLading BillOfLading,
    IReadOnlyList<BLParty> Parties,
    IReadOnlyList<BLContainer> Containers,
    IReadOnlyList<BLCargoItem> CargoItems,
    IReadOnlyList<LocalCharge> Charges);

/// <summary>Encabezado común: número, fecha de emisión (UTC), código de verificación y emisor.</summary>
public sealed record DocumentHeader(string Number, DateTime IssuedAt, string VerificationCode, string Issuer);

/// <summary>Datos que el cliente ingresa en la carta de responsabilidad (M6-06).</summary>
public sealed record ResponsibilityLetterData(
    string OrganizationName,
    string OrganizationTaxId,
    string SignatoryName,
    string SignatoryTaxId,
    string SignatoryPosition,
    string ContactEmail,
    string? ContactPhone,
    string? CargoDescription,
    string? Observations,
    string TermsVersion,
    DateTime AcceptedAt);

/// <summary>
/// Plantillas de los documentos del portal (M6): cada método arma el modelo de un tipo a partir de los
/// datos del embarque, sin acceso a datos ni dependencias, para que el resultado sea reproducible. Los
/// montos usan separador de miles "." y decimal ","; las fechas, el huso del país (NF-22).
/// </summary>
public static class ShipmentDocumentTemplates
{
    private const string ShipperRole = "Shipper";
    private const string ConsigneeRole = "Consignee";
    private const string NotifyRole = "NotifyParty";

    private static readonly NumberFormatInfo Numbers = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 2
    };

    /// <summary>Código de verificación impreso: deriva del número, el BL y el instante de emisión.</summary>
    public static string VerificationCode(string number, string blNumber, DateTime issuedAt)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{number}|{blNumber}|{issuedAt.ToUniversalTime():O}")));
        return $"{hash[..4]}-{hash[4..8]}-{hash[8..12]}-{hash[12..16]}";
    }

    public static string FileName(string documentType, string number) => documentType switch
    {
        ShipmentDocumentTypes.TransshipmentCertificate => $"certificado-transbordo-{number}.pdf",
        ShipmentDocumentTypes.GateOutCoupon => $"cupon-retiro-{number}.pdf",
        ShipmentDocumentTypes.CollectReceipt => $"comprobante-collect-{number}.pdf",
        ShipmentDocumentTypes.BlCopyValued => $"copia-bl-valorada-{number}.pdf",
        ShipmentDocumentTypes.BlCopyNonValued => $"copia-bl-no-valorada-{number}.pdf",
        ShipmentDocumentTypes.ResponsibilityLetter => $"carta-responsabilidad-{number}.pdf",
        ShipmentDocumentTypes.NoDebtCertificate => $"certificado-libre-deuda-{number}.pdf",
        ShipmentDocumentTypes.GateOutAdvanceReceipt => $"recibo-anticipo-gate-out-{number}.pdf",
        _ => $"{number}.pdf"
    };

    /// <summary>Certificado de transbordo (M6-01), firmado, tras el pago del servicio.</summary>
    public static PdfDocumentModel TransshipmentCertificate(
        ShipmentDocumentData data, DocumentHeader header, string clientName, string? clientTaxId, string? paymentReference)
    {
        var bl = data.BillOfLading;
        var operation = IsImport(bl) ? "importación" : "exportación";

        return Model(
            "Certificado de transbordo",
            $"Operación de {operation}",
            data,
            header,
            [
                Parties(data),
                new PdfSection("Cliente", [new("Razón social", clientName), new("RUT / NIT", clientTaxId), new("Pago", paymentReference)]),
                Units(data.Containers),
                new PdfSection("Certificación", Paragraphs:
                [
                    $"{header.Issuer} certifica que la carga amparada en el BL {bl.BLNumber}, transportada en la nave " +
                    $"{bl.Vessel ?? "-"} viaje {bl.Voyage ?? "-"}, con origen en {bl.PortOfLoading ?? "-"} y destino " +
                    $"{bl.PlaceOfDelivery ?? bl.PortOfDischarge ?? "-"}, fue objeto de transbordo en el puerto de " +
                    $"{bl.PortOfDischarge ?? "-"} en las unidades individualizadas en este documento.",
                    "Se emite a solicitud del interesado para los fines que estime convenientes."
                ])
            ],
            signatureNote: "Documento firmado electrónicamente. La validez de la firma se acredita según el mecanismo publicado por Hapag-Lloyd.");
    }

    /// <summary>Cupón de retiro de Gate Out (M6-03): se presenta en el depósito para retirar las unidades pagadas.</summary>
    public static PdfDocumentModel GateOutCoupon(
        ShipmentDocumentData data, DocumentHeader header, string clientName, string? paymentReference, string? chargeDescription)
    {
        return Model(
            "Cupón de retiro - Gate Out",
            "Presentar en el depósito para el retiro de las unidades",
            data,
            header,
            [
                new PdfSection("Titular del cupón", [new("Cliente", clientName), new("Pago", paymentReference), new("Servicio", chargeDescription)]),
                Units(data.Containers),
                new PdfSection("Condiciones", Paragraphs:
                [
                    "El cupón es válido para las unidades indicadas y solo una vez por unidad.",
                    "El depósito puede verificar la emisión con el código de verificación impreso al pie."
                ])
            ],
            signatureNote: null);
    }

    /// <summary>
    /// Recibo del pago anticipado de Gate Out (M3-19): en exportación la factura se emite tras el zarpe, por lo que
    /// el pago de la agencia de aduanas (u otro pagador) queda respaldado con este recibo, que identifica el
    /// embarque, las unidades y el pagador. La factura posterior se presenta vinculada a él.
    /// </summary>
    public static PdfDocumentModel GateOutAdvanceReceipt(
        ShipmentDocumentData data,
        DocumentHeader header,
        string payerName,
        string? payerTaxId,
        string paymentNumber,
        string? receiptNumber,
        string? method,
        PaymentDetail detail,
        DateTime? paidAt)
    {
        var bl = data.BillOfLading;

        return Model(
            "Recibo de pago anticipado - Gate Out",
            "Pago recibido antes de la emisión de la factura",
            data,
            header,
            [
                new PdfSection("Pagador", [new("Razón social", payerName), new("RUT / NIT", payerTaxId)]),
                new PdfSection("Pago",
                [
                    new("Concepto", detail.Description ?? detail.ConceptType),
                    new("Monto del cargo", $"{Money(detail.OriginalAmount ?? detail.Amount + detail.TaxAmount)} {detail.OriginalCurrency ?? detail.Currency}"),
                    new("Monto pagado", $"{Money(detail.Amount + detail.TaxAmount)} {detail.Currency}"),
                    new("Tipo de cambio", detail.ExchangeRate is null ? null : detail.ExchangeRate.Value.ToString("0.######", CultureInfo.InvariantCulture)),
                    new("RUT de facturación", detail.BillingTaxId is null ? null : $"{detail.BillingName} ({detail.BillingTaxId})"),
                    new("Pago", paymentNumber),
                    new("Comprobante de pago", receiptNumber),
                    new("Medio de pago", method),
                    new("Fecha de pago", paidAt is null ? null : LocalDateTime(bl.Country, paidAt.Value))
                ]),
                Units(data.Containers),
                new PdfSection("Vinculación con la factura", Paragraphs:
                [
                    "La factura del Gate Out se emite después del zarpe de la nave y se vincula a este recibo.",
                    "El cargo pagado con este recibo no vuelve a cobrarse al cliente."
                ])
            ],
            signatureNote: null);
    }

    /// <summary>Comprobante del flete Collect pagado (M6-04), visible solo para las agencias de aduanas autorizadas.</summary>
    public static PdfDocumentModel CollectReceipt(
        ShipmentDocumentData data,
        DocumentHeader header,
        string payerName,
        string? payerTaxId,
        string? paymentReference,
        string? receiptNumber,
        decimal amount,
        string currency,
        DateTime? paidAt)
    {
        var bl = data.BillOfLading;

        return Model(
            "Comprobante de flete Collect",
            null,
            data,
            header,
            [
                Parties(data),
                new PdfSection("Pago del flete",
                [
                    new("Condición del flete", bl.FreightTerms ?? "Collect"),
                    new("Monto pagado", $"{Money(amount)} {currency}"),
                    new("Pagador", payerName),
                    new("RUT / NIT del pagador", payerTaxId),
                    new("Pago", paymentReference),
                    new("Comprobante de pago", receiptNumber),
                    new("Fecha de pago", paidAt is null ? null : LocalDateTime(bl.Country, paidAt.Value))
                ]),
                Units(data.Containers)
            ],
            signatureNote: null);
    }

    /// <summary>
    /// Copia del BL (M6-05). La valorada incluye los valores comerciales (flete y cargos); la no valorada
    /// los excluye y lo declara.
    /// </summary>
    public static PdfDocumentModel BlCopy(ShipmentDocumentData data, DocumentHeader header, bool valued, string requestedBy)
    {
        var bl = data.BillOfLading;
        var sections = new List<PdfSection>
        {
            Parties(data),
            new("Transporte",
            [
                new("Nave / viaje", $"{bl.Vessel ?? "-"} / {bl.Voyage ?? "-"}"),
                new("Puerto de carga", bl.PortOfLoading),
                new("Puerto de descarga", bl.PortOfDischarge),
                new("Lugar de entrega", bl.PlaceOfDelivery),
                new("ETD", bl.ETD is null ? null : ScheduleDate(bl.ETD.Value)),
                new("ETA", bl.ETA is null ? null : ScheduleDate(bl.ETA.Value)),
                new("Tipo de BL", bl.BLType),
                new("Incoterm", bl.Incoterm)
            ]),
            Units(data.Containers),
            Cargo(data.CargoItems)
        };

        if (valued)
        {
            sections.Add(new PdfSection("Valores comerciales",
            [
                new("Condición del flete", bl.FreightTerms),
                new("Flete", $"{Money(bl.FreightAmount)} {bl.FreightCurrency}")
            ],
            data.Charges.Count == 0
                ? null
                : new PdfTable(
                    ["Concepto", "Descripción", "Monto", "Impuesto", "Total", "Moneda", "Estado"],
                    data.Charges
                        .OrderBy(c => c.ChargeType, StringComparer.Ordinal)
                        .Select(c => (IReadOnlyList<string>)
                        [
                            c.ChargeType, c.Description ?? "-", Money(c.Amount), Money(c.TaxAmount), Money(c.TotalAmount), c.Currency, c.Status
                        ])
                        .ToList(),
                    [2, 3, 4])));
        }
        else
        {
            sections.Add(new PdfSection("Valores comerciales", Paragraphs:
            [
                "Copia no valorada: no incluye el flete ni los cargos del embarque."
            ]));
        }

        sections.Add(new PdfSection("Solicitud", [new("Solicitada por", requestedBy)]));

        return Model(
            valued ? "Copia de BL - valorada" : "Copia de BL - no valorada",
            "Copia informativa, no negociable",
            data,
            header,
            sections,
            signatureNote: null);
    }

    /// <summary>Carta de responsabilidad (M6-06) con los datos ingresados y la aceptación de los términos.</summary>
    public static PdfDocumentModel ResponsibilityLetter(ShipmentDocumentData data, DocumentHeader header, ResponsibilityLetterData letter)
    {
        var bl = data.BillOfLading;

        return Model(
            ResponsibilityLetterTerms.Title,
            $"Términos {letter.TermsVersion}",
            data,
            header,
            [
                new PdfSection("Organización responsable",
                [
                    new("Razón social", letter.OrganizationName),
                    new("RUT / NIT", letter.OrganizationTaxId)
                ]),
                new PdfSection("Firmante",
                [
                    new("Nombre", letter.SignatoryName),
                    new("Documento de identidad", letter.SignatoryTaxId),
                    new("Cargo", letter.SignatoryPosition),
                    new("Correo de contacto", letter.ContactEmail),
                    new("Teléfono", letter.ContactPhone)
                ]),
                Units(data.Containers),
                new PdfSection("Declaración",
                    [
                        new("Mercancía", letter.CargoDescription ?? data.CargoItems.FirstOrDefault()?.Description),
                        new("Observaciones", letter.Observations)
                    ],
                    Paragraphs:
                    [
                        ResponsibilityLetterTerms.Text,
                        $"Términos aceptados en el portal el {LocalDateTime(bl.Country, letter.AcceptedAt)} (versión {letter.TermsVersion})."
                    ])
            ],
            signatureNote: null);
    }

    /// <summary>Certificado de libre deuda (M6-07), firmado, para importación de Bolivia.</summary>
    public static PdfDocumentModel NoDebtCertificate(ShipmentDocumentData data, DocumentHeader header, string clientName, string? clientTaxId)
    {
        var bl = data.BillOfLading;

        return Model(
            "Certificado de libre deuda",
            "Importación - Bolivia",
            data,
            header,
            [
                Parties(data),
                new PdfSection("Cliente", [new("Razón social", clientName), new("RUT / NIT", clientTaxId)]),
                Units(data.Containers),
                new PdfSection("Certificación", Paragraphs:
                [
                    $"{header.Issuer} certifica que, a la fecha de emisión, el embarque amparado en el BL {bl.BLNumber} " +
                    "no registra deuda pendiente con Hapag-Lloyd por cargos locales, demurrage, facturas ni flete, " +
                    "y que las demoras anticipadas exigidas a la cuenta, cuando corresponden, se encuentran pagadas.",
                    "Este certificado no constituye liberación de la carga ante otras entidades."
                ])
            ],
            signatureNote: "Documento firmado electrónicamente. La validez de la firma se acredita según el mecanismo publicado por Hapag-Lloyd.");
    }

    /// <summary>Comprobante (recibo) de un pago confirmado o boleta de depósito (M7-02).</summary>
    public static PdfDocumentModel PaymentReceipt(
        Payment payment,
        IReadOnlyList<PaymentDetail> details,
        string issuer,
        string number,
        string payerName,
        string? payerTaxId)
    {
        var isReceipt = payment.ReceiptNumber is not null && number == payment.ReceiptNumber;
        var issuedAt = isReceipt ? payment.ConfirmedAt ?? payment.PaymentDate : payment.SlipIssuedAt ?? payment.PaymentDate;

        var rows = details.Select(d => (IReadOnlyList<string>)
            [
                d.BlNumber ?? "-",
                d.ConceptType,
                d.Description ?? "-",
                d.BillingTaxId ?? "-",
                Money(d.Amount),
                Money(d.TaxAmount),
                Money(d.Amount + d.TaxAmount)
            ])
            .ToList();

        return new PdfDocumentModel(
            isReceipt ? "Comprobante de pago" : "Boleta de depósito",
            isReceipt ? null : "Pago pendiente de verificación por Finanzas",
            issuer,
            IssuerDetail(payment.Country),
            number,
            issuedAt,
            BusinessCalendar.TimeZoneId(payment.Country),
            [
                new("Pago", payment.PaymentNumber),
                new("Estado", payment.Status),
                new("Medio de pago", payment.PaymentMethodCode ?? payment.PaymentMethod),
                new("Referencia", payment.ExternalReference),
                new("Moneda", payment.Currency)
            ],
            [
                new PdfSection("Pagador", [new("Razón social", payerName), new("RUT / NIT", payerTaxId)]),
                new PdfSection("Detalle", Table: new PdfTable(
                    ["BL", "Concepto", "Descripción", "RUT facturación", "Neto", "Impuesto", "Total"], rows, [4, 5, 6])),
                new PdfSection("Totales",
                [
                    new("Neto", $"{Money(payment.Amount)} {payment.Currency}"),
                    new("Impuesto", $"{Money(payment.TaxAmount)} {payment.Currency}"),
                    new("Total", $"{Money(payment.TotalAmount)} {payment.Currency}")
                ])
            ],
            VerificationCode(number, payment.PaymentNumber, issuedAt),
            null,
            Footer());
    }

    /// <summary>Orden de servicio solicitada en el portal.</summary>
    public static PdfDocumentModel ServiceOrder(ServiceOrder order, BillOfLading? bl, string issuer, string clientName, string? clientTaxId)
    {
        return new PdfDocumentModel(
            "Orden de servicio",
            order.OrderType,
            issuer,
            IssuerDetail(order.Country),
            order.OrderNumber,
            order.RequestedAt,
            BusinessCalendar.TimeZoneId(order.Country),
            [
                new("BL", bl?.BLNumber),
                new("Booking", bl?.BookingNumber),
                new("Estado", order.Status)
            ],
            [
                new PdfSection("Cliente", [new("Razón social", clientName), new("RUT / NIT", clientTaxId)]),
                new PdfSection("Servicio",
                [
                    new("Tipo", order.OrderType),
                    new("Descripción", order.Description),
                    new("Solicitada", LocalDateTime(order.Country, order.RequestedAt)),
                    new("Completada", order.CompletedAt is null ? null : LocalDateTime(order.Country, order.CompletedAt.Value))
                ])
            ],
            VerificationCode(order.OrderNumber, bl?.BLNumber ?? "-", order.RequestedAt),
            null,
            Footer());
    }

    public static string Money(decimal amount) => amount.ToString("N2", Numbers);

    public static string LocalDateTime(string country, DateTime utc) =>
        $"{BusinessCalendar.ToLocal(country, utc):dd-MM-yyyy HH:mm} ({BusinessCalendar.TimeZoneId(country)})";

    /// <summary>ETD/ETA son fechas de itinerario guardadas a medianoche UTC: se muestran sin convertir de huso.</summary>
    private static string ScheduleDate(DateTime utc) =>
        utc.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    private static PdfDocumentModel Model(
        string title,
        string? subtitle,
        ShipmentDocumentData data,
        DocumentHeader header,
        IReadOnlyList<PdfSection> sections,
        string? signatureNote)
    {
        var bl = data.BillOfLading;

        return new PdfDocumentModel(
            title,
            subtitle,
            header.Issuer,
            IssuerDetail(bl.Country),
            header.Number,
            header.IssuedAt,
            BusinessCalendar.TimeZoneId(bl.Country),
            [
                new("BL", bl.BLNumber),
                new("Booking", bl.BookingNumber),
                new("Operación", IsImport(bl) ? "Importación" : "Exportación"),
                new("País", bl.Country),
                new("Nave / viaje", $"{bl.Vessel ?? "-"} / {bl.Voyage ?? "-"}"),
                new("Ruta", $"{bl.PortOfLoading ?? "-"} - {bl.PortOfDischarge ?? "-"}")
            ],
            sections,
            header.VerificationCode,
            signatureNote,
            Footer());
    }

    private static PdfSection Parties(ShipmentDocumentData data)
    {
        var bl = data.BillOfLading;

        string? Party(string role, string? fallback)
        {
            var party = data.Parties.FirstOrDefault(p => p.Role == role);
            if (party is null)
                return fallback;

            return party.TaxId is null ? party.Name : $"{party.Name} ({TaxIdNormalizer.Normalize(party.TaxId)})";
        }

        return new PdfSection("Partes",
        [
            new("Shipper", Party(ShipperRole, bl.Shipper)),
            new("Consignee", Party(ConsigneeRole, bl.Consignee)),
            new("Notify", Party(NotifyRole, bl.NotifyParty))
        ]);
    }

    private static PdfSection Units(IReadOnlyList<BLContainer> containers) =>
        containers.Count == 0
            ? new PdfSection("Unidades", Paragraphs: ["El BL no registra contenedores."])
            : new PdfSection("Unidades", Table: new PdfTable(
                ["Contenedor", "Tipo", "Sello", "Peso (kg)", "Estado"],
                containers
                    .OrderBy(c => c.ContainerNumber, StringComparer.Ordinal)
                    .Select(c => (IReadOnlyList<string>)
                    [
                        c.ContainerNumber, c.ContainerType, c.SealNumber ?? "-",
                        c.Weight is null ? "-" : Money(c.Weight.Value), c.Status
                    ])
                    .ToList(),
                [3]));

    private static PdfSection Cargo(IReadOnlyList<BLCargoItem> items) =>
        items.Count == 0
            ? new PdfSection("Mercancía", Paragraphs: ["Sin detalle de mercancía registrado."])
            : new PdfSection("Mercancía", Table: new PdfTable(
                ["HS", "Descripción", "Bultos", "Peso bruto (kg)", "Volumen (m3)"],
                items.Select(i => (IReadOnlyList<string>)
                    [
                        i.HsCode ?? "-", i.Description ?? "-",
                        i.PackageCount?.ToString(CultureInfo.InvariantCulture) ?? "-",
                        i.GrossWeight is null ? "-" : Money(i.GrossWeight.Value),
                        i.Volume is null ? "-" : Money(i.Volume.Value)
                    ])
                    .ToList(),
                [2, 3, 4]));

    private static bool IsImport(BillOfLading bl) =>
        string.Equals(bl.ShipmentType, "Import", StringComparison.OrdinalIgnoreCase);

    private static string IssuerDetail(string country) =>
        country == CountryCodes.Bolivia
            ? "Agente de Hapag-Lloyd AG - Operación Bolivia"
            : "Agente de Hapag-Lloyd AG - Operación Chile";

    private static string Footer() =>
        "Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisión con el código indicado.";
}
