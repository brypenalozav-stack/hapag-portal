using HapagPortal.Domain.Results;

namespace HapagPortal.Domain.Errors;

public static class DomainErrors
{
    public static class Client
    {
        public static Error NotFound(Guid id) =>
            new("Client.NotFound", $"The client with ID '{id}' was not found.");

        public static Error AlreadyExists(string taxId) =>
            new("Client.AlreadyExists", $"A client with Tax ID '{taxId}' already exists.");

        public static readonly Error Inactive =
            new("Client.Inactive", "The client is inactive.");

        public static readonly Error EmailNotConfirmed =
            new("Client.EmailNotConfirmed", "The client's email has not been confirmed.");
    }

    public static class BillOfLading
    {
        public static Error NotFound(Guid id) =>
            new("BillOfLading.NotFound", $"The bill of lading with ID '{id}' was not found.");

        public static Error NotFoundByNumber(string blNumber) =>
            new("BillOfLading.NotFound", $"The bill of lading '{blNumber}' was not found.");

        public static readonly Error HasPayments =
            new("BillOfLading.HasPayments", "The bill of lading has associated payments and cannot be removed.");
    }

    public static class Payment
    {
        public static Error NotFound(Guid id) =>
            new("Payment.NotFound", $"The payment with ID '{id}' was not found.");

        public static readonly Error AlreadyConfirmed =
            new("Payment.AlreadyConfirmed", "The payment has already been confirmed.");

        public static readonly Error AlreadyCancelled =
            new("Payment.AlreadyCancelled", "The payment has already been cancelled.");

        public static readonly Error InvalidStatus =
            new("Payment.InvalidStatus", "The payment is not in a valid status for this operation.");

        public static readonly Error InvalidAmount =
            new("Payment.InvalidAmount", "The payment amount is invalid.");
    }

    /// <summary>Ciclo de vida de los pagos de la Ola D (NF-01, NF-02, NF-03, NF-12, M5-02, M8-07).</summary>
    public static class PaymentFlow
    {
        public static Error InvalidTransition(string from, string to) =>
            new("Payment.InvalidTransition", $"The payment cannot change from '{from}' to '{to}'.");

        public static readonly Error ProviderUnavailable =
            new("Payment.ProviderUnavailable", "The payment platform did not respond. The payment was not processed and no charge was made; you can try again.");

        public static Error Blocked(string message) =>
            new("Payment.Blocked", message);

        public static readonly Error SlipAlreadyIssued =
            new("Payment.SlipAlreadyIssued", "The deposit slip was already issued and can no longer be cancelled by the client. Contact Finance.");

        public static readonly Error InProgress =
            new("Payment.InProgress", "The payment is being processed by the payment platform and cannot be cancelled.");

        public static readonly Error NotDeposit =
            new("Payment.NotDeposit", "Only bank deposit payments issue a deposit slip.");

        public static readonly Error IdempotencyKeyRequired =
            new("Payment.IdempotencyKeyRequired", "The Idempotency-Key header is required.");

        public static readonly Error IdempotencyConflict =
            new("PaymentIdempotency.AlreadyExists", "The idempotency key was already used for a different payment request.");

        public static readonly Error ReceiptNotAvailable =
            new("Payment.ReceiptNotAvailable", "The payment has no receipt yet.");

        public static Error OperationNotFound(Guid id) =>
            new("PaymentOperation.NotFound", $"The post-payment operation '{id}' was not found.");

        public static readonly Error OperationNotRetryable =
            new("PaymentOperation.NotRetryable", "Only pending or stuck post-payment operations can be retried.");
    }

    /// <summary>Carro de compra (M5-01, M5-04, M5-07, M5-08, M5-09, M1-18, M4-02, M4-04).</summary>
    public static class Cart
    {
        public static readonly Error CreditCustomer =
            new("Cart.CreditCustomer", "Customers with credit pay their charges from the account payment view, not from the cart.");

        public static readonly Error AssociationRequired =
            new("Cart.AssociationRequired", "Associate yourself to the bill of lading before adding its charges to the cart.");

        public static readonly Error ResponsibilityLetterRequired =
            new("Cart.ResponsibilityLetterRequired", "The responsibility letter is required before paying this bill of lading.");

