namespace HapagPortal.Application.Configuration.Settings;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Configuration.Common;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record GetSettingsQuery(string Scope, Guid? ClientId) : IQuery<List<ConfigurationSettingDto>>;

public sealed class GetSettingsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IQueryHandler<GetSettingsQuery, List<ConfigurationSettingDto>>
{
    public async Task<Result<List<ConfigurationSettingDto>>> Handle(
        GetSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var denied = ConfigurationAccess.Check(currentUser, request.Scope, request.ClientId);
        if (denied is not null)
            return Result<List<ConfigurationSettingDto>>.Failure(denied);

        var items = await dbContext.ConfigurationSettings
            .AsNoTracking()
            .Where(s => s.Scope == request.Scope && s.ClientId == request.ClientId)
            .Select(s => new ConfigurationSettingDto(s.Scope, s.ClientId, s.Key, s.Value))
            .ToListAsync(cancellationToken);

        return Result<List<ConfigurationSettingDto>>.Success(items);
    }
}
