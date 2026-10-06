namespace HapagPortal.Application.Assistant;

using System.Text.RegularExpressions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Constants;

/// <summary>
/// Reglas deterministas del asistente sobre el texto normalizado (minúsculas, sin acentos): consultas fuera del
/// alcance (M10-02: recomendaciones comerciales o legales, comparaciones de tarifas históricas), intención del
/// mensaje, referencias escritas por el usuario (BL, booking, factura) y tema para la casilla de derivación.
/// Las palabras clave son raíces: "recomiend" reconoce "recomienda" y "recomiendan".
/// </summary>
public static partial class AssistantIntentRules
{
    private static readonly string[] CommercialAdvice =
    [
        "recomiend", "recomendac", "aconsej", "me conviene", "conviene mas", "que naviera", "mejor naviera",
        "mejor opcion", "negoci", "descuento", "rebaja", "precio especial", "tarifa especial", "should i",
        "recommend", "which carrier", "discount", "negotiat"
    ];

    private static readonly string[] LegalAdvice =
    [
        "legal", "abogad", "demand", "juicio", "litig", "querell", "tribunal", "indemniz", "lawyer", "lawsuit",
        "sue ", "attorney"
    ];

    private static readonly string[] TariffWords = ["tarifa", "precio", "costo", "valor del flete", "tariff", "price", "rate"];

    private static readonly string[] HistoricalWords =
    [
        "historic", "anterior", "ano pasado", "anos anteriores", "mes pasado", "compar", "evolucion", "subio",
        "bajo el precio", "antes costaba", "aumento", "last year", "previous", "history", "increase"
    ];

    private static readonly string[] TatcWords = ["tatc"];

    private static readonly string[] InvoiceWords = ["factura", "invoice", "folio", "nota de credito", "nota de debito"];

    private static readonly string[] DocumentWords =
    [
        "documento", "copia", "certificado", "descarg", "carta", "comprobante", "cupon", "document", "download",
        "certificate"
    ];

    /// <summary>Verbos de entrega (M10-04): el usuario pide que se le entregue o envíe un documento.</summary>
    private static readonly string[] DeliveryWords =
    [
        "envia", "enviame", "envie", "enviar", "mand", "entreg", "descarg", "dame", "deme", "necesito", "quiero", "obtener",
        "send", "give me", "download", "get me"
    ];

    /// <summary>
    /// Tipos de documento que se pueden pedir por su nombre (M10-04), en el orden en que se evalúan: las frases más
    /// específicas primero (la copia no valorada antes que la valorada).
    /// </summary>
    private static readonly (string[] Words, string[] Kinds)[] DocumentKindWords =
    [
        (["no valorada", "sin valor", "non valued", "unvalued", "not valued"], [ShipmentDocumentTypes.BlCopyNonValued]),
        (["valorada", "valued"], [ShipmentDocumentTypes.BlCopyValued]),
        (["transbordo", "transshipment"], [ShipmentDocumentTypes.TransshipmentCertificate]),
        (["libre deuda", "cld", "no debt"], [ShipmentDocumentTypes.NoDebtCertificate]),
        (["certificado de flete", "certificado del flete", "freight certificate"], [ShipmentDocumentTypes.FreightCertificate]),
        (["liberacion", "desconsolid", "release letter"], [ShipmentDocumentTypes.ReleaseLetter]),
        (["responsabilidad", "responsibility"], [ShipmentDocumentTypes.ResponsibilityLetter]),
        (["collect"], [ShipmentDocumentTypes.CollectReceipt]),
        (["anticipo de gate out", "pago anticipado de gate out", "anticipo gate out"], [ShipmentDocumentTypes.GateOutAdvanceReceipt]),
        (["cupon", "coupon"], [ShipmentDocumentTypes.GateOutCoupon]),
        (["boleta", "recibo", "comprobante de pago", "receipt"], [AssistantDeliveryKinds.Receipt]),
        (["factura", "invoice"], [AssistantDeliveryKinds.Invoice]),
    ];

    /// <summary>Términos genéricos: si no se nombró un tipo puntual, abarcan todos los de su familia.</summary>
    private static readonly (string[] Words, string[] Kinds)[] DocumentFamilyWords =
    [
        (["copia", "copy"], [ShipmentDocumentTypes.BlCopyNonValued, ShipmentDocumentTypes.BlCopyValued]),
        (["certificado", "certificate"],
            [ShipmentDocumentTypes.TransshipmentCertificate, ShipmentDocumentTypes.NoDebtCertificate, ShipmentDocumentTypes.FreightCertificate]),
        (["carta", "letter"], [ShipmentDocumentTypes.ResponsibilityLetter, ShipmentDocumentTypes.ReleaseLetter]),
        (["comprobante"], [ShipmentDocumentTypes.CollectReceipt, AssistantDeliveryKinds.Receipt]),
    ];

    private static readonly string[] ChargeWords =
    [
        "pendiente", "por pagar", "deuda", "debo", "adeud", "cargo", "cobro", "saldo", "pending", "owe", "charge", "debt"
    ];

    private static readonly string[] StatusWords =
    [
        "estado", "donde esta", "eta", "llega", "llegada", "arribo", "zarpe", "emision", "embarque", "status", "where is",
        "arrive", "shipment", "nave"
    ];

    private static readonly string[] GreetingWords =
        ["hola", "buenos dias", "buenas tardes", "buenas noches", "gracias", "hello", "hi", "good morning", "thanks"];