        public static readonly Error ZeroValue =
            new("Cart.ZeroValue", "The item has no amount to pay.");

        public static readonly Error NotPayable =
            new("Cart.NotPayable", "The item is not payable.");

        public static readonly Error AlreadyPaid =
            new("Cart.AlreadyPaid", "The item was already paid.");

        public static readonly Error ItemInPayment =
            new("Cart.ItemInPayment", "The item is included in a payment in progress.");

        public static readonly Error Duplicate =
            new("CartItem.AlreadyExists", "The item is already in the cart.");

        public static Error CurrencyNotAllowed(string currency, IEnumerable<string> allowed) =>
            new("Cart.CurrencyNotAllowed", $"The item cannot be paid in {currency}. Enabled currencies: {string.Join(", ", allowed)}.");

        public static readonly Error NoPaymentCurrency =
            new("Cart.NoPaymentCurrency", "The item has no enabled payment currency.");

        public static readonly Error BillingTaxIdNotAllowed =
            new("Cart.BillingTaxIdNotAllowed", "The billing tax ID is not enabled for this item.");

        public static readonly Error PayDemurrageInvoice =
            new("Cart.PayDemurrageInvoice", "The demurrage line is invoiced; add the invoice to the cart instead.");

        public static Error ItemNotFound(Guid id) =>
            new("CartItem.NotFound", $"The cart item '{id}' was not found.");

        public static readonly Error SourceNotFound =
            new("PayableItem.NotFound", "The item to pay was not found.");

        public static readonly Error Empty =
            new("Cart.Empty", "There are no items to pay in this currency.");

        public static readonly Error ItemLocked =
            new("Cart.ItemLocked", "The item belongs to a payment in progress and cannot be changed.");

        public static Error InvalidItem(string blOrConcept, Error inner) =>
            new(inner.Code, $"{blOrConcept}: {inner.Message}");
    }

    /// <summary>Pago de clientes con condición de crédito desde su vista propia (M5-07).</summary>
    public static class AccountPayment
    {
        public static readonly Error NotCreditCustomer =
            new("AccountPayment.NotCreditCustomer", "Only customers with credit pay from the account payment view; use the cart.");

        public static readonly Error MixedCountries =
            new("AccountPayment.MixedCountries", "All the items of one payment must belong to the same country.");
    }

    public static class PaymentMethodConfig
    {
        public static Error NotFound(Guid id) =>
            new("PaymentMethod.NotFound", $"The payment method '{id}' was not found.");

        public static Error NotAvailable(string code, string currency) =>
            new("PaymentMethod.NotAvailable", $"The payment method '{code}' is not enabled for {currency}.");

        public static readonly Error AlreadyExists =
            new("PaymentMethod.AlreadyExists", "A payment method with the same code already exists for the country.");

        public static readonly Error ProviderRequired =
            new("PaymentMethod.ProviderRequired", "Online payment methods require a registered provider; deposit methods have none.");
    }

    public static class PaymentCurrencyRule
    {
        public static Error NotFound(Guid id) =>
            new("PaymentCurrency.NotFound", $"The payment currency rule '{id}' was not found.");

        public static readonly Error AlreadyExists =
            new("PaymentCurrency.AlreadyExists", "The currency is already configured for the concept and country.");
    }

    public static class PaymentBlockWindow
    {
        public static Error NotFound(Guid id) =>
            new("PaymentBlockWindow.NotFound", $"The payment block window '{id}' was not found.");

        public static readonly Error AlreadyStarted =
            new("PaymentBlockWindow.AlreadyStarted", "The block window already started and can no longer be modified.");

        public static readonly Error AlreadyEnded =
            new("PaymentBlockWindow.AlreadyEnded", "The block window already ended.");
    }

    public static class Invoice
    {
        public static Error NotFound(Guid id) =>
            new("Invoice.NotFound", $"The invoice '{id}' was not found.");

        public static readonly Error NotFoundByNumber =
            new("Invoice.NotFound", "The invoice was not found.");

        public static readonly Error PdfNotAvailable =
            new("Invoice.PdfNotAvailable", "The invoice has no issued tax document to download.");

        public static readonly Error FolioRequired =
            new("Invoice.FolioRequired", "Only invoices with an issued tax folio can be downloaded together.");
    }

