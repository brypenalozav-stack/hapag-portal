namespace HapagPortal.Application.PortalLinks;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Tarifarios oficiales de Hapag-Lloyd que enlaza la página de Tarifas Locales.</summary>
public static class LocalTariffCodes
{
    public const string InlandChile = "INLAND_CL";
    public const string DemurrageDetention = "DEMURRAGE_DETENTION";
    public const string LocalCharges = "LOCAL_CHARGES";

    /// <summary>Orden en que se presentan.</summary>
    public static readonly string[] All = [InlandChile, DemurrageDetention, LocalCharges];
}

/// <summary>Enlace externo resuelto: URL, si está configurada y de dónde viene (parámetro global o despliegue).</summary>
public sealed record ExternalLinkDto(string Code, string? Url, bool Configured, string Source);

/// <summary>
/// Enlaces externos del país: el portal de devoluciones de dinero (se muestra incrustado, con apertura en una
/// ventana nueva) y los tarifarios oficiales (se abren en una pestaña nueva). Sin URL configurada, el portal
/// informa que el acceso aún no está disponible.
/// </summary>
public sealed record ExternalLinksDto(string Country, ExternalLinkDto Refunds, IReadOnlyList<ExternalLinkDto> LocalTariffs);

public sealed record GetExternalLinksQuery(string? Country = null) : IQuery<ExternalLinksDto>;

public sealed class GetExternalLinksQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    PortalLinkSettings settings)
    : IQueryHandler<GetExternalLinksQuery, ExternalLinksDto>
{
    public const string RefundsCode = "REFUNDS";

    /// <summary>Parámetro global (ámbito Global) con la URL del portal de devoluciones por país.</summary>
    public static string RefundsSettingKey(string country) => $"portal.refunds-url.{country}";

    /// <summary>Parámetro global con la URL de un tarifario oficial.</summary>
    public static string TariffSettingKey(string code) => $"portal.tariff-url.{code}";

    private const string FromSetting = "Setting";
    private const string FromAppSettings = "AppSettings";
    private const string NotConfigured = "None";

    public async Task<Result<ExternalLinksDto>> Handle(GetExternalLinksQuery request, CancellationToken cancellationToken)
    {
        var requested = (request.Country ?? currentUserService.Country ?? CountryCodes.Chile).Trim().ToUpperInvariant();
        var country = CountryCodes.ValidCountries.Contains(requested) ? requested : CountryCodes.Chile;

        var keys = LocalTariffCodes.All.Select(TariffSettingKey).Append(RefundsSettingKey(country)).ToList();
        var stored = await dbContext.ConfigurationSettings.AsNoTracking()
            .Where(s => s.Scope == ConfigurationScopes.Global && keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        var refunds = Resolve(RefundsCode, stored.GetValueOrDefault(RefundsSettingKey(country)), settings.Refunds.GetValueOrDefault(country));
        var tariffs = LocalTariffCodes.All
            .Select(code => Resolve(code, stored.GetValueOrDefault(TariffSettingKey(code)), settings.LocalTariffs.GetValueOrDefault(code)))
            .ToList();

        return Result<ExternalLinksDto>.Success(new ExternalLinksDto(country, refunds, tariffs));
    }

    private static ExternalLinkDto Resolve(string code, string? fromSetting, string? fromAppSettings)
    {
        if (IsAbsoluteHttpUrl(fromSetting))
            return new ExternalLinkDto(code, fromSetting!.Trim(), true, FromSetting);
        if (IsAbsoluteHttpUrl(fromAppSettings))
            return new ExternalLinkDto(code, fromAppSettings!.Trim(), true, FromAppSettings);
        return new ExternalLinkDto(code, null, false, NotConfigured);
    }

    private static bool IsAbsoluteHttpUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;
}
