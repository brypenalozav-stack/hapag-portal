namespace HapagPortal.Domain.Constants;

public static class DocumentPrefixes
{
    public const string Payment = "PAY-";
    public const string Receipt = "RCP-";
    public const string ServiceOrder = "ODS-";

    /// <summary>Boleta para el pago por depósito bancario (M5-02, M5-03).</summary>
    public const string DepositSlip = "BDP-";
}