    public static class LocalCharge
    {
        public static Error NotFound(Guid id) =>
            new("LocalCharge.NotFound", $"The local charge with ID '{id}' was not found.");

        public static readonly Error AlreadyPaid =
            new("LocalCharge.AlreadyPaid", "The local charge has already been paid.");
    }

    public static class DemurrageCharge
    {
        public static Error NotFound(Guid id) =>
            new("DemurrageCharge.NotFound", $"The demurrage charge with ID '{id}' was not found.");

        public static readonly Error AlreadyExempt =
            new("DemurrageCharge.AlreadyExempt", "The demurrage charge is already exempt.");
    }

    public static class User
    {
        public static Error NotFound(Guid id) =>
            new("User.NotFound", $"The user with ID '{id}' was not found.");

        public static Error NotFoundByEmail(string email) =>
            new("User.NotFound", $"The user with email '{email}' was not found.");

        public static readonly Error InvalidCredentials =
            new("User.InvalidCredentials", "The provided credentials are invalid.");

        public static readonly Error Inactive =
            new("User.Inactive", "The user account is inactive.");

        public static readonly Error PendingApproval =
            new("User.PendingApproval", "The request to join the organization is pending approval.");

        public static readonly Error MembershipRejected =
            new("User.MembershipRejected", "The request to join the organization was rejected.");
    }

    public static class Organization
    {
        public static Error NotFound(Guid id) =>
            new("Organization.NotFound", $"The organization with ID '{id}' was not found.");

        public static readonly Error NotFoundByTaxId =
            new("Organization.NotFound", "No registered organization matches the given tax ID and country.");

        public static readonly Error NotOperational =
            new("Organization.NotOperational", "The organization registration has not been approved yet.");

        public static Error InvalidStatus(string status) =>
            new("Organization.InvalidStatus", $"The organization is in status '{status}', which does not allow this step.");

        public static Error MatchCodeExists(string matchCode) =>
            new("Organization.MatchCodeExists", $"The Match Code '{matchCode}' is already assigned to another organization.");

        public static readonly Error CountryNotAvailable =
            new("Organization.CountryNotAvailable", "The organization does not operate in the selected country.");

        public static readonly Error InvalidProfile =
            new("Organization.InvalidProfile", "The profile is not an organization profile.");

        public static readonly Error CannotChangeOwnAccount =
            new("Organization.CannotChangeOwnAccount", "You cannot deactivate or change the profile of your own account.");

        public static Error JoinRequestNotFound(Guid userId) =>
            new("JoinRequest.NotFound", $"The join request for user '{userId}' was not found.");

        public static Error DocumentNotFound(Guid id) =>
            new("OrganizationDocument.NotFound", $"The document with ID '{id}' was not found.");
    }

    public static class ShipmentAccess
    {
        public static Error ActionNotFound(string code) =>
            new("ShipmentAction.NotFound", $"The shipment action '{code}' was not found.");

        public static readonly Error InvalidLevel =
            new("ShipmentAccess.InvalidLevel", "The access level must be Allowed, Denied or OnGrant.");

        public static readonly Error InvalidRole =
            new("ShipmentAccess.InvalidRole", "The role is not a column of the access matrix.");
    }

    public static class AccessGrant
    {
        public static Error NotFound(Guid id) =>
            new("AccessGrant.NotFound", $"The access grant '{id}' was not found.");

        public static Error GranteeNotFound(Guid id) =>
            new("AccessGrant.GranteeNotFound", $"The organization '{id}' cannot receive access.");

        public static readonly Error SelfGrant =
            new("AccessGrant.SelfGrant", "An organization cannot grant access to itself.");

        public static readonly Error NotAllowed =
            new("AccessGrant.NotAllowed", "Your organization cannot grant, change or revoke access on this shipment.");

        public static Error ExceedsGrantorLevel(IEnumerable<string> codes) =>
            new("AccessGrant.ExceedsGrantorLevel",
                $"You cannot grant permissions you do not hold on this shipment: {string.Join(", ", codes)}.");

        public static Error NotGrantable(IEnumerable<string> codes) =>
            new("AccessGrant.NotGrantable",
                $"These permissions cannot be granted to the recipient (level X in M1-11): {string.Join(", ", codes)}.");

