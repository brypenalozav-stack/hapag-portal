@echo off
setlocal EnableExtensions
chcp 65001 >nul
title Portal Hapag-Lloyd - depuracion local

rem ============================================================================
rem  Levanta el portal en modo depuracion y abre el navegador:
rem    1. PostgreSQL en localhost:5432 (usa el que ya este corriendo; si no hay,
rem       levanta uno embebido sin Docker, datos en scripts\dev\.pgdata)
rem    2. API .NET en http://localhost:5072 (Swagger en /swagger). Las migraciones
rem       y los datos de demo se aplican solos al iniciar.
rem    3. Frontend Angular (ng serve, recarga en caliente) en http://localhost:4200
rem       con proxy de /api hacia la API.
rem  Cada servicio queda en su propia ventana; ciérrelas para detenerlos.
rem  Usuarios de demo: p. ej. admin@hapag-lloyd.cl / demo@importadorademo.cl,
rem  contraseña Admin123!
rem ============================================================================

set "ROOT=%~dp0"
set "API_URL=http://localhost:5072"
set "WEB_URL=http://localhost:4200"

rem --- Secretos solo para desarrollo local (nunca usar en otros ambientes) ---
set "ASPNETCORE_ENVIRONMENT=Development"
set "Jwt__Secret=local-dev-secret-0123456789abcdef0123456789abcdef"
set "Secrets__MasterKey=MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY="
set "Integrations__Storage__Mode=Local"
set "ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=HapagPortalDb;Username=postgres;Password=postgres"
rem El proyecto apunta a .NET 9; permite ejecutarlo con un runtime mas nuevo si es el unico instalado.
set "DOTNET_ROLL_FORWARD=LatestMajor"

echo.
echo === Verificando requisitos ===
where dotnet >nul 2>&1 || (echo [ERROR] No se encontro el SDK de .NET. Instale .NET 9 o superior: https://dotnet.microsoft.com/download & goto :fin_error)
where node >nul 2>&1 || (echo [ERROR] No se encontro Node.js. Instale Node 20 o superior: https://nodejs.org & goto :fin_error)
where npm >nul 2>&1 || (echo [ERROR] No se encontro npm. & goto :fin_error)
echo OK: dotnet y node disponibles.

echo.
echo === 1/3 PostgreSQL ===
call :puerto_activo 5432
if %ERRORLEVEL%==0 (
  echo Ya hay un PostgreSQL escuchando en el puerto 5432; se usara ese.
  echo Debe aceptar usuario postgres / clave postgres, o ajuste ConnectionStrings__DefaultConnection en este archivo.
) else (
  if not exist "%ROOT%scripts\dev\node_modules\embedded-postgres" (
    echo Instalando PostgreSQL embebido ^(solo la primera vez^)...
    pushd "%ROOT%scripts\dev"
    call npm install --no-audit --no-fund || (popd & echo [ERROR] Fallo npm install en scripts\dev & goto :fin_error)
    popd
  )
  start "Portal - PostgreSQL" /D "%ROOT%scripts\dev" cmd /k node postgres-local.mjs
  echo Esperando a PostgreSQL...
  call :esperar_puerto 5432 60 || (echo [ERROR] PostgreSQL no respondio. Revise la ventana "Portal - PostgreSQL". & goto :fin_error)
)
echo PostgreSQL listo.

echo.
echo === 2/3 API .NET ===
call :puerto_activo 5072
if %ERRORLEVEL%==0 (
  echo Ya hay algo escuchando en el puerto 5072; se asume que es la API.
) else (
  start "Portal - API (.NET)" /D "%ROOT%backend\src\HapagPortal.WebApi" cmd /k dotnet run --launch-profile http
  echo Compilando e iniciando la API ^(la primera vez puede tardar unos minutos^)...
  call :esperar_url "%API_URL%/health" 300 || (echo [ERROR] La API no respondio. Revise la ventana "Portal - API (.NET)". & goto :fin_error)
)
echo API lista en %API_URL%  ^(Swagger: %API_URL%/swagger^)

echo.
echo === 3/3 Frontend Angular ===
if not exist "%ROOT%frontend\node_modules" (
  echo Instalando dependencias del frontend ^(solo la primera vez^)...
  pushd "%ROOT%frontend"
  call npm ci --no-audit --no-fund || (popd & echo [ERROR] Fallo npm ci en frontend & goto :fin_error)
  popd
)
call :puerto_activo 4200
if %ERRORLEVEL%==0 (
  echo Ya hay algo escuchando en el puerto 4200; se asume que es el frontend.
) else (
  start "Portal - Frontend (Angular)" /D "%ROOT%frontend" cmd /k npx ng serve --port 4200 --open=false
  echo Compilando el frontend...
  call :esperar_url "%WEB_URL%" 300 || (echo [ERROR] El frontend no respondio. Revise la ventana "Portal - Frontend (Angular)". & goto :fin_error)
)

echo.
echo === Listo ===
echo   Sitio:   %WEB_URL%
echo   API:     %API_URL%/swagger
echo   Usuarios de demo: admin@hapag-lloyd.cl o demo@importadorademo.cl / Admin123!
echo   Pagos en linea en modo de prueba: el portal muestra un simulador de la pasarela ^(sin cargo real^).
echo   Para depurar el backend en VS Code o Visual Studio: "Asociar al proceso" HapagPortal.WebApi.
echo   Para detener todo, cierre las ventanas "Portal - ...".
start "" "%WEB_URL%"
echo.
pause
exit /b 0

rem ---------------------------------------------------------------------------
rem :puerto_activo <puerto>  -> ERRORLEVEL 0 si hay un proceso escuchando
:puerto_activo
powershell -NoProfile -Command "if (Get-NetTCPConnection -State Listen -LocalPort %1 -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"
exit /b %ERRORLEVEL%

rem :esperar_puerto <puerto> <segundos>
:esperar_puerto
powershell -NoProfile -Command "$t=(Get-Date).AddSeconds(%2); while((Get-Date) -lt $t){ if(Get-NetTCPConnection -State Listen -LocalPort %1 -ErrorAction SilentlyContinue){exit 0}; Start-Sleep -Seconds 1 }; exit 1"
exit /b %ERRORLEVEL%

rem :esperar_url <url> <segundos>
:esperar_url
powershell -NoProfile -Command "$t=(Get-Date).AddSeconds(%2); while((Get-Date) -lt $t){ try { Invoke-WebRequest -UseBasicParsing -TimeoutSec 5 '%~1' | Out-Null; exit 0 } catch { Start-Sleep -Seconds 2 } }; exit 1"
exit /b %ERRORLEVEL%

:fin_error
echo.
echo No se pudo levantar el portal. Revise el mensaje anterior.
pause
exit /b 1
