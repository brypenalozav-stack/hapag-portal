namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.AccountPayments;
using HapagPortal.Application.AccountStatement;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.PostPayment;
using HapagPortal.Application.Payments.Commands.Confirm;
using HapagPortal.Application.Payments.PostProcessing;
using HapagPortal.Application.Reinvoicing;
using HapagPortal.Application.ServiceRequests.PostPayment;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using NSubstitute;

/// <summary>
/// Contexto de pruebas de la Ola H sobre el de la Ola G: estado de cuenta (M7-03), cierre con forma de pago por ítem
/// (M5-10), refacturación IAO (M3-11) con correo y fuente de facturación simulados, y la cola posterior al pago con
/// todos sus pasos (liberación, aviso, documentos, solicitudes y emisión de la refacturación).
/// </summary>
public sealed class FinanceFixture
{
    public static readonly DateOnly CreditFrom = new(2026, 1, 1);

    public FinanceFixture()
    {
        Db.ChargeConceptList.Add(new ChargeConcept { Code = ChargeConceptCodes.Isps, Name = "ISPS", Category = ChargeCategories.LocalCharge });
        Db.ChargeConceptList.Add(new ChargeConcept { Code = ChargeConceptCodes.BlFee, Name = "BL Fee", Category = ChargeCategories.LocalCharge });
        Db.ChargeConceptList.Add(new ChargeConcept { Code = ChargeConceptCodes.Reinvoicing, Name = "Refacturación IAO", Category = ChargeCategories.Service });
        Db.ChargeConceptList.Add(new ChargeConcept { Code = ChargeConceptCodes.VatLoss, Name = "Pérdida de IVA", Category = ChargeCategories.Service });

        InvoiceProvider.IssueAsync(default!, default)
            .ReturnsForAnyArgs(call =>
            {
                var request = call.Arg<InvoiceIssueRequest>();
                Issued.Add(request);
                return Task.FromResult(Result<InvoiceDocument>.Success(new InvoiceDocument(
                    $"{900000 + Issued.Count}", request.DocumentType, request.ExternalReference, "ACCEPTED", null, request.IssueDate,
                    request.Totals.TotalAmount, request.Currency, null, null)));
            });
        Email.SendEmailAsync(default!, default!, default!, default)
            .ReturnsForAnyArgs(call =>
            {
                Emails.Add((call.ArgAt<string>(0), call.ArgAt<string>(1), call.ArgAt<string>(2)));
                return Task.CompletedTask;
            });
    }

    public ServiceRequestsFixture Services { get; } = new();
    public PaymentsFixture Payments => Services.Payments;
    public ChargeRulesFixture Rules => Payments.Rules;
    public MockApplicationDbContext Db => Payments.Db;
    public PaymentsFixture.Actor Owner => Payments.Owner;
    public string OwnTaxId => TaxIdNormalizer.Normalize(Owner.Organization.TaxId);
    public IInvoiceProvider InvoiceProvider { get; } = Substitute.For<IInvoiceProvider>();
    public List<InvoiceIssueRequest> Issued { get; } = [];
    public IEmailService Email { get; } = Substitute.For<IEmailService>();
    public List<(string To, string Subject, string Body)> Emails { get; } = [];
    public InMemoryFileStorage Storage { get; } = new();
    public FakePdfDocumentRenderer Renderer { get; } = new();

    /// <summary>El propietario tiene crédito vigente en Nexus (M8-02) con el cupo indicado (CLP).</summary>
    public void Credit(decimal? limit = 5_000_000m, params string[] concepts) =>
        Rules.Conditions(OwnTaxId, false, new CreditCondition(
            concepts.Length == 0 ? [CreditCoverageConcepts.LocalCharges] : concepts, 30, CreditFrom, null, limit, limit is null ? null : "CLP"));

    public CreditImputationRule AddCreditRule(string concept, string country = CountryCodes.Chile, string nexusConcept = CreditCoverageConcepts.LocalCharges)
    {
        var rule = new CreditImputationRule { Country = country, ConceptCode = concept, NexusCreditConcept = nexusConcept, IsEnabled = true };
        Db.CreditImputationRuleList.Add(rule);
        return rule;
    }

