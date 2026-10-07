using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Config.Features;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Impersonation;
using HapagPortal.Application.PortalLinks;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.Infrastructure.Customs;
using HapagPortal.Infrastructure.Documents;
using HapagPortal.Infrastructure.Notifications;
using HapagPortal.Infrastructure.Persistence;
using HapagPortal.Infrastructure.Persistence.Interceptors;
using HapagPortal.Infrastructure.Secrets;
using HapagPortal.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HapagPortal.Infrastructure.DependencyInjection;

public static partial class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("HapagPortal.DatabaseMigrations");
                    npgsqlOptions.CommandTimeout(120);
                });
            options.AddInterceptors(interceptor);
            options.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<ISecretProtector, AesSecretProtector>();
        services.AddScoped<ISecretResolver, SecretResolver>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<ICustomsTransmitter, StubCustomsTransmitter>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        services.AddSingleton<IWebhookAuthenticator, WebhookAuthenticator>();

        // Generación documental (Ola E): plantilla PDF con PDFsharp/MigraDoc y configuración "Documents".
        services.AddSingleton<IPdfDocumentRenderer, MigraDocPdfRenderer>();
        var documentSettings = configuration.GetSection(DocumentSettings.SectionName).Get<DocumentSettings>() ?? new DocumentSettings();
        // M6-02: la primera entrega del certificado de flete es sin pago; el modo pagado (BOB) aún no existe.
        if (documentSettings.FreightCertificateMode != FreightCertificateModes.Free)
            throw new InvalidOperationException(
                $"Documents:FreightCertificateMode '{documentSettings.FreightCertificateMode}' is not available: only '{FreightCertificateModes.Free}' is supported until the BOB payment flow is defined.");
        services.AddSingleton(documentSettings);

        // Ola I: «Vista como cliente» (M8-08), sección "Impersonation" (duración y escrituras permitidas; vacío = solo consulta).
        services.AddSingleton(configuration.GetSection(ImpersonationSettings.SectionName).Get<ImpersonationSettings>() ?? new ImpersonationSettings());

        // Cierre de Fase 1: flags de funcionalidades (sección "Features"); Fase 2 apagada por defecto, salvo la carta de
        // liberación y Counter. Se reactivan por configuración (Features__X=true).
        services.AddSingleton(new FeatureSettings(configuration.GetSection(FeatureSettings.SectionName).Get<Dictionary<string, bool>>()));

        services.AddIntegrations(configuration);

        // Ola F: motor del asistente (M10-01) y enlaces externos del portal, como el de Dispute (M2-05).
        services.AddAssistantEngine(configuration);
        services.AddSingleton(configuration.GetSection(PortalLinkSettings.SectionName).Get<PortalLinkSettings>() ?? new PortalLinkSettings());

        services.AddHttpContextAccessor();

        services.AddJwtAuthentication(configuration);
        services.AddAuthorizationPolicies();

        return services;
    }
}
