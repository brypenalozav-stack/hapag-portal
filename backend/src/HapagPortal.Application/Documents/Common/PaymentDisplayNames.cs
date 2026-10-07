namespace HapagPortal.Application.Documents.Common;

using System.Globalization;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;

/// <summary>
/// Textos en español de los códigos de un pago para los documentos: estado, medio de pago y concepto. Los documentos
/// nunca imprimen el código crudo: primero se usa el nombre configurado (mantenedor de medios, catálogo de conceptos),
/// luego estos nombres y, por último, el código legible.
/// </summary>
public static class PaymentDisplayNames
{
    private static readonly Dictionary<string, string> Methods = new(StringComparer.OrdinalIgnoreCase)
    {
        [PaymentMethodCodes.Khipu] = "Khipu",
        [PaymentMethodCodes.BankButtonBancoChile] = "Botón de pago Banco de Chile",
        [PaymentMethodCodes.BankButtonSantander] = "Botón de pago Santander",
        [PaymentMethodCodes.BankButtonBci] = "Botón de pago Bci",
        [PaymentMethodCodes.Deposit] = "Depósito bancario",
        [PaymentMethodCodes.DigitalUsd] = "Dólares digitales",
        [PaymentMethodCodes.CreditLine] = "Línea de crédito",
        [PaymentMethods.CreditCard] = "Tarjeta de crédito",
        [PaymentMethods.DebitCard] = "Tarjeta de débito",
        [PaymentMethods.BankTransfer] = "Transferencia bancaria",
        [PaymentMethods.WebPay] = "Webpay",
        [PaymentMethods.Cash] = "Efectivo",
        [PaymentMethods.Check] = "Cheque",
        [PaymentMethods.BankButton] = "Botón de pago",
        [PaymentMethods.BCI] = "Bci"
    };

    private static readonly Dictionary<string, string> Concepts = new(StringComparer.OrdinalIgnoreCase)
    {
        [ChargeConceptCodes.GateIn] = "Gate In",
        [ChargeConceptCodes.Eds] = "EDS",
        [ChargeConceptCodes.GateOut] = "Gate Out",
        [ChargeConceptCodes.Ipo] = "Recargo IPO",
        [ChargeConceptCodes.Mhd] = "MHD",
        [ChargeConceptCodes.Demurrage] = "Demurrage (sobreestadía)",
        [ChargeConceptCodes.WarehouseChange] = "Cambio de almacén",
        [ChargeConceptCodes.AdvanceDemurrageBo] = "Demoras anticipadas",
        [ChargeConceptCodes.LateArrival] = "Late Arrival",
        [ChargeConceptCodes.TransshipmentCertificate] = "Certificado de transbordo",
        [ChargeConceptCodes.SealManagement] = "Gestión de sellos",
        [ChargeConceptCodes.EarlyArrival] = "Early Arrival",
        [ChargeConceptCodes.DropOff] = "Drop Off",
        [ChargeConceptCodes.ContainerAdministrationXom] = "Administración de contenedores (XOM)",
        [ChargeConceptCodes.BlCorrection] = "Corrección de BL",
        [ChargeConceptCodes.BlHouseTransmission] = "Transmisión de BL house",
        [ChargeConceptCodes.MatrixLate] = "Matriz fuera de plazo",
        [ChargeConceptCodes.Opening] = "Apertura",
        [ChargeConceptCodes.Valuation] = "Valorización",
        [ChargeConceptCodes.Reinvoicing] = "Refacturación",
        [ChargeConceptCodes.VatLoss] = "Pérdida de IVA",
        [ChargeConceptCodes.FreightCertificate] = "Certificado de flete",
        [ChargeConceptCodes.ReleaseLetter] = "Carta de liberación",
        [ChargeConceptCodes.Thc] = "Terminal Handling Charge",
        [ChargeConceptCodes.ThcReefer] = "Terminal Handling Charge Reefer",
        [ChargeConceptCodes.BlFee] = "Emisión de BL",
        [ChargeConceptCodes.Isps] = "Recargo de seguridad ISPS",
        [ChargeConceptCodes.TransitFee] = "Documentación de tránsito Bolivia",
        [PaymentConcepts.Freight] = "Flete",
        [PaymentConcepts.Invoice] = "Factura"
    };

    /// <summary>Estado legible y tono de la insignia. Una boleta pendiente dice «Pendiente de verificación».</summary>
    public static (string Label, string Tone) Status(string status) => status switch
    {
        PaymentStatus.Confirmed => ("Pagado", PdfTones.Success),
        PaymentStatus.PendingVerification => ("Pendiente de verificación", PdfTones.Warning),
        PaymentStatus.Processing => ("En proceso", PdfTones.Info),
        PaymentStatus.Pending => ("Pendiente", PdfTones.Neutral),
        PaymentStatus.Failed => ("Rechazado", PdfTones.Danger),
        PaymentStatus.Cancelled => ("Anulado", PdfTones.Danger),
        _ => (Readable(status), PdfTones.Neutral)
    };

    /// <summary>Nombre del medio: el configurado en el mantenedor (M5-03) o el nombre conocido del código.</summary>
    public static string Method(string? code, string? configuredName = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredName))
            return configuredName.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return "-";
        return Methods.TryGetValue(code, out var name) ? name : Readable(code);
    }

    /// <summary>Nombre del concepto: el del catálogo de cargos, el conocido del código, la descripción o el código legible.</summary>
    public static string Concept(string code, IReadOnlyDictionary<string, string>? catalog = null, string? description = null)
    {
        if (catalog is not null && catalog.TryGetValue(code, out var configured) && !string.IsNullOrWhiteSpace(configured))
            return configured.Trim();
        if (Concepts.TryGetValue(code, out var known))
            return known;
        return string.IsNullOrWhiteSpace(description) ? Readable(code) : description.Trim();
    }

    /// <summary><c>SOME_CODE</c> o <c>SomeCode</c> como «Some code».</summary>
    private static string Readable(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "-";

        var spaced = code.Contains('_', StringComparison.Ordinal)
            ? code.Replace('_', ' ').ToLowerInvariant()
            : string.Concat(code.Select((c, i) => i > 0 && char.IsUpper(c) && !char.IsUpper(code[i - 1]) ? " " + char.ToLowerInvariant(c) : c.ToString()));
        return char.ToUpper(spaced[0], CultureInfo.InvariantCulture) + spaced[1..];
    }
}
