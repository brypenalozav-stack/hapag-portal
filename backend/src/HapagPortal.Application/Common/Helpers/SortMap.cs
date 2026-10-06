namespace HapagPortal.Application.Common.Helpers;

using System.Linq.Expressions;

/// <summary>
/// Orden por columna de los listados paginados: una lista blanca de columnas (nombre público → expresión), así el
/// cliente nunca ordena por un campo arbitrario. Sirve para consultas de EF Core y para listas en memoria
/// (<c>AsQueryable()</c>). Dirección: <c>asc</c> o <c>desc</c>; sin <c>sort</c> se usa el orden por defecto del listado.
/// </summary>
public sealed class SortMap<T>
{
    public const string Ascending = "asc";
    public const string Descending = "desc";

    private readonly Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> columns =
        new(StringComparer.OrdinalIgnoreCase);

    public SortMap<T> Add<TKey>(string name, Expression<Func<T, TKey>> key)
    {
        columns[name] = (query, descending) => descending ? query.OrderByDescending(key) : query.OrderBy(key);
        return this;
    }

    public IReadOnlyCollection<string> Names => columns.Keys;

    /// <summary>Valida el nombre recibido; vacío es válido (orden por defecto).</summary>
    public bool IsValid(string? sort) => string.IsNullOrWhiteSpace(sort) || columns.ContainsKey(sort.Trim());

    public static bool IsValidDirection(string? direction) =>
        string.IsNullOrWhiteSpace(direction)
        || string.Equals(direction.Trim(), Ascending, StringComparison.OrdinalIgnoreCase)
        || string.Equals(direction.Trim(), Descending, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Ordena por la columna pedida y luego por el desempate del listado (orden estable entre páginas); sin columna
    /// válida devuelve <paramref name="defaultOrder"/>.
    /// </summary>
    public IOrderedQueryable<T> Apply(
        IQueryable<T> query,
        string? sort,
        string? direction,
        Func<IQueryable<T>, IOrderedQueryable<T>> defaultOrder,
        Func<IOrderedQueryable<T>, IOrderedQueryable<T>> tieBreaker)
    {
        if (string.IsNullOrWhiteSpace(sort) || !columns.TryGetValue(sort.Trim(), out var order))
            return defaultOrder(query);

        var descending = string.Equals(direction?.Trim(), Descending, StringComparison.OrdinalIgnoreCase);
        return tieBreaker(order(query, descending));
    }
}