        public static Error UnknownActions(IEnumerable<string> codes) =>
            new("AccessGrant.UnknownActions", $"Unknown or inactive action codes: {string.Join(", ", codes)}.");

        public static Error InvalidValidity(string message) =>
            new("AccessGrant.InvalidValidity", message);

        public static readonly Error NotOpen =
            new("AccessGrant.NotOpen", "The access grant is no longer active.");

        public static readonly Error NotPendingAcceptance =
            new("AccessGrant.NotPendingAcceptance", "The mandate is not pending terms acceptance.");

        public static Error TermsVersionMismatch(string current) =>
            new("AccessGrant.TermsVersionMismatch", $"The current mandate terms version is '{current}'.");

        public static readonly Error EarlyBookingNotAllowed =
            new("AccessGrant.EarlyBookingNotAllowed", "Only the customer of the booking can grant early booking access.");

        public static readonly Error EarlyBookingRecipient =
            new("AccessGrant.EarlyBookingRecipient",
                "Early booking access is received by the future shipper or consignee, or by a Freight Forwarder as third party.");
    }

    public static class DefaultGrantee
    {
        public static Error NotFound(Guid id) =>
            new("DefaultGrantee.NotFound", $"The default grantee '{id}' was not found.");

        public static readonly Error AlreadyExists =
            new("DefaultGrantee.AlreadyExists", "The organization is already configured as a default grantee.");
    }

    public static class OpenAccess
    {
        public static readonly Error NotAllowed =
            new("OpenAccess.NotAllowed", "Your organization cannot configure open access by BL number.");

        public static readonly Error NotAvailable =
            new("OpenAccess.NotAvailable", "Self-association is only available for a BL viewed through open access.");
    }

    public static class ShipmentAssociation
    {
        public static readonly Error AlreadyExists =
            new("ShipmentAssociation.AlreadyExists", "Your organization is already associated with this BL.");
    }

    public static class VisibilityWidening
    {
        public static Error NotFound(Guid id) =>
            new("VisibilityWidening.NotFound", $"The visibility widening '{id}' was not found.");

        public static readonly Error InvalidTargetRole =
            new("VisibilityWidening.InvalidTargetRole", "The target role must be Customer, Shipper or Consignee and differ from yours.");

        public static Error NotWidenable(IEnumerable<string> codes) =>
            new("VisibilityWidening.NotWidenable",
                $"These data can only be widened when the target role has level X (o): {string.Join(", ", codes)}.");
    }

    public static class ServiceOrder
    {
        public static Error NotFound(Guid id) =>
            new("ServiceOrder.NotFound", $"The service order with ID '{id}' was not found.");
    }

    public static class CreditClient
    {
        public static Error NotFound(Guid id) =>
            new("CreditClient.NotFound", $"The credit client with ID '{id}' was not found.");

        public static readonly Error LimitExceeded =
            new("CreditClient.LimitExceeded", "The credit limit has been exceeded.");
    }

    public static class DemurrageExemption
    {
        public static Error NotFound(Guid id) =>
            new("DemurrageExemption.NotFound", $"The demurrage exemption with ID '{id}' was not found.");
    }

    public static class FAQ
    {
        public static Error NotFound(Guid id) =>
            new("FAQ.NotFound", $"The FAQ with ID '{id}' was not found.");
    }

    public static class TaxConfiguration
    {
        public static Error NotFound(Guid id) =>
            new("TaxConfiguration.NotFound", $"The tax configuration with ID '{id}' was not found.");
    }

    public static class WarehouseChange
    {
        public static Error NotFound(Guid id) =>
            new("WarehouseChange.NotFound", $"The warehouse change with ID '{id}' was not found.");

        public static Error ContainerNotFound(string containerNumber) =>
            new("WarehouseChange.ContainerNotFound", $"The container '{containerNumber}' does not belong to the bill of lading.");

        public static Error BatchNotFound(Guid id) =>
            new("WarehouseChangeBatch.NotFound", $"The bulk warehouse change request '{id}' was not found.");

        public static readonly Error SameWarehouse =
            new("WarehouseChange.SameWarehouse", "The destination warehouse must be different from the current one.");
    }

