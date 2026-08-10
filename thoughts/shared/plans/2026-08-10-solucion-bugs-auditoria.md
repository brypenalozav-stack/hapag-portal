# Plan de solución de los bugs de la auditoría — Portal Hapag-Lloyd

## Overview

Arreglar los 19 bugs documentados en [thoughts/shared/research/2026-08-10-auditoria-completa-codigo-y-bugs.md](../research/2026-08-10-auditoria-completa-codigo-y-bugs.md), en 8 fases ordenadas por dependencia y riesgo. Backend .NET 9 (Clean Architecture + MediatR + EF Core/PostgreSQL) y frontend Angular 21.

## Decisiones tomadas (cerradas, sin preguntas abiertas)

1. **BUG-1**: se arregla el **frontend** para hablar el contrato del backend (`clientType` con `"Client"`/`"CustomsAgent"`). El backend es la fuente de verdad (tiene tests). El campo `type` de la *respuesta* ya es correcto y no se toca.
2. **BUG-5 (email)**: implementar `IEmailService` con **SMTP real configurable por variables de entorno** Y, en entornos no-producción, que `forgot-password` devuelva el token en la respuesta para poder probar sin servidor de correo.
3. **BUG-12 (confirmar email)**: **no** se exige email confirmado para login (se mantiene el comportamiento actual). Sí se corrige que la confirmación marque al `User`, que el token expire y el caso de usuario sin cliente.
4. **BUG-14 (rol BA)**: **eliminar** `"BA"` del backend y del frontend.
5. **BUG-7 (multi-tenancy)**: añadir `clientId` como **claim del JWT** y exponerlo en `ICurrentUserService`.
6. **BUG-8 (doble IVA)**: subtotal = `SUM(lc.Amount)` (base), respetando `IsTaxable`/`TaxRate` por cargo.
7. **BUG-9 (emails duplicados)**: normalizar email a minúsculas al escribir y al buscar (sin `citext` ni migración de índice).
8. **BUG-13 (almacén)**: alinear nombres de campo y validar `Amount > 0`; **no** se inventa tarifa en el servidor (queda como pendiente de producto).
9. **BUG-13 (nombre en perfil)**: se **quita "Nombre"** del formulario de perfil y se añaden Address/City. El backend solo permite Phone/Address/City y no se extiende.
10. **BUG-16 (contrato registro vs login)**: el registro pasa a devolver **`User.Id`** y el `role` normalizado (`"USER"`/`"ADMIN"`), igual que el login. Hoy devuelve `Client.Id` y el `UserType` crudo; nada en el frontend consume ese `id`, así que el cambio es seguro y elimina el doble vocabulario.
11. **BUG-12 (columna de confirmación)**: se añade `User.IsEmailConfirmed` y `User.EmailConfirmationTokenExpiry`. La confirmación marca **ambos** (`User` y `Client`) para no romper nada que ya lea el del cliente.
12. **Alcance de `GetAllBLs`** (Fase 6): si el usuario tiene rol `Admin` puede pasar `clientId`; cualquier otro rol queda forzado a su propio `ClientId`.

## Protocolo de ejecución

- **Una rama** (`feature/fix-auditoria-bugs`), **un commit por fase**, **un PR** a `develop` al final.
- **Checkpoint obligatorio al terminar cada fase**: correr la verificación automatizada, y luego **detenerse** a que el usuario valide manualmente antes de empezar la siguiente. No se avanza de fase sin confirmación explícita.
- Si una fase falla la verificación, se corrige dentro de esa misma fase; no se arrastra al siguiente commit.

## Current State Analysis

- Verificado en vivo contra `https://adventurous-adventure-production-83dd.up.railway.app` el 2026-08-10.
- **No hay `Makefile`**: los criterios automatizados usan `dotnet` y `ng` directamente.
- Solución backend: `backend/HapagPortal.sln`. Frontend: `frontend/` (Angular 21, scripts `ng build`).
- Rama base de trabajo: **`develop`**. Rama nueva: `feature/fix-auditoria-bugs` (o una por fase; ver "Estrategia de ramas").
- Tests: 405 unitarios backend (xUnit + FluentAssertions + NSubstitute); frontend **0 tests**.
- El `MockApplicationDbContext` de los tests de Application no aplica índices únicos ni `Include()` reales; los tests que dependan de unicidad/normalización deben ir en `HapagPortal.UnitTests.Infrastructure` (EF InMemory) o como pruebas HTTP.

