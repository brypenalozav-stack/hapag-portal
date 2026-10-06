namespace HapagPortal.UnitTests.Application.Dashboard;

using FluentAssertions;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Dashboard;
using HapagPortal.Application.ServiceRequests.Requests;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Seguimiento de la Ola G: el dashboard (M1-05) incluye las solicitudes de servicios on demand de la organización en
/// las gestiones en curso, y el cliente consulta las definiciones activas para sus filtros.
/// </summary>
public sealed class DashboardServiceRequestsTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly Client _org;
    private readonly IShipmentAccessEvaluator _evaluator;
    private readonly IChargeRulesService _rules = Substitute.For<IChargeRulesService>();

    public DashboardServiceRequestsTests()
    {
        var client = AccessTestData.ClientContext(_db);
        _org = client.Organization;
        _evaluator = client.Evaluator;
        _rules.GetConditionsAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>())
            .Returns(new CommercialConditionsDto(true, "NEXUS", _org.TaxId, null, false, null, [], null, null, false, false, false, null));
    }

    private ServiceRequest Request(Guid organizationId, string number, string status, DateTime createdAt) => new()
    {
        RequestNumber = number,
        DefinitionCode = ServiceDefinitionCodes.BlCorrection,
        OrganizationId = organizationId,
        BlNumber = "BL-SRV",
        Country = CountryCodes.Chile,
        Operation = ServiceOperations.Import,
        Status = status,
        StatusChangedAt = createdAt,
        CreatedAt = createdAt
    };

    [Fact]
    public async Task Dashboard_ShouldListTheOrganizationServiceRequests()
    {
        var now = DateTime.UtcNow;
        var inProgress = Request(_org.Id, "SRV-1", ServiceRequestStatus.InProgress, now.AddDays(-60));
        var pending = Request(_org.Id, "SRV-2", ServiceRequestStatus.PendingPayment, now.AddDays(-1));
        var recent = Request(_org.Id, "SRV-3", ServiceRequestStatus.Completed, now.AddDays(-2));
        recent.CompletedAt = now.AddDays(-1);
        var old = Request(_org.Id, "SRV-4", ServiceRequestStatus.Completed, now.AddDays(-90));
        old.StatusChangedAt = now.AddDays(-80);
        var foreign = Request(Guid.NewGuid(), "SRV-5", ServiceRequestStatus.InProgress, now);
        _db.ServiceRequestList.AddRange([inProgress, pending, recent, old, foreign]);

        var result = await new GetDashboardQueryHandler(_db, _evaluator, _rules).Handle(new GetDashboardQuery(), CancellationToken.None);

        var requests = result.Value.Requests.Items.Where(r => r.Kind == DashboardTargets.ServiceRequest).ToList();
        requests.Select(r => r.Reference).Should().BeEquivalentTo("SRV-1", "SRV-2", "SRV-3");
        requests.Single(r => r.Reference == "SRV-1").InProgress.Should().BeTrue();
        requests.Single(r => r.Reference == "SRV-2").Status.Should().Be(ServiceRequestStatus.PendingPayment);
        requests.Single(r => r.Reference == "SRV-3").InProgress.Should().BeFalse();
        requests.Single(r => r.Reference == "SRV-3").CompletedAt.Should().Be(recent.CompletedAt);
        requests.Should().OnlyContain(r => r.Target.Kind == DashboardTargets.ServiceRequest && r.Target.Id == r.Id);
        result.Value.Requests.InProgress.Should().Be(2);
    }

    [Fact]
    public async Task Catalog_ShouldListActiveDefinitionsForTheFilters()
    {
        _db.ServiceDefinitionList.AddRange(
        [
            new ServiceDefinition { Code = "SEALS", NameEs = "Sellos", NameEn = "Seals", Operations = "EXPORT", Countries = "CL", ActionCode = "x", DisplayOrder = 2 },
            new ServiceDefinition { Code = ServiceDefinitionCodes.IaoReinvoicing, NameEs = "IAO", NameEn = "IAO", Operations = "IMPORT,EXPORT", Countries = "CL", ActionCode = "x", DisplayOrder = 3 },
            new ServiceDefinition { Code = "XOM", NameEs = "XOM", NameEn = "XOM", Operations = "IMPORT,EXPORT", Countries = "BO", ActionCode = "x", DisplayOrder = 1 },
            new ServiceDefinition { Code = "OLD", NameEs = "Antiguo", NameEn = "Old", Operations = "IMPORT", Countries = "CL", ActionCode = "x", IsActive = false },
        ]);
        var handler = new GetServiceCatalogQueryHandler(_db);

        var all = await handler.Handle(new GetServiceCatalogQuery(), CancellationToken.None);
        var chileImport = await handler.Handle(new GetServiceCatalogQuery("cl", "import"), CancellationToken.None);

        all.Value.Select(d => d.Code).Should().Equal("XOM", "SEALS", ServiceDefinitionCodes.IaoReinvoicing);
        all.Value.Single(d => d.Code == "XOM").Operations.Should().Equal("IMPORT", "EXPORT");
        all.Value.Single(d => d.Code == "SEALS").NameEn.Should().Be("Seals");
        all.Value.Single(d => d.Code == ServiceDefinitionCodes.IaoReinvoicing).DedicatedFlow.Should().BeTrue();
        chileImport.Value.Select(d => d.Code).Should().Equal(ServiceDefinitionCodes.IaoReinvoicing);
    }
}
