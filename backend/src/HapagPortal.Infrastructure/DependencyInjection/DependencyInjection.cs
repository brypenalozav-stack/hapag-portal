using HapagPortal.Application.Auth.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.Infrastructure.Persistence;
using HapagPortal.Infrastructure.Persistence.Interceptors;
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
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<IPaymentGatewayService, PaymentGatewayService>();

        services.AddHttpContextAccessor();

        services.AddJwtAuthentication(configuration);
        services.AddAuthorizationPolicies();

        return services;
    }
}
