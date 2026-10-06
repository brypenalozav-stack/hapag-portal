namespace HapagPortal.Application.Assistant;

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Inicia una conversación con el asistente (M10-01) en el país del usuario (M1-04).</summary>
public sealed record StartAssistantSessionCommand : ICommand<AssistantSessionDto>;

/// <summary>Envía un mensaje y devuelve la respuesta del asistente (M10-02, M10-03).</summary>
public sealed record SendAssistantMessageCommand(Guid SessionId, string Message) : ICommand<AssistantReplyDto>;

/// <summary>Conversación con su historial, solo para su dueño.</summary>
public sealed record GetAssistantSessionQuery(Guid SessionId) : IQuery<AssistantSessionDto>;

/// <summary>
/// Cierra la conversación y, si el usuario lo acepta, envía el respaldo al correo registrado o al que indique
/// (M10-05). El respaldo es solo documental: no modifica ninguna gestión ni reemplaza una solicitud formal.
/// </summary>
public sealed record EndAssistantSessionCommand(Guid SessionId, bool SendTranscript = false, string? Email = null)
    : ICommand<EndAssistantSessionResultDto>;

public sealed class SendAssistantMessageCommandValidator : AbstractValidator<SendAssistantMessageCommand>
{
    public const int MaxMessageLength = 1000;

    public SendAssistantMessageCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(MaxMessageLength);
    }
}

public sealed class EndAssistantSessionCommandValidator : AbstractValidator<EndAssistantSessionCommand>
{
    public EndAssistantSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

/// <summary>Acceso al asistente (acción <c>assistant.use</c> de M1-11) y vistas de la conversación.</summary>
internal static class AssistantSessions
{
    public const string Disclaimer =
        "El asistente responde con la base de conocimiento de Hapag-Lloyd y con los datos del portal que su usuario " +
        "puede ver. No entrega recomendaciones comerciales ni legales, y sus respuestas no reemplazan las solicitudes formales.";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<Result<AccessScope>> AuthorizeAsync(
        IShipmentAccessEvaluator accessEvaluator,
        CancellationToken cancellationToken)
    {
        var scope = await accessEvaluator.GetScopeAsync(cancellationToken);
        if (scope.UserId is null || !scope.IsOperational)
            return Result<AccessScope>.Failure(Error.Forbidden);

        if (!scope.IsAdmin)
        {
            var matrix = await accessEvaluator.GetMatrixAsync(cancellationToken);
            if (!matrix.OrganizationCan(scope.OrganizationType, ShipmentActionCodes.UseAssistant))
                return Result<AccessScope>.Failure(Error.Forbidden);
        }

        return Result<AccessScope>.Success(scope);
    }

    public static async Task<Result<AssistantSession>> LoadOwnAsync(
        IApplicationDbContext dbContext,
        AccessScope scope,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.AssistantSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == scope.UserId, cancellationToken);

        // NF-05: la conversación de otro usuario no se distingue de una inexistente.
        return session is null
            ? Result<AssistantSession>.Failure(DomainErrors.AssistantSession.NotFound(sessionId))
            : Result<AssistantSession>.Success(session);
    }