## Desired End State

Registro, login, refresh y recuperación de contraseña funcionan end-to-end desde la web; ningún flujo de validación devuelve 500; los endpoints solo devuelven datos del cliente autenticado; el CI corre en cada PR; el despliegue no depende de un JSON roto. Verificable con: build+test verdes, `ng build` verde, y la batería de pruebas HTTP de cada fase.

### Key Discoveries
- Causa raíz de los 500: [ValidationBehavior.cs:29](../../../backend/src/HapagPortal.Application/Common/Behaviors/ValidationBehavior.cs#L29) usa `PropertyName` como `Code` + [ApiController.cs:33](../../../backend/src/HapagPortal.WebApi/Abstractions/ApiController.cs#L33) hace `Errors.Add(code, ...)` con clave duplicada.
- Contrato de registro: [RegisterCommand.cs:12](../../../backend/src/HapagPortal.Application/Auth/Register/RegisterCommand.cs#L12) espera `ClientType`; frontend envía `type` ([register.ts:23](../../../frontend/src/app/features/auth/register/register.ts#L23)).
- Refresh sin validar: [RefreshTokenCommandHandler.cs:20-40](../../../backend/src/HapagPortal.Application/Auth/RefreshToken/RefreshTokenCommandHandler.cs#L20-L40).
- Email stub: [EmailService.cs](../../../backend/src/HapagPortal.Infrastructure/Services/EmailService.cs).
- `ICurrentUserService` no expone `ClientId`: [ICurrentUserService.cs](../../../backend/src/HapagPortal.Application/Common/Interfaces/ICurrentUserService.cs).

## What We're NOT Doing

- No se implementa una tarifa de servidor para cambios de almacén (BUG-13, solo se alinea el contrato).
- No se migra a `citext` ni se cambian índices únicos para el email (BUG-9 se resuelve normalizando).
- No se implementa el rol `BA` (se elimina).
- No se hace obligatoria la confirmación de email en login (BUG-12).
- No se implementa generación real de PDF (sigue siendo placeholder; fuera de alcance).
- No se implementan pasarelas de pago reales (Khipu/BancoChile siguen simuladas; solo se endurecen los webhooks).
- No se crea una suite de tests de frontend desde cero (solo se corrigen contratos; los tests de frontend quedan como recomendación futura).

## Estrategia de ramas

**Una sola rama** para todo el trabajo, creada desde `develop`:

```
git checkout develop && git pull
git checkout -b feature/fix-auditoria-bugs
```

Las 8 fases se implementan en esa misma rama, en orden. Se hace **un commit por fase** (mensaje `fix(fase-N): ...`) para mantener el historial legible y poder revertir una fase concreta si algo falla, pero todo vive en `feature/fix-auditoria-bugs` y se abre **un único PR** hacia `develop` al final.
No commitear ni pushear sin autorización explícita del usuario.

---

## Phase 1: Desbloqueo de despliegue y CI

### Overview
Arreglar el `appsettings.Staging.json` inválido (BUG-6) y reubicar el workflow de CI para que efectivamente corra (BUG-18).

### Changes Required

#### 1. appsettings.Staging.json (BUG-6)
**File**: `backend/src/HapagPortal.WebApi/appsettings.Staging.json`
**Changes**: eliminar las líneas 25-27 sobrantes (el objeto ya cierra en la línea 24). Corregir `ExpirationInMinutes` → `ExpirationMinutes` (el código lee `Jwt:ExpirationMinutes` en [JwtTokenService.cs:26](../../../backend/src/HapagPortal.Infrastructure/Authentication/JwtTokenService.cs#L26)).

#### 2. CI a la raíz del repo (BUG-18)
**File**: mover `backend/.github/workflows/pr-tests.yml` → `.github/workflows/pr-tests.yml`
**Changes**: añadir `defaults.run.working-directory: backend` (o `working-directory` por step), ampliar el trigger a PRs contra `develop` **y** `main`, y añadir un job de frontend con `working-directory: frontend` (`npm ci` + `npx ng build --configuration production`). Recoger cobertura con `--collect:"XPlat Code Coverage"`.

> Verificado en el repo: `frontend/package-lock.json` existe y **está versionado en git**, así que `npm ci` es válido en CI (no hay que degradar a `npm install`).

### Success Criteria

#### Automated Verification:
- [x] JSON válido: `Get-Content backend/src/HapagPortal.WebApi/appsettings.Staging.json | ConvertFrom-Json` no lanza. ✅
- [x] La app arranca con `ASPNETCORE_ENVIRONMENT=Staging` sin excepción de configuración. ✅ (cubierto por la validez del JSON, que es el punto exacto donde `JsonConfigurationProvider` fallaba; no se hizo boot completo por requerir BD)
- [x] El workflow está en `.github/workflows/` y `dotnet test backend/HapagPortal.sln` pasa localmente. ✅ (408/408 tests; requirió `DOTNET_ROLL_FORWARD=LatestMajor` porque la máquina solo tiene runtime .NET 10, no 9)

#### Manual Verification:
- [ ] Un PR de prueba contra `develop` dispara el workflow en GitHub Actions y aparece en la pestaña Checks.
- [ ] El deploy en Railway sigue sano tras el cambio (`/health` → 200).

**Implementation Note**: pausar aquí para confirmación manual antes de la fase 2.

---

## Phase 2: Errores de validación (los 500 → 400) — BUG-2, BUG-17

### Overview
Transversal. Corrige que 2+ errores en la misma propiedad devuelvan 500, y el mapeo de errores de conflicto a 409.

### Changes Required

#### 1. Agrupar errores por código (BUG-2)
**File**: `backend/src/HapagPortal.WebApi/Abstractions/ApiController.cs`
**Changes**: reemplazar el `foreach ... Errors.Add(error.Code, [error.Message])` (líneas 31-34) por una agrupación:

```csharp
foreach (var group in validationResult.Errors.GroupBy(e => e.Code))
{
    problemDetails.Errors[group.Key] = group.Select(e => e.Message).ToArray();
}
```

#### 2. Mapeo de conflictos a 409 (BUG-17)
**File**: `backend/src/HapagPortal.WebApi/Abstractions/ApiController.cs`
**Changes**: en el switch (líneas 39-47), reconocer los códigos reales: `.AlreadyExists`, `.EmailExists`, `.HasPayments` → 409. Alternativa: normalizar los códigos en `DomainErrors`, pero cambiar el switch es menos invasivo y no rompe otros consumidores.

### Success Criteria

#### Automated Verification:
- [ ] Nuevo test WebApi: un command con 2 errores en la misma propiedad → `ValidationProblemDetails` 400 con array de 2 mensajes (no excepción).
- [ ] `dotnet test backend/HapagPortal.sln` verde.
- [ ] Prueba HTTP: `POST /auth/register` con `password:"abcdefg1"` → **400** (antes 500).
- [ ] Prueba HTTP: `POST /auth/register` con RUT duplicado → **409** (antes 400).

#### Manual Verification:
- [ ] El formulario de registro muestra mensajes de validación legibles en vez de "error inesperado".

**Implementation Note**: pausar para confirmación manual.

---

## Phase 3: Registro funcionando end-to-end — BUG-1, BUG-9, BUG-15, BUG-16, BUG-19

### Overview
Que registrarse desde la web funcione y sea consistente.

### Changes Required

#### 1. Contrato del formulario (BUG-1)
**File**: `frontend/src/app/features/auth/register/register.ts`, `frontend/src/app/core/models/client.model.ts`
**Changes**: el control y el payload deben enviar `clientType` con `"Client"`/`"CustomsAgent"` en vez de `type` con `"CLIENT"`/`"AGENT"`. Mapear la selección de UI a esos valores. Actualizar `RegisterRequest` ([client.model.ts:27-36](../../../frontend/src/app/core/models/client.model.ts#L27-L36)).

#### 2. Normalización de email (BUG-9)
**File**: `backend/src/HapagPortal.Application/Auth/Register/RegisterCommandHandler.cs` y los handlers de Login/Forgot/Reset/ConfirmEmail
**Changes**: normalizar `request.Email.Trim().ToLowerInvariant()` antes de comparar y de persistir (Email y Username). Centralizar en un helper para no duplicar.

#### 3. Validación de Clients.Email en registro (BUG-19)
**File**: `backend/src/HapagPortal.Application/Auth/Register/RegisterCommandHandler.cs`
**Changes**: añadir comprobación de `Clients.Email` duplicado (hoy solo valida `Users.Email` y `(TaxId,Country)`), devolviendo un `Error` de conflicto en vez de reventar en `SaveChanges`.

#### 4. Consistencia ClientType/type (BUG-15) y contrato de respuesta (BUG-16)
**File**: `backend/src/HapagPortal.Application/Common/Dtos/ClientResponseDto.cs`, `RegisterCommandHandler.cs`, `LoginCommandHandler.cs`
**Changes**:
- `FromClient` debe comparar contra `"CustomsAgent"` (además de `"Agent"`, por los datos del seed) para mapear a `"AGENT"`.
- **El registro pasa a devolver el mismo contrato que el login**: `Id = user.Id` (no `client.Id`) y `Role` normalizado a `"USER"`/`"ADMIN"` (no el `UserType` crudo `"Client"`/`"Agent"`). Se construye el DTO en el handler con los datos del `User` recién creado en vez de usar `FromClient`, o se extiende `FromClient` con un parámetro de `userId`.
- Verificado que es seguro: el frontend tipa la respuesta como `Client` pero **descarta el valor** ([register.ts:85-88](../../../frontend/src/app/features/auth/register/register.ts#L85-L88)) — solo usa el callback de éxito para redirigir a login. Ningún consumidor depende del `id` ni del `role` del registro.

### Success Criteria

#### Automated Verification:
- [ ] Test: registro con email en mayúsculas + email existente en minúsculas → conflicto (no dos cuentas). (En `UnitTests.Infrastructure` con EF InMemory o prueba HTTP.)
- [ ] Test Application nuevo: rama `ClientType == "CustomsAgent"` → `UserType = Agent`.
- [ ] `dotnet test` y `npx ng build --configuration production` verdes.
- [ ] Prueba HTTP con el payload EXACTO del formulario corregido → **201 Created**.
- [ ] Prueba HTTP: registrar `x@test.cl` y luego `X@TEST.CL` → la segunda da conflicto.

#### Manual Verification:
- [ ] Registro completo desde la web (CL cliente y BO agente) crea la cuenta y redirige a login.
- [ ] Login inmediato con la cuenta recién creada funciona.

**Implementation Note**: pausar para confirmación manual.

---

## Phase 4: Sesión y tokens — BUG-3, BUG-10

### Overview
Validar y rotar el refresh token; revocar sesiones al cambiar contraseña.

### Changes Required

#### 1. Validar y rotar refresh token (BUG-3)
**File**: `backend/src/HapagPortal.Application/Auth/RefreshToken/RefreshTokenCommandHandler.cs`
**Changes**: tras obtener el usuario, comparar `request.RefreshToken == user.RefreshToken` y `user.RefreshTokenExpiryTime > UtcNow`; si no, `User.InvalidCredentials`. Generar un refresh token nuevo, persistirlo con nueva expiría, y devolverlo en `AuthResponseDto` (añadir el 4º parámetro que hoy falta en [línea 65](../../../backend/src/HapagPortal.Application/Auth/RefreshToken/RefreshTokenCommandHandler.cs#L65)).

#### 2. Revocar sesión al resetear contraseña (BUG-10)
**File**: `backend/src/HapagPortal.Application/Auth/ResetPassword/ResetPasswordCommandHandler.cs`
**Changes**: al cambiar el hash, poner `RefreshToken = null`, `RefreshTokenExpiryTime = null`. Comprobar `user.IsActive` antes de permitir el reset.

### Success Criteria

#### Automated Verification:
- [ ] Test: refresh con `refreshToken` que no coincide → falla. (Reescribir el test engañoso `InvalidRefreshToken_ShouldReturnFailure`.)
- [ ] Test: refresh con `RefreshTokenExpiryTime` en el pasado → falla.
- [ ] Test: refresh exitoso rota el token (el nuevo ≠ el viejo) y viaja en la respuesta.
- [ ] Test: tras reset, `RefreshToken` queda null.
- [ ] Prueba HTTP: refresh con refreshToken basura → **401/400** (antes 200).

#### Manual Verification:
- [ ] Cambiar la contraseña invalida las sesiones existentes (un refresh posterior con el token viejo falla).

**Implementation Note**: pausar para confirmación manual.

---

## Phase 5: Email real + recuperación de contraseña — BUG-5, BUG-11, BUG-12

### Overview
Implementar envío real de email y cerrar los huecos del flujo de recuperación y confirmación.

### Changes Required

#### 1. IEmailService real + atajo de dev (BUG-5)
**File**: `backend/src/HapagPortal.Infrastructure/Services/EmailService.cs`, `DependencyInjection.cs`, `appsettings*.json`
**Changes**: implementar SMTP con `System.Net.Mail.SmtpClient` (o `MailKit`) configurable por `Smtp:Host/Port/User/Password/From` desde variables de entorno. Añadir una bandera que, en entorno no-producción, haga que `ForgotPassword` devuelva el token en la respuesta (por ejemplo, un `Result<...>` con el token cuando `IHostEnvironment.IsProduction()` es false).

#### 2. Enumeración en reset (BUG-11)
**File**: `backend/src/HapagPortal.Application/Auth/ResetPassword/ResetPasswordCommandHandler.cs`
**Changes**: no devolver el email en el mensaje de error; usar un error genérico "token inválido o expirado" para email-no-existe, token-incorrecto y token-expirado (coherente con la anti-enumeración de ForgotPassword).

#### 3. Confirmación de email (BUG-12, sin exigirla en login)
**File**: `backend/src/HapagPortal.Application/Auth/ConfirmEmail/ConfirmEmailCommandHandler.cs`, `backend/src/HapagPortal.Domain/Entities/User.cs`, `Persistence/Configurations/UserConfiguration.cs`, nueva migración
**Changes**:
- Añadir a `User` las propiedades `IsEmailConfirmed` (bool, default `false`) y `EmailConfirmationTokenExpiry` (DateTime?).
- `RegisterCommandHandler` fija la expiración del token de confirmación (p. ej. +48h) al generarlo.
- `ConfirmEmailCommandHandler`: validar la expiración; marcar **ambos** `user.IsEmailConfirmed = true` y, si existe cliente, `user.Client.IsEmailConfirmed = true` (así no se rompe nada que ya lea el del cliente); si `user.Client is null` **no** devolver `Client.NotFound` — confirmar solo el usuario.
- Login **se mantiene sin exigir** confirmación (decisión cerrada).
- Migración EF: `dotnet ef migrations add AddUserEmailConfirmation -p backend/src/HapagPortal.DatabaseMigrations -s backend/src/HapagPortal.WebApi`.

### Success Criteria

#### Automated Verification:
- [ ] Test: reset con email inexistente / token malo / token expirado → todos el mismo error genérico, sin filtrar el email.
- [ ] Test: token de confirmación expirado → falla.
- [ ] `dotnet test` verde (incluye migración nueva compilando).
- [ ] Prueba HTTP: en entorno dev, `forgot-password` devuelve el token; `reset-password` con ese token cambia la contraseña y permite login con la nueva.

#### Manual Verification:
- [ ] Con SMTP configurado, llega el correo de recuperación con el token/enlace.
- [ ] El enlace `reset-password?token=...&email=...` del correo completa el flujo en la web.

**Implementation Note**: pausar para confirmación manual.

---

## Phase 6: Multi-tenancy / IDOR — BUG-7

### Overview
Añadir `clientId` como claim del JWT y usarlo para que los 12 endpoints y las escrituras solo toquen datos del cliente autenticado.

### Changes Required

#### 1. Claim clientId
**File**: `backend/src/HapagPortal.Infrastructure/Authentication/JwtTokenService.cs`, `CurrentUserService.cs`, `Application/Common/Interfaces/ICurrentUserService.cs`
**Changes**: emitir `clientId` como claim en `GenerateToken`; exponer `Guid? ClientId` en `ICurrentUserService` leyéndolo del claim.

#### 2. Filtrar los 12 handlers de lectura
**File**: handlers listados en el research (GetBLByNumber, GetChargesByBL, GetContainersByBL, GetLocalCharges*, GetDemurrage*, GetPaymentById, GetReceiptById/Pdf, GetWarehouseChangeById, GetServiceOrderPdf, GetAllBLs)
**Changes**: comprobar que el recurso pertenece a `currentUser.ClientId`; si no, devolver **`NotFound`** (no `Forbidden`, para no revelar que el recurso existe).
**Caso `GetAllBLs`** (decisión cerrada): si el usuario tiene rol `Admin`, puede pasar `clientId` libremente; **cualquier otro rol queda forzado a su propio `ClientId`**, ignorando el parámetro recibido. Mismo criterio para el `country`.

#### 3. Escrituras con verificación de propiedad
**File**: `CancelPaymentCommandHandler`, `CreateWarehouseChangeCommandHandler`, `CreateReceiptCommandHandler`, `CreateServiceOrderCommandHandler`
**Changes**: validar propiedad del BL/pago contra `ClientId` antes de mutar; inyectar `ICurrentUserService` donde falte.

### Success Criteria

#### Automated Verification:
- [ ] Tests: un usuario del cliente A recibe `NotFound/Forbidden` al pedir un recurso del cliente B (por id/número).
- [ ] Test: `CancelPayment` de un pago ajeno falla.
- [ ] `dotnet test` verde.
- [ ] Pruebas HTTP con dos usuarios de clientes distintos: A no ve datos de B en los 12 endpoints.

#### Manual Verification:
- [ ] Navegando como cliente normal no se pueden abrir BLs/pagos/recibos de otro cliente cambiando el id/número en la URL.
- [ ] Tras el cambio de claim hay que volver a iniciar sesión (los tokens viejos no traen `clientId`) — verificar que el re-login funciona.

**Implementation Note**: pausar para confirmación manual.

---

## Phase 7: Contratos frontend↔backend restantes — BUG-4, BUG-13

### Overview
Alinear los formularios que hoy envían payloads que el backend rechaza.

### Changes Required

#### 1. GET payments (BUG-4)
**File**: `frontend/src/app/core/services/payment.service.ts`
**Changes**: cambiar `get('payments')` por `get('payments/my')`. Verificar Dashboard y Payment List.

#### 2. Órdenes de servicio (BUG-13)
**File**: `frontend/src/app/features/profile/service-orders/service-orders.ts` (+ modelo)
**Changes**: enviar `{ orderType, description, billOfLadingId (Guid), country }`. Requiere resolver el `billOfLadingId` real (Guid) a partir del BL seleccionado, y el `country` del usuario.

#### 3. Cambio de almacén (BUG-13)
**File**: `frontend/src/app/features/profile/warehouse/warehouse.ts`
**Changes**: enviar `{ fromWarehouse, toWarehouse, billOfLadingId, country, amount }`; validar `amount > 0`. Backend: `CreateWarehouseChangeCommandValidator` valida `Amount > 0`. (Sin tarifa de servidor.)

#### 4. Admin crédito (BUG-13)
**File**: `frontend/src/app/features/admin/credit-clients/credit-clients.ts`
**Changes**: seleccionar un `clientId` existente en vez de escribir nombre/RUT; enviar `{ clientId, country, creditLimit, expiresAt? }`.

#### 5. Perfil (BUG-13)
**File**: `frontend/src/app/features/profile/profile.ts`, `profile.html`, `frontend/src/app/core/services/client.service.ts` (`UpdateProfileRequest`)
**Changes** (decisión cerrada): **quitar "Nombre" del formulario editable** y añadir **Address** y **City**, que el backend acepta y el frontend hoy no envía. `UpdateProfileRequest` pasa a ser `{ phone?, address?, city? }`, alineado con `UpdateMyClientCommand`. El nombre se sigue mostrando, pero como texto de solo lectura. **No se extiende el backend.**

### Success Criteria

#### Automated Verification:
- [ ] `npx ng build --configuration production` verde.
- [ ] Pruebas HTTP: cada formulario corregido devuelve 2xx con el payload que genera el frontend.

#### Manual Verification:
- [ ] Dashboard y página de Pagos cargan datos reales.
- [ ] Crear orden de servicio, cambio de almacén y (como admin) cliente de crédito funcionan desde la web.
- [ ] Editar perfil persiste los cambios (los campos que el backend acepta).

**Implementation Note**: pausar para confirmación manual.

---

## Phase 8: IVA, webhooks y deuda técnica — BUG-8, BUG-14 + menores

### Overview
Corregir el doble IVA, endurecer webhooks, eliminar el rol BA y limpiar la deuda de bajo riesgo.

### Changes Required

#### 1. Doble IVA (BUG-8)
**File**: `backend/src/HapagPortal.Application/Payments/Create/CreatePaymentCommandHandler.cs`
**Changes**: subtotal de cargos locales = `SUM(lc.Amount)` (base), respetar `IsTaxable`/`TaxRate` por cargo; no re-sumar impuesto sobre `TotalAmount`. Considerar `EffectiveFrom/To` y `ServiceType` en la consulta de `TaxConfiguration`.

#### 2. Eliminar rol BA (BUG-14)
**File**: 5 `[Authorize(Roles="Admin,BA")]` en controllers + `auth.service.ts` `isAdmin()` + `client.model.ts` (union de `role`)
**Changes**: quitar `"BA"` en backend y frontend.

#### 3. Webhooks (menor)
**File**: `KhipuWebhookCommandHandler`, `BancoChileWebhookCommandHandler`
**Changes**: verificar el token/firma del webhook, validar que el importe coincida, y no confirmar pagos ya cancelados.

#### 4. Deuda de bajo riesgo
**Changes**: `nvarchar(max)` → `text` en `AuditLogConfiguration.cs`; soft delete de hijos (`UserRole`/`PaymentDetail`) consistente; `/health` que compruebe la BD; efectos de confirmación de pago (marcar `LocalCharge.Status = Paid`) — evaluar cuáles entran según tiempo.

### Success Criteria

#### Automated Verification:
- [ ] Test: pago de cargos locales de BL01 → total = 303.450 CLP (no 361.105,50).
- [ ] Test: webhook `done` sobre pago cancelado no lo confirma.
- [ ] Grep: 0 ocurrencias de `"BA"` en `[Authorize]` y en `isAdmin()`.
- [ ] `dotnet test` + `ng build` verdes.

#### Manual Verification:
- [ ] Un pago de cargos locales muestra el importe correcto en la web.
- [ ] `/health` refleja el estado real de la BD.

**Implementation Note**: fase final; validar el conjunto completo.

---

## Testing Strategy

### Unit Tests (backend):
- Rehacer el test engañoso de refresh token (Fase 4).
- Añadir tests de: agrupación de errores de validación (Fase 2), normalización de email y `CustomsAgent` (Fase 3), revocación de sesión (Fase 4), anti-enumeración y expiración de confirmación (Fase 5), aislamiento por cliente (Fase 6), cálculo de IVA y webhooks (Fase 8).
- Tests de unicidad/normalización van en `UnitTests.Infrastructure` (EF InMemory) o como pruebas HTTP, porque `MockApplicationDbContext` no aplica índices.

### Integration / HTTP:
- Reutilizar la batería de PowerShell contra Railway usada en la auditoría, adaptada como criterios de cada fase (dos usuarios de clientes distintos para la Fase 6).

### Manual Testing:
- Recorrido completo por la web: registro → login → recuperar contraseña → ver BLs/pagos → crear orden/almacén → admin crédito.

## Migration Notes

- Fase 5 y (posible) Fase 8 añaden columnas (`IsEmailConfirmed`/`EmailConfirmationTokenExpiry` en User; tipos de AuditLog). Generar migración EF y verificar que aplica sobre la BD de Railway.
- Fase 6 cambia los claims del JWT: **los tokens vigentes no traen `clientId`**, los usuarios deben re-loguearse. Comunicar o forzar re-login.

## References

- Investigación: [thoughts/shared/research/2026-08-10-auditoria-completa-codigo-y-bugs.md](../research/2026-08-10-auditoria-completa-codigo-y-bugs.md)
- Guía de deploy: [RAILWAY-DEPLOY.md](../../../RAILWAY-DEPLOY.md)
