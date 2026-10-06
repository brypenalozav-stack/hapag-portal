using HapagPortal.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HapagPortal.Infrastructure.Integrations.Payments;

/// <summary>
/// Resuelve el <see cref="IPaymentProvider"/> registrado con la clave del medio de pago (M5-03), sea el
/// Dummy o el cliente Real según <c>Integrations:&lt;Proveedor&gt;:Mode</c>.
/// </summary>
public sealed class PaymentProviderResolver(IServiceProvider serviceProvider) : IPaymentProviderResolver
{
    public IPaymentProvider? Resolve(string providerKey) =>
        string.IsNullOrWhiteSpace(providerKey)
            ? null
            : serviceProvider.GetKeyedService<IPaymentProvider>(providerKey);
}