    public static Task<List<AssistantMessage>> MessagesAsync(
        IApplicationDbContext dbContext, Guid sessionId, CancellationToken cancellationToken) =>
        dbContext.AssistantMessages.AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Sequence)
            .ToListAsync(cancellationToken);

    public static bool IsExpired(AssistantSession session, DateTime now, int idleMinutes) =>
        session.Status == AssistantSessionStatus.Active && session.LastActivityAt.AddMinutes(idleMinutes) < now;

    public static AssistantMessageDto ToDto(AssistantMessage m) => new(
        m.Id,
        m.Sequence,
        m.Role,
        m.Content,
        m.Intent,
        m.AnswerType,
        Read<AssistantCitationDto>(m.CitationsJson),
        Read<AssistantActionDto>(m.ActionsJson),
        m.Engine,
        m.EngineFallback,
        m.ElapsedMs,
        m.CreatedAt);

    public static AssistantSessionDto ToDto(AssistantSession s, IEnumerable<AssistantMessage> messages, int idleMinutes) => new(
        s.Id,
        s.Country,
        s.Language,
        s.Status,
        s.EngineMode,
        s.StartedAt,
        s.LastActivityAt,
        s.EndedAt,
        idleMinutes,
        s.UserEmail,
        s.TranscriptSentTo,
        s.TranscriptSentAt,
        Disclaimer,
        messages.OrderBy(m => m.Sequence).Select(ToDto).ToList());

    public static string? Write<T>(IReadOnlyList<T> items) =>
        items.Count == 0 ? null : JsonSerializer.Serialize(items, JsonOptions);

    private static IReadOnlyList<T> Read<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
}

/// <summary>Respaldo de la conversación por correo (M10-05): fecha, hora y contenido completo.</summary>
public static class AssistantTranscript
{
    public static (string Subject, string Body) Build(
        AssistantSession session,
        IReadOnlyList<AssistantMessage> messages,
        string? organizationName,
        DateTime endedAt)
    {
        string Local(DateTime utc) =>
            BusinessCalendar.ToLocal(session.Country, utc).ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        var zone = BusinessCalendar.TimeZoneId(session.Country);
        var subject = $"Respaldo de su conversación con el asistente del portal Hapag-Lloyd ({Local(session.StartedAt)[..10]})";

        var body = new StringBuilder()
            .AppendLine("Respaldo de la conversación con el asistente del portal de clientes de Hapag-Lloyd.")
            .AppendLine()
            .AppendLine($"Conversación: {session.Id}")
            .AppendLine($"Usuario: {session.UserEmail}");

        if (!string.IsNullOrWhiteSpace(organizationName))
            body.AppendLine($"Organización: {organizationName}");

        body.AppendLine($"Inicio: {Local(session.StartedAt)} ({zone})")
            .AppendLine($"Término: {Local(endedAt)} ({zone})")
            .AppendLine();

        foreach (var message in messages.OrderBy(m => m.Sequence))
        {
            var author = message.Role == AssistantRoles.User ? "Usuario" : "Asistente";
            body.AppendLine($"[{Local(message.CreatedAt)}] {author}:")
                .AppendLine(message.Content)
                .AppendLine();
        }

        body.AppendLine("Este correo es un respaldo documental de la conversación. No modifica el estado de ninguna gestión " +
                        "ni reemplaza una solicitud formal en el portal.");

        return (subject, body.ToString());
    }
}

public sealed class StartAssistantSessionCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    ICurrentUserService currentUserService,
    IAssistantEngine engine,
    AssistantSettings settings)
    : ICommandHandler<StartAssistantSessionCommand, AssistantSessionDto>
{
    public async Task<Result<AssistantSessionDto>> Handle(StartAssistantSessionCommand request, CancellationToken cancellationToken)
    {
        var authorized = await AssistantSessions.AuthorizeAsync(accessEvaluator, cancellationToken);
        if (authorized.IsFailure)
            return Result<AssistantSessionDto>.Failure(authorized.Error);

        var scope = authorized.Value;
        var organizationCountry = scope.OrganizationId is { } organizationId
            ? await dbContext.Clients.AsNoTracking().Where(c => c.Id == organizationId).Select(c => c.Country).FirstOrDefaultAsync(cancellationToken)
            : null;

        // M10-02: la base de conocimiento es la del país de operación del usuario (M1-04).
        var requested = (currentUserService.Country ?? organizationCountry ?? CountryCodes.Chile).Trim().ToUpperInvariant();
        var country = CountryCodes.ValidCountries.Contains(requested) ? requested : CountryCodes.Chile;
        var now = DateTime.UtcNow;

        var session = new AssistantSession
        {
            UserId = scope.UserId!.Value,
            OrganizationId = scope.OrganizationId,
            UserEmail = currentUserService.Email ?? string.Empty,
            Country = country,
            Language = "es",
            Status = AssistantSessionStatus.Active,
            EngineMode = engine.Name,
            StartedAt = now,
            LastActivityAt = now
        };

        var welcome = new AssistantMessage
        {
            SessionId = session.Id,
            Sequence = 1,
            Role = AssistantRoles.Assistant,
            Content = AssistantResponder.Welcome(country),
            Intent = AssistantIntents.Greeting,
            AnswerType = AssistantAnswerTypes.Greeting,
            Engine = AssistantEngineModes.Rules,
            CreatedAt = now
        };

        dbContext.AssistantSessions.Add(session);
        dbContext.AssistantMessages.Add(welcome);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AssistantSessionDto>.Success(AssistantSessions.ToDto(session, [welcome], settings.SessionIdleMinutes));
    }
}

