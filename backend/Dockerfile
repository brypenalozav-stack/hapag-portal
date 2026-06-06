# ── Build stage ──
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY HapagPortal.sln ./
COPY src/HapagPortal.Domain/HapagPortal.Domain.csproj src/HapagPortal.Domain/
COPY src/HapagPortal.Application/HapagPortal.Application.csproj src/HapagPortal.Application/
COPY src/HapagPortal.Infrastructure/HapagPortal.Infrastructure.csproj src/HapagPortal.Infrastructure/
COPY src/HapagPortal.WebApi/HapagPortal.WebApi.csproj src/HapagPortal.WebApi/
COPY src/HapagPortal.DatabaseMigrations/HapagPortal.DatabaseMigrations.csproj src/HapagPortal.DatabaseMigrations/

RUN dotnet restore src/HapagPortal.WebApi/HapagPortal.WebApi.csproj
RUN dotnet restore src/HapagPortal.DatabaseMigrations/HapagPortal.DatabaseMigrations.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish src/HapagPortal.WebApi/HapagPortal.WebApi.csproj -c Release -o /app/publish --no-restore
RUN dotnet publish src/HapagPortal.DatabaseMigrations/HapagPortal.DatabaseMigrations.csproj -c Release -o /app/migrations --no-restore

# ── Runtime stage ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /app/migrations ./migrations

# Railway injects PORT env var
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Staging

# Entrypoint: run migrations then start API
COPY docker-entrypoint.sh .
RUN chmod +x docker-entrypoint.sh

EXPOSE 8080
ENTRYPOINT ["./docker-entrypoint.sh"]
