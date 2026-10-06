namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using System.Net;
using System.Text;
using FluentAssertions;
using HapagPortal.Application.Assistant;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.Infrastructure.DependencyInjection;
using HapagPortal.Infrastructure.Integrations.Assistant;
using HapagPortal.Infrastructure.Integrations.Tatc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

/// <summary>
/// Puertos de la Ola F: sistema de TATC (CT-TATC, M2-09) en modo Dummy y Real con caché corta, y motor del
/// asistente (M10-01) en modo Rules u Ollama con su cliente HTTP.
/// </summary>
public sealed class TatcAndAssistantIntegrationTests
{
    private static IConfiguration Configuration(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    private static ServiceProvider Integrations(Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ISecretResolver>());
        services.AddIntegrations(Configuration(settings));
        return services.BuildServiceProvider();
    }

    private static ServiceProvider Assistant(Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<RulesAssistantEngine>();
        services.AddAssistantEngine(Configuration(settings));
        return services.BuildServiceProvider();
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request, body));
            return respond(request);
        }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static ISecretResolver Secrets()
    {
        var secrets = Substitute.For<ISecretResolver>();
        secrets.ResolveAsync(SecretTypes.TatcApiKey, null, Arg.Any<CancellationToken>()).Returns("tatc-key");
        return secrets;
    }

    [Fact]
    public void Tatc_WithoutConfiguration_ShouldUseTheDummy()
    {
        using var provider = Integrations([]);

        provider.GetRequiredService<ITatcProvider>().Should().BeOfType<DummyTatcProvider>();
    }

    [Fact]
    public void Tatc_RealMode_ShouldUseTheHttpClientBehindAShortCache()
    {
        using var provider = Integrations(new Dictionary<string, string?>
        {
            ["Integrations:Tatc:Mode"] = "Real",
            ["Integrations:Tatc:BaseUrl"] = "http://localhost/tatc",
        });

        provider.GetRequiredService<ITatcProvider>().Should().BeOfType<CachedTatcProvider>();
    }

    [Fact]
    public void Tatc_RealModeWithoutBaseUrl_ShouldStopTheStartup()
    {
        var register = () => Integrations(new Dictionary<string, string?> { ["Integrations:Tatc:Mode"] = "Real" });

        register.Should().Throw<InvalidOperationException>().WithMessage("Integrations:Tatc:BaseUrl debe ser una URL absoluta*");
    }

    [Fact]
    public async Task DummyTatc_ShouldReturnTheDemoStatesAndAcceptPendingBls()
    {
        var dummy = new DummyTatcProvider(NullLogger<DummyTatcProvider>.Instance);

        var partial = await dummy.GetByBlNumberAsync("hlcuval250100123");
        var unknown = await dummy.GetByBlNumberAsync("HLCUXXXX0000");
        var receipt = await dummy.RequestGenerationAsync(new TatcGenerationRequest(
            Guid.NewGuid().ToString(), "CL", "CLSAI", "76123456-7", ["HLCUVAL250100123", "HLCUSAI260501240", "HLCUXXXX0000"]));

        partial.Value!.Containers.Select(c => c.Status).Should().Equal(TatcSourceStatuses.Issued, TatcSourceStatuses.NotIssued);
        unknown.Value.Should().BeNull();
        receipt.Value.Items.Select(i => (i.Accepted, i.ReasonCode)).Should().Equal(
            (true, (string?)null), (false, TatcBatchReasons.AlreadyIssued), (false, TatcBatchReasons.NotFound));
    }

    [Fact]
    public async Task CachedTatc_ShouldCacheSuccessesBrieflyAndForgetGeneratedBls()
    {
        var inner = Substitute.For<ITatcProvider>();
        var record = new BlTatcRecord("BL-1", "CL", DateTime.UtcNow, []);
        inner.GetByBlNumberAsync("BL-1", Arg.Any<CancellationToken>()).Returns(Result<BlTatcRecord?>.Success(record));
        inner.GetByBlNumberAsync("BL-DOWN", Arg.Any<CancellationToken>())
            .Returns(Result<BlTatcRecord?>.Failure(DomainErrors.Integration.Unavailable("Tatc")));
        inner.RequestGenerationAsync(Arg.Any<TatcGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<TatcGenerationReceipt>.Success(new TatcGenerationReceipt("R", [])));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedTatcProvider(inner, cache, TimeSpan.FromSeconds(30));

        await cached.GetByBlNumberAsync("BL-1");
        await cached.GetByBlNumberAsync("bl-1");
        await cached.GetByBlNumberAsync("BL-DOWN");
        await cached.GetByBlNumberAsync("BL-DOWN");
        await cached.RequestGenerationAsync(new TatcGenerationRequest("r", "CL", "CLSAI", "1-9", ["BL-1"]));
        await cached.GetByBlNumberAsync("BL-1");

        await inner.Received(2).GetByBlNumberAsync("BL-1", Arg.Any<CancellationToken>());
        await inner.Received(2).GetByBlNumberAsync("BL-DOWN", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HttpTatc_ShouldReadTheContractAndSendTheGenerationWithIdempotency()
    {
        var handler = new StubHandler(request => request.Method == HttpMethod.Get
            ? request.RequestUri!.AbsolutePath.EndsWith("/HLCU1/tatc", StringComparison.Ordinal)
                ? Json("""{"blNumber":"HLCU1","country":"CL","updatedAt":"2026-10-20T13:05:00Z","containers":[{"containerNumber":"C1","tatcNumber":null,"status":"NOT_ISSUED","issuedAt":null,"warehouseCode":"ALM-1","pendingReasons":["PAYMENT_PENDING"]}]}""")
                : new HttpResponseMessage(HttpStatusCode.NotFound)
            : Json("""{"requestId":"REQ-9","items":[{"blNumber":"HLCU1","accepted":true}]}""", HttpStatusCode.Accepted));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/tatc/") };
        var tatc = new HttpTatcProvider(client, Secrets());

        var found = await tatc.GetByBlNumberAsync("HLCU1");
        var missing = await tatc.GetByBlNumberAsync("HLCU2");
        var receipt = await tatc.RequestGenerationAsync(new TatcGenerationRequest("ref-1", "CL", "CLSAI", "76123456-7", ["HLCU1"]));

        found.Value!.Containers.Single().PendingReasons.Should().Equal("PAYMENT_PENDING");
        missing.Value.Should().BeNull();
        receipt.Value.RequestId.Should().Be("REQ-9");
        var post = handler.Requests.Single(r => r.Request.Method == HttpMethod.Post);
        post.Request.RequestUri!.AbsolutePath.Should().Be("/tatc/tatc/generation-requests");
        post.Request.Headers.GetValues("Idempotency-Key").Should().Equal("ref-1");
        post.Request.Headers.GetValues("X-Api-Key").Should().Equal("tatc-key");
        post.Body.Should().Contain("\"locationCode\":\"CLSAI\"");
    }

    [Fact]
    public async Task HttpTatc_ServerError_ShouldBeUnavailable()
    {
        var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))
        {
            BaseAddress = new Uri("http://localhost/tatc/")
        };

        var result = await new HttpTatcProvider(client, Secrets()).GetByBlNumberAsync("HLCU1");

        result.Error.Code.Should().Be("Integration.Unavailable");
    }

    [Fact]
    public void Assistant_DefaultMode_ShouldUseTheRulesEngine()
    {
        using var provider = Assistant([]);

        provider.GetRequiredService<IAssistantEngine>().Should().BeOfType<RulesAssistantEngine>();
        provider.GetRequiredService<AssistantSettings>().Mode.Should().Be(AssistantEngineModes.Rules);
    }

    [Fact]
    public void Assistant_OllamaMode_ShouldUseTheOllamaEngine()
    {
        using var provider = Assistant(new Dictionary<string, string?>
        {
            ["Assistant:Mode"] = "ollama",
            ["Assistant:Ollama:BaseUrl"] = "http://localhost:11434",
            ["Assistant:Ollama:Model"] = "phi3.5",
        });

        provider.GetRequiredService<IAssistantEngine>().Should().BeOfType<OllamaAssistantEngine>();
        provider.GetRequiredService<AssistantSettings>().Mode.Should().Be(AssistantEngineModes.Ollama);
    }

    [Theory]
    [InlineData("Ollama", null, "Assistant:Ollama:BaseUrl debe ser una URL absoluta*")]
    [InlineData("OpenAI", null, "Assistant:Mode='OpenAI' no es válido*")]
    public void Assistant_InvalidConfiguration_ShouldStopTheStartup(string mode, string? baseUrl, string message)
    {
        var register = () => Assistant(new Dictionary<string, string?>
        {
            ["Assistant:Mode"] = mode,
            ["Assistant:Ollama:BaseUrl"] = baseUrl,
        });

        register.Should().Throw<InvalidOperationException>().WithMessage(message);
    }

    private static OllamaAssistantEngine Ollama(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/") }, new OllamaEngineOptions("test-model"),
            NullLogger<OllamaAssistantEngine>.Instance);

    [Fact]
    public async Task Ollama_ShouldClassifyWithOnlyTheUserMessage()
    {
        var handler = new StubHandler(_ => Json("""{"message":{"role":"assistant","content":"{\"intent\":\"ShipmentStatus\"}"},"done":true}"""));

        var result = await Ollama(handler).ClassifyAsync(new AssistantClassificationInput("estado del BL HLCU1", AssistantIntents.Classifiable));

        result.Value.Intent.Should().Be(AssistantIntents.ShipmentStatus);
        var request = handler.Requests.Single();
        request.Request.RequestUri!.AbsolutePath.Should().Be("/api/chat");
        request.Body.Should().Contain("\"model\":\"test-model\"").And.Contain("\"format\":\"json\"").And.Contain("estado del BL HLCU1");
    }

    [Fact]
    public async Task Ollama_UnknownIntentOrServerDown_ShouldFail()
    {
        var unknown = await Ollama(new StubHandler(_ => Json("""{"message":{"role":"assistant","content":"{\"intent\":\"BuyShares\"}"}}""")))
            .ClassifyAsync(new AssistantClassificationInput("x", AssistantIntents.Classifiable));
        var down = await Ollama(new StubHandler(_ => throw new HttpRequestException("refused")))
            .ComposeAsync(new AssistantCompositionInput(AssistantIntents.ShipmentStatus, "x", "borrador", []));

        unknown.Error.Code.Should().Be("Integration.InvalidResponse");
        down.Error.Code.Should().Be("Integration.Unavailable");
    }
}
