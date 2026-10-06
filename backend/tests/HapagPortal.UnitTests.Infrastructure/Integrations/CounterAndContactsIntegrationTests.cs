namespace HapagPortal.UnitTests.Infrastructure.Integrations;

using System.Net;
using System.Text;
using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.DependencyInjection;
using HapagPortal.Infrastructure.Integrations.Contacts;
using HapagPortal.Infrastructure.Integrations.Nexus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

/// <summary>
/// Puertos de la Ola I: Counter de Nexus (CT-COUNTER, M8-09) y registro de contactos (CT-CONTACTS, P0060, M1-06), con
/// adaptador Dummy por defecto y cliente HTTP según el contrato propuesto.
/// </summary>
public sealed class CounterAndContactsIntegrationTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ISecretResolver>());
        services.AddIntegrations(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
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

    private static ISecretResolver Secrets(string type, string key)
    {
        var secrets = Substitute.For<ISecretResolver>();
        secrets.ResolveAsync(type, null, Arg.Any<CancellationToken>()).Returns(key);
        return secrets;
    }

    private static CounterRecordRequest Request(string bl = "HLCUARI0001") =>
        new(bl, CountryCodes.Bolivia, new DateOnly(2026, 10, 1), true, new DateOnly(2026, 10, 2), false, null, "nota", "admin@hapag-lloyd.cl");

    [Fact]
    public void Registration_ShouldBeDummyByDefault_AndRealWhenConfigured()
    {
        using (var dummy = Build([]))
        {
            dummy.GetRequiredService<ICounterRecorder>().Should().BeOfType<DummyCounterRecorder>();
            dummy.GetRequiredService<IContactListProvider>().Should().BeOfType<DummyContactListProvider>();
        }

        using var real = Build(new Dictionary<string, string?>
        {
            ["Integrations:Nexus:Mode"] = "Real",
            ["Integrations:Nexus:BaseUrl"] = "http://localhost/nexus",
            ["Integrations:Contacts:Mode"] = "Real",
            ["Integrations:Contacts:BaseUrl"] = "http://localhost/contacts",
        });
        real.GetRequiredService<ICounterRecorder>().Should().BeOfType<HttpCounterRecorder>();
        real.GetRequiredService<IContactListProvider>().Should().BeOfType<HttpContactListProvider>();
    }

    [Fact]
    public void Contacts_RealModeWithoutBaseUrl_ShouldThrow()
    {
        var act = () => Build(new Dictionary<string, string?> { ["Integrations:Contacts:Mode"] = "Real" });

        act.Should().Throw<InvalidOperationException>().WithMessage("Integrations:Contacts:BaseUrl debe ser una URL absoluta*");
    }

    [Fact]
    public async Task DummyCounter_ShouldRecordIdempotently_AndSimulateFailures()
    {
        var recorder = new DummyCounterRecorder(NullLogger<DummyCounterRecorder>.Instance);

        var first = await recorder.RecordAsync(Request(), "counter-key-00000001");
        var replay = await recorder.RecordAsync(Request() with { Notes = "otra" }, "counter-key-00000001");
        var read = await recorder.GetAsync("HLCUARI0001");
        var failure = await recorder.RecordAsync(Request("HLCU" + DummyCounterRecorder.FailureMarker), "k2");
        var seeded = await recorder.GetAsync("HLCUARI260300830");

        first.Value.SourceReference.Should().Be("CNT-BO-00000001");
        replay.Value.Should().BeSameAs(first.Value);
        read.Value.Should().BeSameAs(first.Value);
        failure.Error.Code.Should().Be("Integration.Unavailable");
        seeded.Value!.HblReceived.Should().BeTrue();
    }

    [Fact]
    public async Task DummyContacts_ShouldReadSeededListsAndApplyChanges()
    {
        var provider = new DummyContactListProvider(NullLogger<DummyContactListProvider>.Instance);

        var lists = await provider.GetListsAsync("MC100010", CountryCodes.Chile);
        var updated = await provider.UpdateListAsync("MC100010", CountryCodes.Chile, ContactReportTypes.Invoices, ["nuevo@importadorademo.cl"],
            "demo@importadorademo.cl", "contacts-abc12345");
        var after = await provider.GetListsAsync("MC100010", CountryCodes.Chile);
        var failure = await provider.GetListsAsync(DummyContactListProvider.FailurePrefix + "1", CountryCodes.Chile);

        lists.Value.Select(l => l.ReportType).Should().Contain(ContactReportTypes.ArrivalNotice);
        updated.Value.SourceReference.Should().Be("P0060-ABC12345");
        after.Value.Single(l => l.ReportType == ContactReportTypes.Invoices).Emails.Should().Equal("nuevo@importadorademo.cl");
        failure.Error.Code.Should().Be("Integration.Unavailable");
    }

    [Fact]
    public async Task HttpCounter_ShouldPutWithIdempotencyKeyAndReadTheRecord()
    {
        var handler = new StubHandler(_ => Json("""
            {"blNumber":"HLCUARI0001","country":"BO","exchangeDate":"2026-10-01","hblReceived":true,"hblReceivedAt":"2026-10-02",
             "deconsolidated":false,"sourceReference":"CNT-77","recordedAt":"2026-10-06T12:00:00Z","recordedBy":"admin@hapag-lloyd.cl"}
            """));
        var recorder = new HttpCounterRecorder(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/nexus/") },
            Secrets(SecretTypes.NexusApiKey, "nexus-key"));

        var result = await recorder.RecordAsync(Request(), "counter-1");

        result.IsSuccess.Should().BeTrue();
        result.Value.SourceReference.Should().Be("CNT-77");
        var (request, body) = handler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri!.AbsolutePath.Should().Be("/nexus/counter/bills-of-lading/HLCUARI0001");
        request.Headers.GetValues("Idempotency-Key").Should().Equal("counter-1");
        request.Headers.GetValues("X-Api-Key").Should().Equal("nexus-key");
        body.Should().Contain("\"hblReceived\":true").And.Contain("\"recordedBy\":\"admin@hapag-lloyd.cl\"");
    }

    [Fact]
    public async Task HttpCounter_UnknownBl_ShouldReadNull()
    {
        var recorder = new HttpCounterRecorder(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))) { BaseAddress = new Uri("http://localhost/nexus/") },
            Secrets(SecretTypes.NexusApiKey, "nexus-key"));

        var result = await recorder.GetAsync("NOPE");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task HttpContacts_ShouldReadAndUpdateDistributionLists()
    {
        var handler = new StubHandler(request => request.Method == HttpMethod.Get
            ? Json("""{"matchCode":"MC1","country":"CL","lists":[{"reportType":"INVOICES","emails":["a@b.cl"],"updatedAt":"2026-10-01T00:00:00Z","updatedBy":"P0060"}]}""")
            : Json("""{"list":{"reportType":"INVOICES","emails":["c@d.cl"]},"changeReference":"CHG-9"}"""));
        var provider = new HttpContactListProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/contacts/") },
            Secrets(SecretTypes.ContactsApiKey, "contacts-key"));

        var lists = await provider.GetListsAsync("MC1", CountryCodes.Chile);
        var updated = await provider.UpdateListAsync("MC1", CountryCodes.Chile, ContactReportTypes.Invoices, ["c@d.cl"], "user@org.cl", "contacts-1");

        lists.Value.Should().ContainSingle().Which.Emails.Should().Equal("a@b.cl");
        updated.Value.SourceReference.Should().Be("CHG-9");
        handler.Requests[0].Request.RequestUri!.PathAndQuery.Should().Be("/contacts/contacts/MC1/distribution-lists?country=CL");
        handler.Requests[1].Request.Method.Should().Be(HttpMethod.Put);
        handler.Requests[1].Request.RequestUri!.AbsolutePath.Should().Be("/contacts/contacts/MC1/distribution-lists/INVOICES");
        handler.Requests[1].Request.Headers.GetValues("Idempotency-Key").Should().Equal("contacts-1");
        handler.Requests[1].Body.Should().Contain("\"emails\":[\"c@d.cl\"]");
    }
}
