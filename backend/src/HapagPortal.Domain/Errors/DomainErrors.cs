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

        public static readonly Error CreditImputed =
            new("Cart.CreditImputed", "The item was imputed to the credit line and is no longer pending payment.");

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

        /// <summary>
        /// Otro cierre bloqueó o cambió los mismos ítems al mismo tiempo (concurrencia optimista): no se cobró
        /// nada en esta solicitud. Revisar el historial de pagos y recargar el carro.
        /// </summary>
        public static readonly Error Conflict =
            new("Cart.Conflict", "The cart items were locked or changed by another payment request at the same time. Nothing was charged by this request: reload the cart and check your payment history.");

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

        public static readonly Error CreditNotEligible =
            new("AccountPayment.CreditNotEligible", "The item cannot be imputed to the credit line: its concept is not eligible or not covered by your credit condition.");

        public static readonly Error PaymentDataRequired =
            new("AccountPayment.PaymentDataRequired", "Indicate the payment currency and method for the items paid now.");
    }

    /// <summary>Conceptos imputables a la línea de crédito (M5-10, mantenedor NF-15).</summary>
    public static class CreditImputationRule
    {
        public static Error NotFound(Guid id) =>
            new("CreditImputationRule.NotFound", $"The credit imputation rule '{id}' was not found.");

        public static readonly Error AlreadyExists =
            new("CreditImputationRule.AlreadyExists", "There is already a rule for the concept in the country.");
    }

    /// <summary>Comprobante de depósito bancario y su verificación por Finanzas (M5-06).</summary>
    public static class DepositProof
    {
        public static Error NotFound(Guid id) =>
            new("DepositProof.NotFound", $"The deposit proof '{id}' was not found.");

        public static readonly Error PaymentNotAwaitingProof =
            new("DepositProof.PaymentNotAwaitingProof", "Only deposit payments pending or awaiting verification accept a deposit proof.");

        public static readonly Error PendingReview =
            new("DepositProof.PendingReview", "The payment already has a deposit proof waiting for Finance; wait for its review.");

        public static readonly Error NotPendingReview =
            new("DepositProof.NotPendingReview", "The deposit proof was already reviewed.");

        public static readonly Error ContentNotFound =
            new("DepositProofContent.NotFound", "The file of the deposit proof is not available.");
    }

    /// <summary>Anticipos e imputaciones a crédito y su cruce con las facturas (M7-03, M3-19, NF-04).</summary>
    public static class Settlement
    {
        public static Error NotFound(Guid id) =>
            new("Settlement.NotFound", $"The settlement '{id}' was not found.");

        public static readonly Error AlreadyMatched =
            new("Settlement.AlreadyMatched", "The settlement is already matched to an invoice.");

        public static readonly Error InvoiceNotMatchable =
            new("Settlement.InvoiceNotMatchable", "The invoice cannot be matched: it must be an invoice (not a credit note) of the same shipment and currency, not cancelled or superseded.");
    }

    /// <summary>Estado de cuenta en línea (M7-03).</summary>
    public static class Statement
    {
        public static readonly Error Unavailable =
            new("Statement.Unavailable", "The account statement is not available for your profile.");
    }

    /// <summary>Refacturación IAO con pérdida de IVA (M3-11).</summary>
    public static class Reinvoicing
    {
        public static Error NotFound(Guid id) =>
            new("Reinvoicing.NotFound", $"The re-invoicing request '{id}' was not found.");

        public static readonly Error InvoiceNotEligible =
            new("Reinvoicing.InvoiceNotEligible", "Only issued invoices (with tax folio) of a Chilean shipment that are not cancelled or already re-invoiced can be re-invoiced.");

        public static readonly Error SameTaxId =
            new("Reinvoicing.SameTaxId", "The new legal entity must have a tax ID different from the invoiced one.");

        public static readonly Error AlreadyRequested =
            new("Reinvoicing.AlreadyRequested", "The invoice already has a re-invoicing request in progress.");

        public static readonly Error ApprovalRequired =
            new("Reinvoicing.ApprovalRequired", "Attach the approval of the new legal entity before sending the request.");

        public static readonly Error UseDedicatedFlow =
            new("Reinvoicing.UseDedicatedFlow", "The re-invoicing is requested from the invoice (/reinvoicing), not from the shipment services.");

        public static readonly Error AcceptanceNotFound =
            new("ReinvoicingAcceptance.NotFound", "The acceptance link is not valid.");

        public static readonly Error AcceptanceExpired =
            new("Reinvoicing.AcceptanceExpired", "The acceptance link expired. Ask the requester to send it again.");

        public static readonly Error AcceptanceClosed =
            new("Reinvoicing.AcceptanceClosed", "The charge was already accepted or declined.");

        public static readonly Error AcceptorTaxIdRequired =
            new("Reinvoicing.AcceptorTaxIdRequired", "The name and tax ID of the person accepting are required.");

        public static readonly Error AcceptanceNotPending =
            new("Reinvoicing.AcceptanceNotPending", "The acceptance can only be sent again while it is pending.");

        public static readonly Error InvoiceNotIssued =
            new("Reinvoicing.InvoiceNotIssued", "The new invoice is issued automatically after payment and acceptance; the request cannot be completed manually before.");
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

    /// <summary>Repositorio y emisión de documentos del embarque (M6-01 a M6-09, Ola E).</summary>
    public static class ShipmentDocument
    {
        public static Error NotFound(Guid id) =>
            new("ShipmentDocument.NotFound", $"The document '{id}' was not found.");

        /// <summary>El registro existe pero su archivo no está en el almacenamiento.</summary>
        public static readonly Error ContentNotFound =
            new("ShipmentDocumentContent.NotFound", "The document file is not available in the storage.");

        public static readonly Error NoRecipient =
            new("ShipmentDocument.NoRecipient", "The organization has no registered email to send the document to.");

        public static Error NotAvailable(string documentType, string country) =>
            new("ShipmentDocument.NotAvailable", $"The document '{documentType}' is not available for the '{country}' operation.");
    }

    /// <summary>Carta de responsabilidad (M6-06).</summary>
    public static class ResponsibilityLetter
    {
        public static readonly Error TermsNotAccepted =
            new("ResponsibilityLetter.TermsNotAccepted", "The terms of the responsibility letter must be accepted.");

        public static Error TermsVersionMismatch(string current) =>
            new("ResponsibilityLetter.TermsVersionMismatch", $"The accepted terms are not the current version '{current}'.");
    }

    /// <summary>Certificado de flete, importación de Bolivia (M6-02).</summary>
    public static class FreightCertificate
    {
        public static readonly Error NotApplicable =
            new("FreightCertificate.NotApplicable", "The freight certificate is only issued for Bolivia import shipments.");
    }

    /// <summary>Carta de liberación y desconsolidado, importación de Bolivia (M6-08).</summary>
    public static class ReleaseLetter
    {
        public static readonly Error NotApplicable =
            new("ReleaseLetter.NotApplicable", "The release and deconsolidation letter is only issued for Bolivia import shipments.");

        public static readonly Error AlreadyRequested =
            new("ReleaseLetter.AlreadyRequested", "A release letter of your organization for some of the selected containers is pending approval.");

        public static readonly Error CarrierRequired =
            new("ReleaseLetter.CarrierRequired", "Indicate a registered carrier or the carrier's name and tax ID.");

        public static readonly Error CarrierNotFound =
            new("ReleaseLetter.CarrierNotFound", "The selected carrier is not a registered carrier organization.");

        public static Error TatcNotIssued(string containers) =>
            new("ReleaseLetter.TatcNotIssued", $"The TATC of the selected containers must be issued before approving the letter: {containers}.");

        public static readonly Error TatcUnavailable =
            new("ReleaseLetter.TatcUnavailable", "The TATC system did not answer; the letter cannot be approved until its status is known.");
    }

    /// <summary>Certificado de libre deuda (M6-07).</summary>
    public static class NoDebtCertificate
    {
        public static readonly Error NotApplicable =
            new("NoDebtCertificate.NotApplicable", "The no-debt certificate is only issued for Bolivia import shipments.");

        /// <summary>El detalle enumera los bloqueos (código y referencias) para presentarlos al cliente.</summary>
        public static Error DebtPending(string blockers) =>
            new("NoDebtCertificate.DebtPending", $"The shipment has pending debt: {blockers}");
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

    /// <summary>Reglas de publicación por DIFU de destino final (M2-01).</summary>
    public static class ShipmentPublicationRule
    {
        public static Error NotFound(Guid id) =>
            new("ShipmentPublicationRule.NotFound", $"The publication rule with ID '{id}' was not found.");

        public static readonly Error AlreadyExists =
            new("ShipmentPublicationRule.AlreadyExists", "An active publication rule already exists for this country, final destination and discharge port.");
    }

    /// <summary>Consulta y solicitud masiva de TATC (M2-09).</summary>
    public static class Tatc
    {
        public static readonly Error NotApplicable =
            new("Tatc.NotApplicable", "The TATC applies only to import shipments.");

        public static readonly Error NoValidItems =
            new("Tatc.NoValidItems", "None of the bills of lading can be included in the TATC request.");

        public static Error BatchNotFound(Guid id) =>
            new("TatcBatch.NotFound", $"The TATC request with ID '{id}' was not found.");
    }

    /// <summary>Base de conocimiento y casillas del asistente (M10-02).</summary>
    public static class KnowledgeArticle
    {
        public static Error NotFound(Guid id) =>
            new("KnowledgeArticle.NotFound", $"The knowledge article with ID '{id}' was not found.");
    }

    /// <summary>Conversaciones con el asistente (M10-01, M10-05).</summary>
    public static class AssistantSession
    {
        public static Error NotFound(Guid id) =>
            new("AssistantSession.NotFound", $"The assistant session with ID '{id}' was not found.");

        public static readonly Error Ended =
            new("AssistantSession.Ended", "The assistant session has ended. Start a new conversation.");

        public static readonly Error Expired =
            new("AssistantSession.Expired", "The assistant session expired due to inactivity. Start a new conversation.");

        public static readonly Error RateLimited =
            new("Assistant.RateLimited", "Too many messages in a short time. Wait a moment and try again.");

        public static readonly Error NoRecipient =
            new("AssistantSession.NoRecipient", "There is no e-mail address to send the conversation transcript to.");

        public static Error DeliveryNotFound(Guid id) =>
            new("AssistantDelivery.NotFound", $"The document delivery '{id}' was not found.");
    }

    /// <summary>Definiciones de servicios on demand (M2-03, M2-04).</summary>
    public static class ServiceDefinition
    {
        public static Error NotFound(Guid id) =>
            new("ServiceDefinition.NotFound", $"The service definition with ID '{id}' was not found.");

        public static Error NotFoundByCode(string code) =>
            new("ServiceDefinition.NotFound", $"The service '{code}' was not found.");

        public static readonly Error AlreadyExists =
            new("ServiceDefinition.AlreadyExists", "A service definition with the same code already exists.");

        public static Error UnknownAction(string code) =>
            new("ServiceDefinition.UnknownAction", $"The action '{code}' is not an active action of the access matrix.");

        public static Error InvalidSchema(string reason) =>
            new("ServiceDefinition.InvalidSchema", reason);

        public static Error Invalid(string reason) =>
            new("ServiceDefinition.Invalid", reason);
    }

    /// <summary>Solicitudes de servicios on demand (M2-03, M2-04, M3-07 a M3-15).</summary>
    public static class ServiceRequest
    {
        public static Error NotFound(Guid id) =>
            new("ServiceRequest.NotFound", $"The service request with ID '{id}' was not found.");

        public static Error NotAvailable(string reasons) =>
            new("ServiceRequest.NotAvailable", $"The service cannot be requested for this shipment: {reasons}.");

        public static Error InvalidTransition(string from, string to) =>
            new("ServiceRequest.InvalidTransition", $"The service request cannot change from '{from}' to '{to}'.");

        public static readonly Error NotEditable =
            new("ServiceRequest.NotEditable", "Only draft service requests can be changed.");

        public static readonly Error BillingDataRequired =
            new("ServiceRequest.BillingDataRequired", "The billing data (tax ID, legal name, address and e-mail) is required before requesting the service.");

        public static readonly Error BillingTaxIdNotAllowed =
            new("ServiceRequest.BillingTaxIdNotAllowed", "The billing tax ID must be your organization's or a principal's whose access on the shipment allows the service.");

        public static readonly Error TariffNotAccepted =
            new("ServiceRequest.TariffNotAccepted", "The tariff of the service must be accepted before sending the request.");

        public static Error TariffChanged(decimal total, string currency) =>
            new("ServiceRequest.TariffChanged", $"The tariff changed since it was shown: the current total is {total:0.##} {currency}. Accept it again.");

        public static readonly Error ContainersRequired =
            new("ServiceRequest.ContainersRequired", "Select at least one container of the shipment.");

        public static readonly Error MeasureRequired =
            new("ServiceRequest.MeasureRequired", "The value that determines the tariff tier is required.");

        public static readonly Error NoSourceCharge =
            new("ServiceRequest.NoSourceCharge", "The source system has no pending charge of this service for the shipment.");

        public static readonly Error PaymentInProgress =
            new("ServiceRequest.PaymentInProgress", "The charge of the request is included in a payment in progress and cannot be cancelled.");

        public static readonly Error OutputDocumentRequired =
            new("ServiceRequest.OutputDocumentRequired", "Attach the output document before completing the request.");

        public static readonly Error NotAssignedToTeam =
            new("ServiceRequest.NotAssignedToTeam", "The service request is not waiting for an internal team.");

        public static Error AttachmentNotFound(Guid id) =>
            new("ServiceRequestAttachment.NotFound", $"The attachment '{id}' was not found.");

        public static readonly Error UnknownFileField =
            new("ServiceRequest.UnknownFileField", "The field is not a file field of the service form.");

        /// <summary>El servicio se solicita por su flujo propio (refacturación IAO, certificado de flete, carta de liberación).</summary>
        public static Error UseDedicatedFlow(string definitionCode) =>
            definitionCode == Constants.ServiceDefinitionCodes.IaoReinvoicing
                ? Reinvoicing.UseDedicatedFlow
                : new("ServiceRequest.UseDedicatedFlow", $"The service '{definitionCode}' is requested from the shipment documents (/documents), not from the shipment services.");
    }

    /// <summary>Base de referencia de mercancías peligrosas (M10-06).</summary>
    public static class DangerousGood
    {
        public static Error InvalidImport(string reason) =>
            new("DangerousGood.InvalidImport", $"The dangerous goods file is not valid: {reason}");
    }

    /// <summary>Bandeja de notificaciones y preferencias de correo (M1-25).</summary>
    public static class Notification
    {
        public static Error NotFound(Guid id) =>
            new("Notification.NotFound", $"The notification '{id}' was not found.");

        public static Error UnknownType(string type) =>
            new("Notification.UnknownType", $"The notification type '{type}' does not exist.");

        public static Error EmailNotAvailable(string type) =>
            new("Notification.EmailNotAvailable", $"Notifications of type '{type}' are only shown in the portal inbox.");

        public static Error EmailMandatory(string type) =>
            new("Notification.EmailMandatory", $"Notifications of type '{type}' are always sent by e-mail.");
    }

    /// <summary>Comunicados masivos (M1-26).</summary>
    public static class Announcement
    {
        public static Error NotFound(Guid id) =>
            new("Announcement.NotFound", $"The announcement '{id}' was not found.");

        public static Error InvalidTransition(string from, string to) =>
            new("Announcement.InvalidTransition", $"The announcement cannot change from '{from}' to '{to}'.");

        public static readonly Error Expired =
            new("Announcement.Expired", "The announcement validity already ended; change its dates before publishing it.");
    }

    /// <summary>Modo guía (M1-27).</summary>
    public static class Guide
    {
        public static Error NotFound(string code) =>
            new("Guide.NotFound", $"The guide '{code}' was not found.");

        public static readonly Error AlreadyExists =
            new("Guide.AlreadyExists", "A guide with the same code already exists.");

        public static Error InvalidSteps(string reason) =>
            new("Guide.InvalidSteps", reason);
    }

    /// <summary>«Vista como cliente» (M8-08).</summary>
    public static class Impersonation
    {
        public static Error NotFound(Guid id) =>
            new("ImpersonationSession.NotFound", $"The impersonation session '{id}' was not found.");

        public static readonly Error NotInternalActor =
            new("Impersonation.NotInternalActor", "Only authorized internal Hapag-Lloyd users can view the portal as a client.");

        public static readonly Error Nested =
            new("Impersonation.Nested", "An impersonation session cannot start another one; end the current session first.");

        public static readonly Error TargetNotAllowed =
            new("Impersonation.TargetNotAllowed", "The selected user cannot be impersonated: it must be an active user of a client organization, without internal roles.");

        public static readonly Error TargetNotFound =
            new("Impersonation.TargetNotFound", "The user does not belong to the selected organization.");

        public static readonly Error NotImpersonating =
            new("Impersonation.NotImpersonating", "The current session is not an impersonation session.");

        public static readonly Error Ended =
            new("Impersonation.Ended", "The impersonation session ended or expired.");

        public static readonly Error ReadOnly =
            new("Impersonation.ReadOnly", "The portal is in read-only client view: this action is not allowed while impersonating.");

        public static readonly Error NotActive =
            new("Impersonation.NotActive", "The impersonation session is no longer active.");
    }

    /// <summary>Counter Bolivia/Ultramar (M8-09).</summary>
    public static class Counter
    {
        public static Error NotFound(string blNumber) =>
            new("CounterRecord.NotFound", $"There is no Counter record for the bill of lading '{blNumber}'.");

        public static readonly Error NothingToSync =
            new("Counter.AlreadySynced", "The Counter record is already synchronized with Nexus.");

        public static Error Invalid(string reason) =>
            new("Counter.Invalid", reason);
    }

    /// <summary>Listas de distribución de contactos (M1-06).</summary>
    public static class ContactList
    {
        public static readonly Error NotAllowed =
            new("ContactList.NotAllowed", "Your organization cannot update its distribution lists (M1-11).");

        public static readonly Error MatchCodeRequired =
            new("ContactList.MatchCodeRequired", "The organization has no Match Code yet; the distribution lists are available after its approval.");

        public static Error UnknownReportType(string reportType) =>
            new("ContactList.UnknownReportType", $"The report type '{reportType}' does not exist.");
    }

    /// <summary>Pre-creación de transportistas (M1-09).</summary>
    public static class CarrierPreCreation
    {
        public static Error NotFound(Guid id) =>
            new("CarrierPreRegistration.NotFound", $"The pre-created carrier '{id}' was not found.");

        public static readonly Error NotAllowed =
            new("CarrierPreCreation.NotAllowed", "Your organization cannot pre-create carriers (M1-11).");

        public static Error AlreadyRegistered(string name) =>
            new("CarrierPreCreation.AlreadyRegistered", $"The carrier is already registered in the portal as '{name}': grant it access directly.");

        public static readonly Error NotACarrier =
            new("CarrierPreCreation.NotACarrier", "The tax ID or e-mail belongs to an organization that is not a carrier.");

        public static readonly Error EmailInUse =
            new("CarrierPreCreation.EmailInUse", "The e-mail belongs to a user of another organization.");

        public static readonly Error AlreadyActivated =
            new("CarrierPreCreation.AlreadyActivated", "The carrier already activated its account: grant it access directly.");
    }

    /// <summary>Registro de una cuenta ya pre-creada (M1-09).</summary>
    public static class Registration
    {
        public static readonly Error PreCreatedAccount =
            new("Registration.PreCreatedAccountExists",
                "An account was already created for your organization by a customer. Log in with your e-mail; if you have no password yet, use the invitation code sent to you or request a new one.");
    }

    /// <summary>Empresa matriz (M1-21).</summary>
    public static class ParentLink
    {
        public static Error NotFound(Guid id) =>
            new("ParentLink.NotFound", $"The parent company link '{id}' was not found.");

        public static readonly Error NotAllowed =
            new("ParentLink.NotAllowed", "Your organization cannot share its shipments with a parent company (M1-11).");

        public static readonly Error AlreadyExists =
            new("ParentLink.AlreadyExists", "The organization already has a parent company link pending or active.");

        public static readonly Error SelfLink =
            new("ParentLink.SelfLink", "An organization cannot be its own parent company.");

        public static readonly Error InvalidParent =
            new("ParentLink.InvalidParent", "The parent company must be an approved customer or Freight Forwarder organization.");

        public static readonly Error Cycle =
            new("ParentLink.Cycle", "The parent company is already a subsidiary of this organization.");

        public static readonly Error NotPending =
            new("ParentLink.NotPending", "The parent company link is not pending approval.");

        public static readonly Error NotActive =
            new("ParentLink.NotActive", "The parent company link is not active.");
    }

    /// <summary>Clientes del canal Web Service y sus claves (M3-17, NF-09).</summary>
    public static class ApiClient
    {
        public static Error NotFound(Guid id) =>
            new("ApiClient.NotFound", $"The web service client '{id}' was not found.");

        public static Error KeyNotFound(Guid id) =>
            new("ApiClientKey.NotFound", $"The key '{id}' was not found.");

        public static readonly Error InvalidKey =
            new("WebService.InvalidApiKey", "The API key is missing, unknown, expired or revoked.");

        public static readonly Error AlreadyRevoked =
            new("ApiClient.AlreadyRevoked", "The web service client is already revoked.");

        public static readonly Error OrganizationNotAllowed =
            new("ApiClient.OrganizationNotAllowed", "Web service clients are created for approved client organizations only.");

        public static Error ScopeNotGranted(string scope) =>
            new("WebService.ScopeNotGranted", $"The client is not enabled for '{scope}'.");

        public static readonly Error SignatoryNotConfigured =
            new("WebService.SignatoryNotConfigured", "The organization has no signatory configured for the responsibility letter on this channel.");

        public static readonly Error RateLimited =
            new("WebService.RateLimited", "Too many requests for this client in the last minute. Retry later.");

        public static readonly Error IdempotencyKeyRequired =
            new("WebService.IdempotencyKeyRequired", "The Idempotency-Key header is required (1 to 100 characters).");

        public static readonly Error IdempotencyKeyReused =
            new("IdempotencyKey.Conflict", "The Idempotency-Key was already used with a different request.");

        public static readonly Error IdempotencyInProgress =
            new("IdempotencyKey.Conflict", "A request with the same Idempotency-Key is still being processed. Retry later.");

        public static Error RequestNotFound(Guid id) =>
            new("WebServiceRequest.NotFound", $"The request '{id}' was not found.");
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
