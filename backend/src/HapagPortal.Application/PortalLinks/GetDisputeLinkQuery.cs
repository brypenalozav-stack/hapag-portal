namespace HapagPortal.Application.PortalLinks;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Enlaces externos del portal leídos de la sección <c>PortalLinks</c> de la configuración. <c>Dispute</c>: URL
/// del sitio de Dispute de productos digitales de Hapag-Lloyd por país (M2-05, CT-DISP). La URL definitiva
/// está pendiente de validación con Customer Service.
/// </summary>
public sealed class PortalLinkSettings
{
    public const string SectionName = "PortalLinks";

    public Dictionary<string, string> Dispute { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Acceso al módulo de Dispute (M2-05). <c>Source</c>: <c>Setting</c> (parámetro global del portal, editable sin
/// desplegar), <c>AppSettings</c> (configuración del despliegue) o <c>None</c>. El portal no envía datos del
/// usuario ni del embarque en la URL (CT-DISP); el enlace se abre en una pestaña nueva.
/// </summary>
public sealed record DisputeLinkDto(string Country, string? Url, bool Configured, string Source, bool OpensInNewTab);

public sealed record GetDisputeLinkQuery(string? Country = null) : IQuery<DisputeLinkDto>;

public sealed class GetDisputeLinkQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    PortalLinkSettings settings)
    : IQueryHandler<GetDisputeLinkQuery, DisputeLinkDto>
{
    /// <summary>Clave del parámetro global (<c>/configuration/settings</c>, ámbito Global) por país.</summary>
    public static string SettingKey(string country) => $"portal.dispute-url.{country}";

    private const string FromSetting = "Setting";
    private const string FromAppSettings = "AppSettings";
    private const string NotConfigured = "None";

    public async Task<Result<DisputeLinkDto>> Handle(GetDisputeLinkQuery request, CancellationToken cancellationToken)
    {
        var requested = (request.Country ?? currentUserService.Country ?? CountryCodes.Chile).Trim().ToUpperInvariant();
        var country = CountryCodes.ValidCountries.Contains(requested) ? requested : CountryCodes.Chile;
        var key = SettingKey(country);

        var setting = await dbContext.ConfigurationSettings.AsNoTracking()
            .Where(s => s.Scope == ConfigurationScopes.Global && s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (IsAbsoluteHttpUrl(setting))
            return Result<DisputeLinkDto>.Success(new DisputeLinkDto(country, setting!.Trim(), true, FromSetting, true));

        if (settings.Dispute.TryGetValue(country, out var configured) && IsAbsoluteHttpUrl(configured))
            return Result<DisputeLinkDto>.Success(new DisputeLinkDto(country, configured.Trim(), true, FromAppSettings, true));

        return Result<DisputeLinkDto>.Success(new DisputeLinkDto(country, null, false, NotConfigured, true));
    }

    private static bool IsAbsoluteHttpUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
