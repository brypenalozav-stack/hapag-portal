namespace HapagPortal.IntegrationTests.Integrations;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Infrastructure.Integrations;
using HapagPortal.IntegrationTests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cliente Real de Nexus contra el simulador en memoria, con la tubería real: escenarios de datos y de
/// falla (error500, timeout, circuito, lento, 429) y el log estructurado de NF-27.
/// </summary>
public sealed class NexusRealClientTests(WebApplicationFactory<Program> simulator)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly DateOnly At = new(2026, 10, 20);

    private readonly SimulatorTraffic _traffic = new();

    private ServiceProvider Build(IDictionary<string, string?>? settings = null, ILoggerProvider? logs = null) =>
        SimulatorClientHost.Build(simulator, "Nexus", _traffic, settings, logs);

    [Fact]
    public async Task ExemptCustomer_ShouldReturnGateInAndEds()
    {
        using var provider = Build();

        var result = await provider.GetRequiredService<IExemptionReader>().GetExemptionsAsync("76000001-1", null, At);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(e => e.Concept).Should().Equal("GATE_IN", "EDS");
        result.Value.Should().OnlyContain(e => e.Amount == null && e.ValidFrom == new DateOnly(2026, 1, 1));
    }

    [Fact]
    public async Task CustomerWithoutExemptions_ShouldReturnEmptyList()
    {
        using var provider = Build();

        var result = await provider.GetRequiredService<IExemptionReader>().GetExemptionsAsync("77999999-9", null, At);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task CreditCustomer_ShouldReturnThirtyDaysCredit()
    {
        using var provider = Build();

        var result = await provider.GetRequiredService<ICreditConditionReader>().GetConditionsAsync("76000002-2", "MC000202");

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsFreightForwarder.Should().BeFalse();
        result.Value.MatchCode.Should().Be("MC000202");
        result.Value.Credit!.CreditDays.Should().Be(30);
        result.Value.Credit.Concepts.Should().Equal("LOCAL_CHARGES", "MHD");
    }

    [Fact]
    public async Task FreightForwarder_ShouldBeFlaggedWithoutCredit()
    {
        using var provider = Build();

        var result = await provider.GetRequiredService<ICreditConditionReader>().GetConditionsAsync("76000003-3", null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsFreightForwarder.Should().BeTrue();
        result.Value.Credit.Should().BeNull();
    }

    [Fact]
    public async Task UnknownCustomer_NotFound_ShouldReturnSuccessNull()
    {
        using var provider = Build();

        var result = await provider.GetRequiredService<ICreditConditionReader>().GetConditionsAsync("77999999-9", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task ExchangeRate_UsdToClp_ShouldBe950()
    {
        using var provider = Build();

        var result = await provider.GetRequiredService<IExchangeRateProvider>().GetRateAsync("USD", "CLP", At);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rate.Should().Be(950m);
        result.Value.EffectiveDate.Should().Be(At);
        result.Value.Approved.Should().BeTrue();
    }

    [Fact]
    public async Task Tariffs_ShouldReturnWarehouseChange()
    {
        using var provider = Build();

        var result = await provider.GetRequiredService<ITariffProvider>().GetTariffsAsync("CL", "WAREHOUSE_CHANGE", At);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Amount.Should().Be(9940m);
    }

    [Fact]
    public async Task ApiKeyAndCorrelationId_ShouldBeSent()
    {
        using var provider = Build();

        await provider.GetRequiredService<IExemptionReader>().GetExemptionsAsync("76000001-1", null, At);

        _traffic.CorrelationIds.Should().ContainSingle().Which.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Error500_ShouldRetryThreeTimesAndReturnUnavailable()
    {
        _traffic.ScenarioForAll = "error500";
        using var provider = Build();

        var result = await provider.GetRequiredService<IExemptionReader>().GetExemptionsAsync("76000001-1", null, At);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Integration.Unavailable");
        _traffic.Requests.Should().Be(4, "un intento más tres reintentos");
    }

    [Fact]
    public async Task Timeout_ShouldReturnTimeoutWithinTotalRequestTimeout()
    {
        _traffic.ScenarioForAll = "timeout";
        using var provider = Build(new Dictionary<string, string?>
        {
            ["TimeoutSeconds"] = "1",
            ["TotalTimeoutSeconds"] = "5",
        });

        var stopwatch = Stopwatch.StartNew();
        var result = await provider.GetRequiredService<IExemptionReader>().GetExemptionsAsync("76000001-1", null, At);
        stopwatch.Stop();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Integration.Timeout");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5 + 2));
    }

    [Fact]
    public async Task FiveFailuresInWindow_ShouldOpenCircuitAndFailWithoutCallingSimulator()
    {
        _traffic.ScenarioForAll = "error500";
        using var provider = Build();
        var reader = provider.GetRequiredService<IExemptionReader>();

        // 1.ª llamada: 4 intentos fallidos; 2.ª: el 5.º intento fallido abre el circuito.
        await reader.GetExemptionsAsync("76000001-1", null, At);
        await reader.GetExemptionsAsync("76000001-1", null, At);
        var requestsBefore = _traffic.Requests;
        requestsBefore.Should().BeGreaterThanOrEqualTo(5);

        var result = await reader.GetExemptionsAsync("76000001-1", null, At);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Integration.Unavailable");
        _traffic.Requests.Should().Be(requestsBefore, "con el circuito abierto la llamada no llega al simulador");
    }

    [Fact]
    public async Task Slow_ShouldSucceed()
    {
        _traffic.ScenarioForAll = "lento";
        using var provider = Build();

        var stopwatch = Stopwatch.StartNew();
        var result = await provider.GetRequiredService<IExemptionReader>().GetExemptionsAsync("76000001-1", null, At);
        stopwatch.Stop();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(2.9));
        _traffic.Requests.Should().Be(1);
    }

    [Fact]
    public async Task TooManyRequests_ShouldRetryAfterRetryAfterAndSucceed()
    {
        _traffic.EnqueueScenario("429");
        using var provider = Build();

        var result = await provider.GetRequiredService<IExemptionReader>().GetExemptionsAsync("76000001-1", null, At);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _traffic.Requests.Should().Be(2);
    }

    [Fact]
    public async Task Failure_ShouldEmitNf27StructuredLogAndErrorMetric()
    {
        _traffic.ScenarioForAll = "error500";
        var logs = new CapturingLoggerProvider();
        using var provider = Build(logs: logs);

        long errors = 0;
        var meterFactory = provider.GetRequiredService<IMeterFactory>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == IntegrationMetrics.ErrorsCounterName && ReferenceEquals(instrument.Meter.Scope, meterFactory))
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == IntegrationMetrics.SystemTag && Equals(tag.Value, "Nexus"))
                    Interlocked.Add(ref errors, value);
            }
        });
        listener.Start();

        await provider.GetRequiredService<IExemptionReader>().GetExemptionsAsync("76000001-1", null, At);

        var entry = logs.Entries.Should().ContainSingle(e =>
            e.Category == typeof(IntegrationLoggingHandler).FullName && e.Level == LogLevel.Warning).Subject;
        entry.Properties["System"].Should().Be("Nexus");
        entry.Properties["Operation"].Should().Be("getExemptions");
        entry.Properties["StatusCode"].Should().Be(500);
        entry.Properties["DurationMs"].Should().BeOfType<long>();
        entry.Properties["CorrelationId"].Should().Be(_traffic.CorrelationIds.First());
        _traffic.CorrelationIds.Distinct().Should().ContainSingle("todos los intentos llevan el mismo X-Correlation-Id");
        Interlocked.Read(ref errors).Should().Be(1);
    }
}
