namespace HapagPortal.UnitTests.Application.TestHelpers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

public static class MockDbSetHelper
{
    public static DbSet<T> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        return new TestDbSet<T>(queryable, data);
    }
}

internal sealed class TestDbSet<T> : DbSet<T>, IQueryable<T>, IAsyncEnumerable<T> where T : class
{
    private readonly IQueryable<T> _queryable;
    private readonly List<T> _data;

    public TestDbSet(IQueryable<T> queryable, List<T> data)
    {
        _queryable = new TestAsyncEnumerable<T>(queryable.Expression, queryable.Provider);
        _data = data;
    }

    public override IEntityType EntityType => throw new NotSupportedException("EntityType is not supported in test DbSet.");

    public override Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> Add(T entity)
    {
        _data.Add(entity);
        return null!;
    }

    IQueryProvider IQueryable.Provider => ((IQueryable<T>)_queryable).Provider;
    System.Linq.Expressions.Expression IQueryable.Expression => _queryable.Expression;
    Type IQueryable.ElementType => _queryable.ElementType;
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _data.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _data.GetEnumerator();

    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) =>
        new TestAsyncEnumerator<T>(_data.GetEnumerator());
}

internal sealed class TestAsyncQueryProvider<T>(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) =>
        new TestAsyncEnumerable<T>(expression, inner);

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression, inner);

    public object? Execute(System.Linq.Expressions.Expression expression) =>
        inner.Execute(expression);

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) =>
        inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executeMethod = typeof(IQueryProvider)
            .GetMethods()
            .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
            .MakeGenericMethod(resultType);

        var result = executeMethod.Invoke(inner, [expression]);
        return (TResult)(typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [result])!);
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    private readonly IQueryProvider _provider;

    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression, IQueryProvider provider)
        : base(expression)
    {
        _provider = new TestAsyncQueryProvider<T>(provider);
    }

    IQueryProvider IQueryable.Provider => _provider;

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() =>
        new(inner.MoveNext());
}
