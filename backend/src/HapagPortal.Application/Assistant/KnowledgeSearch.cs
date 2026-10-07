namespace HapagPortal.Application.Assistant;

using HapagPortal.Domain.Common;
using HapagPortal.Domain.Entities;

/// <summary>
/// Búsqueda en la base de conocimiento del país (M10-02) por coincidencia de palabras: el título y las palabras
/// clave pesan más que el contenido. Solo devuelve un artículo con una coincidencia suficiente en el título o las
/// palabras clave; si no, el asistente indica que no tiene la respuesta y deriva, sin elaborar una propia.
/// </summary>
public static class KnowledgeSearch
{
    private const int TitleWeight = 3;
    private const int KeywordWeight = 3;
    private const int ContentWeight = 1;
    private const int MinScore = 4;
    private const int StemLength = 5;

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "que", "como", "cual", "cuales", "para", "por", "con", "una", "uno", "unos", "unas", "los", "las", "del",
        "mis", "mi", "sus", "este", "esta", "esto", "ese", "esa", "donde", "cuando", "puedo", "puede", "hay",
        "hacer", "tengo", "tiene", "quiero", "necesito", "saber", "sobre", "the", "and", "for", "how", "what",
        "can", "you", "are", "portal", "hapag", "lloyd"
    };

    public static KnowledgeArticle? FindBest(IEnumerable<KnowledgeArticle> articles, string question)
    {
        var tokens = SearchText.Words(question, minLength: 3).Where(t => !StopWords.Contains(t)).Distinct().ToList();
        if (tokens.Count == 0)
            return null;

        KnowledgeArticle? best = null;
        var bestScore = 0;

        foreach (var article in articles)
        {
            var title = SearchText.Words(article.Title, minLength: 3);
            var keywords = SearchText.Words(article.Keywords, minLength: 3);
            var content = SearchText.Words(article.Content, minLength: 3);

            var strong = 0;
            var score = 0;
            foreach (var token in tokens)
            {
                if (Matches(title, token))
                {
                    score += TitleWeight;
                    strong++;
                }
                else if (Matches(keywords, token))
                {
                    score += KeywordWeight;
                    strong++;
                }
                else if (Matches(content, token))
                {
                    score += ContentWeight;
                }
            }

            if (strong == 0 || score < MinScore)
                continue;

            if (score > bestScore || (score == bestScore && best is not null && article.SortOrder < best.SortOrder))
            {
                best = article;
                bestScore = score;
            }
        }

        return best;
    }

    /// <summary>Misma palabra o misma raíz (los primeros caracteres), para tolerar plurales y conjugaciones.</summary>
    private static bool Matches(IReadOnlyList<string> words, string token)
    {
        var stem = token.Length > StemLength ? token[..StemLength] : token;
        return words.Any(w => w == token || (stem.Length == StemLength && w.StartsWith(stem, StringComparison.Ordinal)));
    }
}
