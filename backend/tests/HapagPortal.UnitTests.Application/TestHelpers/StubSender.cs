namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Domain.Results;
using MediatR;

/// <summary>
/// <see cref="ISender"/> de pruebas con respuestas fijas (o calculadas) por tipo de solicitud y registro de las
/// solicitudes enviadas, para probar manejadores que componen otras consultas sin montar sus dependencias.
/// </summary>
public sealed class StubSender : ISender
{
    private readonly Dictionary<Type, Func<object, CancellationToken, Task<object>>> _handlers = [];

    public List<object> Sent { get; } = [];

    public StubSender On<TRequest, TValue>(Func<TRequest, Result<TValue>> respond)
        where TRequest : IRequest<Result<TValue>>
    {
        _handlers[typeof(TRequest)] = (request, _) => Task.FromResult<object>(respond((TRequest)request));
        return this;
    }

    public StubSender On<TRequest, TValue>(Result<TValue> response)
        where TRequest : IRequest<Result<TValue>> =>
        On<TRequest, TValue>(_ => response);

    public StubSender OnAsync<TRequest, TValue>(Func<TRequest, CancellationToken, Task<Result<TValue>>> respond)
        where TRequest : IRequest<Result<TValue>>
    {
        _handlers[typeof(TRequest)] = async (request, cancellationToken) => await respond((TRequest)request, cancellationToken);
        return this;
    }

    public IEnumerable<T> SentOf<T>() => Sent.OfType<T>();

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        Sent.Add(request);
        if (!_handlers.TryGetValue(request.GetType(), out var handler))
            throw new NotSupportedException($"No stub for {request.GetType().Name}.");

        return (TResponse)await handler(request, cancellationToken);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest =>
        throw new NotSupportedException();

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
