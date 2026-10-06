namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using FluentAssertions;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.DangerousGoods;
using HapagPortal.Application.Shipments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Integrations.Fis;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

/// <summary>
/// Semilla de la Ola F sobre un proveedor EF real (InMemory): BL14 con destino final Antofagasta sin DIFU queda
/// fuera para el cliente y visible para el administrador (M2-01), los BL guardan el estado de emisión que informa
/// FIS (M2-02), la base de conocimiento por país (M10-02) y la muestra DG (M10-06).
/// </summary>
public sealed class PortalAssistantSeedTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public PortalAssistantSeedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private ShipmentAccessEvaluator EvaluatorFor(Guid userId, params string[] permissions)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.HasPermission(Arg.Any<string>()).Returns(call => permissions.Contains(call.Arg<string>()));
        return new ShipmentAccessEvaluator(_context, currentUser);
    }

    [Fact]
    public async Task UnpublishedBl_ShouldBeHiddenFromTheClientAndVisibleToTheAdmin()
    {
        var client = EvaluatorFor(SeedDataIds.DemoUserCL);
        var admin = EvaluatorFor(SeedDataIds.AdminUser, AccessPermissions.ViewAllShipments);

        var clientBls = await client.FilterAccessible(_context.BillsOfLading.AsNoTracking(), await client.GetScopeAsync())
            .Select(b => b.BLNumber).ToListAsync();
        var adminUnpublished = await ShipmentPublicationFilter
            .Unpublished(admin.FilterAccessible(_context.BillsOfLading.AsNoTracking(), await admin.GetScopeAsync()), _context.ShipmentPublicationRules)
            .Select(b => b.BLNumber).ToListAsync();
        var bl14 = await _context.BillsOfLading.AsNoTracking().SingleAsync(b => b.Id == SeedDataIds.BL14);

        clientBls.Should().Contain("HLCUSAI260601520").And.NotContain("HLCUSAI260601410");
        adminUnpublished.Should().Equal("HLCUSAI260601410");
        (await admin.GetPublicationAsync(bl14)).ReasonCode.Should().Be(ShipmentPublicationReasons.DifuMissing);
        (await client.EvaluateAsync(await client.GetScopeAsync(), bl14)).HasAccess.Should().BeFalse();
    }

    [Fact]
    public async Task SeededBls_ShouldMatchWhatTheDummySourceReports()
    {
        var source = new DummyShipmentSource(NullLogger<DummyShipmentSource>.Instance);
        var bls = await _context.BillsOfLading.AsNoTracking().ToListAsync();

        foreach (var bl in bls)
        {
            var record = (await source.GetByBlNumberAsync(bl.BLNumber)).Value;
            record.Should().NotBeNull(bl.BLNumber);
            bl.FinalDestinationCode.Should().Be(record!.FinalDestinationCode, bl.BLNumber);
        }

        bls.Single(b => b.Id == SeedDataIds.BL04).IssuanceStatus.Should().Be(BlIssuanceStatuses.AuthorizedAtDestination);
        bls.Single(b => b.Id == SeedDataIds.BL07).EblPlatform.Should().Be(EblPlatforms.Wave);
        bls.Single(b => b.Id == SeedDataIds.BL02).TransportDocumentType.Should().Be(TransportDocumentTypes.Swb);
    }

    [Fact]
    public async Task KnowledgeBase_ShouldHaveArticlesAndAMailboxPerCountry()
    {
        var articles = await _context.KnowledgeArticles.AsNoTracking().ToListAsync();
        var mailboxes = await _context.AssistantMailboxes.AsNoTracking().ToListAsync();

        articles.Where(a => a.Country == CountryCodes.Chile).Should().Contain(a => a.SourceFaqId == SeedDataIds.FaqCL03);
        articles.Where(a => a.Country == CountryCodes.Bolivia).Should().Contain(a => a.Title.Contains("CLD"));
        mailboxes.Select(m => (m.Country, m.Topic)).Should().BeEquivalentTo(
            [(CountryCodes.Chile, AssistantTopics.General), (CountryCodes.Bolivia, AssistantTopics.General)]);
        (await _context.MaintainerChangeLogs.CountAsync(c => c.Maintainer == MaintainerNames.KnowledgeArticle)).Should().Be(articles.Count);
    }

    [Fact]
    public async Task DangerousGoodsSample_ShouldBeSearchable()
    {
        var handler = new SearchDangerousGoodsQueryHandler(_context);

        var gasoline = await handler.Handle(new SearchDangerousGoodsQuery("bencina"), CancellationToken.None);
        var lithium = await handler.Handle(new SearchDangerousGoodsQuery("UN3480"), CancellationToken.None);
        var furniture = await handler.Handle(new SearchDangerousGoodsQuery("muebles"), CancellationToken.None);

        gasoline.Value.Items.Should().ContainSingle(i => i.UnNumber == "UN1203");
        lithium.Value.Items.Should().ContainSingle(i => i.HazardClass == "9");
        furniture.Value.ResultCode.Should().Be(DangerousGoodResultCodes.NotClassified);
    }

    public void Dispose() => _context.Dispose();
}