    public AccountStatementBuilder StatementBuilder(PaymentsFixture.Actor? actor = null)
    {
        actor ??= Owner;
        return new(Db, actor.Evaluator(Db), Rules.ChargeRules(), Rules.ExchangeRates(), actor.CurrentUser);
    }

    public GetAccountStatementQueryHandler Statement(PaymentsFixture.Actor? actor = null) => new(StatementBuilder(actor));

    public ExportAccountStatementQueryHandler Export(PaymentsFixture.Actor? actor = null) => new(StatementBuilder(actor));

    public CheckoutAccountItemsCommandHandler AccountCheckout(PaymentsFixture.Actor? actor = null)
    {
        actor ??= Owner;
        return new(Db, actor.CurrentUser, Payments.Resolver(actor), Payments.Checkout(actor));
    }

    public ShipmentDocumentService Documents() =>
        new(Db, Renderer, new FakeDocumentSigner(), Storage, Email, new DocumentSettings());

    public ReinvoicingService Reinvoicing() =>
        new(Db, Rules.TariffResolver(), Rules.ExchangeRates(), Email, InvoiceProvider, Payments.Notifications);

    /// <summary>Cola posterior al pago con todos los pasos (NF-03).</summary>
    public PaymentPostProcessor Processor() =>
        Payments.Processor(
            new NotifyServiceRequestsStep(Db, Services.Workflow()),
            new GeneratePaymentDocumentsStep(Db, Documents(), Payments.Notifications),
            new ReinvoicingIssueStep(Db, Reinvoicing(), Services.Workflow()));

    public async Task ProcessOutboxAsync()
    {
        var processor = Processor();
        while (await processor.ProcessDueAsync(50, DateTime.UtcNow, CancellationToken.None) > 0)
        {
        }
    }

    /// <summary>Paga por el carro, confirma y procesa la cola.</summary>
    public async Task<Payment> PayByCartAsync(PaymentsFixture.Actor actor, string billingTaxId, params (string ItemType, Guid SourceId)[] items)
    {
        foreach (var (itemType, sourceId) in items)
        {
            var added = await Payments.Add(actor).Handle(new AddCartItemCommand(itemType, sourceId, null, billingTaxId, "CLP"), CancellationToken.None);
            if (added.IsFailure)
                throw new InvalidOperationException($"{added.Error.Code}: {added.Error.Message}");
        }

        var checkout = await Payments.CheckoutCart(actor).Handle(
            new CheckoutCartCommand(CountryCodes.Chile, "CLP", PaymentMethodCodes.Khipu, $"key-{Guid.NewGuid():N}"), CancellationToken.None);
        if (checkout.IsFailure)
            throw new InvalidOperationException($"{checkout.Error.Code}: {checkout.Error.Message}");

        var payment = Db.PaymentList.Single(p => p.Id == checkout.Value.Payment.Id);
        await ConfirmAsync(payment);
        return payment;
    }

    public async Task ConfirmAsync(Payment payment)
    {
        var confirmed = await new ConfirmPaymentCommandHandler(Db, AccessTestData.CurrentUser(Owner.User))
            .Handle(new ConfirmPaymentCommand(payment.Id), CancellationToken.None);
        if (confirmed.IsFailure)
            throw new InvalidOperationException(confirmed.Error.Code);

        await ProcessOutboxAsync();
    }

    public CustomerInvoice AddInvoice(
        Client organization,
        BillOfLading? bl,
        decimal net,
        decimal tax,
        DateOnly? due,
        string? concept = null,
        string? sii = "100500",
        DateOnly? issue = null,
        string? taxId = null)
    {
        var invoice = Payments.AddInvoice(organization, bl, net + tax, due: due, sii: sii);
        invoice.NetAmount = net;
        invoice.TaxAmount = tax;
        invoice.ConceptCode = concept;
        invoice.IssueDate = issue ?? new DateOnly(2026, 9, 1);
        invoice.TaxId = taxId ?? TaxIdNormalizer.Normalize(organization.TaxId);
        invoice.Country = bl?.Country ?? CountryCodes.Chile;
        return invoice;
    }
}
