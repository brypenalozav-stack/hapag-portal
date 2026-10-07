namespace HapagPortal.UnitTests.Application.TestHelpers;

using MediatR;

/// <summary>
/// <see cref="ISender"/> de pruebas que despacha a manejadores reales registrados por tipo de solicitud, para
/// probar componentes que consultan otras consultas del portal (p. ej. el asistente) con permisos reales.
/// </summary>
public sealed class TestSender : ISender
{
    private readonly Dictionary<Type, Func<object, CancellationToken, Task<object?>>> _handlers = [];

    public TestSender Register<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
        where TRequest : IRequest<TResponse>
    {
        _handlers[typeof(TRequest)] = async (request, cancellationToken) =>
            await handler.Handle((TRequest)request, cancellationToken);
        return this;
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(request.GetType(), out var handler))
            throw new NotSupportedException($"No test handler for {request.GetType().Name}.");

        return (TResponse)(await handler(request, cancellationToken))!;
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
