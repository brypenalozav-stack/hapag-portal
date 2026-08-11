namespace HapagPortal.Domain.Validation;

/// <summary>
/// Valida el RUT chileno con dígito verificador módulo 11.
/// Acepta formatos con o sin puntos y guión (p. ej. 12.345.678-5 o 123456785).
/// </summary>
public static class RutValidator
{
    public static bool IsValid(string? rut)
    {
        if (string.IsNullOrWhiteSpace(rut))
            return false;

        var clean = rut.Replace(".", string.Empty).Replace("-", string.Empty).Trim().ToUpperInvariant();
        if (clean.Length < 2)
            return false;

        var body = clean[..^1];
        var checkDigit = clean[^1];

        if (!body.All(char.IsDigit))
            return false;

        var sum = 0;
        var factor = 2;
        for (var i = body.Length - 1; i >= 0; i--)
        {
            sum += (body[i] - '0') * factor;
            factor = factor == 7 ? 2 : factor + 1;
        }

        var remainder = 11 - (sum % 11);
        var expected = remainder switch
        {
            11 => '0',
            10 => 'K',
            _ => (char)('0' + remainder)
        };

        return expected == checkDigit;
    }
}
