namespace HapagPortal.UnitTests.Application.Assistant;

using FluentAssertions;
using HapagPortal.Application.Assistant;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Dashboard;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.Search;
using HapagPortal.Application.Shipments.Tatc;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

/// <summary>
/// Asistente (M10-01 a M10-03, M10-05): base de conocimiento por país, consultas dinámicas con los permisos del
/// usuario sin revelar embarques ajenos, rechazo de consultas fuera del alcance, respaldo con Rules cuando el motor
/// Ollama falla o inventa datos, respaldo de la conversación por correo y límite de mensajes por usuario.
/// </summary>
public sealed class AssistantTests
{
    private const string OwnBl = "HLCU-OWN-001";
    private const string ForeignBl = "HLCU-FOREIGN-9";

    private readonly MockApplicationDbContext _db = new();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IChargeRulesService _rules = Substitute.For<IChargeRulesService>();
    private readonly AssistantSettings _settings = new();
    private readonly Client _org;
    private readonly User _user;
    private readonly ICurrentUserService _currentUser;

    public AssistantTests()
    {
        var client = AccessTestData.ClientContext(_db);
        _org = client.Organization;
        _user = client.User;
        _currentUser = client.CurrentUser;
        _rules.GetConditionsAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>())
            .Returns(call => new CommercialConditionsDto(true, "NEXUS", call.Arg<Client>().TaxId, null, false, null, [], null, null, false, false, false, null));

        var own = AccessTestData.AddBl(_db, Guid.NewGuid(), OwnBl, vessel: "Own Vessel", status: "Arrived");
        AccessTestData.AddRole(_db, own, _org, ShipmentRoleCodes.Consignee);
        own.LocalCharges.Add(new LocalCharge
        {
            ChargeType = ChargeConceptCodes.Thc, Currency = "CLP", Status = ChargeStatus.Pending, Amount = 185000m, TotalAmount = 220150m,
            Description = "THC", BillOfLadingId = own.Id
        });
        own.FreightPaidAt = DateTime.UtcNow;

        AccessTestData.AddBl(_db, AccessTestData.AddOrganization(_db).Id, ForeignBl, vessel: "Secret Vessel", status: "InTransit");

