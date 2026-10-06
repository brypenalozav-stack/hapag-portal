namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using FluentAssertions;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.ExchangeRates.Common;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.ServiceRequests.Requests;
using HapagPortal.Application.Tariffs.Common;
using HapagPortal.Application.WarehouseChanges.History;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.ServiceRequests;
using HapagPortal.Infrastructure.Integrations.Nexus;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

/// <summary>
/// Semilla de la Ola G sobre un proveedor EF real (InMemory): definiciones de servicios on demand coherentes con
/// sus catálogos y tarifas (M2-03, M2-04), solicitudes de demostración en cada estado, servicios aplicables a los
/// BL de demostración y el historial de cambio de almacén con el RUT del pagador (M3-06).
/// </summary>
public sealed class OnDemandServicesSeedTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public OnDemandServicesSeedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private ShipmentAccessEvaluator EvaluatorFor(Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.HasPermission(Arg.Any<string>()).Returns(call => call.Arg<string>() == AccessPermissions.OperateShipments);
        return new ShipmentAccessEvaluator(_context, currentUser);
    }

    private ServiceCatalogEvaluator Catalog()
    {
        var exchangeRates = new ExchangeRateService(_context, new DummyExchangeRateProvider(NullLogger<DummyExchangeRateProvider>.Instance));
        var rules = new ChargeRulesService(
            _context,
            new DummyExemptionReader(NullLogger<DummyExemptionReader>.Instance),
            new DummyCreditConditionReader(NullLogger<DummyCreditConditionReader>.Instance),
            exchangeRates,
            new ResponsibilityLetterStatus(_context));
        return new ServiceCatalogEvaluator(
            _context, new TariffResolver(_context, new DummyTariffProvider(NullLogger<DummyTariffProvider>.Instance)), rules);
    }

    [Fact]
    public async Task Definitions_ShouldReferenceExistingCatalogs_HaveValidForms_AndTariffsInForce()
    {
        var definitions = await _context.ServiceDefinitions.AsNoTracking().ToListAsync();
        var concepts = await _context.ChargeConcepts.AsNoTracking().Select(c => c.Code).ToListAsync();
        var actions = await _context.ShipmentActions.AsNoTracking().Select(a => a.Code).ToListAsync();
        var logs = await _context.MaintainerChangeLogs.AsNoTracking()
            .Where(l => l.Maintainer == MaintainerNames.ServiceDefinition)
            .ToListAsync();
        var resolver = new TariffResolver(_context, new DummyTariffProvider(NullLogger<DummyTariffProvider>.Instance));
        var date = new DateOnly(2026, 10, 6);

        // Ola H suma la refacturación IAO (M3-11), activa; Ola J, el certificado de flete (M6-02) y la carta de liberación
        // y desconsolidado (M6-08), activos y sin cobro.
        definitions.Should().HaveCount(14);
        definitions.Count(d => d.IsActive).Should().Be(12);
        definitions.Where(d => !d.IsActive).Select(d => d.Code)
            .Should().BeEquivalentTo([ServiceDefinitionCodes.Opening, ServiceDefinitionCodes.Valuation]);

        foreach (var definition in definitions)
        {
            ServiceInputSchema.ValidateSchema(ServiceInputSchema.Parse(definition.InputSchemaJson)!).Should().BeEmpty(definition.Code);
            actions.Should().Contain(definition.ActionCode, definition.Code);
            concepts.Should().Contain(definition.ChargeConceptCode, definition.Code);
            logs.Should().ContainSingle(l => l.EntityId == definition.Id && l.Action == MaintainerActions.Created, definition.Code);

            if (definition.PricingMode != ServicePricingModes.Tariff)
                continue;

            foreach (var country in definition.Countries.Split(','))
            {
                var tariffs = await resolver.GetInForceAsync(new TariffLookup(country, definition.ChargeConceptCode!, date));
                tariffs.Should().NotBeEmpty($"{definition.Code} {country}");
            }
        }
    }

    [Fact]
    public async Task DemoRequests_ShouldCoverEveryStateWithAConsistentTimeline()
    {
        var requests = await _context.ServiceRequests.AsNoTracking().ToListAsync();
        var events = await _context.ServiceRequestEvents.AsNoTracking().ToListAsync();
        var links = await _context.ServiceRequestCharges.AsNoTracking().ToListAsync();
        var charges = await _context.LocalCharges.AsNoTracking().ToListAsync();

        requests.Select(r => r.Status).Should().BeEquivalentTo([
            ServiceRequestStatus.PendingApproval, ServiceRequestStatus.PendingPayment, ServiceRequestStatus.InProgress,
            ServiceRequestStatus.Completed, ServiceRequestStatus.Rejected, ServiceRequestStatus.Cancelled,
            ServiceRequestStatus.Completed, ServiceRequestStatus.Draft,
            // Ola H: refacturación IAO pendiente de pago y de aceptación (M3-11).
            ServiceRequestStatus.PendingPayment,
            // Ola J: certificado de flete completado (M6-02) y carta de liberación pendiente de aprobación (M6-08).
            ServiceRequestStatus.Completed, ServiceRequestStatus.PendingApproval]);

        foreach (var request in requests)
        {
            var timeline = events.Where(e => e.ServiceRequestId == request.Id).OrderBy(e => e.Sequence).ToList();
            timeline.Last().ToStatus.Should().Be(request.Status, request.RequestNumber);
            timeline.Count.Should().Be(request.TimelineSequence, request.RequestNumber);
            timeline.First().FromStatus.Should().BeNull(request.RequestNumber);
            timeline.Skip(1).All(e => e.FromStatus != null).Should().BeTrue(request.RequestNumber);
        }

        foreach (var link in links)
            charges.Should().Contain(c => c.Id == link.LocalChargeId);

        var pendingPayment = requests.Single(r => r.Id == SeedDataIds.ServiceRequestSealsPendingPayment);
        charges.Single(c => c.Id == links.Single(l => l.ServiceRequestId == pendingPayment.Id).LocalChargeId)
            .TotalAmount.Should().Be(pendingPayment.TotalAmount);
        requests.Single(r => r.Id == SeedDataIds.ServiceRequestXomExempt).IsExempt.Should().BeTrue();
        requests.Single(r => r.Id == SeedDataIds.ServiceRequestDropOffPending).AssignedTeam.Should().Be(ServiceTeams.Ed);
    }

    [Fact]
    public async Task ExportDemoBl_ShouldOfferTheExportServices_WithTheLateTierOfTheHouseTransmission()
    {
        var handler = new GetAvailableServicesQueryHandler(_context, EvaluatorFor(SeedDataIds.DemoUserCL), Catalog());

        var result = await handler.Handle(new GetAvailableServicesQuery("HLCUSAI260901610", null, IncludeUnavailable: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Code : null);
        var services = result.Value.Services.ToDictionary(s => s.Code);
        services[ServiceDefinitionCodes.BlHouseTransmission].Available.Should().BeTrue();
        services[ServiceDefinitionCodes.BlHouseTransmission].Quote!.Timing.Should().Be(ServiceTimings.Late);
        services[ServiceDefinitionCodes.BlHouseTransmission].Quote!.TariffCode.Should().Be("FUERA_PLAZO");
        services[ServiceDefinitionCodes.MatrixLate].Available.Should().BeTrue();
        services[ServiceDefinitionCodes.SealManagement].UnavailableReasons.Should().Contain(ServiceUnavailableReasons.AlreadyDeparted);
        services.Should().NotContainKey(ServiceDefinitionCodes.DropOff);
        services.Should().NotContainKey(ServiceDefinitionCodes.ContainerAdministrationXom);
    }

    [Fact]
    public async Task WarehouseChangeHistory_ShouldShowThePayerTaxId_AndTheBulkRequestLine()
    {
        var handler = new GetWarehouseChangeHistoryQueryHandler(_context, EvaluatorFor(SeedDataIds.DemoUserCL));

        var history = await handler.Handle(new GetWarehouseChangeHistoryQuery(PageSize: 50), CancellationToken.None);

        history.IsSuccess.Should().BeTrue();
        var paid = history.Value.Items.Single(i => i.Id == SeedDataIds.WarehouseChange03);
        paid.Payer!.TaxId.Should().Be("76123456-7");
        paid.Payer.Name.Should().Be("Importadora Demo SpA");
        paid.PaymentNumber.Should().Be("PAY-20261002-6F5E4D3C");
        paid.ReceiptNumber.Should().Be("RCP-20261002-7A8B9C0D");
        var bulk = history.Value.Items.Single(i => i.Id == SeedDataIds.WarehouseChange04);
        bulk.BatchId.Should().Be(SeedDataIds.WarehouseChangeBatch01);
        bulk.BatchLineNumber.Should().Be(1);
        bulk.IsFree.Should().BeTrue();
        history.Value.Items.Should().NotContain(i => i.Id == SeedDataIds.WarehouseChange02);
    }

    public void Dispose() => _context.Dispose();
}
