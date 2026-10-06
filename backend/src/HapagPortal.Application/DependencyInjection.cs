namespace HapagPortal.Application;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Demurrage.Common;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.PostPayment;
using HapagPortal.Application.ExchangeRates.Common;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.Payments.PostProcessing;
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

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
