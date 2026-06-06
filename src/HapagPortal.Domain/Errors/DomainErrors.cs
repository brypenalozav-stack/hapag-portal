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
}
