#!/bin/bash
set -e

echo "=== Running database migrations ==="
# Build connection string from Railway PostgreSQL env vars
export HAPAG_PORTAL_CONNECTION_STRING="Host=${PGHOST};Port=${PGPORT};Database=${PGDATABASE};Username=${PGUSER};Password=${PGPASSWORD};SSL Mode=Require;Trust Server Certificate=true"

# Override appsettings connection string via env var
export ConnectionStrings__DefaultConnection="$HAPAG_PORTAL_CONNECTION_STRING"

# Run migrations
dotnet ./migrations/HapagPortal.DatabaseMigrations.dll --connection-string "$HAPAG_PORTAL_CONNECTION_STRING" || echo "Migration warning (may already be up to date)"

echo "=== Starting API server ==="
exec dotnet HapagPortal.WebApi.dll
