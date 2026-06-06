FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HapagPortal.sln ./
COPY src/HapagPortal.Domain/HapagPortal.Domain.csproj src/HapagPortal.Domain/
COPY src/HapagPortal.Application/HapagPortal.Application.csproj src/HapagPortal.Application/
COPY src/HapagPortal.Infrastructure/HapagPortal.Infrastructure.csproj src/HapagPortal.Infrastructure/
COPY src/HapagPortal.WebApi/HapagPortal.WebApi.csproj src/HapagPortal.WebApi/
COPY src/HapagPortal.DatabaseMigrations/HapagPortal.DatabaseMigrations.csproj src/HapagPortal.DatabaseMigrations/

RUN dotnet restore src/HapagPortal.WebApi/HapagPortal.WebApi.csproj

COPY . .
RUN dotnet publish src/HapagPortal.WebApi/HapagPortal.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Staging

CMD ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet HapagPortal.WebApi.dll
