namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.ExchangeRates.Common;
using HapagPortal.Application.Tariffs.Common;
using HapagPortal.Application.WarehouseChanges.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using NSubstitute;

/// <summary>
/// Contexto de pruebas de Ola C: organización cliente operativa con la matriz de M1-11, catálogo de
/// conceptos de cobro y puertos de Nexus/FIS simulados (sin exenciones, sin crédito, tipos de cambio con
/// base USD como el Dummy).
/// </summary>
public sealed class ChargeRulesFixture
{
    public static readonly DateOnly Validity = new(2026, 1, 1);

    private static readonly Dictionary<string, decimal> UnitsPerUsd = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 1m,
        ["CLP"] = 950m,
        ["BOB"] = 6.91m,
        ["EUR"] = 0.92m,
    };

    public ChargeRulesFixture()
    {
        var context = AccessTestData.ClientContext(Db);
        Organization = context.Organization;
        Organization.TaxId = "76.123.456-7";
        Organization.MatchCode = "MC100010";
        User = context.User;
        CurrentUser = context.CurrentUser;
        Evaluator = context.Evaluator;

        SeedCatalog(Db);

        Exemptions.GetExemptionsAsync(default!, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(Result<IReadOnlyList<ExemptionInfo>>.Success([])));
        Credit.GetConditionsAsync(default!, default, default)
            .ReturnsForAnyArgs(Task.FromResult(Result<CustomerConditions?>.Success(null)));
        NexusTariffs.GetTariffsAsync(default!, default!, default, default)
            .ReturnsForAnyArgs(Task.FromResult(Result<IReadOnlyList<TariffItem>>.Success([])));
        Source.GetByBlNumberAsync(default!, default)
            .ReturnsForAnyArgs(Task.FromResult(Result<ShipmentRecord?>.Success(null)));
        Rates.GetRateAsync(default!, default!, default, default)
            .ReturnsForAnyArgs(call =>
            {
                var from = call.ArgAt<string>(0);
                var to = call.ArgAt<string>(1);
                ExchangeRateQuote? quote = UnitsPerUsd.TryGetValue(from, out var f) && UnitsPerUsd.TryGetValue(to, out var t)
                    ? new ExchangeRateQuote(from, to, Math.Round(t / f, 6), call.ArgAt<DateOnly>(2), "TEST", true)
                    : null;
                return Task.FromResult(Result<ExchangeRateQuote?>.Success(quote));
            });
    }

    public MockApplicationDbContext Db { get; } = new();
    public Client Organization { get; }
    public User User { get; }
    public ICurrentUserService CurrentUser { get; }
    public ShipmentAccessEvaluator Evaluator { get; }

    public IExemptionReader Exemptions { get; } = Substitute.For<IExemptionReader>();
    public ICreditConditionReader Credit { get; } = Substitute.For<ICreditConditionReader>();
    public IExchangeRateProvider Rates { get; } = Substitute.For<IExchangeRateProvider>();
    public ITariffProvider NexusTariffs { get; } = Substitute.For<ITariffProvider>();
    public IShipmentSource Source { get; } = Substitute.For<IShipmentSource>();

    public ExchangeRateService ExchangeRates() => new(Db, Rates);

    public ChargeRulesService ChargeRules() =>
        new(Db, Exemptions, Credit, ExchangeRates(), new ResponsibilityLetterStatus(Db));

    public TariffResolver TariffResolver() => new(Db, NexusTariffs);

    public DemurrageStatusBuilder DemurrageStatus() => new(Db, TariffResolver(), ExchangeRates(), Source);

    public WarehouseChangeService WarehouseChanges() => new(Db, ChargeRules(), TariffResolver(), ExchangeRates(), Source);

    /// <summary>Exenciones vigentes de Nexus para un RUT (normalizado, como lo consulta el portal).</summary>
    public void Exempt(string taxId, params ExemptionInfo[] items) =>
        Exemptions.GetExemptionsAsync(taxId, Arg.Any<string?>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<ExemptionInfo>>.Success(items)));

    public void Conditions(string taxId, bool isFreightForwarder, CreditCondition? credit) =>
        Credit.GetConditionsAsync(taxId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CustomerConditions?>.Success(
                new CustomerConditions(taxId, null, isFreightForwarder, credit))));

    public BillOfLading OwnBl(string blNumber, string country = CountryCodes.Chile, bool consignee = true, DateTime? eta = null)
    {
        var bl = AccessTestData.AddBl(Db, Organization.Id, blNumber, country: country);
        bl.ETA = eta ?? new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        if (consignee)
            AccessTestData.AddRole(Db, bl, Organization, ShipmentRoleCodes.Consignee);
        return bl;
    }

    public LocalCharge AddCharge(
        BillOfLading bl,
        string concept,
        decimal amount,
        string currency = "CLP",
        decimal taxRate = 19m,
        string status = ChargeStatus.Pending)
    {
        var tax = taxRate == 0m ? 0m : Math.Round(amount * taxRate / 100m, 2);
        var charge = new LocalCharge
        {
            BillOfLadingId = bl.Id,
            ChargeType = concept,
            Description = concept,
            Amount = amount,
            Currency = currency,
            Status = status,
            IsTaxable = taxRate > 0m,
            TaxRate = taxRate,
            TaxAmount = tax,
            TotalAmount = amount + tax
        };
        Db.LocalChargeList.Add(charge);
        return charge;
    }

    public BLContainer AddContainer(BillOfLading bl, string number, string type)
    {
        var container = new BLContainer
        {
            BillOfLadingId = bl.Id,
            ContainerNumber = number,
            ContainerType = type,
            Status = "Discharged"
        };
        Db.BLContainerList.Add(container);
        return container;
    }

    public Tariff AddTariff(
        string concept,
        decimal amount,
        string currency,
        string country = CountryCodes.Chile,
        string? code = null,
        string? containerType = null,
        DateOnly? validFrom = null,
        DateOnly? validTo = null,
        string tierUnit = TariffTierUnits.None,
        string tierMode = TariffTierModes.Flat,
        params (int From, int? To, decimal Amount)[] tiers)
    {
        var tariff = new Tariff
        {
            ConceptCode = concept,
            Code = code,
            Country = country,
            Currency = currency,
            ContainerType = containerType,
            Amount = amount,
            TierUnit = tierUnit,
            TierMode = tierMode,
            ValidFrom = validFrom ?? Validity,
            ValidTo = validTo
        };

        foreach (var tier in tiers)
            tariff.Tiers.Add(new TariffTier { TariffId = tariff.Id, FromUnit = tier.From, ToUnit = tier.To, Amount = tier.Amount });

        Db.TariffList.Add(tariff);
        return tariff;
    }

    public InternalChargeRule AddRule(string ruleType, string country, string? taxId, string? matchCode, int? maxUses = null)
    {
        var rule = new InternalChargeRule
        {
            RuleType = ruleType,
            Country = country,
            TaxId = taxId,
            MatchCode = matchCode,
            MaxUsesPerBl = maxUses,
            Reason = "Regla de prueba",
            ValidFrom = Validity
        };
        Db.InternalChargeRuleList.Add(rule);
        return rule;
    }

    public static void SeedCatalog(MockApplicationDbContext db)
    {
        var concepts = new (string Code, string Category, bool NexusTariff, bool Exemptible)[]
        {
            (ChargeConceptCodes.GateIn, ChargeCategories.LocalCharge, false, true),
            (ChargeConceptCodes.Eds, ChargeCategories.LocalCharge, false, true),
            (ChargeConceptCodes.GateOut, ChargeCategories.LocalCharge, false, true),
            (ChargeConceptCodes.Ipo, ChargeCategories.LocalCharge, true, false),
            (ChargeConceptCodes.Thc, ChargeCategories.LocalCharge, false, false),
            (ChargeConceptCodes.Mhd, ChargeCategories.Demurrage, false, false),
            (ChargeConceptCodes.Demurrage, ChargeCategories.Demurrage, false, false),
            (ChargeConceptCodes.AdvanceDemurrageBo, ChargeCategories.Demurrage, false, false),
            (ChargeConceptCodes.WarehouseChange, ChargeCategories.Service, true, false),
            (ChargeConceptCodes.LateArrival, ChargeCategories.Service, false, false),
        };

        var order = 0;
        foreach (var c in concepts)
        {
            db.ChargeConceptList.Add(new ChargeConcept
            {
                Code = c.Code,
                Name = c.Code,
                Category = c.Category,
                NexusTariff = c.NexusTariff,
                NexusExemptible = c.Exemptible,
                DisplayOrder = ++order * 10
            });
        }
    }
}
