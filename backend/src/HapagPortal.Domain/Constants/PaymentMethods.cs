namespace HapagPortal.Domain.Constants;

public static class PaymentMethods
{
    public const string CreditCard = "CreditCard";
    public const string DebitCard = "DebitCard";
    public const string BankTransfer = "BankTransfer";
    public const string WebPay = "WebPay";
    public const string Cash = "Cash";
    public const string Check = "Check";
    // Legacy/future methods
    public const string Khipu = "Khipu";
    public const string BankButton = "BankButton";
    public const string Deposit = "Deposit";
    public const string BCI = "BCI";
}
