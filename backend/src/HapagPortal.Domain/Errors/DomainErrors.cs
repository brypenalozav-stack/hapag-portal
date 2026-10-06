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