    private static readonly (string Topic, string[] Words)[] TopicWords =
    [
        (AssistantTopics.Payments, ["pago", "pagar", "factura", "boleta", "deposito", "transferencia", "khipu", "carro", "payment", "invoice"]),
        (AssistantTopics.Demurrage, ["demurrage", "sobreestadia", "mhd", "demora", "dias libres"]),
        (AssistantTopics.Documentation, ["documento", "certificado", "copia", "carta", "tatc", "cld", "emision", "document"]),
        (AssistantTopics.Shipping, ["almacen", "retiro", "gate", "contenedor", "nave", "embarque", "booking", "container"]),
    ];

    /// <summary>Motivo de rechazo (<c>AssistantRefusalReasons</c>) o nulo si la consulta está dentro del alcance.</summary>
    public static string? RefusalReason(string message)
    {
        var text = Prepare(message);

        if (HasAny(text, LegalAdvice))
            return AssistantRefusalReasons.LegalAdvice;
        if (HasAny(text, TariffWords) && HasAny(text, HistoricalWords))
            return AssistantRefusalReasons.HistoricalTariffs;
        if (HasAny(text, CommercialAdvice))
            return AssistantRefusalReasons.CommercialAdvice;
        return null;
    }

    /// <summary>Intención del mensaje y referencias escritas por el usuario.</summary>
    public static AssistantIntentResult Classify(string message)
    {
        var text = Prepare(message);
        var references = ExtractReferences(message);

        var requested = RequestedDocuments(message);

        string intent;
        if (HasAny(text, TatcWords))
            intent = AssistantIntents.TatcStatus;
        else if (IsDocumentDelivery(text, requested))
            intent = AssistantIntents.DocumentDelivery;
        else if (HasAny(text, InvoiceWords))
            intent = AssistantIntents.InvoiceDetail;
        else if (HasAny(text, DocumentWords))
            intent = AssistantIntents.ShipmentDocuments;
        else if (HasAny(text, ChargeWords))
            intent = AssistantIntents.PendingCharges;
        else if (references.Count > 0 || HasAny(text, StatusWords))
            intent = AssistantIntents.ShipmentStatus;
        else if (HasAny(text, GreetingWords) && SearchText.Words(message).Count <= 4)
            intent = AssistantIntents.Greeting;
        else
            intent = AssistantIntents.Knowledge;

        return new AssistantIntentResult(intent, references);
    }

    /// <summary>
    /// Documentos que el usuario pide por su nombre (M10-04): tipos del repositorio (<c>ShipmentDocumentTypes</c>) y
    /// recibos o facturas (<c>AssistantDeliveryKinds</c>). Vacío si no nombró ninguno.
    /// </summary>
    public static IReadOnlyList<string> RequestedDocuments(string message)
    {
        var text = Prepare(message);
        var kinds = new List<string>();

        // "copia no valorada" no debe pedir también la valorada: la frase específica consume su texto.
        var remaining = text;
        foreach (var (words, types) in DocumentKindWords)
        {
            if (!HasAny(remaining, words))
                continue;

            kinds.AddRange(types);
            foreach (var word in words)
                remaining = remaining.Replace(" " + word, " ", StringComparison.Ordinal);
        }

        if (kinds.Count == 0)
        {
            foreach (var (words, types) in DocumentFamilyWords)
            {
                if (HasAny(text, words))
                    kinds.AddRange(types);
            }
        }

        return kinds.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Pedido de entrega de un documento puntual (M10-04): se nombra un tipo de documento del embarque, o se piden recibos o
    /// facturas con un verbo de entrega ("envíame las facturas del BL ..."). El detalle de una factura sigue siendo una
    /// consulta de factura.
    /// </summary>
    private static bool IsDocumentDelivery(string text, IReadOnlyList<string> requested)
    {
        if (requested.Count == 0)
            return false;

        var onlyBilling = requested.All(k => k is AssistantDeliveryKinds.Receipt or AssistantDeliveryKinds.Invoice);
        return !onlyBilling || HasAny(text, DeliveryWords);
    }

    /// <summary>Tema de la consulta para elegir la casilla de derivación del país.</summary>
    public static string Topic(string message)
    {
        var text = Prepare(message);
        foreach (var (topic, words) in TopicWords)
        {
            if (HasAny(text, words))
                return topic;
        }

        return AssistantTopics.General;
    }

    /// <summary>
    /// Referencias escritas por el usuario: códigos de 5 a 30 caracteres con al menos un dígito (BL, booking,
    /// número de factura) o folios numéricos. Se devuelven en mayúsculas y sin repetir.
    /// </summary>
    public static IReadOnlyList<string> ExtractReferences(string message) =>
        ReferencePattern().Matches(message.ToUpperInvariant())
            .Select(m => m.Value.Trim('-'))
            .Where(v => v.Length >= 5 && v.Any(char.IsDigit) && !IsYear(v))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static bool IsYear(string value) => value.Length == 4 && value.All(char.IsDigit);

    /// <summary>Palabras normalizadas separadas por un espacio, con espacios en los extremos.</summary>
    private static string Prepare(string message) => " " + string.Join(' ', SearchText.Words(message)) + " ";

    /// <summary>Raíces de una palabra: el texto contiene una palabra que empieza con la raíz; frases: el texto la contiene.</summary>
    private static bool HasAny(string text, IEnumerable<string> keywords) =>
        keywords.Any(k => k.Contains(' ')
            ? text.Contains(" " + k.Trim() + " ", StringComparison.Ordinal) || text.Contains(" " + k, StringComparison.Ordinal)
            : text.Contains(" " + k, StringComparison.Ordinal));

    [GeneratedRegex(@"(?<![A-Z0-9-])[A-Z0-9][A-Z0-9-]{3,29}(?![A-Z0-9-])")]
    private static partial Regex ReferencePattern();
}
