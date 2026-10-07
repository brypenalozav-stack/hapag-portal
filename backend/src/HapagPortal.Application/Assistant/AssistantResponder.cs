namespace HapagPortal.Application.Assistant;

using System.Text.RegularExpressions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Config.Features;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Respuesta final: la armada por el portal, el texto entregado, el motor que lo redactó y la casilla de derivación.</summary>
public sealed record AssistantResponse(
    AssistantAnswer Answer,
    string Text,
    string Engine,
    bool EngineFallback,
    string? MailboxEmail);

/// <summary>
/// Orquesta la respuesta del asistente (M10-01 a M10-03):
/// 1) las consultas fuera del alcance (recomendación comercial o legal, comparación de tarifas históricas) se
///    rechazan con reglas, antes de cualquier motor, y se derivan a la casilla del país;
/// 2) el motor configurado clasifica la intención (solo recibe el texto del usuario); las referencias (BL,
///    booking, factura) se toman siempre del texto escrito por el usuario;
/// 3) los datos se obtienen con las consultas del portal y los permisos del usuario (<see cref="AssistantDataRetriever"/>)
///    o de la base de conocimiento del país (M10-02); sin respuesta, se deriva sin elaborar una propia;
/// 4) solo las respuestas de datos pueden pasar por la redacción del motor, que recibe únicamente esos hechos y
///    se descarta si menciona cifras o códigos que no estén en ellos.
/// Si un motor distinto de Rules falla o no responde, se responde con Rules (<c>EngineFallback</c>).
/// </summary>
public sealed partial class AssistantResponder(
    IApplicationDbContext dbContext,
    IAssistantEngine engine,
    RulesAssistantEngine rules,
    AssistantDataRetriever retriever,
    FeatureSettings features)
{
    private const int MaxComposedLength = 4000;

    public Task<AssistantResponse> RespondAsync(AssistantSession session, string message, CancellationToken cancellationToken) =>
        RespondAsync(session, message, Guid.NewGuid(), cancellationToken);

    /// <summary>
    /// Respuesta a un mensaje. <paramref name="replyMessageId"/> es el identificador que tendrá el mensaje de respuesta: las
    /// entregas de documentos (M10-04) quedan asociadas a él.
    /// </summary>
    public async Task<AssistantResponse> RespondAsync(
        AssistantSession session,
        string message,
        Guid replyMessageId,
        CancellationToken cancellationToken)
    {
        var refusal = AssistantIntentRules.RefusalReason(message);
        if (refusal is not null)
        {
            var refusalTopic = refusal == AssistantRefusalReasons.LegalAdvice ? AssistantTopics.General : AssistantTopics.Commercial;
            var refusalMailbox = await MailboxAsync(session.Country, refusalTopic, cancellationToken);
            var refused = Refused(refusal, refusalTopic, refusalMailbox);
            return new AssistantResponse(refused, refused.Text, rules.Name, EngineFallback: false, refusalMailbox);
        }

        var (intent, fallback) = await ClassifyAsync(message, cancellationToken);
        var topic = AssistantIntentRules.Topic(message);

        AssistantAnswer answer;
        if (intent.Intent == AssistantIntents.Greeting)
        {
            answer = Greeting();
        }
        else if (AssistantIntents.DataIntents.Contains(intent.Intent)
            && (intent.References.Count > 0 || intent.Intent == AssistantIntents.PendingCharges))
        {
            // Sin la entrega de documentos (M10-04, apagada en el cierre de Fase 1) el pedido se responde con el listado de
            // documentos del embarque, sin enlaces de descarga.
            answer = intent.Intent == AssistantIntents.DocumentDelivery && features.IsEnabled(FeatureNames.AssistantDelivery)
                ? await retriever.DeliverDocumentsAsync(
                    new AssistantDeliveryRequest(message, intent.References, topic, session.Id, replyMessageId, session.UserEmail),
                    cancellationToken)
                : await retriever.AnswerAsync(intent.Intent, intent.References, topic, cancellationToken);
        }
        else
        {
            // Conocimiento, o consulta de datos sin referencia: primero la base de conocimiento del país.
            var articles = await dbContext.KnowledgeArticles.AsNoTracking()
                .Where(a => a.Country == session.Country && a.IsActive)
                .ToListAsync(cancellationToken);
            var article = KnowledgeSearch.FindBest(articles, message);

            answer = article is not null
                ? Knowledge(article)
                : AssistantIntents.DataIntents.Contains(intent.Intent)
                    ? NeedsReference(intent.Intent, topic)
                    : NoAnswer(topic);
        }

        string? mailbox = null;
        if (answer.AnswerType is AssistantAnswerTypes.NoAnswer or AssistantAnswerTypes.NotAvailable or AssistantAnswerTypes.SourceUnavailable)
        {
            mailbox = await MailboxAsync(session.Country, answer.Topic, cancellationToken);
            answer = WithMailbox(answer, mailbox);
        }

        // Si el motor ya falló al clasificar no se le pide redactar: se evita una segunda espera (NF-18).
        var text = answer.Text;
        var usedEngine = false;
        if (answer.Composable && engine.Name != rules.Name && !fallback)
        {
            try
            {
                var composed = await engine.ComposeAsync(
                    new AssistantCompositionInput(answer.Intent, message, answer.Text, answer.Facts), cancellationToken);
                if (composed.IsSuccess && IsFaithful(composed.Value, answer))
                {
                    text = composed.Value.Trim();
                    usedEngine = true;
                }
                else
                {
                    fallback = true;
                }
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                fallback = true;
            }
        }

        var engineName = engine.Name != rules.Name && !fallback && (usedEngine || !answer.Composable) ? engine.Name : rules.Name;
        return new AssistantResponse(answer, text, engineName, fallback, mailbox);
    }

    public static string Welcome(string country) =>
        $"Hola, soy el asistente del portal de Hapag-Lloyd {(country == CountryCodes.Bolivia ? "Bolivia" : "Chile")}. " +
        "Respondo consultas sobre procesos y procedimientos, y sobre el estado de sus embarques, documentos, cargos " +
        "pendientes, facturas y TATC, y le entrego los documentos disponibles de sus embarques, según los permisos de su " +
        "usuario. No entrego recomendaciones comerciales ni " +
        "legales ni comparaciones de tarifas históricas. Mis respuestas son informativas y no reemplazan las " +
        "solicitudes formales del portal.";

    /// <summary>
    /// La redacción del motor solo se acepta si toda cifra o código que menciona (palabras con dígitos) aparece en
    /// la respuesta armada por el portal: el motor no puede agregar ni inferir datos (M10-03).
    /// </summary>
    public static bool IsFaithful(string composed, AssistantAnswer answer)
    {
        if (string.IsNullOrWhiteSpace(composed) || composed.Length > MaxComposedLength)
            return false;

        var allowed = answer.Text + "\n" + string.Join("\n", answer.Facts.Select(f => $"{f.Label}: {f.Value}"));
        return DataToken().Matches(composed)
            .Select(m => m.Value.TrimEnd('.', ',', ':', ';', ')'))
            .All(token => allowed.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<(AssistantIntentResult Intent, bool Fallback)> ClassifyAsync(
        string message,
        CancellationToken cancellationToken)
    {
        // Las referencias salen siempre del texto del usuario: el motor no puede introducir otro BL.
        var byRules = AssistantIntentRules.Classify(message);
        if (engine.Name == rules.Name)
            return (byRules, false);

        try
        {
            var classified = await engine.ClassifyAsync(
                new AssistantClassificationInput(message, AssistantIntents.Classifiable), cancellationToken);
            if (classified.IsSuccess && AssistantIntents.Classifiable.Contains(classified.Value.Intent))
                return (byRules with { Intent = classified.Value.Intent }, false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Motor caído o sin respuesta: se clasifica con reglas.
        }

        return (byRules, true);
    }

    private async Task<string?> MailboxAsync(string country, string topic, CancellationToken cancellationToken)
    {
        var mailboxes = await dbContext.AssistantMailboxes.AsNoTracking()
            .Where(m => m.Country == country && m.IsActive && (m.Topic == topic || m.Topic == AssistantTopics.General))
            .ToListAsync(cancellationToken);

        return (mailboxes.FirstOrDefault(m => m.Topic == topic) ?? mailboxes.FirstOrDefault(m => m.Topic == AssistantTopics.General))?.Email;
    }

    private static AssistantAnswer Greeting() => new(
        AssistantIntents.Greeting,
        AssistantAnswerTypes.Greeting,
        "Hola. Puede consultarme por un BL, booking o factura (por ejemplo, \"estado del BL HLCU...\" o \"documentos del BL HLCU...\"), " +
        "por sus cargos pendientes o por los procesos del portal.",
        [], [], [], AssistantTopics.General);

    private static AssistantAnswer Knowledge(KnowledgeArticle article) => new(
        AssistantIntents.Knowledge,
        AssistantAnswerTypes.Knowledge,
        $"{article.Title}\n{article.Content}",
        [],
        [new AssistantCitationDto(AssistantReferences.KnowledgeArticle, article.Id.ToString(), article.Title, null)],
        [],
        article.Topic);

    private static AssistantAnswer NeedsReference(string intent, string topic)
    {
        var subject = intent switch
        {
            AssistantIntents.ShipmentDocuments => "los documentos de un embarque",
            AssistantIntents.DocumentDelivery => "un documento de un embarque",
            AssistantIntents.TatcStatus => "el estado del TATC",
            AssistantIntents.InvoiceDetail => "una factura",
            _ => "el estado de un embarque",
        };
        var reference = intent == AssistantIntents.InvoiceDetail ? "el número de la factura o su folio SII" : "el número de BL o de booking";

        return new AssistantAnswer(
            intent, AssistantAnswerTypes.NeedsReference, $"Para consultar {subject}, indíqueme {reference}.", [], [], [], topic);
    }

    private static AssistantAnswer NoAnswer(string topic) => new(
        AssistantIntents.Knowledge,
        AssistantAnswerTypes.NoAnswer,
        "No dispongo de una respuesta para esa consulta en la base de conocimiento del portal.",
        [], [], [], topic);

    private static AssistantAnswer Refused(string reason, string topic, string? mailbox)
    {
        var subject = reason switch
        {
            AssistantRefusalReasons.LegalAdvice => "recomendaciones legales",
            AssistantRefusalReasons.HistoricalTariffs => "comparaciones de tarifas históricas",
            _ => "recomendaciones comerciales",
        };

        var answer = new AssistantAnswer(
            AssistantIntents.OutOfScope,
            AssistantAnswerTypes.Refused,
            $"No puedo entregar {subject}. El asistente solo informa procesos del portal y los datos registrados de sus embarques.",
            [], [], [], topic);
        return WithMailbox(answer, mailbox);
    }

    private static AssistantAnswer WithMailbox(AssistantAnswer answer, string? mailbox)
    {
        if (mailbox is null)
        {
            return answer with
            {
                Text = $"{answer.Text} Si necesita más ayuda, contacte a Customer Service de Hapag-Lloyd."
            };
        }

        return answer with
        {
            Text = $"{answer.Text} Si necesita más ayuda, escriba a {mailbox}.",
            Actions = [.. answer.Actions, new AssistantActionDto(AssistantReferences.ContactMailbox, $"Escribir a {mailbox}", $"mailto:{mailbox}", null, null)]
        };
    }

    [GeneratedRegex(@"[\p{L}\p{N}][\p{L}\p{N}.,:/-]*\d[\p{L}\p{N}.,:/-]*")]
    private static partial Regex DataToken();
}
