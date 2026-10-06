using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Caching.Memory;

namespace HapagPortal.Infrastructure.Integrations.Tatc;

/// <summary>
/// Caché corta del estado del TATC por BL (M2-09: el portal muestra el estado vigente en el origen, nunca el de
/// una carga anterior). Solo guarda respuestas exitosas durante <c>Integrations:Tatc:CacheSeconds</c> (30 s por
/// defecto; 0 la desactiva) y descarta los BL de una solicitud de generación aceptada para leerlos de nuevo.
/// </summary>
public sealed class CachedTatcProvider(ITatcProvider inner, IMemoryCache cache, TimeSpan timeToLive) : ITatcProvider
{
    private const string KeyPrefix = "tatc:";

    public async Task<Result<BlTatcRecord?>> GetByBlNumberAsync(string blNumber, CancellationToken cancellationToken = default)
    {
        var key = Key(blNumber);
        if (timeToLive > TimeSpan.Zero && cache.TryGetValue(key, out BlTatcRecord? cached))
            return Result<BlTatcRecord?>.Success(cached);

        var result = await inner.GetByBlNumberAsync(blNumber, cancellationToken);
        if (result.IsSuccess && timeToLive > TimeSpan.Zero)
            cache.Set(key, result.Value, timeToLive);

        return result;
    }

    public async Task<Result<TatcGenerationReceipt>> RequestGenerationAsync(
        TatcGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.RequestGenerationAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            foreach (var blNumber in request.BlNumbers)
                cache.Remove(Key(blNumber));
        }

        return result;
    }

    private static string Key(string blNumber) => KeyPrefix + blNumber.Trim().ToUpperInvariant();
}
