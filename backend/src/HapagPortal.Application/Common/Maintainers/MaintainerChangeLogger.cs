namespace HapagPortal.Application.Common.Maintainers;

using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Entities;

/// <summary>
/// Registro de cambios de los mantenedores internos (NF-15): cada alta, modificación o baja guarda el
/// usuario, la fecha y las instantáneas anterior y nueva, de modo que se pueda reconstruir el valor que
/// estaba vigente en cualquier fecha.
/// </summary>
public static class MaintainerChangeLogger
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static MaintainerChangeLog Log<TSnapshot>(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        string maintainer,
        Guid entityId,
        string action,
        TSnapshot? previous,
        TSnapshot? current,
        DateTime changedAt)
        where TSnapshot : class
    {
        var entry = new MaintainerChangeLog
        {
            Maintainer = maintainer,
            EntityId = entityId,
            Action = action,
            PreviousValue = previous is null ? null : JsonSerializer.Serialize(previous, JsonOptions),
            NewValue = current is null ? null : JsonSerializer.Serialize(current, JsonOptions),
            ChangedAt = changedAt,
            ChangedByUserId = currentUserService.UserId,
            ChangedBy = currentUserService.Email ?? currentUserService.UserId?.ToString() ?? "system"
        };

        dbContext.MaintainerChangeLogs.Add(entry);
        return entry;
    }

    public static TSnapshot? Read<TSnapshot>(string? json)
        where TSnapshot : class =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<TSnapshot>(json, JsonOptions);
}