public sealed class SendAssistantMessageCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    AssistantResponder responder,
    AssistantSettings settings)
    : ICommandHandler<SendAssistantMessageCommand, AssistantReplyDto>
{
    public async Task<Result<AssistantReplyDto>> Handle(SendAssistantMessageCommand request, CancellationToken cancellationToken)
    {
        var authorized = await AssistantSessions.AuthorizeAsync(accessEvaluator, cancellationToken);
        if (authorized.IsFailure)
            return Result<AssistantReplyDto>.Failure(authorized.Error);

        var scope = authorized.Value;
        var loaded = await AssistantSessions.LoadOwnAsync(dbContext, scope, request.SessionId, cancellationToken);
        if (loaded.IsFailure)
            return Result<AssistantReplyDto>.Failure(loaded.Error);

        var session = loaded.Value;
        var now = DateTime.UtcNow;

        if (session.Status == AssistantSessionStatus.Ended)
            return Result<AssistantReplyDto>.Failure(DomainErrors.AssistantSession.Ended);
        if (AssistantSessions.IsExpired(session, now, settings.SessionIdleMinutes))
            return Result<AssistantReplyDto>.Failure(DomainErrors.AssistantSession.Expired);

        // Límite por usuario (todas sus conversaciones) en el último minuto.
        var userId = scope.UserId!.Value;
        var since = now.AddMinutes(-1);
        var sessionIds = dbContext.AssistantSessions.Where(s => s.UserId == userId).Select(s => s.Id);
        var recent = await dbContext.AssistantMessages
            .CountAsync(m => sessionIds.Contains(m.SessionId) && m.Role == AssistantRoles.User && m.CreatedAt >= since, cancellationToken);
        if (recent >= settings.RateLimitPerMinute)
            return Result<AssistantReplyDto>.Failure(DomainErrors.AssistantSession.RateLimited);

        var lastSequence = await dbContext.AssistantMessages
            .Where(m => m.SessionId == session.Id)
            .Select(m => (int?)m.Sequence)
            .MaxAsync(cancellationToken) ?? 0;

        var message = request.Message.Trim();
        var stopwatch = Stopwatch.StartNew();
        var response = await responder.RespondAsync(session, message, cancellationToken);
        stopwatch.Stop();
        var elapsed = (int)Math.Min(int.MaxValue, stopwatch.ElapsedMilliseconds);

        var userMessage = new AssistantMessage
        {
            SessionId = session.Id,
            Sequence = lastSequence + 1,
            Role = AssistantRoles.User,
            Content = message,
            CreatedAt = now
        };

        var reply = new AssistantMessage
        {
            SessionId = session.Id,
            Sequence = lastSequence + 2,
            Role = AssistantRoles.Assistant,
            Content = response.Text,
            Intent = response.Answer.Intent,
            AnswerType = response.Answer.AnswerType,
            CitationsJson = AssistantSessions.Write(response.Answer.Citations),
            ActionsJson = AssistantSessions.Write(response.Answer.Actions),
            Engine = response.Engine,
            EngineFallback = response.EngineFallback,
            ElapsedMs = elapsed,
            CreatedAt = now.AddMilliseconds(Math.Max(1, elapsed))
        };

        dbContext.AssistantMessages.Add(userMessage);
        dbContext.AssistantMessages.Add(reply);
        session.LastActivityAt = reply.CreatedAt;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AssistantReplyDto>.Success(new AssistantReplyDto(
            session.Id,
            AssistantSessions.ToDto(userMessage),
            AssistantSessions.ToDto(reply),
            response.MailboxEmail,
            settings.ResponseTargetMs,
            elapsed <= settings.ResponseTargetMs));
    }
}

public sealed class GetAssistantSessionQueryHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    AssistantSettings settings)
    : IQueryHandler<GetAssistantSessionQuery, AssistantSessionDto>
{
    public async Task<Result<AssistantSessionDto>> Handle(GetAssistantSessionQuery request, CancellationToken cancellationToken)
    {
        var authorized = await AssistantSessions.AuthorizeAsync(accessEvaluator, cancellationToken);
        if (authorized.IsFailure)
            return Result<AssistantSessionDto>.Failure(authorized.Error);

        var loaded = await AssistantSessions.LoadOwnAsync(dbContext, authorized.Value, request.SessionId, cancellationToken);
        if (loaded.IsFailure)
            return Result<AssistantSessionDto>.Failure(loaded.Error);

        var messages = await AssistantSessions.MessagesAsync(dbContext, loaded.Value.Id, cancellationToken);
        return Result<AssistantSessionDto>.Success(AssistantSessions.ToDto(loaded.Value, messages, settings.SessionIdleMinutes));
    }
}

public sealed class EndAssistantSessionCommandHandler(
    IApplicationDbContext dbContext,
    IShipmentAccessEvaluator accessEvaluator,
    IEmailService emailService)
    : ICommandHandler<EndAssistantSessionCommand, EndAssistantSessionResultDto>
{
    public async Task<Result<EndAssistantSessionResultDto>> Handle(EndAssistantSessionCommand request, CancellationToken cancellationToken)
    {
        var authorized = await AssistantSessions.AuthorizeAsync(accessEvaluator, cancellationToken);
        if (authorized.IsFailure)
            return Result<EndAssistantSessionResultDto>.Failure(authorized.Error);

        var loaded = await AssistantSessions.LoadOwnAsync(dbContext, authorized.Value, request.SessionId, cancellationToken);
        if (loaded.IsFailure)
            return Result<EndAssistantSessionResultDto>.Failure(loaded.Error);

        var session = loaded.Value;
        var now = DateTime.UtcNow;

        if (session.Status != AssistantSessionStatus.Ended)
        {
            session.Status = AssistantSessionStatus.Ended;
            session.EndedAt = now;
        }

        string? sentTo = null;
        if (request.SendTranscript)
        {
            sentTo = string.IsNullOrWhiteSpace(request.Email) ? session.UserEmail : request.Email.Trim();
            if (string.IsNullOrWhiteSpace(sentTo))
                return Result<EndAssistantSessionResultDto>.Failure(DomainErrors.AssistantSession.NoRecipient);

            var messages = await AssistantSessions.MessagesAsync(dbContext, session.Id, cancellationToken);
            var organizationName = session.OrganizationId is { } organizationId
                ? await dbContext.Clients.AsNoTracking().Where(c => c.Id == organizationId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
                : null;

            var (subject, body) = AssistantTranscript.Build(session, messages, organizationName, session.EndedAt ?? now);
            await emailService.SendEmailAsync(sentTo, subject, body, cancellationToken);

            session.TranscriptSentTo = sentTo;
            session.TranscriptSentAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<EndAssistantSessionResultDto>.Success(
            new EndAssistantSessionResultDto(session.Id, session.EndedAt ?? now, sentTo is not null, sentTo));
    }
}
