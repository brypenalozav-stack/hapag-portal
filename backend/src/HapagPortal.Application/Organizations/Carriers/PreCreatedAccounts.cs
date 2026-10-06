namespace HapagPortal.Application.Organizations.Carriers;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Detección de una cuenta pre-creada al registrarse (M1-09): el correo pertenece al usuario invitado de un
/// transportista pre-creado, o la identificación tributaria (normalizada) coincide con una organización pre-creada del
/// país. En ese caso el registro no crea nada y el usuario es dirigido al ingreso.
/// </summary>
public static class PreCreatedAccounts
{
    public static async Task<bool> ExistsAsync(
        IApplicationDbContext dbContext,
        string normalizedEmail,
        string? taxId,
        string? country,
        CancellationToken cancellationToken)
    {
        var byEmail = await (
                from u in dbContext.Users.AsNoTracking()
                join c in dbContext.Clients.AsNoTracking() on u.ClientId equals c.Id
                where u.Email == normalizedEmail && c.RegistrationStatus == OrganizationStatus.PreCreated
                select u.Id)
            .AnyAsync(cancellationToken);
        if (byEmail)
            return true;

        if (string.IsNullOrWhiteSpace(taxId) || string.IsNullOrWhiteSpace(country))
            return false;

        var preCreated = await dbContext.Clients.AsNoTracking()
            .Where(c => c.RegistrationStatus == OrganizationStatus.PreCreated && c.Country == country)
            .Select(c => c.TaxId)
            .ToListAsync(cancellationToken);
        return preCreated.Any(t => TaxIdNormalizer.AreEqual(t, taxId));
    }
}
