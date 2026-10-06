using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using HapagPortal.Application;
using HapagPortal.Infrastructure.DependencyInjection;
using HapagPortal.Infrastructure.Persistence;
using HapagPortal.WebApi.BackgroundServices;
using HapagPortal.WebApi.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Build connection string from Railway PG environment variables if available
var pgHost = Environment.GetEnvironmentVariable("PGHOST");
var pgPort = Environment.GetEnvironmentVariable("PGPORT");
var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
var pgUser = Environment.GetEnvironmentVariable("PGUSER");
var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");

if (!string.IsNullOrEmpty(pgHost))
{
    var connectionString = $"Host={pgHost};Port={pgPort};Database={pgDatabase};Username={pgUser};Password={pgPassword}";
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}

// Application & Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Vencimiento automático de accesos a terceros y mandatos (M1-14, M1-03).
builder.Services.AddHostedService<AccessGrantExpiryWorker>();

// Solicitudes masivas de cambio de almacén en segundo plano (M3-05, NF-19).
builder.Services.AddHostedService<WarehouseChangeBatchWorker>();

// Pasos posteriores a la confirmación de pagos con reintento (NF-03).
builder.Services.AddHostedService<PaymentOutboxWorker>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hapag Portal API",
        Version = "v1",
        Description = "Hapag-Lloyd Customer Portal API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Authentication & Authorization are registered in Infrastructure layer
// via AddInfrastructureServices -> AddJwtAuthentication / AddAuthorizationPolicies

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(
                    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// HttpContextAccessor is registered in Infrastructure layer

// HTTPS detrás del proxy de Railway, que termina el TLS y reenvía X-Forwarded-For/Proto.
// Se limpian las listas de proxies conocidos: se confía en cualquier proxy que envíe X-Forwarded-*,
// aceptable porque el contenedor solo es accesible a través del proxy de Railway. Revisar si el
// servicio pasa a estar expuesto directamente.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = 443);

var app = builder.Build();

// Auto-apply pending migrations on startup
try
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    logger.LogInformation("Checking database connection...");
    var canConnect = await db.Database.CanConnectAsync();
    logger.LogInformation("Database connection: {CanConnect}", canConnect);
    
    var pending = await db.Database.GetPendingMigrationsAsync();
    logger.LogInformation("Pending migrations: {Count} - {Migrations}", 
        pending.Count(), string.Join(", ", pending));
    
    await db.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied successfully");
    
    var applied = await db.Database.GetAppliedMigrationsAsync();
    logger.LogInformation("Applied migrations: {Count} - {Migrations}", 
        applied.Count(), string.Join(", ", applied));
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to apply database migrations: {Message}", ex.Message);
    // Don't crash the app - let it start so we can see the error in health/logs
}

// Middleware pipeline
app.UseForwardedHeaders();

// HTTPS/HSTS fuera de Development; Security:EnforceHttps=false lo desactiva sin redesplegar.
// /health queda fuera de la redirección para el chequeo del proxy.
if (!app.Environment.IsDevelopment() && app.Configuration.GetValue("Security:EnforceHttps", true))
{
    app.UseHsts();
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/health"),
        branch => branch.UseHttpsRedirection());
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();

// Webhooks de pago: el cuerpo crudo se relee en el controlador para verificar la firma (X-Signature).
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1/payments/webhook"))
        context.Request.EnableBuffering();

    await next(context);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hapag Portal API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Hapag Portal API - Swagger UI";
    });
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
