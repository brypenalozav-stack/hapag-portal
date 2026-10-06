using System.Globalization;
using System.Text;

namespace HapagPortal.Domain.Common;

/// <summary>
/// Texto normalizado para búsquedas libres (asistente, buscador DG): minúsculas, sin acentos y con un solo
/// espacio entre palabras, de modo que "Ácido sulfúrico" y "acido  SULFURICO" coincidan.
/// </summary>
public static class SearchText
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var lastWasSpace = false;

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                    builder.Append(' ');
                lastWasSpace = true;
                continue;
            }

            builder.Append(c);
            lastWasSpace = false;
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
    }

    /// <summary>Palabras normalizadas (solo letras y dígitos) de al menos <paramref name="minLength"/> caracteres.</summary>
    public static IReadOnlyList<string> Words(string? text, int minLength = 1)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        foreach (var c in Normalize(text).Append(' '))
        {
            if (char.IsLetterOrDigit(c))
            {
                current.Append(c);
                continue;
            }

            if (current.Length >= minLength)
                words.Add(current.ToString());
            current.Clear();
        }

        return words;
    }
}
