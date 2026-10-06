namespace HapagPortal.Application;

using FluentValidation;
using HapagPortal.Application.AccountStatement;
using HapagPortal.Application.Announcements;
using HapagPortal.Application.Assistant;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Counter;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.PostPayment;
using HapagPortal.Application.ExchangeRates.Common;
using HapagPortal.Application.Impersonation;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.Payments.PostProcessing;
using HapagPortal.Application.Reinvoicing;
using HapagPortal.Application.Reports.Transactions;
using HapagPortal.Application.ServiceRequests.Common;
using HapagPortal.Application.ServiceRequests.PostPayment;
using HapagPortal.Application.Shipments.Issuance;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Application.Tariffs.Common;
using HapagPortal.Application.WarehouseChanges.Common;
using HapagPortal.Application.Common.Behaviors;
using HapagPortal.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IShipmentAccessEvaluator, ShipmentAccessEvaluator>();

        // Fase 1 Ola C: reglas de cobro, tarifas, tipo de cambio, demurrage y cambio de almacén.
        services.AddScoped<IChargeRulesService, ChargeRulesService>();
        services.AddScoped<ITariffResolver, TariffResolver>();
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        services.AddScoped<DemurrageStatusBuilder>();
        services.AddScoped<WarehouseChangeService>();

        // Fase 1 Ola D: carro, pagos y pasos posteriores a la confirmación (NF-03).
        services.AddScoped<PayableItemResolver>();
        services.AddScoped<PaymentCheckoutService>();
        services.AddScoped<CartViewBuilder>();
        services.AddScoped<PaymentPostProcessor>();
        services.AddScoped<IPaymentPostStep, ReleasePaymentItemsStep>();
        services.AddScoped<IPaymentPostStep, NotifyPaymentStep>();

        // Fase 1 Ola E: documentos del embarque (M6), su emisión tras el pago y la carta FFWW (M4-04).
        // DocumentSettings lo registra Infrastructure desde la sección "Documents" de la configuración.
        services.AddScoped<ShipmentDocumentService>();
        services.AddScoped<NoDebtEvaluator>();
        services.AddScoped<IResponsibilityLetterStatus, ResponsibilityLetterStatus>();
        services.AddScoped<IPaymentPostStep, GeneratePaymentDocumentsStep>();

        // Fase 1 Ola F: emisión del BL (M2-02) y asistente (M10-01 a M10-05). El motor configurado
        // (IAssistantEngine), AssistantSettings y PortalLinkSettings los registra Infrastructure.
        services.AddScoped<ShipmentIssuanceReader>();
        services.AddSingleton<RulesAssistantEngine>();
        services.AddScoped<AssistantDataRetriever>();
        services.AddScoped<AssistantResponder>();

        // Fase 2 Ola G: modelo estándar de servicios on demand (M2-03, M2-04) y avance de sus solicitudes
        // por la liberación del pago (aviso en la cola recuperable, NF-03).
        services.AddScoped<ServiceCatalogEvaluator>();
        services.AddScoped<ServiceRequestWorkflow>();
        services.AddScoped<IPaymentPostStep, NotifyServiceRequestsStep>();

        // Fase 2 Ola H: estado de cuenta (M7-03) y emisión de la refacturación IAO tras el pago y la aceptación (M3-11).
        services.AddScoped<AccountStatementBuilder>();
        services.AddScoped<ReinvoicingService>();
        services.AddScoped<IPaymentPostStep, ReinvoicingIssueStep>();

        // Fase 2 Ola I: comunicados (M1-26), reportería (M9-01), Counter (M8-09) y control de la «Vista como cliente»
        // (M8-08). ImpersonationSettings lo registra Infrastructure desde la sección "Impersonation".
        services.AddScoped<AnnouncementPublisher>();
        services.AddScoped<TransactionReportBuilder>();
        services.AddScoped<CounterSynchronizer>();
        services.AddScoped<ImpersonationGuard>();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
