namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Resuelve el <see cref="IPaymentProvider"/> registrado con la clave configurada en el medio de pago
/// (M5-03). Cambiar Khipu por otro proveedor = registrar su adaptador con una clave y apuntar el medio a ella.
/// </summary>
public interface IPaymentProviderResolver
{
    /// <summary>Proveedor registrado con la clave, o nulo si no hay ninguno.</summary>
    IPaymentProvider? Resolve(string providerKey);
}
