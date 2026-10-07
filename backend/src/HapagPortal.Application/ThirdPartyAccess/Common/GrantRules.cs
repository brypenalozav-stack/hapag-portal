namespace HapagPortal.Application.ThirdPartyAccess.Common;

using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;

/// <summary>Vigencia resuelta de un acceso (M1-14).</summary>
public sealed record ValidityWindow(string Type, DateTime From, DateTime? To, int? DurationDays)
{
    /// <summary>Un derivado nunca dura más que su origen (M1-22).</summary>
    public ValidityWindow ClampTo(DateTime? parentValidTo) =>
        parentValidTo is null || (To is not null && To <= parentValidTo)
            ? this
            : this with { Type = AccessValidityTypes.UntilDate, To = parentValidTo, DurationDays = null };
}

/// <summary>
/// Reglas comunes del otorgamiento (M1-11, M1-12, M1-14, M1-15): conjunto explícito válido,
/// receptor capaz de recibirlo, nunca más que el otorgante y vigencia por tiempo o por fecha.
/// </summary>
public static class GrantRules
{
    public const int MaxDurationDays = 3650;

    /// <summary>
    /// Normaliza el conjunto explícito: solo acciones activas sobre el embarque y siempre la vista del
    /// BL (sin ella el acceso no mostraría nada). Nulo se conserva: nivel base de M1-11.
    /// </summary>
    public static Result<IReadOnlyList<string>?> NormalizeExplicit(AccessMatrixSnapshot matrix, IReadOnlyList<string>? codes)
    {
        if (codes is null)
            return Result<IReadOnlyList<string>?>.Success(null);

        var known = matrix.ShipmentScopedActions.Select(a => a.Code).ToHashSet(StringComparer.Ordinal);
        var unknown = codes.Where(c => !known.Contains(c)).Distinct().ToList();
        if (unknown.Count > 0)
            return Result<IReadOnlyList<string>?>.Failure(DomainErrors.AccessGrant.UnknownActions(unknown));

        return Result<IReadOnlyList<string>?>.Success(
            matrix.Ordered(codes.Append(ShipmentActionCodes.ViewShipment)));
    }

    /// <summary>X en la columna del receptor: no se le puede otorgar.</summary>
    public static Error? CheckGrantable(
        AccessMatrixSnapshot matrix,
        string granteeOrganizationType,
        string receiverRole,
        IReadOnlyList<string>? explicitActions)
    {
        if (explicitActions is null)
            return null;

        var grantable = matrix.GrantableTo(granteeOrganizationType, receiverRole);
        var notGrantable = explicitActions.Where(c => !grantable.Contains(c)).ToList();
        return notGrantable.Count > 0 ? DomainErrors.AccessGrant.NotGrantable(notGrantable) : null;
    }

    /// <summary>Ningún rol puede otorgar un permiso que él mismo no posee sobre ese BL o booking.</summary>
    public static Error? CheckGrantorLevel(IReadOnlyList<string>? explicitActions, IReadOnlyCollection<string> grantorActions)
    {
        if (explicitActions is null)
            return null;

        var exceeding = explicitActions.Where(c => !grantorActions.Contains(c)).ToList();
        return exceeding.Count > 0 ? DomainErrors.AccessGrant.ExceedsGrantorLevel(exceeding) : null;
    }

    /// <summary>Techo del acceso: lo que el otorgante posee sobre el embarque.</summary>
    public static List<string> CeilingOf(AccessMatrixSnapshot matrix, ShipmentPermissionSet grantorPermissions)
    {
        var shipmentScoped = matrix.ShipmentScopedActions.Select(a => a.Code).ToHashSet(StringComparer.Ordinal);
        return grantorPermissions.AllowedActions.Where(shipmentScoped.Contains).ToList();
    }

    /// <summary>Rol con el que otorga: el propio del embarque o, si accede por un acceso, Tercero.</summary>
    public static string GrantorRoleOf(ShipmentPermissionSet grantorPermissions) =>
        grantorPermissions.OwnActions.Count == 0
            ? ShipmentRoleCodes.ThirdParty
            : grantorPermissions.Roles.FirstOrDefault(r => ShipmentRoleCodes.ShipmentParties.Contains(r))
              ?? ShipmentRoleCodes.ThirdParty;

    /// <summary>
    /// Acceso de origen cuando lo que se otorga proviene de un acceso recibido y no de los roles propios
    /// (M1-22): el que más acciones aporta de las que no son propias.
    /// </summary>
    public static ShipmentGrantAccess? OriginOf(ShipmentPermissionSet grantorPermissions, IEnumerable<string> grantedActions)
    {
        var borrowed = grantedActions.Where(c => !grantorPermissions.OwnActions.Contains(c)).ToList();
        if (borrowed.Count == 0)
            return null;

        return grantorPermissions.Grants
            .OrderByDescending(g => g.Actions.Count(borrowed.Contains))
            .FirstOrDefault(g => g.Actions.Any(borrowed.Contains));
    }

    /// <summary>Vigencia por cantidad de tiempo o hasta una fecha, o sin término (M1-14).</summary>
    public static Result<ValidityWindow> ResolveValidity(
        string validityType,
        DateTime? validFrom,
        DateTime? validTo,
        int? durationDays,
        DateTime now)
    {
        var from = validFrom is null ? now : DateTime.SpecifyKind(validFrom.Value, DateTimeKind.Utc);

        switch (validityType)
        {
            case AccessValidityTypes.Indefinite:
                return Result<ValidityWindow>.Success(new ValidityWindow(validityType, from, null, null));

            case AccessValidityTypes.Duration:
                if (durationDays is null or < 1 or > MaxDurationDays)
                    return Result<ValidityWindow>.Failure(DomainErrors.AccessGrant.InvalidValidity(
                        $"The duration must be between 1 and {MaxDurationDays} days."));
                return Result<ValidityWindow>.Success(
                    new ValidityWindow(validityType, from, from.AddDays(durationDays.Value), durationDays));

            case AccessValidityTypes.UntilDate:
                if (validTo is null)
                    return Result<ValidityWindow>.Failure(DomainErrors.AccessGrant.InvalidValidity("An end date is required."));
                var to = DateTime.SpecifyKind(validTo.Value, DateTimeKind.Utc);
                if (to <= from || to <= now)
                    return Result<ValidityWindow>.Failure(DomainErrors.AccessGrant.InvalidValidity(
                        "The end date must be later than the start date and than now."));
                return Result<ValidityWindow>.Success(new ValidityWindow(validityType, from, to, null));

            default:
                return Result<ValidityWindow>.Failure(DomainErrors.AccessGrant.InvalidValidity(
                    "The validity type must be Indefinite, Duration or UntilDate."));
        }
    }

    /// <summary>Organización que puede recibir accesos: aprobada, activa, externa y distinta del otorgante.</summary>
    public static Error? CheckGrantee(Client? grantee, Guid granteeId, Guid grantorId)
    {
        if (granteeId == grantorId)
            return DomainErrors.AccessGrant.SelfGrant;

        if (grantee is null
            || !grantee.IsActive
            || grantee.RegistrationStatus != OrganizationStatus.Approved
            || grantee.OrganizationType == OrganizationTypes.Internal)
            return DomainErrors.AccessGrant.GranteeNotFound(granteeId);

        return null;
    }
}
