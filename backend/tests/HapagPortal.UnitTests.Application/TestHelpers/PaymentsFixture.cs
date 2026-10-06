namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.Payments.PostProcessing;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;

/// <summary>
/// Contexto de pruebas de la Ola D sobre el de la Ola C: medios de pago por país (M5-03), proveedores de
/// pago controlables y los servicios del carro y del cierre armados para un actor (usuario + evaluador).
/// </summary>
public sealed class PaymentsFixture
{
    public PaymentsFixture()
    {
        Owner = new Actor(Rules.Organization, Rules.User, Rules.CurrentUser);
        Providers.Register(PaymentProviderKeys.Khipu, Khipu);
        Providers.Register(PaymentProviderKeys.BancoChile, BancoChile);

        AddMethod(CountryCodes.Chile, PaymentMethodCodes.Khipu, PaymentMethodKinds.Online, PaymentProviderKeys.Khipu, "CLP");
        AddMethod(CountryCodes.Chile, PaymentMethodCodes.BankButtonBancoChile, PaymentMethodKinds.Online, PaymentProviderKeys.BancoChile, "CLP,USD");
        AddMethod(CountryCodes.Chile, PaymentMethodCodes.Deposit, PaymentMethodKinds.Deposit, null, "CLP,USD,EUR");
        AddMethod(CountryCodes.Bolivia, PaymentMethodCodes.Deposit, PaymentMethodKinds.Deposit, null, "BOB,USD");
    }

    public sealed record Actor(Client Organization, User User, ICurrentUserService CurrentUser)
    {
        /// <summary>Evaluador nuevo, como en cada solicitud.</summary>
        public ShipmentAccessEvaluator Evaluator(MockApplicationDbContext db) => new(db, CurrentUser);
    }

    public ChargeRulesFixture Rules { get; } = new();
    public MockApplicationDbContext Db => Rules.Db;
    public Actor Owner { get; }
    public ScriptedPaymentProvider Khipu { get; } = new(PaymentProviderKeys.Khipu);
    public ScriptedPaymentProvider BancoChile { get; } = new(PaymentProviderKeys.BancoChile);
    public FakePaymentProviderResolver Providers { get; } = new();
    public FakeNotificationPublisher Notifications { get; } = new();

    public Actor NewActor(string organizationType = OrganizationTypes.Customer, string? profile = RoleCodes.OrgAdmin)
    {
        var actor = ThirdPartyTestData.Organization(Db, organizationType, profile: profile);
        return new Actor(actor.Organization, actor.User, actor.CurrentUser);
    }

    public PayableItemResolver Resolver(Actor actor) =>
        new(Db, actor.Evaluator(Db), Rules.ChargeRules(), Rules.DemurrageStatus(), new PendingResponsibilityLetterStatus());

    public AddCartItemCommandHandler Add(Actor? actor = null)
    {
        actor ??= Owner;
        var resolver = Resolver(actor);
        return new(Db, actor.CurrentUser, resolver, Rules.ExchangeRates(), new CartViewBuilder(Db, resolver));
    }

    public GetCartQueryHandler Cart(Actor? actor = null)
    {
        actor ??= Owner;
        var resolver = Resolver(actor);
        return new(Db, actor.CurrentUser, resolver, new CartViewBuilder(Db, resolver));
    }

    public PaymentCheckoutService Checkout(Actor actor) =>
        new(Db, actor.CurrentUser, Rules.ExchangeRates(), Providers);

    public CheckoutCartCommandHandler CheckoutCart(Actor? actor = null)
    {
        actor ??= Owner;
        return new(Db, actor.CurrentUser, Resolver(actor), Checkout(actor));
    }

    public PaymentPostProcessor Processor(params IPaymentPostStep[] extra) =>
        new(Db, [new ReleasePaymentItemsStep(Db), new NotifyPaymentStep(Notifications), .. extra]);

    public PaymentMethodConfig AddMethod(string country, string code, string kind, string? provider, string currencies, bool enabled = true)
    {
        var method = new PaymentMethodConfig
        {
            Code = code,
            Name = code,
            Country = country,
            Kind = kind,
            ProviderKey = provider,
            Currencies = currencies,
            IsEnabled = enabled
        };
        Db.PaymentMethodConfigList.Add(method);
        return method;
    }

