namespace HapagPortal.UnitTests.Application.DangerousGoods;

using FluentAssertions;
using HapagPortal.Application.DangerousGoods;
using HapagPortal.Application.PortalLinks;
using HapagPortal.Application.Shipments.Publication;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Buscador DG (M10-06): coincidencia exacta por número ONU, parcial por nombre o palabra clave sin importar
/// acentos, resultado explícito "no clasificada" y la importación CSV de la base de referencia; además, el enlace
/// de Dispute por país (M2-05) y el mantenedor de reglas de publicación (M2-01, NF-15).
/// </summary>
public sealed class DangerousGoodSearchTests
{
    private readonly MockApplicationDbContext _db = new();

    public DangerousGoodSearchTests()
    {
        Add("1203", "Gasolina", "Gasoline (motor spirit)", "3", "bencina, nafta");
        Add("1830", "Ácido sulfúrico", "Sulphuric acid", "8", null);
        Add("3480", "Baterías de ion litio", "Lithium ion batteries", "9", "pilas de litio");
        Add(null, "Muebles de madera", "Wooden furniture", null, "muebles");
    }

    private void Add(string? un, string es, string en, string? hazardClass, string? keywords)
    {
        var entry = new DangerousGood
        {
            UnNumber = un, ProperShippingNameEs = es, ProperShippingNameEn = en, HazardClass = hazardClass, Keywords = keywords,
            IsClassified = hazardClass is not null, Source = DangerousGoodSources.Sample, SearchText = string.Empty, CreatedBy = "t"
        };
        entry.SearchText = DangerousGoodCatalog.BuildSearchText(entry);
        _db.DangerousGoodList.Add(entry);
    }

    private async Task<DangerousGoodSearchResultDto> Search(string query) =>
        (await new SearchDangerousGoodsQueryHandler(_db).Handle(new SearchDangerousGoodsQuery(query), CancellationToken.None)).Value;

    [Theory]
    [InlineData("1203")]
    [InlineData("UN1203")]
    [InlineData("un 1203")]
    public async Task ExactUnNumber_ShouldReturnTheClassification(string query)
    {
        var result = await Search(query);

        result.ResultCode.Should().Be(DangerousGoodResultCodes.Classified);
        var item = result.Items.Should().ContainSingle().Subject;
        item.UnNumber.Should().Be("UN1203");
        item.HazardClass.Should().Be("3");
        item.HazardClassNameEs.Should().Be("Líquidos inflamables");
        result.Disclaimer.Should().Contain("No constituye la aprobación operacional");
    }

    [Theory]
    [InlineData("acido sulfurico", "UN1830")]
    [InlineData("SULFÚRICO", "UN1830")]
    [InlineData("bencina", "UN1203")]
    [InlineData("lithium", "UN3480")]
    public async Task PartialNameOrKeyword_ShouldMatchIgnoringAccents(string query, string un)
    {
        var result = await Search(query);

        result.Items.Select(i => i.UnNumber).Should().Equal(un);
        result.Classified.Should().BeTrue();
    }

    [Fact]
    public async Task CargoMarkedAsNotDangerous_ShouldSayItIsNotClassified()
    {
        var result = await Search("muebles");

        result.ResultCode.Should().Be(DangerousGoodResultCodes.NotClassified);
        result.Classified.Should().BeFalse();
        result.Message.Should().Contain("no está clasificada como mercancía peligrosa");
    }

    [Fact]
    public async Task NoMatch_ShouldSayExplicitlyThatThereIsNoClassification()
    {
        var result = await Search("cajas de cartón vacías");

        result.ResultCode.Should().Be(DangerousGoodResultCodes.NoMatch);
        result.Items.Should().BeEmpty();
        result.Message.Should().Contain("no tiene clasificación de mercancía peligrosa");
    }

