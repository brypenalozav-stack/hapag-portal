using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using HapagPortal.Application;
using HapagPortal.Infrastructure.DependencyInjection;
using HapagPortal.Infrastructure.Persistence;
using HapagPortal.WebApi.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Application & Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

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
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();

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
