namespace HapagPortal.Application.Configuration.Settings;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Configuration.Common;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record UpsertSettingCommand(string Scope, Guid? ClientId, string Key, string? Value)
    : ICommand<ConfigurationSettingDto>;

public sealed class UpsertSettingCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : ICommandHandler<UpsertSettingCommand, ConfigurationSettingDto>
{
    public async Task<Result<ConfigurationSettingDto>> Handle(
        UpsertSettingCommand request,
        CancellationToken cancellationToken)
    {
        var denied = ConfigurationAccess.Check(currentUser, request.Scope, request.ClientId);
        if (denied is not null)
            return Result<ConfigurationSettingDto>.Failure(denied);

        var existing = await dbContext.ConfigurationSettings
            .FirstOrDefaultAsync(
                s => s.Scope == request.Scope && s.ClientId == request.ClientId && s.Key == request.Key,
                cancellationToken);

        if (existing is null)
        {
            dbContext.ConfigurationSettings.Add(new ConfigurationSetting
            {
                Scope = request.Scope,
                ClientId = request.ClientId,
                Key = request.Key,
                Value = request.Value
            });
        }
        else
        {
            existing.Value = request.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ConfigurationSettingDto>.Success(
            new ConfigurationSettingDto(request.Scope, request.ClientId, request.Key, request.Value));
    }
}