    [Fact]
    public async Task Import_ShouldCreateUpdateAndReportInvalidRows()
    {
        var currentUser = Substitute.For<HapagPortal.Application.Common.Interfaces.ICurrentUserService>();
        currentUser.Email.Returns("admin@hapag-lloyd.cl");
        const string csv =
            "unNumber;nameEs;nameEn;class;packingGroup;keywords\n" +
            "UN1203;Gasolina;Gasoline (motor spirit);3;II;bencina, combustible\n" +
            "1090;Acetona;Acetone;3;II;solvente\n" +
            "12;Sin número;No number;3;;\n" +
            "1789;\"Ácido clorhídrico; solución\";Hydrochloric acid;8;IV;\n";

        var result = await new ImportDangerousGoodsCommandHandler(_db, currentUser)
            .Handle(new ImportDangerousGoodsCommand(csv), CancellationToken.None);

        result.Value.TotalRows.Should().Be(4);
        result.Value.Created.Should().Be(1);
        result.Value.Updated.Should().Be(1);
        result.Value.Skipped.Should().Be(2);
        result.Value.Errors.Select(e => e.Line).Should().Equal(4, 5);
        (await Search("acetona")).Items.Should().ContainSingle(i => i.UnNumber == "UN1090" && i.Source == DangerousGoodSources.Import);
        (await Search("combustible")).Items.Should().ContainSingle(i => i.UnNumber == "UN1203" && i.PackingGroup == "II");
        _db.MaintainerChangeLogList.Should().HaveCount(2).And.OnlyContain(c => c.Maintainer == MaintainerNames.DangerousGood);
    }

    [Fact]
    public async Task Import_WithoutRequiredColumns_ShouldFail()
    {
        var result = await new ImportDangerousGoodsCommandHandler(_db, Substitute.For<HapagPortal.Application.Common.Interfaces.ICurrentUserService>())
            .Handle(new ImportDangerousGoodsCommand("un,name\n1203,Gasolina"), CancellationToken.None);

        result.Error.Code.Should().Be("DangerousGood.InvalidImport");
    }

    [Fact]
    public async Task DisputeLink_ShouldPreferThePortalSettingOverAppSettings()
    {
        var currentUser = Substitute.For<HapagPortal.Application.Common.Interfaces.ICurrentUserService>();
        currentUser.Country.Returns(CountryCodes.Bolivia);
        var settings = new PortalLinkSettings { Dispute = { ["CL"] = "https://dispute.example/cl", ["BO"] = "https://dispute.example/bo" } };
        _db.ConfigurationSettingList.Add(new ConfigurationSetting
        {
            Scope = ConfigurationScopes.Global, Key = GetDisputeLinkQueryHandler.SettingKey("CL"), Value = "https://portal.example/dispute-cl", CreatedBy = "t"
        });
        var handler = new GetDisputeLinkQueryHandler(_db, currentUser, settings);

        var chile = await handler.Handle(new GetDisputeLinkQuery("cl"), CancellationToken.None);
        var bolivia = await handler.Handle(new GetDisputeLinkQuery(), CancellationToken.None);
        var none = await new GetDisputeLinkQueryHandler(_db, currentUser, new PortalLinkSettings())
            .Handle(new GetDisputeLinkQuery("BO"), CancellationToken.None);

        chile.Value.Should().Be(new DisputeLinkDto("CL", "https://portal.example/dispute-cl", true, "Setting", true));
        bolivia.Value.Url.Should().Be("https://dispute.example/bo");
        bolivia.Value.Source.Should().Be("AppSettings");
        none.Value.Configured.Should().BeFalse();
        none.Value.Url.Should().BeNull();
    }

    [Fact]
    public async Task PublicationRules_ShouldRejectDuplicatesAndKeepTheChangeLog()
    {
        var currentUser = Substitute.For<HapagPortal.Application.Common.Interfaces.ICurrentUserService>();
        currentUser.Email.Returns("admin@hapag-lloyd.cl");
        var create = new CreateShipmentPublicationRuleCommandHandler(_db, currentUser);

        var created = await create.Handle(new CreateShipmentPublicationRuleCommand("cl", "clanf", "Antofagasta", "clsai", null), CancellationToken.None);
        var duplicate = await create.Handle(new CreateShipmentPublicationRuleCommand("CL", "CLANF", null, "CLSAI", null), CancellationToken.None);
        await new DeactivateShipmentPublicationRuleCommandHandler(_db, currentUser)
            .Handle(new DeactivateShipmentPublicationRuleCommand(created.Value.Id), CancellationToken.None);
        var history = await new GetShipmentPublicationRuleHistoryQueryHandler(_db)
            .Handle(new GetShipmentPublicationRuleHistoryQuery(created.Value.Id), CancellationToken.None);

        created.Value.FinalDestinationCode.Should().Be("CLANF");
        created.Value.DischargePortCode.Should().Be("CLSAI");
        duplicate.Error.Code.Should().Be("ShipmentPublicationRule.AlreadyExists");
        history.Value.Select(h => h.Action).Should().BeEquivalentTo([MaintainerActions.Created, MaintainerActions.Deactivated]);
        history.Value.Should().OnlyContain(h => h.ChangedBy == "admin@hapag-lloyd.cl");
    }
}