    public static class ChargeConcept
    {
        public static Error NotFound(string code) =>
            new("ChargeConcept.NotFound", $"The charge concept '{code}' was not found.");
    }

    public static class Tariff
    {
        public static Error NotFound(Guid id) =>
            new("Tariff.NotFound", $"The tariff with ID '{id}' was not found.");

        public static Error NotInForce(string concept, string country) =>
            new("Tariff.NotInForce", $"There is no tariff in force for '{concept}' in '{country}'.");

        public static Error InvalidTiers(string reason) =>
            new("Tariff.InvalidTiers", reason);

        public static readonly Error Overlaps =
            new("Tariff.Overlaps", "Another active tariff with the same concept, country, currency, container type and code overlaps this validity.");

        public static readonly Error NotCovered =
            new("Tariff.NotCovered", "The measured value falls outside the tariff tiers.");
    }

    public static class InternalChargeRule
    {
        public static Error NotFound(Guid id) =>
            new("InternalChargeRule.NotFound", $"The internal charge rule with ID '{id}' was not found.");

        public static readonly Error MissingIdentifier =
            new("InternalChargeRule.MissingIdentifier", "The rule must identify the account by tax ID or Match Code.");
    }

    public static class ChargeRules
    {
        public static readonly Error ConditionsUnavailable =
            new("ChargeRules.ConditionsUnavailable", "The commercial conditions could not be read from Nexus. Try again later.");

        public static readonly Error NoChargesToApply =
            new("ChargeRules.NoChargesToApply", "The bill of lading has no pending charges to apply rules to.");
    }

    public static class Demurrage
    {
        public static readonly Error InvoiceExists =
            new("Demurrage.InvoiceExists", "The bill of lading already has a demurrage invoice; pay the invoice instead of recalculating.");

        public static readonly Error NotImport =
            new("Demurrage.NotImport", "Demurrage applies only to import bills of lading.");

        public static readonly Error NotArrived =
            new("Demurrage.NotArrived", "The bill of lading has no discharge date yet.");

        public static readonly Error AdvanceNotRequired =
            new("Demurrage.AdvanceNotRequired", "The account is not subject to advance demurrage.");
    }

    public static class ExchangeRate
    {
        public static Error NotAvailable(string from, string to, DateOnly date) =>
            new("ExchangeRate.NotFound", $"There is no exchange rate {from}->{to} for {date:yyyy-MM-dd}.");

        public static Error NotApproved(string from, string to) =>
            new("ExchangeRate.NotApproved", $"The exchange rate {from}->{to} is not approved for transactions.");
    }

    public static class Customs
    {
        public static Error ManifestNotFound(Guid id) =>
            new("Customs.ManifestNotFound", $"El manifiesto con ID '{id}' no existe.");

        public static Error TransmissionNotFound(Guid id) =>
            new("Customs.TransmissionNotFound", $"La transmisión con ID '{id}' no existe.");

        public static readonly Error AlreadyAccepted =
            new("Customs.AlreadyAccepted", "La transmisión ya fue aceptada por Aduana; no admite reintento ni reenvío.");

        public static readonly Error HeaderNotAccepted =
            new("Customs.HeaderNotAccepted", "El encabezado del manifiesto debe estar aceptado antes de transmitir los B/L.");

        public static readonly Error ParentNotTransmitted =
            new("Customs.ParentNotTransmitted", "El B/L padre (Máster) debe estar transmitido y aceptado antes que el B/L Hijo.");

        public static Error IncompleteBL(string reason) =>
            new("Customs.IncompleteBL", $"El B/L no está completo para transmitir: {reason}");
    }

    public static class Integration
    {
        public static Error Unavailable(string system) =>
            new("Integration.Unavailable", $"El sistema externo '{system}' no está disponible.");

        public static Error Timeout(string system) =>
            new("Integration.Timeout", $"El sistema externo '{system}' no respondió a tiempo.");

        public static Error InvalidResponse(string system) =>
            new("Integration.InvalidResponse", $"El sistema externo '{system}' devolvió una respuesta inválida.");

        public static Error NotConfigured(string system) =>
            new("Integration.NotConfigured", $"La integración con '{system}' no está configurada.");
    }
}