    public void EnableCurrencies(string country, string concept, params string[] currencies)
    {
        foreach (var currency in currencies)
            Db.PaymentCurrencyRuleList.Add(new PaymentCurrencyRule { Country = country, ConceptCode = concept, Currency = currency });
    }

    public CustomerInvoice AddInvoice(
        Client organization,
        BillOfLading? bl,
        decimal total,
        string currency = "CLP",
        string? sii = "100001",
        string status = InvoiceStatus.Pending,
        DateOnly? due = null,
        string type = InvoiceDocumentTypes.Invoice,
        string? source = null,
        bool payable = true)
    {
        var invoice = new CustomerInvoice
        {
            OrganizationId = organization.Id,
            SiiNumber = sii,
            SourceNumber = source ?? $"SRC-{Guid.NewGuid():N}"[..14],
            DocumentType = type,
            IssueDate = new DateOnly(2026, 9, 1),
            DueDate = due ?? new DateOnly(2027, 1, 1),
            BillOfLadingId = bl?.Id,
            BlNumber = bl?.BLNumber,
            BookingNumber = bl?.BookingNumber,
            LegalName = organization.Name,
            TaxId = organization.TaxId,
            NetAmount = total,
            TaxAmount = 0m,
            TotalAmount = total,
            Currency = currency,
            Status = status,
            IsPayable = payable,
            Country = bl?.Country ?? organization.Country,
            SyncedAt = new DateTime(2026, 10, 5, 11, 0, 0, DateTimeKind.Utc),
            Source = "TEST"
        };
        Db.CustomerInvoiceList.Add(invoice);
        return invoice;
    }

    /// <summary>Simula la respuesta de Nexus sin condiciones (disponible) para un RUT normalizado.</summary>
    public void NoConditions(Client organization) =>
        Rules.Conditions(HapagPortal.Application.Common.Helpers.TaxIdNormalizer.Normalize(organization.TaxId), false, null);
}

/// <summary>Proveedor de pago con resultado programable: éxito, fallo o excepción (NF-12).</summary>
public sealed class ScriptedPaymentProvider(string providerCode) : IPaymentProvider
{
    public string ProviderCode { get; } = providerCode;
    public bool VerifiesNotifications => false;
    public Result<PaymentInitiation>? Failure { get; set; }
    public bool Throws { get; set; }
    public List<PaymentInitiationRequest> Requests { get; } = [];

    public Task<Result<PaymentInitiation>> InitiateAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);

        if (Throws)
            throw new HttpRequestException("Connection refused");

        return Task.FromResult(Failure ?? Result<PaymentInitiation>.Success(new PaymentInitiation(
            $"PRV-{request.ExternalReference}", request.ExternalReference, PaymentStatus.Pending, $"https://pay.test/{request.ExternalReference}")));
    }

    public Task<Result<PaymentVerification>> VerifyNotificationAsync(string notificationToken, string externalReference, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<PaymentVerification>.Failure(Error.Unauthorized));
}

public sealed class FakePaymentProviderResolver : IPaymentProviderResolver
{
    private readonly Dictionary<string, IPaymentProvider> _providers = new(StringComparer.Ordinal);

    public void Register(string key, IPaymentProvider provider) => _providers[key] = provider;

    public IPaymentProvider? Resolve(string providerKey) => _providers.GetValueOrDefault(providerKey);
}

/// <summary>Paso posterior al pago que falla las primeras <paramref name="failures"/> veces (NF-03).</summary>
public sealed class FlakyPostStep(string jobType, int failures) : IPaymentPostStep
{
    public int Calls { get; private set; }

    public string JobType { get; } = jobType;

    public Task ExecuteAsync(Payment payment, IReadOnlyList<PaymentDetail> details, DateTime now, CancellationToken cancellationToken)
    {
        Calls++;
        if (Calls <= failures)
            throw new InvalidOperationException($"Source system unavailable (attempt {Calls})");
        return Task.CompletedTask;
    }
}
