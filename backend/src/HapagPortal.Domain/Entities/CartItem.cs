using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Ítem del carro: fuente pagable validada al agregarla, su monto en la moneda del cargo y en la moneda
/// de pago elegida (con el tipo de cambio de M5-05 si hubo conversión) y el RUT de facturación elegido al
/// agregarlo (M5-09). Mientras un pago lo incluye queda bloqueado por ese pago; al confirmarse, sale del carro.
/// </summary>
public sealed class CartItem : GuidEntity
{
    public Guid CartId { get; set; }
    public required string ItemType { get; set; }
    public Guid SourceId { get; set; }
    public Guid? BillOfLadingId { get; set; }
    public string? BlNumber { get; set; }
    public string? BookingNumber { get; set; }
    public required string Country { get; set; }
    public required string ConceptCode { get; set; }
    public string? Description { get; set; }

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public required string Currency { get; set; }

    public required string PaymentCurrency { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public DateOnly? RateEffectiveDate { get; set; }
    public string? RateSource { get; set; }

    public required string BillingTaxId { get; set; }
    public required string BillingName { get; set; }
    public Guid? BillingOrganizationId { get; set; }

    /// <summary>NF-14: mandante y acceso otorgado bajo el que se agregó el ítem.</summary>
    public Guid? OnBehalfOfClientId { get; set; }
    public Guid? AccessGrantId { get; set; }

    public Guid AddedByUserId { get; set; }
    public DateTime AddedAt { get; set; }
    public Guid? LockedByPaymentId { get; private set; }

    /// <summary>
    /// Token de concurrencia optimista: cambia en cada bloqueo, desbloqueo o conversión. Dos cierres
    /// simultáneos del mismo sub-carro (aunque usen claves de idempotencia distintas) leen el mismo valor;
    /// solo el primero en guardar bloquea los ítems y el otro recibe un conflicto (NF-01).
    /// </summary>
    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

    public Cart Cart { get; set; } = null!;

    /// <summary>El ítem queda reservado para el pago en curso (no se puede quitar ni convertir).</summary>
    public void LockFor(Guid paymentId)
    {
        LockedByPaymentId = paymentId;
        Touch();
    }

    /// <summary>El pago falló o se anuló: el ítem vuelve a estar disponible en el carro.</summary>
    public void Unlock()
    {
        LockedByPaymentId = null;
        Touch();
    }

    /// <summary>Renueva el token de concurrencia tras modificar el ítem.</summary>
    public void Touch() => ConcurrencyStamp = Guid.NewGuid();
}
