namespace HapagPortal.Application.Config.Features;

using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;

/// <summary>Flags de funcionalidades para el frontend: nombre → encendido.</summary>
public sealed record GetFeaturesQuery : IQuery<IReadOnlyDictionary<string, bool>>;

public sealed class GetFeaturesQueryHandler(FeatureSettings features)
    : IQueryHandler<GetFeaturesQuery, IReadOnlyDictionary<string, bool>>
{
    public Task<Result<IReadOnlyDictionary<string, bool>>> Handle(GetFeaturesQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result<IReadOnlyDictionary<string, bool>>.Success(features.Snapshot()));
}
