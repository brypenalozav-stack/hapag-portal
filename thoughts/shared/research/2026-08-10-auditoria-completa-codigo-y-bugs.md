---
date: 2026-08-10T13:54:04-04:00
researcher: bryvargas@strongtie.com
git_commit: 439e83b7e49a67f000f4306e91873134aa35de69
branch: main
repository: brypenalozav-stack/hapag-portal
topic: "Cómo funciona todo el código fuente y qué bugs tiene (foco en crear cuenta y recuperación de contraseña)"
tags: [research, codebase, auth, register, password-reset, multi-tenancy, validation, railway]
status: complete
last_updated: 2026-08-10
last_updated_by: bryvargas@strongtie.com
---

# Investigación: arquitectura completa y bugs del Portal Hapag-Lloyd

**Fecha**: 2026-08-10 13:54 -04:00
**Commit**: `439e83b` · **Rama**: `main` · **Repo**: `brypenalozav-stack/hapag-portal`
**Entorno verificado en vivo**: backend `adventurous-adventure-production-83dd.up.railway.app`, frontend `hapag-portal-production.up.railway.app`

## Pregunta de investigación

Investigar todo el código fuente para entender cómo funciona, y determinar si hay bugs — en particular en el flujo de **crear cuenta** y en el de **recuperación de contraseña**.

---

## Resumen ejecutivo

El sistema es un monorepo con **backend .NET 9** (Clean Architecture + CQRS con MediatR + EF Core/PostgreSQL) y **frontend Angular 21** (100% standalone, signals, lazy loading por componente). La arquitectura está bien separada y hay 405 tests unitarios en backend.

**Sí hay bugs, y el de "crear cuenta" es real y bloqueante.** Se confirmaron **6 bugs críticos** ejecutando peticiones reales contra el backend en Railway (no solo por lectura de código):

| # | Bug | Impacto | Estado |
|---|---|---|---|
| 1 | El formulario de registro envía `type`; el backend espera `clientType` | **Registrarse desde la web es imposible** | ✅ Probado en vivo |
| 2 | 2+ errores de validación en la misma propiedad → **HTTP 500** | Registro y reset de contraseña devuelven 500 opaco | ✅ Probado en vivo |
| 3 | El refresh token **nunca se valida** | Bypass de autenticación; sesiones no revocables | ✅ Probado en vivo |
| 4 | `GET /payments` no existe (405) | Página de Pagos y métricas del Dashboard rotas | ✅ Probado en vivo |
| 5 | `IEmailService` es un stub que solo escribe en el log | **Recuperar contraseña es imposible de completar** | ✅ Confirmado en código |
| 6 | `appsettings.Staging.json` es JSON inválido | El próximo deploy con `ASPNETCORE_ENVIRONMENT=Staging` no arranca | ✅ Validado con parser |

Además: **12 endpoints exponen datos de otros clientes** (IDOR), **doble cobro de IVA** en pagos de cargos locales, y **4 formularios más** del frontend envían payloads que el backend no acepta.

---

## Parte 1 — Cómo funciona el sistema (mapa técnico)

### 1.1 Backend: capas y dependencias

5 proyectos en [backend/src/](../../../backend/src/), todos `net9.0`:

```
Domain  ←  Application  ←  Infrastructure  ←  WebApi
                                                 └→ DatabaseMigrations
```

| Proyecto | Rol | NuGet clave |
|---|---|---|
| `HapagPortal.Domain` | Entidades, constantes, `Result`, `DomainErrors` | MediatR (solo por `IDomainEvent`) |
| `HapagPortal.Application` | 51 commands/queries + handlers, DTOs, 15 validators | FluentValidation, MediatR, EFCore |
| `HapagPortal.Infrastructure` | DbContext, JWT, BCrypt, servicios | Npgsql, BCrypt.Net-Next, JwtBearer |
| `HapagPortal.WebApi` | 13 controladores, middleware | Asp.Versioning, Swashbuckle |
| `HapagPortal.DatabaseMigrations` | Migraciones EF + CLI migrador | EFCore.Design |

Las reglas de capas se verifican con NetArchTest en [LayerDependencyTests.cs](../../../backend/tests/HapagPortal.ArchitectureTests/LayerDependencyTests.cs).

### 1.2 El pipeline de una petición

```
HTTP → ExceptionHandlingMiddleware → CORS → UseAuthentication → UseAuthorization
     → Controller (hereda ApiController) → Sender.Send(command)
     → MediatR → ValidationBehavior → Handler → Result<T>
     → result.IsSuccess ? Ok(value) : HandleFailure(result)
```