        _db.KnowledgeArticleList.Add(Article(CountryCodes.Chile, "¿Cómo solicito un cambio de almacén?", "CL: solicite el cambio desde el detalle del BL."));
        _db.KnowledgeArticleList.Add(Article(CountryCodes.Bolivia, "¿Cómo solicito un cambio de almacén?", "BO: el cambio se coordina con el depósito boliviano."));
        _db.AssistantMailboxList.Add(new AssistantMailbox { Country = CountryCodes.Chile, Topic = AssistantTopics.General, Email = "cl@test.cl", CreatedBy = "t" });
        _db.AssistantMailboxList.Add(new AssistantMailbox { Country = CountryCodes.Bolivia, Topic = AssistantTopics.General, Email = "bo@test.bo", CreatedBy = "t" });
    }

    private static KnowledgeArticle Article(string country, string title, string content) => new()
    {
        Country = country, Topic = AssistantTopics.Shipping, Title = title, Content = content, CreatedBy = "t"
    };

    private sealed record Handlers(
        StartAssistantSessionCommandHandler Start,
        SendAssistantMessageCommandHandler Send,
        EndAssistantSessionCommandHandler End,
        GetAssistantSessionQueryHandler Get);

    private Handlers For(ICurrentUserService currentUser, IAssistantEngine? engine = null)
    {
        var evaluator = AccessTestData.Evaluator(_db, currentUser);
        var tatc = Substitute.For<ITatcProvider>();
        tatc.GetByBlNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result<BlTatcRecord?>.Success(null));

        var sender = new TestSender()
            .Register(new GetShipmentDetailQueryHandler(_db, evaluator, _rules, TestShipmentSources.IssuanceReader()))
            .Register(new SearchShipmentsQueryHandler(_db, evaluator))
            .Register(new GetShipmentDocumentsQueryHandler(_db, evaluator, _rules, new ResponsibilityLetterStatus(_db)))
            .Register(new GetShipmentTatcQueryHandler(_db, evaluator, tatc))
            .Register(new GetDashboardQueryHandler(_db, evaluator, _rules));

        var rules = new RulesAssistantEngine();
        engine ??= rules;
        var responder = new AssistantResponder(_db, engine, rules, new AssistantDataRetriever(sender, _db, evaluator));

        return new Handlers(
            new StartAssistantSessionCommandHandler(_db, evaluator, currentUser, engine, _settings),
            new SendAssistantMessageCommandHandler(_db, evaluator, responder, _settings),
            new EndAssistantSessionCommandHandler(_db, evaluator, _email),
            new GetAssistantSessionQueryHandler(_db, evaluator, _settings));
    }

    private static async Task<AssistantReplyDto> Ask(Handlers handlers, Guid sessionId, string message)
    {
        var reply = await handlers.Send.Handle(new SendAssistantMessageCommand(sessionId, message), CancellationToken.None);
        reply.IsSuccess.Should().BeTrue(reply.IsFailure ? reply.Error.Message : null);
        return reply.Value;
    }

    private async Task<(Handlers Handlers, Guid SessionId)> StartAsync(ICurrentUserService? currentUser = null, IAssistantEngine? engine = null)
    {
        var handlers = For(currentUser ?? _currentUser, engine);
        var session = await handlers.Start.Handle(new StartAssistantSessionCommand(), CancellationToken.None);
        session.IsSuccess.Should().BeTrue();
        return (handlers, session.Value.Id);
    }

    [Fact]
    public async Task Start_ShouldOpenASessionInTheUserCountryWithAWelcome()
    {
        var handlers = For(_currentUser);

        var session = await handlers.Start.Handle(new StartAssistantSessionCommand(), CancellationToken.None);

        session.Value.Country.Should().Be(CountryCodes.Chile);
        session.Value.EngineMode.Should().Be(AssistantEngineModes.Rules);
        session.Value.Messages.Should().ContainSingle(m => m.Role == AssistantRoles.Assistant && m.AnswerType == AssistantAnswerTypes.Greeting);
    }

    [Fact]
    public async Task Knowledge_ShouldAnswerFromTheUserCountry()
    {
        var bolivia = AccessTestData.AddOrganization(_db, country: CountryCodes.Bolivia);
        var boliviaUser = AccessTestData.CurrentUser(AccessTestData.AddMember(_db, bolivia), AccessPermissions.OperateShipments);
        var (chileHandlers, chileSession) = await StartAsync();
        var (boliviaHandlers, boliviaSession) = await StartAsync(boliviaUser);

        var chile = await Ask(chileHandlers, chileSession, "¿Cómo solicito un cambio de almacén?");
        var boliviaAnswer = await Ask(boliviaHandlers, boliviaSession, "como solicitar cambio de almacen");

        chile.Reply.AnswerType.Should().Be(AssistantAnswerTypes.Knowledge);
        chile.Reply.Content.Should().Contain("CL:").And.NotContain("BO:");
        chile.Reply.Citations.Should().ContainSingle(c => c.Kind == AssistantReferences.KnowledgeArticle);
        boliviaAnswer.Reply.Content.Should().Contain("BO:").And.NotContain("CL:");
    }

    [Fact]
    public async Task Knowledge_WithoutAnswer_ShouldSayItAndDeriveToTheMailbox()
    {
        var (handlers, session) = await StartAsync();

        var reply = await Ask(handlers, session, "¿Cuál es el horario del casino del puerto?");

        reply.Reply.AnswerType.Should().Be(AssistantAnswerTypes.NoAnswer);
        reply.MailboxEmail.Should().Be("cl@test.cl");
        reply.Reply.Content.Should().Contain("No dispongo de una respuesta").And.Contain("cl@test.cl");
        reply.Reply.Actions.Should().ContainSingle(a => a.Type == AssistantReferences.ContactMailbox && a.Path == "mailto:cl@test.cl");
    }

    [Fact]
    public async Task ShipmentStatus_OwnBl_ShouldAnswerWithPortalDataOnly()
    {
        var (handlers, session) = await StartAsync();

        var reply = await Ask(handlers, session, $"¿Cuál es el estado del BL {OwnBl}?");

        reply.Reply.Intent.Should().Be(AssistantIntents.ShipmentStatus);
        reply.Reply.AnswerType.Should().Be(AssistantAnswerTypes.Data);
        reply.Reply.Content.Should().Contain(OwnBl).And.Contain("Own Vessel").And.Contain("Arribado");
        reply.Reply.Content.Should().Contain("ETA: no disponible");
        reply.Reply.Citations.Should().ContainSingle(c => c.Kind == AssistantReferences.Shipment && c.Reference == OwnBl);
    }

    [Theory]
    [InlineData("¿Cuál es el estado del BL {0}?")]
    [InlineData("documentos del BL {0}")]
    [InlineData("cargos pendientes del BL {0}")]
    [InlineData("estado del TATC del BL {0}")]
    public async Task DataQuestions_OnAnotherOrganizationBl_ShouldNeverLeak(string template)
    {
        var (handlers, session) = await StartAsync();

        var foreign = await Ask(handlers, session, string.Format(template, ForeignBl));
        var missing = await Ask(handlers, session, string.Format(template, "HLCU-NOPE-777"));

        foreign.Reply.AnswerType.Should().Be(AssistantAnswerTypes.NotAvailable);
        foreign.Reply.Content.Should().NotContain("Secret Vessel").And.NotContain("En tránsito");
        foreign.Reply.Actions.Should().NotContain(a => a.BlNumber == ForeignBl);
        foreign.Reply.Content.Replace(ForeignBl, "#").Should().Be(missing.Reply.Content.Replace("HLCU-NOPE-777", "#"));
    }

    [Fact]
    public async Task PendingCharges_OfOwnBl_ShouldListThemWithTotals()
    {
        var (handlers, session) = await StartAsync();

        var reply = await Ask(handlers, session, $"¿Qué cargos pendientes tiene el {OwnBl}?");

        reply.Reply.AnswerType.Should().Be(AssistantAnswerTypes.Data);
        reply.Reply.Content.Should().Contain("THC").And.Contain("Total CLP");
        reply.Reply.Actions.Should().Contain(a => a.Type == AssistantReferences.OpenCart);
    }

    [Fact]
    public async Task PendingCharges_WithoutReference_ShouldSummarizeTheDashboard()
    {
        var (handlers, session) = await StartAsync();

        var reply = await Ask(handlers, session, "¿Tengo cargos pendientes de pago?");

        reply.Reply.AnswerType.Should().Be(AssistantAnswerTypes.Data);
        reply.Reply.Content.Should().Contain("Servicios pendientes: 1").And.NotContain(ForeignBl);
    }

    [Theory]
    [InlineData("¿Qué naviera me recomienda para exportar fruta?")]
    [InlineData("Necesito asesoría legal para demandar al transportista")]
    [InlineData("Compare la tarifa de THC con la del año pasado")]
    public async Task OutOfScope_ShouldBeRefusedAndDerived(string message)
    {
        var engine = Substitute.For<IAssistantEngine>();
        engine.Name.Returns(AssistantEngineModes.Ollama);
        var (handlers, session) = await StartAsync(engine: engine);

        var reply = await Ask(handlers, session, message);

        reply.Reply.Intent.Should().Be(AssistantIntents.OutOfScope);
        reply.Reply.AnswerType.Should().Be(AssistantAnswerTypes.Refused);
        reply.Reply.Content.Should().StartWith("No puedo entregar").And.Contain("cl@test.cl");
        await engine.DidNotReceiveWithAnyArgs().ClassifyAsync(default!, default);
    }

    [Fact]
    public async Task Ollama_WhenDown_ShouldAnswerWithRules()
    {
        var engine = Substitute.For<IAssistantEngine>();
        engine.Name.Returns(AssistantEngineModes.Ollama);
        engine.ClassifyAsync(Arg.Any<AssistantClassificationInput>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));
        engine.ComposeAsync(Arg.Any<AssistantCompositionInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(DomainErrors.Integration.Unavailable("Assistant")));
        var (handlers, session) = await StartAsync(engine: engine);

        var reply = await Ask(handlers, session, $"estado del BL {OwnBl}");

        reply.Reply.AnswerType.Should().Be(AssistantAnswerTypes.Data);
        reply.Reply.Content.Should().Contain("Own Vessel");
        reply.Reply.Engine.Should().Be(AssistantEngineModes.Rules);
        reply.Reply.EngineFallback.Should().BeTrue();
        await engine.DidNotReceiveWithAnyArgs().ComposeAsync(default!, default);
    }

    [Fact]
    public async Task Ollama_ComposingInventedData_ShouldBeDiscarded()
    {
        var engine = Substitute.For<IAssistantEngine>();
        engine.Name.Returns(AssistantEngineModes.Ollama);
        engine.ClassifyAsync(Arg.Any<AssistantClassificationInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<AssistantIntentResult>.Success(new AssistantIntentResult(AssistantIntents.ShipmentStatus, ["HLCU-OTHER-555"])));
        engine.ComposeAsync(Arg.Any<AssistantCompositionInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success($"Su BL {OwnBl} llegará el 31-12-2030 al puerto."));
        var (handlers, session) = await StartAsync(engine: engine);

        var reply = await Ask(handlers, session, $"¿dónde está mi BL {OwnBl}?");

        reply.Reply.Content.Should().NotContain("31-12-2030").And.Contain("Own Vessel");
        reply.Reply.EngineFallback.Should().BeTrue();
        await engine.Received(1).ComposeAsync(
            Arg.Is<AssistantCompositionInput>(i => i.DraftAnswer.Contains(OwnBl) && !i.DraftAnswer.Contains("Secret Vessel")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ollama_FaithfulComposition_ShouldBeUsed()
    {
        var engine = Substitute.For<IAssistantEngine>();
        engine.Name.Returns(AssistantEngineModes.Ollama);
        engine.ClassifyAsync(Arg.Any<AssistantClassificationInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<AssistantIntentResult>.Success(new AssistantIntentResult(AssistantIntents.ShipmentStatus, [])));
        engine.ComposeAsync(Arg.Any<AssistantCompositionInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success($"Su BL {OwnBl} está arribado y viaja en la nave Own Vessel."));
        var (handlers, session) = await StartAsync(engine: engine);

        var reply = await Ask(handlers, session, $"¿dónde está mi BL {OwnBl}?");

        reply.Reply.Content.Should().Be($"Su BL {OwnBl} está arribado y viaja en la nave Own Vessel.");
        reply.Reply.Engine.Should().Be(AssistantEngineModes.Ollama);
        reply.Reply.EngineFallback.Should().BeFalse();
    }

    [Fact]
    public async Task End_WithTranscript_ShouldEmailDateTimeAndTheFullConversation()
    {
        var (handlers, session) = await StartAsync();
        await Ask(handlers, session, $"estado del BL {OwnBl}");

        var ended = await handlers.End.Handle(new EndAssistantSessionCommand(session, SendTranscript: true), CancellationToken.None);

        ended.Value.TranscriptSent.Should().BeTrue();
        ended.Value.TranscriptSentTo.Should().Be(_user.Email);
        await _email.Received(1).SendEmailAsync(
            _user.Email,
            Arg.Is<string>(s => s.StartsWith("Respaldo de su conversación")),
            Arg.Is<string>(b =>
                b.Contains($"Usuario: {_user.Email}")
                && b.Contains("Inicio: ") && b.Contains("(America/Santiago)")
                && b.Contains($"Usuario:\nestado del BL {OwnBl}".Replace("\n", Environment.NewLine))
                && b.Contains("Own Vessel")
                && b.Contains("Hola, soy el asistente")
                && b.Contains("ni reemplaza una solicitud formal")),
            Arg.Any<CancellationToken>());

        var reopened = await handlers.Get.Handle(new GetAssistantSessionQuery(session), CancellationToken.None);
        reopened.Value.Status.Should().Be(AssistantSessionStatus.Ended);
        (await handlers.Send.Handle(new SendAssistantMessageCommand(session, "hola"), CancellationToken.None))
            .Error.Should().Be(DomainErrors.AssistantSession.Ended);
    }

    [Fact]
    public async Task End_ToAnotherAddress_ShouldUseIt()
    {
        var (handlers, session) = await StartAsync();

        var ended = await handlers.End.Handle(new EndAssistantSessionCommand(session, true, "otro@cliente.cl"), CancellationToken.None);

        ended.Value.TranscriptSentTo.Should().Be("otro@cliente.cl");
        await _email.Received(1).SendEmailAsync("otro@cliente.cl", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Messages_ShouldBeLimitedPerUser()
    {
        _settings.RateLimitPerMinute = 2;
        var (handlers, session) = await StartAsync();

        await Ask(handlers, session, "hola");
        await Ask(handlers, session, "hola");
        var third = await handlers.Send.Handle(new SendAssistantMessageCommand(session, "hola"), CancellationToken.None);

        third.Error.Should().Be(DomainErrors.AssistantSession.RateLimited);
    }

    [Fact]
    public async Task Sessions_OfAnotherUser_ShouldNotExist()
    {
        var (_, session) = await StartAsync();
        var otherUser = AccessTestData.CurrentUser(AccessTestData.AddMember(_db, _org), AccessPermissions.OperateShipments);

        var result = await For(otherUser).Get.Handle(new GetAssistantSessionQuery(session), CancellationToken.None);

        result.Error.Code.Should().Be("AssistantSession.NotFound");
    }

    [Theory]
    [InlineData("¿Cuál es el estado del BL HLCUVAL250100123?", AssistantIntents.ShipmentStatus)]
    [InlineData("documentos del BL HLCUVAL250100123", AssistantIntents.ShipmentDocuments)]
    // M10-04 (Ola J): pedir un documento puntual por su nombre es una entrega de documentos.
    [InlineData("quiero descargar la copia del BL HLCUVAL250100123", AssistantIntents.DocumentDelivery)]
    [InlineData("detalle de la factura HL-CL-2026-003987", AssistantIntents.InvoiceDetail)]
    [InlineData("¿cuánto debo?", AssistantIntents.PendingCharges)]
    [InlineData("estado del TATC HLCUSAI260400910", AssistantIntents.TatcStatus)]
    [InlineData("hola", AssistantIntents.Greeting)]
    [InlineData("¿Cómo funciona el cambio de almacén?", AssistantIntents.Knowledge)]
    public void Rules_ShouldClassifyTheIntent(string message, string intent)
    {
        AssistantIntentRules.Classify(message).Intent.Should().Be(intent);
    }

    [Fact]
    public void Rules_ShouldExtractOnlyTheReferencesWrittenByTheUser()
    {
        AssistantIntentRules.ExtractReferences("estado del bl hlcuval250100123 y booking HLCUBKG2501001 en 2026")
            .Should().Equal("HLCUVAL250100123", "HLCUBKG2501001");
    }
}
