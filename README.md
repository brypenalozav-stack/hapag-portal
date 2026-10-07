# Portal de Clientes Hapag-Lloyd 2.0 (Chile y Bolivia)

Portal web donde los clientes de Hapag-Lloyd en Chile y Bolivia consultan sus embarques, revisan los requisitos de liberación de su carga, pagan cargos y descargan documentos. Incluye además un **Backoffice** para los equipos internos (operación, finanzas y administración).

La fuente de verdad de los requerimientos es la *Especificación Funcional Portal 2.0 (validada)*. Hoy está habilitada la **Fase 1**; las funciones de Fase 2 existen en el código, pero vienen apagadas por configuración (ver [Funcionalidades por fase](#funcionalidades-por-fase)).

---

## Contenido

- [Qué hace el portal](#qué-hace-el-portal)
- [Tecnología](#tecnología)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Levantar el portal en local](#levantar-el-portal-en-local)
- [Usuarios de demostración](#usuarios-de-demostración)
- [Pagos en línea](#pagos-en-línea)
- [Funcionalidades por fase](#funcionalidades-por-fase)
- [Pruebas y controles de calidad](#pruebas-y-controles-de-calidad)
- [Configuración](#configuración)
- [Despliegue](#despliegue)
- [Flujo de trabajo con git](#flujo-de-trabajo-con-git)
- [Documentación](#documentación)

---

## Qué hace el portal

**Clientes**

- **Embarques:** listado, detalle del BL con encabezado fijo y la "próxima acción", contenedores, cargos, documentos y accesos.
- **Consulta de BL y liberación:** muestra paso a paso los requisitos para liberar la carga.
  - Chile: flete pagado, cargos locales, carta de responsabilidad y demurrage.
  - Bolivia: además, el certificado de no deuda y la carta de liberación.
  - Con los requisitos cumplidos se solicita el **TATC** y se descarga su comprobante con código QR.
- **Pagos:** carro de compra por país y moneda, medios de pago en línea y depósito con boleta, historial y comprobantes en PDF.
- **Documentos y trámites:** facturas, carta de liberación, solicitud de TATC, cambio de almacén (individual y masivo), tarifas locales y devoluciones.
- **Dashboard** orientado a la acción: lo primero es "Requiere su acción", agrupado por BL y ordenado por urgencia.
- **Búsqueda universal** de BL, booking o contenedor desde la barra superior.
- **Asistente** de ayuda flotante.
- **Idiomas y accesibilidad:** español e inglés, tema claro y oscuro, WCAG 2.2 AA.

**Backoffice (equipos internos)**

- **Operación:** solicitudes, organizaciones, Counter, embarques, carga de BL y transmisión a Aduana.
- **Finanzas:** pagos y conciliación, bloqueo de pagos, monedas y medios de pago.
- **Reportes y auditoría.**
- **Administración:** usuarios, matriz de accesos, tarifas, reglas de cobro, contenido y asistente.

## Tecnología

| Capa | Tecnología |
|---|---|
| Frontend | Angular 21 (componentes *standalone* y *signals*), Bootstrap 5.3, Transloco (ES/EN), TypeScript 5.9 |
| Backend | .NET 9, Clean Architecture con CQRS (MediatR 12.4.1), FluentValidation, Entity Framework Core 9 |
| Base de datos | PostgreSQL (Npgsql) |
| Documentos | PDFsharp-MigraDoc (PDF) y QRCoder (códigos QR) |
| Pruebas | xUnit, FluentAssertions 7.2.0 y NSubstitute (backend); Playwright con axe (frontend) |
| CI | GitHub Actions: build y pruebas del backend, más validación de contratos con schemathesis |

Todo es código abierto. **No actualice MediatR (12.4.1) ni FluentAssertions (7.2.0):** sus versiones posteriores tienen licencia comercial.

## Estructura del repositorio

```text
backend/
  src/
    HapagPortal.Domain/              Entidades, constantes, errores y reglas de dominio
    HapagPortal.Application/         Casos de uso (comandos y consultas), DTO y validaciones
    HapagPortal.Infrastructure/      EF Core, integraciones (pagos, Navesoft, DBNet...), PDF y secretos
    HapagPortal.WebApi/              Controladores REST v1, autenticación JWT, filtros y appsettings
    HapagPortal.DatabaseMigrations/  Migraciones de EF Core (se aplican solas al iniciar la API)
  tests/                             Pruebas unitarias, de arquitectura y de integración
  tools/                             Simulador de integraciones (pruebas de contrato)
frontend/
  src/app/core/                      Servicios, modelos, guards e interceptores
  src/app/shared/                    Componentes compartidos (menú, tablas, modales, avisos...)
  src/app/features/                  Pantallas por funcionalidad
  public/i18n/                       Textos en es.json y en.json
  e2e/                               Pruebas Playwright (funcionales, UX y accesibilidad)
  scripts/                           Controles (textos, colores, i18n, tablas)
docs/
  requerimientos/                    Especificación, matriz de trazabilidad, decisiones y plantillas NF
  integraciones/                     Contratos OpenAPI, guía de pasarelas de pago y paso de simulado a real
  documentacion.html                 Documentación general del portal
scripts/dev/                         Herramientas de depuración local (PostgreSQL embebido, pasarelas de prueba)
thoughts/shared/                     Investigaciones, planes e informes del desarrollo
iniciar-depuracion.bat               Levanta todo el portal en local y abre el navegador
```

## Levantar el portal en local

**Requisitos:** .NET SDK 9 o superior, y Node.js 20 o superior. No hace falta instalar PostgreSQL ni Docker.

### Opción rápida (Windows)

Ejecute `iniciar-depuracion.bat` (doble clic). El script:

1. Levanta PostgreSQL en el puerto 5432. Usa el que ya esté corriendo o, si no hay ninguno, inicia uno embebido con los datos en `scripts/dev/.pgdata`.
2. Inicia la API en http://localhost:5072, con Swagger en `/swagger`. Las migraciones y los datos de demostración se aplican solos.
3. Inicia el frontend con recarga en caliente en http://localhost:4200, con proxy de `/api` hacia la API.
4. Abre el navegador en el sitio.

Cada servicio corre en su propia ventana ("Portal - …"). Para detenerlos, cierre esas ventanas. La primera vez tarda unos minutos, porque instala dependencias y compila.

### Opción manual

```bash
# API (con PostgreSQL disponible en localhost:5432, usuario y clave postgres)
cd backend/src/HapagPortal.WebApi
set ASPNETCORE_ENVIRONMENT=Development
set Jwt__Secret=<clave de al menos 32 caracteres>
set Secrets__MasterKey=<base64 de 32 bytes>
dotnet run --launch-profile http

# Frontend
cd frontend
npm ci
npx ng serve --port 4200
```

Para depurar el backend desde VS Code o Visual Studio, use "Asociar al proceso" sobre `HapagPortal.WebApi`.

## Usuarios de demostración

Todos los usuarios de demostración usan la contraseña `Admin123!`.

| Usuario | Perfil |
|---|---|
| `demo@importadorademo.cl` | Cliente importador de Chile |
| `demo@altiplano.bo` | Cliente de Bolivia |
| `admin@hapag-lloyd.cl` | Interno: entra al Backoffice |

Los datos de demostración se cargan con las migraciones. Para partir desde cero, detenga PostgreSQL y borre `scripts/dev/.pgdata`.

## Pagos en línea

| Medio | Integración | Estado |
|---|---|---|
| Khipu | API v3 (`payment-api.khipu.com`) | Implementada. Falta la API key y el secreto de la cuenta de cobro. |
| Santander | Getnet Web Checkout | Implementada y **probada** contra `checkout.test.getnet.cl`. |
| BCI | Bci Pagos (ex Pago Fácil) | Implementada. Faltan las credenciales del contrato. |
| Banco de Chile | Formulario firmado configurable | Falta el manual técnico del banco, que se entrega con el contrato. |
| Depósito bancario | Boleta en PDF con instrucciones de pago | Operativo. |

- **Confirmación del pago:** nunca se da por pagado solo porque el cliente volvió al portal. Siempre se consulta la pasarela y se comparan la referencia, el monto y la moneda. Un proceso en segundo plano concilia los pagos que siguen en curso.
- **Credenciales:** se guardan cifradas en el portal. La guía completa, con las variables por pasarela, las URL de notificación que hay que registrar y los pasos de certificación, está en [docs/integraciones/pasarelas-pago.md](docs/integraciones/pasarelas-pago.md).
- **En local:** `iniciar-depuracion.bat` lee `scripts/dev/credenciales-prueba.json`, un archivo fuera de git que se crea a partir de `credenciales-prueba.example.json`.
  - Cada pasarela con credenciales completas funciona en modo real contra su ambiente de pruebas. Santander (Getnet) ya viene configurado.
  - Las pasarelas sin credenciales muestran un **simulador** del portal, sin cargo real, donde se elige el resultado: pagar, rechazar o dejar pendiente.

## Funcionalidades por fase

Las funciones de Fase 2 están detrás de *feature flags*, en la sección `Features` de `appsettings.json`. Vienen apagadas por defecto, salvo `ReleaseLetter` (carta de liberación) y `Counter`, que se mantienen por decisión del negocio.

- Para encender una función, cambie su valor a `true` o use una variable de entorno, por ejemplo `Features__OnDemandServices=true`.
- Con el flag apagado, la API responde 404 y el frontend oculta el menú, las rutas y los botones de esa función.
- El frontend lee los flags desde `GET /api/v1/config/features`.

## Pruebas y controles de calidad

```bash
# Backend: compilación y todas las pruebas
dotnet build backend/HapagPortal.sln
dotnet test backend/HapagPortal.sln

# Frontend: lint y controles
cd frontend
npx ng lint
npm run check:i18n          # paridad es/en y claves definidas
npm run check:i18n-text     # sin textos literales en las plantillas
npm run check:hex           # colores solo como tokens en src/styles/_tokens.scss
npm run check:table-scope   # encabezados de tabla con scope

# Frontend: pruebas extremo a extremo (incluyen accesibilidad con axe)
npx ng build --configuration e2e
npx playwright test
```

Las pruebas de Playwright no necesitan backend: la API se simula con los *fixtures* de `frontend/e2e/fixtures`.

**Convenciones de la interfaz**

- Todo texto pasa por Transloco, con claves en `es.json` y `en.json`.
- El español se escribe en trato formal ("usted").
- Los colores se usan solo como tokens.
- Cada pantalla debe pasar axe en tema claro y oscuro.

## Configuración

Las principales secciones de `backend/src/HapagPortal.WebApi/appsettings.json` son:

| Sección | Para qué sirve |
|---|---|
| `ConnectionStrings:DefaultConnection` | Base de datos PostgreSQL. |
| `Jwt:Secret`, `Secrets:MasterKey` | Firma de sesiones y cifrado de secretos. Se entregan siempre por variable de entorno, nunca en el archivo. |
| `Integrations:<Sistema>:Mode` | `Dummy` (simulado) o `Real` para cada integración: pagos, Navesoft, DBNet, TATC, almacenamiento, etc. |
| `Payments` | URL públicas del sitio y la API, notificaciones y conciliación de pagos. |
| `Documents` | Emisores de los documentos y datos bancarios del depósito (`DepositInstructions`). |
| `PortalLinks` | Enlaces externos: tarifas locales, devoluciones y Dispute. |
| `Features` | Flags de funcionalidades de Fase 2. |

## Despliegue

El backend y el frontend tienen su propio `Dockerfile`; el frontend se sirve con nginx. El instructivo para Railway está en [RAILWAY-DEPLOY.md](RAILWAY-DEPLOY.md).

## Flujo de trabajo con git

- Las ramas nuevas salen de `develop` con el formato `feature/nombre-rama`.
- Los PR van hacia `develop`. Para pasar a producción se hace un PR de `develop` a `main`.
- El CI (`.github/workflows/pr-tests.yml`) compila y prueba el backend y valida los contratos de integración en cada PR.

## Documentación

- [docs/documentacion.html](docs/documentacion.html): documentación general y guía de usuario.
- [docs/requerimientos/](docs/requerimientos/): especificación, matriz de trazabilidad, registro de decisiones, glosario, guía de UI y accesibilidad, licencias, evaluaciones (M3-05, M5-03) y plantillas de requisitos no funcionales.
- [docs/integraciones/](docs/integraciones/): contratos OpenAPI, guía de pasarelas de pago, depósito, Dispute, almacenamiento y paso de simulado a real.
- [thoughts/shared/](thoughts/shared/): investigaciones, planes de implementación e informes de UX.