- **Punto de entrada**: [Program.cs](../../../backend/src/HapagPortal.WebApi/Program.cs) — 173 líneas, todo inline.
- **Cadena de conexión**: se construye desde las variables `PGHOST/PGPORT/PGDATABASE/PGUSER/PGPASSWORD` de Railway ([Program.cs:14-24](../../../backend/src/HapagPortal.WebApi/Program.cs#L14-L24)); si no existe `PGHOST`, cae al `appsettings.json`.
- **Migraciones al arranque**: [Program.cs:119-146](../../../backend/src/HapagPortal.WebApi/Program.cs#L119-L146) ejecuta `MigrateAsync()` y **se traga la excepción** con el comentario explícito `// Don't crash the app`.
- **Swagger solo en Development** ([Program.cs:153](../../../backend/src/HapagPortal.WebApi/Program.cs#L153)) — por eso `/swagger` da 404 en Railway.
- **`/health`** ([Program.cs:165](../../../backend/src/HapagPortal.WebApi/Program.cs#L165)) devuelve `healthy` incondicionalmente; no comprueba la BD.
- **Único middleware custom**: [ExceptionHandlingMiddleware.cs](../../../backend/src/HapagPortal.WebApi/Middleware/ExceptionHandlingMiddleware.cs) — captura `Exception` genérica y responde siempre 500.

### 1.3 CQRS y el patrón Result

Interfaces marcadoras propias sobre MediatR en [Common/Messaging/](../../../backend/src/HapagPortal.Application/Common/Messaging/): `ICommand : IRequest<Result>`, `IQuery<T> : IRequest<Result<T>>`. Registro por escaneo de assembly en [DependencyInjection.cs:13-18](../../../backend/src/HapagPortal.Application/DependencyInjection.cs#L13-L18), con **un solo pipeline behavior**: `ValidationBehavior`.

Todo handler devuelve `Result`/`Result<T>` y nunca lanza. El mapeo a HTTP vive en [ApiController.cs:39-47](../../../backend/src/HapagPortal.WebApi/Abstractions/ApiController.cs#L39-L47):

| Sufijo del código de error | Status |
|---|---|
| `.NotFound` | 404 |
| `Error.Unauthorized` | 401 |
| `Error.Forbidden` | 403 |
| `.HasData` / `.Exists` | 409 |
| cualquier otro | 400 |

### 1.4 Autenticación (el flujo completo)

**Registro** → [RegisterCommandHandler.cs](../../../backend/src/HapagPortal.Application/Auth/Register/RegisterCommandHandler.cs):
1. Valida RUT/NIT duplicado por `(TaxId, Country)` en `Clients`.
2. Valida email duplicado en `Users`.
3. Crea `Client` + `User` (`IsActive = true`) + `UserRole`.
4. `PasswordHash = BCrypt(password, workFactor 12)` ([PasswordHasher.cs:9](../../../backend/src/HapagPortal.Infrastructure/Authentication/PasswordHasher.cs#L9)).
5. Genera `EmailConfirmationToken` y "envía" email.

**Login** → [LoginCommandHandler.cs](../../../backend/src/HapagPortal.Application/Auth/Login/LoginCommandHandler.cs): busca por email → comprueba `IsActive` → verifica password → emite JWT (HS256, 60 min) + refresh token (64 bytes aleatorios) y persiste `RefreshToken` + `RefreshTokenExpiryTime` (+7 días). **No exige email confirmado.**

**Claims emitidos** ([JwtTokenService.cs:31-42](../../../backend/src/HapagPortal.Infrastructure/Authentication/JwtTokenService.cs#L31-L42)): `sub`, `email`, `jti`, `country` + un `ClaimTypes.Role` por rol. Verificado en vivo:

```json
{"sub":"5ad2edbf-...","email":"probe1@test.cl","jti":"0520b8b4-...","country":"CL",
 "http://schemas.microsoft.com/ws/2008/06/identity/claims/role":"Client",
 "exp":1786388249,"iss":"HapagPortal","aud":"HapagPortalUsers"}
```

> Nota forense: el `aud` en vivo es `HapagPortalUsers`, pero **ambos** `appsettings` committeados dicen `HapagPortalClients`. La configuración del despliegue viene de variables de entorno de Railway, no del repo.

**Recuperación de contraseña** (2 pasos):
- [ForgotPasswordCommandHandler.cs](../../../backend/src/HapagPortal.Application/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs): genera `Guid.NewGuid()` como token, expiry +1h, y llama a `IEmailService`. **Siempre devuelve éxito** para no revelar si el email existe.
- [ResetPasswordCommandHandler.cs](../../../backend/src/HapagPortal.Application/Auth/ResetPassword/ResetPasswordCommandHandler.cs): valida email → token → expiración → re-hashea y limpia el token.

### 1.5 Autorización real

Solo dos formas: `[Authorize]` a nivel de clase y `[Authorize(Roles = "Admin,BA")]` (5 sitios). **No hay ninguna policy registrada** ([DI.Auth.Partial.cs:44-48](../../../backend/src/HapagPortal.Infrastructure/DependencyInjection/DI.Auth.Partial.cs#L44-L48)).

[Permissions.cs](../../../backend/src/HapagPortal.Domain/Constants/Permissions.cs) (78 líneas, ~30 permisos) y [HasPermissionAttribute.cs](../../../backend/src/HapagPortal.Infrastructure/Authentication/HasPermissionAttribute.cs) están **completamente sin usar** en producción.

### 1.6 Persistencia

- [ApplicationDbContext.cs](../../../backend/src/HapagPortal.Infrastructure/Persistence/ApplicationDbContext.cs): 17 DbSets + seed masivo (4 usuarios, 5 BLs, 8 pagos, 13 FAQs, 3 órdenes…).
- **Soft delete**: `HasQueryFilter(e => e.DeletedAt == null)` aplicado a las 14 entidades `BaseAuditableEntity` vía [ConfigurationExtensions.cs:33](../../../backend/src/HapagPortal.Infrastructure/Persistence/Configurations/ConfigurationExtensions.cs#L33). `UserRole`, `PaymentDetail` y `AuditLog` **no** lo tienen.
- **Único interceptor**: [AuditableEntityInterceptor.cs](../../../backend/src/HapagPortal.Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs) — rellena campos de auditoría y convierte `Deleted` → `Modified` + `DeletedAt`.
- **Índices únicos**: `Users.Email`, `Users.Username`, `Clients.Email`, `Clients.(TaxId,Country)`, `BLNumber`, `PaymentNumber`, etc.
- **Servicios que son placeholder**: `EmailService`, `PaymentGatewayService`, y la generación de PDF (`"%PDF-1.4 placeholder"`).

### 1.7 Frontend

Angular **21.2**, 100% standalone, sin NgModules, estado con signals. 22 rutas en [app.routes.ts](../../../frontend/src/app/app.routes.ts), todas con `loadComponent`.

- **Token en `localStorage`**: claves `hl_token`, `hl_refresh_token`, `hl_user` ([auth.service.ts:20-22](../../../frontend/src/app/core/services/auth.service.ts#L20-L22)).
- `isAuthenticated = computed(() => !!currentUser() && !!getToken())` — **nunca decodifica el JWT ni comprueba expiración**.
- **Interceptor** ([auth.interceptor.ts](../../../frontend/src/app/core/interceptors/auth.interceptor.ts)): adjunta `Bearer` a todas las peticiones y, ante un 401 que no sea de `auth/`, hace refresh automático con cola de peticiones concurrentes.
- **Guards**: `authGuard` (solo presencia de token) y `adminGuard` (rol `ADMIN` o `BA`).
- `environment.prod.ts` apunta al backend de Railway; en dev usa `/api/v1` con proxy a `localhost:5072`.

---

## Parte 2 — Bugs

Cada bug incluye **evidencia**. Los marcados 🔴 se reprodujeron con peticiones reales contra Railway.

### 🔴 BUG-1 (CRÍTICO) · Registrarse desde la web es imposible: `type` vs `clientType`

El formulario Angular envía la clave **`type`** con valores `"CLIENT"`/`"AGENT"`; el backend espera **`clientType`** con valores `"Client"`/`"CustomsAgent"`.

- Frontend: [register.ts:23](../../../frontend/src/app/features/auth/register/register.ts#L23) y el payload se construye en [register.ts:81-82](../../../frontend/src/app/features/auth/register/register.ts#L81-L82).
- Backend: [RegisterCommand.cs:12](../../../backend/src/HapagPortal.Application/Auth/Register/RegisterCommand.cs#L12) → `string ClientType`.
- Valores permitidos: [RegisterCommandValidator.cs:8](../../../backend/src/HapagPortal.Application/Auth/Register/RegisterCommandValidator.cs#L8) → `["Client", "CustomsAgent"]` (case-sensitive).

**Doble fallo**: aunque se renombrara la clave, `"CLIENT"`/`"AGENT"` tampoco están en la lista permitida. **No existe ningún valor que el formulario actual pueda enviar y que el backend acepte.**

**Evidencia en vivo** — payload exacto del formulario:
```
POST /api/v1/auth/register
{"type":"CLIENT","country":"CL","name":"Importadora Demo SpA","taxId":"76.123.456-7",
 "email":"...","phone":"+56 9 1234 5678","password":"Password1!","agentCode":""}
=> 400 {"errors":{"ClientType":["The ClientType field is required."]}}
```
El mismo payload con `clientType: "Client"` → **201 Created**. Confirmado.

### 🔴 BUG-2 (CRÍTICO) · Dos o más errores de validación en la misma propiedad devuelven 500

**Causa raíz exacta**, en dos archivos:

1. [ValidationBehavior.cs:29-31](../../../backend/src/HapagPortal.Application/Common/Behaviors/ValidationBehavior.cs#L29-L31) construye un `Error` por cada regla fallida usando **`f.PropertyName` como `Code`**. `.Distinct()` solo deduplica pares `(Code, Message)` idénticos, así que dos mensajes distintos sobre `Password` producen **dos `Error` con el mismo `Code`**.
2. [ApiController.cs:31-34](../../../backend/src/HapagPortal.WebApi/Abstractions/ApiController.cs#L31-L34) hace:
   ```csharp
   foreach (var error in validationResult.Errors)
       problemDetails.Errors.Add(error.Code, [error.Message]);
   ```
   `ValidationProblemDetails.Errors` es un `IDictionary<string, string[]>` y **`Add` lanza `ArgumentException` con clave duplicada** → excepción no controlada → `ExceptionHandlingMiddleware` → **500**.

Como `Error` es un `record` de `(Code, Message)` ([Error.cs:3](../../../backend/src/HapagPortal.Domain/Results/Error.cs#L3)), el `Distinct()` no protege de nada aquí.

**Evidencia en vivo** (prueba falsable diseñada para aislarlo):

| Contraseña en `POST /auth/register` | Reglas que incumple | Resultado |
|---|---|---|
| `Abcdefg1` | 1 (falta carácter especial) | **400** con detalle correcto |
| `abcdefg1` | 2 (falta mayúscula y especial) | **500** genérico |
| `abc` | 4 | **500** genérico |

Y en el flujo de **recuperación de contraseña**, idéntico:

| `newPassword` en `POST /auth/reset-password` | Resultado |
|---|---|
| `Abcdefg1` (1 error) | **400** `{"NewPassword":["Password must contain at least one special character."]}` |
| `abcdefg1` (2 errores) | **500** |

**Alcance**: afecta a los 6 validators con varias reglas por propiedad, incluido `LoginCommandValidator` con email vacío (`NotEmpty` + `EmailAddress` = 2 errores sobre `Email`). Es el bug que con más probabilidad estás viendo: el usuario pone una contraseña floja y recibe un 500 sin explicación.

### 🔴 BUG-3 (CRÍTICO, seguridad) · El refresh token nunca se valida

[RefreshTokenCommandHandler.cs:20-40](../../../backend/src/HapagPortal.Application/Auth/RefreshToken/RefreshTokenCommandHandler.cs#L20-L40) extrae el email del access token con `ValidateLifetime = false`, comprueba que el usuario exista y esté activo, y **emite un token nuevo**. Nunca compara `request.RefreshToken` con `user.RefreshToken`, ni revisa `user.RefreshTokenExpiryTime`. Ambos campos se escriben en el login y **jamás se leen**.

**Evidencia en vivo**, mismo access token, dos refresh tokens distintos:
```
POST /auth/refresh-token  {token: <válido>, refreshToken: <el correcto>}          => 200
POST /auth/refresh-token  {token: <válido>, refreshToken: "BASURA-TOTAL-NO..."}   => 200
```

**Consecuencias**: un access token filtrado se puede renovar indefinidamente; el logout es solo del lado cliente (borra `localStorage`) y no revoca nada en servidor; cambiar la contraseña no invalida sesiones.

El commit [9b6ddc3](https://github.com/brypenalozav-stack/hapag-portal/commit/9b6ddc3) ("fix: resolve refresh token claim mapping for email extraction") arregló la **extracción del claim**, no la validación — el hueco sigue abierto.

Agravante de cobertura: el test llamado `InvalidRefreshToken_ShouldReturnFailure` ([RefreshTokenCommandHandlerTests.cs:74](../../../backend/tests/HapagPortal.UnitTests.Application/Auth/RefreshTokenCommandHandlerTests.cs#L74)) en realidad stubea `GetEmailFromExpiredToken → null`: prueba un access token inválido, **no** el refresh token. Da falsa confianza.

Añadido: `RefreshTokenCommandHandler` construye `new AuthResponseDto(token, expirationMinutes, userDto)` sin el 4º parámetro, así que **nunca rota el refresh token** y la clave `refreshToken` se omite del JSON (por `WhenWritingNull`).

### 🔴 BUG-4 (CRÍTICO) · `GET /payments` no existe → Dashboard y Pagos rotos

El frontend llama `GET payments` en [payment.service.ts:19](../../../frontend/src/app/core/services/payment.service.ts#L19), consumido por [payment-list.ts:36](../../../frontend/src/app/features/payments/payment-list/payment-list.ts#L36) (única carga de la página) y [dashboard.ts:31](../../../frontend/src/app/features/dashboard/dashboard.ts#L31) (métricas). El backend solo tiene `POST /payments`, `GET /payments/{id}` y `GET /payments/my`.

**Evidencia en vivo**:
```
GET /api/v1/payments          => 405 Method Not Allowed
GET /api/v1/payments/my       => 200 []
GET /api/v1/bills-of-lading/my => 200 []   (control)
```
Inconsistencia interna: para BLs, recibos y órdenes el frontend sí usa el sufijo `/my`; para pagos, no. Y como [dashboard.ts:38](../../../frontend/src/app/features/dashboard/dashboard.ts#L38) traga los errores (`/* non-critical */`), el Dashboard muestra ceros en silencio.

### 🔴 BUG-5 (CRÍTICO) · Recuperar contraseña no se puede completar: no se envía ningún email

[EmailService.cs](../../../backend/src/HapagPortal.Infrastructure/Services/EmailService.cs) es un stub:
```csharp
logger.LogInformation("Email sent (placeholder) - To: {To}, Subject: {Subject}, ...");
return Task.CompletedTask;
```
No hay SMTP, ni SendGrid, ni paquete de correo en el `.csproj`, ni claves `Smtp*` en ningún `appsettings`.

**Consecuencia**: el token de reset generado por `ForgotPassword` (y el `EmailConfirmationToken` del registro) **solo existe en los logs de la aplicación y en la BD**. El usuario nunca lo recibe, así que `reset-password` — que exige `token` + `email` por query params ([reset-password.ts:103-104](../../../frontend/src/app/features/auth/reset-password/reset-password.ts#L103-L104)) — es inalcanzable por la vía normal.

**Evidencia en vivo** del comportamiento actual del flujo:
```
POST /auth/forgot-password  {email: "no-existe-jamas-999@test.cl"}  => 200 (correcto: no revela existencia)
```
El endpoint responde 200, el token se guarda… y ahí muere.

### 🔴 BUG-6 (CRÍTICO, despliegue) · `appsettings.Staging.json` es JSON inválido

[appsettings.Staging.json](../../../backend/src/HapagPortal.WebApi/appsettings.Staging.json): el objeto raíz cierra en la línea 24 y **las líneas 25-27 sobran** (`},`, `"AllowedHosts": "*"`, `}`).

**Validado con parser**: `ConvertFrom-Json` → `Primitivo JSON no válido: ,`

Introducido en `0fa1167` (2026-06-07), que **es ancestro de `main`**. El [Dockerfile:20](../../../backend/Dockerfile#L20) fija `ENV ASPNETCORE_ENVIRONMENT=Staging`, y [RAILWAY-DEPLOY.md:57](../../../RAILWAY-DEPLOY.md#L57) instruye poner esa misma variable.

**Por qué la app funciona hoy a pesar de esto**: `JsonConfigurationProvider` lanza al parsear un archivo malformado (el flag `optional` cubre la ausencia, no la invalidez), así que si el proceso cargara este archivo no arrancaría. La evidencia (el `aud` en vivo es `HapagPortalUsers`, que no está en ningún `appsettings` del repo) indica que **la instancia desplegada no está en el entorno `Staging`** y toma su configuración de variables de entorno de Railway.

**Riesgo**: en el momento en que alguien fije `ASPNETCORE_ENVIRONMENT=Staging` (justo lo que dice la guía de deploy), la API deja de arrancar. Es una mina activada.

### 🟠 BUG-7 (ALTO, seguridad) · 12 endpoints exponen datos de otros clientes (IDOR)

Existe un patrón `GetMy*` que filtra correctamente por `ClientId` (p. ej. [GetMyBLsQueryHandler.cs:34-39](../../../backend/src/HapagPortal.Application/BillsOfLading/Read/GetMyBLs/GetMyBLsQueryHandler.cs#L34-L39)), pero sus equivalentes por id/número **no filtran nada** y solo requieren `[Authorize]` — o sea, cualquier cliente autenticado:

`GetAllBLs` (acepta `clientId` como parámetro del caller), `GetBLByNumber`, `GetChargesByBL`, `GetContainersByBL`, `GetLocalChargesByBL`, `GetLocalChargesByContainer`, `GetDemurrageByBL`, `GetDemurrageByContainer`, `GetPaymentById`, `GetReceiptById`, `GetReceiptPdf`, `GetWarehouseChangeById`, `GetServiceOrderPdf`.

**Escrituras sin comprobar propiedad**:
- `CancelPaymentCommandHandler` cancela **cualquier** pago por id, y el endpoint no exige rol.
- `CreateWarehouseChangeCommandHandler` no inyecta `ICurrentUserService`: permite crear solicitudes sobre el BL de otro cliente, con `Amount` enviado por el cliente y sin validar.
- `CreateReceiptCommandHandler` emite `ReceiptNumber` sobre cualquier pago confirmado ajeno.

Causa estructural: `ICurrentUserService` **no expone `ClientId`**, así que cada handler que quiere multi-tenancy repite un query extra a `Users`; los que no lo hacen, quedan abiertos.

### 🟠 BUG-8 (ALTO, dinero) · Doble IVA en pagos de cargos locales

[CreatePaymentCommandHandler.cs:98](../../../backend/src/HapagPortal.Application/Payments/Create/CreatePaymentCommandHandler.cs#L98) toma `subtotal = SUM(lc.TotalAmount)`, pero `LocalCharge.TotalAmount` **ya incluye el impuesto** (seed: `Amount=185000, TaxAmount=35150, TotalAmount=220150`). Luego las líneas 120-121 vuelven a calcular `taxAmount = subtotal * taxRate / 100`.

Para BL01 (3 cargos, 303.450 CLP con IVA) el pago sale **361.105,50 CLP** en lugar de 303.450.

Relacionado: la consulta de IVA ignora `ServiceType` y las ventanas `EffectiveFrom`/`EffectiveTo`, y aplica la tasa del país a todo, ignorando `LocalCharge.IsTaxable`/`TaxRate` propios.

### 🔴 BUG-9 (ALTO) · El email es case-sensitive: se pueden crear cuentas duplicadas

Todas las búsquedas usan `u.Email == request.Email` y el registro **no normaliza** el email. PostgreSQL compara con distinción de mayúsculas, y el índice único `IX_Users_Email` tampoco es case-insensitive.

**Evidencia en vivo** (`probe1@test.cl` ya existía):
```
POST /auth/register  {email: "PROBE1@TEST.CL", ...}  => 201 Created
POST /auth/login     {email: "probe1@test.cl", ...}  => 200  (sub: 5ad2edbf-...)
POST /auth/login     {email: "PROBE1@TEST.CL", ...}  => 200  (sub: e9e5b47a-...)
```
Dos cuentas distintas, mismo correo real. Afecta también a `forgot-password`: pedir el reset con distinta capitalización no encuentra al usuario y devuelve 200 igual (silencioso).

### 🟠 BUG-10 (ALTO, seguridad) · El reset de contraseña no revoca sesiones ni comprueba `IsActive`

[ResetPasswordCommandHandler.cs:39-43](../../../backend/src/HapagPortal.Application/Auth/ResetPassword/ResetPasswordCommandHandler.cs#L39-L43) cambia el hash y limpia el token, pero **no toca `RefreshToken` ni `RefreshTokenExpiryTime`**. Combinado con BUG-3, un atacante con un access token viejo mantiene el acceso después de que la víctima cambie la contraseña.

Tampoco comprueba `user.IsActive`: una cuenta desactivada puede cambiar su contraseña. `ForgotPassword` tiene el mismo hueco y además no tiene rate-limiting.

### 🟡 BUG-11 (MEDIO) · `ResetPassword` filtra qué emails existen

`ForgotPassword` oculta deliberadamente la existencia del email ([comentario en línea 36](../../../backend/src/HapagPortal.Application/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs#L36)), pero `ResetPassword` devuelve `User.NotFound` **con el email en el mensaje**:

```
POST /auth/reset-password {email: "no-existe-jamas-999@test.cl", ...}
=> 404 {"title":"User.NotFound","detail":"The user with email 'no-existe-jamas-999@test.cl' was not found."}
```
Permite enumerar cuentas y contradice la decisión de diseño del paso anterior.

### 🟡 BUG-12 (MEDIO) · La confirmación de email es decorativa

- El login **no comprueba** si el email está confirmado → las cuentas son usables de inmediato.
- [ConfirmEmailCommandHandler.cs:34](../../../backend/src/HapagPortal.Application/Auth/ConfirmEmail/ConfirmEmailCommandHandler.cs#L34) marca `user.Client.IsEmailConfirmed`, no al usuario (`User` no tiene ese campo).
- Si `user.Client is null` (caso de un Admin sin cliente) → falla con `Client.NotFound`.
- El `EmailConfirmationToken` **no tiene expiración** (no existe análogo a `PasswordResetTokenExpiry`): vale para siempre.
- No hay ruta ni componente `confirm-email` en el frontend, ni endpoint de reenvío.

### 🟠 BUG-13 (ALTO) · Otros 4 formularios del frontend envían payloads que el backend rechaza

Mismo patrón que BUG-1:

| Formulario | Frontend envía | Backend espera | Efecto |
|---|---|---|---|
| Órdenes de servicio | `{blNumber, type, description}` | `{OrderType, Description, BillOfLadingId (Guid), Country}` | 400 con 3 errores |
| Cambio de almacén | `{blNumber, containerNumber, currentWarehouse, requestedWarehouse, reason, contactPhone}` | `{FromWarehouse, ToWarehouse, BillOfLadingId, Country, Amount}` | **0 campos coinciden** |
| Admin · crédito (POST/PUT) | `{name, taxId, country, creditLimit, currency}` | `{ClientId (Guid), Country, CreditLimit, ExpiresAt}` | falta `clientId` → `Guid.Empty` |
| Perfil (`PUT clients/me`) | `{name, phone}` | `{Phone, Address, City}` | **el nombre se descarta en silencio** |

El caso del perfil es el más engañoso: el formulario presenta "Nombre" como editable y obligatorio, la petición devuelve 200, y el cambio nunca se persiste.

### 🟡 BUG-14 (MEDIO) · El rol `BA` no existe en ninguna parte

`[Authorize(Roles = "Admin,BA")]` aparece en 5 endpoints, pero `"BA"` no se asigna nunca: el seed crea `Admin` y `User`; el registro asigna `Client` o `Agent`. En la práctica esos endpoints son solo-Admin. El frontend también acepta `BA` en `isAdmin()` ([auth.service.ts:28-32](../../../frontend/src/app/core/services/auth.service.ts#L28-L32)), rama inalcanzable porque el backend solo emite `ADMIN`/`USER` en `user.role`.

### 🟡 BUG-15 (MEDIO) · `ClientType == "Agent"` es inalcanzable → los agentes se ven como cliente

El registro persiste `ClientType` con `"Client"` o `"CustomsAgent"`, pero tanto [LoginCommandHandler.cs:50-54](../../../backend/src/HapagPortal.Application/Auth/Login/LoginCommandHandler.cs#L50-L54) como [ClientResponseDto.cs:28](../../../backend/src/HapagPortal.Application/Common/Dtos/ClientResponseDto.cs#L28) comparan contra `"Agent"`. Verificado en vivo: registrando con `clientType: "CustomsAgent"` la respuesta trae `"type":"CLIENT"`.

### 🟡 BUG-16 (MEDIO) · El registro y el login devuelven contratos distintos para el mismo usuario

Verificado en vivo con la misma cuenta:

| Campo | `POST /auth/register` | `POST /auth/login` |
|---|---|---|
| `id` | `220be6d1-…` (**`Client.Id`**) | `48bb50c6-…` (**`User.Id`**) |
| `role` | `"Client"` (UserType) | `"USER"` (rol normalizado) |

`ClientResponseDto.FromClient` usa `client.Id` y pasa el `userType` como `role`, mientras el login usa `user.Id` y `"ADMIN"`/`"USER"`. Dos vocabularios y dos identificadores para la misma entidad.

### 🟡 BUG-17 (MEDIO) · `Client.AlreadyExists` devuelve 400 en vez de 409

El switch de [ApiController.cs:44-45](../../../backend/src/HapagPortal.WebApi/Abstractions/ApiController.cs#L44-L45) busca sufijos `.Exists`/`.HasData`, pero los códigos reales son `Client.AlreadyExists` y `BillOfLading.HasPayments` — que **no terminan** con esos sufijos.

**Evidencia en vivo**: `POST /auth/register` con RUT duplicado → `400` con `"title":"Client.AlreadyExists"` (debería ser 409). Lo mismo con `User.EmailExists`.

El propio test lo documenta y lo sortea sustituyendo el código por `"Client.Exists"`, un código que ningún handler produce ([AuthControllerTests.cs:86-93](../../../backend/tests/HapagPortal.UnitTests.WebApi/Controllers/AuthControllerTests.cs#L86-L93)).

### 🟡 BUG-18 (MEDIO, proceso) · El CI probablemente no se ejecuta nunca

El único workflow está en [backend/.github/workflows/pr-tests.yml](../../../backend/.github/workflows/pr-tests.yml). GitHub Actions solo lee `<raíz-del-repo>/.github/workflows`, y **no existe `.github/` en la raíz**. Además solo dispara en PRs hacia `develop`, y los `run:` no definen `working-directory` (la solución está en `backend/`). `coverlet` está instalado en los 5 proyectos pero nunca se invoca.

### 🟡 BUG-19 (MEDIO) · Índices únicos sin filtro de soft delete

`IX_Users_Email`, `IX_Clients_Email` e `IX_Clients_TaxId_Country` se crean **sin `filter:`**, mientras las comprobaciones del código pasan por el query filter `DeletedAt == null`. Resultado: el email/RUT de un cliente borrado lógicamente no se puede reutilizar, y el fallo llega como `DbUpdateException` → 500 genérico.

Relacionado: el registro valida `Users.Email` pero **no `Clients.Email`**, que también es único → dos altas con RUT distinto y mismo correo revientan en el `SaveChanges` con 500.

### Otros hallazgos (menores o de deuda técnica)

- **Soft delete mixto**: el interceptor solo recorre `BaseAuditableEntity`; al borrar un `User`, sus `UserRole` (que heredan solo `GuidEntity`) se borran **físicamente** por cascada. Igual con `Payment`/`PaymentDetail`.
- **`AuditLogs` no se escribe nunca**: la tabla existe y está sembrada, pero ningún handler inserta. `AuditLoggablePropertyAttribute` no decora ninguna propiedad.
- **`nvarchar(max)` (tipo de SQL Server)** en [AuditLogConfiguration.cs:28](../../../backend/src/HapagPortal.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs#L28) sobre un modelo PostgreSQL; la deriva está oculta porque se silencia `PendingModelChangesWarning`.
- **Webhooks de pago** (`[AllowAnonymous]`): no verifican el `NotificationToken` ni el `TransactionId`/`Amount`, y no comprueban el estado previo — un webhook `done` sobre un pago cancelado lo pasa a `Confirmed`.
- **Contraseña del seed en el código**: los 4 usuarios sembrados comparten el hash de `Admin123!`, y `SeedDemoData` se aplica **sin condición de entorno**.
- **PDFs falsos**: recibos y órdenes devuelven `"%PDF-1.4 placeholder"` servido como `application/pdf`.
- **Exenciones de demurrage nunca se aplican**; no hay cálculo de demurrage en el código (todo viene del seed).
- **Confirmar un pago no tiene efectos**: no marca `LocalCharge.Status = "Paid"` ni genera el recibo. `LocalCharge.Status` es inmutable en toda la app.
- **`CreditUsed` se calcula de 3 formas distintas** y el límite de crédito nunca se valida (`CreditClient.LimitExceeded` sin usar).
- **`CreateCreditClient` solo acepta Bolivia** (`.Equal("BO")`), aunque el seed tiene 3 clientes de crédito chilenos.
- **Validaciones cliente más laxas que servidor**: el frontend solo exige `minLength(8)` en contraseña y nada de formato en RUT/NIT — de ahí que se llegue tan fácil al BUG-2.
- **Frontend con 0 tests**: no hay ningún `.spec.ts`, `angular.json` no define target `test`, y `npm test` fallaría.
- **`/health` no comprueba la BD** y el fallo de migración no tumba la app → un despliegue sin migrar se reporta sano.

---

## Cobertura de tests (backend)

405 tests en 5 proyectos (Domain 181, Application 140, WebApi 46, Infrastructure 23, Architecture 15). xUnit + FluentAssertions + NSubstitute.

Los 45 tests de Auth cubren bien los caminos felices y varios errores, pero los huecos coinciden exactamente con los bugs encontrados:

| Hueco | Bug relacionado |
|---|---|
| El refresh token nunca se valida (y el test tiene nombre engañoso) | BUG-3 |
| `GetEmailFromExpiredToken` sin ningún test | BUG-3 |
| Rama de email duplicado en Register sin test | BUG-19 |
| Complejidad de contraseña de Register sin test (solo `ShortPassword`) | BUG-2 |
| Reutilización de token de reset, `IsActive`, revocación de sesiones | BUG-10 |
| 4 de 6 endpoints de Auth sin test de controlador | BUG-2, BUG-17 |
| Sin tests de integración ni de autorización (no hay `WebApplicationFactory`) | BUG-7 |

**Limitación estructural**: los tests de Application usan un `MockApplicationDbContext` hecho a mano donde `Include()` es un no-op, **no existen índices únicos**, y `SaveChangesAsync` nunca falla. Por diseño no pueden detectar BUG-9, BUG-19 ni ningún `DbUpdateException`.

---

## Verificación empírica realizada

Todas contra `https://adventurous-adventure-production-83dd.up.railway.app/api/v1` el 2026-08-10:

| Prueba | Resultado | Confirma |
|---|---|---|
| `register` con payload exacto del frontend | 400 `ClientType required` | BUG-1 |
| `register` con `clientType` correcto | 201 | BUG-1 |
| `register` password 1 error / 2 errores | 400 / **500** | BUG-2 |
| `reset-password` newPassword 1 error / 2 errores | 400 / **500** | BUG-2 |
| `refresh-token` con refreshToken correcto / basura | 200 / **200** | BUG-3 |
| `GET /payments` vs `/payments/my` | 405 / 200 | BUG-4 |
| `forgot-password` email inexistente | 200 (correcto) | BUG-5 |
| `reset-password` email inexistente | 404 con el email | BUG-11 |
| `register` con email en mayúsculas | 201 (cuenta duplicada) | BUG-9 |
| `register` con RUT duplicado | 400 `Client.AlreadyExists` | BUG-17 |
| `register` con `clientType: CustomsAgent` | 201 con `"type":"CLIENT"` | BUG-15 |

---

## Orden de arreglo sugerido

1. **BUG-1** — renombrar `type` → `clientType` y mapear `CLIENT→Client`, `AGENT→CustomsAgent`. Desbloquea el registro. Cambio de 2 líneas en el frontend.
2. **BUG-2** — en [ApiController.cs:33](../../../backend/src/HapagPortal.WebApi/Abstractions/ApiController.cs#L33), cambiar `Errors.Add(...)` por una agrupación por `Code` (p. ej. `GroupBy` y un array de mensajes). Elimina todos los 500 de validación de golpe.
3. **BUG-3** — comparar `request.RefreshToken` con `user.RefreshToken`, validar `RefreshTokenExpiryTime` y rotar el refresh token en cada renovación.
4. **BUG-6** — borrar las líneas 25-27 de `appsettings.Staging.json` antes del próximo deploy.
5. **BUG-4** — cambiar `'payments'` por `'payments/my'` en `payment.service.ts`.
6. **BUG-5** — implementar `IEmailService` de verdad (o, mientras sea desarrollo, exponer el token de reset por un canal controlado).
7. **BUG-7** — añadir `ClientId` a `ICurrentUserService` y filtrar en los 12 handlers + comprobar propiedad en las escrituras.
8. **BUG-8** — usar `SUM(lc.Amount)` como subtotal, o no recalcular el impuesto.

---

## Preguntas abiertas

- ¿Cuál es el valor real de `ASPNETCORE_ENVIRONMENT` en el servicio de Railway? Determina si BUG-6 es una bomba de relojería o si ya se esquivó a propósito.
- ¿El rol `BA` es una intención de diseño pendiente (Business Analyst) o un residuo? Condiciona si BUG-14 se arregla emitiéndolo o borrándolo.
- ¿`Permissions.cs` (~30 permisos) es la dirección deseada para la autorización, o se abandonó en favor de roles?
- ¿El `Amount` del cambio de almacén debería venir de una tarifa del servidor en vez del cliente?
- ¿Se pretende que la confirmación de email sea obligatoria para el login? Hoy no lo es.

## Notas de la investigación

- El comando `humanlayer thoughts sync` que sugiere la plantilla original **no existe** en el CLI instalado (`@humanlayer/cli` 0.31.0 expone `login`, `logout`, `daemon`, `api`, `hook`, `mcp`, `agents`, `codelayer`). Este documento no se sincronizó a ninguna nube; vive solo en el repo.
- Durante las pruebas se crearon varias cuentas de prueba en la BD de desarrollo (`dev@test.cl`, `probe1@test.cl`, `PROBE1@TEST.CL`, y algunas `e1*`/`ok*`/`frontend*`). El script `reset_users.sql` preparado en la conversación previa las elimina todas.
- Un `POST /auth/register` con email duplicado devolvió 500 una vez y 400 en un reintento posterior del mismo camino; no fue reproducible y no se incluye como bug.
