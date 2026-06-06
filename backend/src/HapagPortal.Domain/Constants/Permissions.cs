namespace HapagPortal.Domain.Constants;

public static class Permissions
{
    public static class Clients
    {
        public const string Read = "HapagPortal.Clients.Read";
        public const string Create = "HapagPortal.Clients.Create";
        public const string Update = "HapagPortal.Clients.Update";
    }

    public static class BillsOfLading
    {
        public const string Read = "HapagPortal.BillsOfLading.Read";
    }

    public static class Payments
    {
        public const string Read = "HapagPortal.Payments.Read";
        public const string Create = "HapagPortal.Payments.Create";
        public const string Confirm = "HapagPortal.Payments.Confirm";
        public const string Cancel = "HapagPortal.Payments.Cancel";
    }

    public static class LocalCharges
    {
        public const string Read = "HapagPortal.LocalCharges.Read";
        public const string Pay = "HapagPortal.LocalCharges.Pay";
    }

    public static class Demurrage
    {
        public const string Read = "HapagPortal.Demurrage.Read";
        public const string Pay = "HapagPortal.Demurrage.Pay";
    }

    public static class Warehouse
    {
        public const string Read = "HapagPortal.Warehouse.Read";
        public const string Request = "HapagPortal.Warehouse.Request";
    }

    public static class ServiceOrders
    {
        public const string Read = "HapagPortal.ServiceOrders.Read";
        public const string Create = "HapagPortal.ServiceOrders.Create";
    }

    public static class CreditClients
    {
        public const string Read = "HapagPortal.CreditClients.Read";
        public const string Manage = "HapagPortal.CreditClients.Manage";
    }

    public static class DemurrageExemptions
    {
        public const string Read = "HapagPortal.DemurrageExemptions.Read";
        public const string Manage = "HapagPortal.DemurrageExemptions.Manage";
    }

    public static class FAQ
    {
        public const string Read = "HapagPortal.FAQ.Read";
        public const string Manage = "HapagPortal.FAQ.Manage";
    }

    public static class Config
    {
        public const string Read = "HapagPortal.Config.Read";
        public const string Manage = "HapagPortal.Config.Manage";
    }

    public static class Users
    {
        public const string Read = "HapagPortal.Users.Read";
        public const string Manage = "HapagPortal.Users.Manage";
    }
}
