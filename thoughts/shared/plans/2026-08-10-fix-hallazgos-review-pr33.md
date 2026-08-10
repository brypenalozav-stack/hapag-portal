# Corrección de los hallazgos P1 de la revisión del PR #33

## Overview

Cerrar los 4 hallazgos P1 de la revisión de código del PR #33 (todos verificados contra el código actual, ninguno es falso positivo), más los dos bloqueos de entorno que impidieron al revisor ejecutar tests y build de producción.

## Current State Analysis

Los 4 hallazgos son **reales**:

1. **IDOR en `CreatePayment`** — [CreatePaymentCommandHandler.cs:71-78](../../../backend/src/HapagPortal.Application/Payments/Create/CreatePaymentCommandHandler.cs#L71-L78) busca el BL solo por `request.BlId`, sin filtrar por cliente. Es una omisión de la Fase 6 del PR: `CreateServiceOrder` y `CreateWarehouseChange` sí validan propiedad, este no. Además persiste `ClientId = user.ClientId ?? Guid.Empty`.
2. **Token de reset expuesto** — [ForgotPasswordCommandHandler.cs:41-43](../../../backend/src/HapagPortal.Application/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs#L41-L43) devuelve el token cuando `!IsProduction`, lo que **incluye Staging**. Y `ResetToken` no-null vs null permite enumerar cuentas, rompiendo la anti-enumeración que el propio handler declara.
3. **Webhook Khipu sin autenticar** — [KhipuWebhookCommandHandler.cs](../../../backend/src/HapagPortal.Application/Payments/Commands/Webhooks/KhipuWebhookCommandHandler.cs): `NotificationToken` existe en el comando y **nunca se usa**; se confía en `Status` del JSON. El guard de estado terminal solo da idempotencia, no autenticación.
4. **Webhook Banco de Chile sin autenticar** — [BancoChileWebhookCommandHandler.cs](../../../backend/src/HapagPortal.Application/Payments/Commands/Webhooks/BancoChileWebhookCommandHandler.cs): `TransactionId` y `Amount` existen y **nunca se validan**.

### Restricciones descubiertas (condicionan el diseño)

- **El gateway de pagos es un stub simulado**: [PaymentGatewayService.cs](../../../backend/src/HapagPortal.Infrastructure/Services/PaymentGatewayService.cs) siempre devuelve éxito con referencias `SIM-…`. **No hay integración real con Khipu ni Banco de Chile**, ni credenciales, ni contrato de firma. Validar "contra la API oficial del proveedor" no es implementable hoy.
- **No existe patrón `IOptions`** en el repo (grep: 0 usos). La configuración se lee con `IConfiguration` directo (p. ej. `Jwt:Secret` en [JwtTokenService.cs:22](../../../backend/src/HapagPortal.Infrastructure/Authentication/JwtTokenService.cs#L22)). Se seguirá ese patrón.
- **El frontend no lee la respuesta de `forgot-password`**: está tipada como `Observable<unknown>` ([auth.service.ts:48](../../../frontend/src/app/core/services/auth.service.ts#L48)) y el componente solo usa el callback. Quitar el token del contrato **no rompe nada**.
- `Payment` **no tiene** campo `TransactionId`; guardar el id de transacción del banco requeriría migración.
- `index.html` carga Google Fonts desde CDN → el build de producción de Angular intenta inlinar las fuentes y **falla sin red**.
- Los proyectos de test son `net9.0`; con solo el runtime .NET 10 instalado, `dotnet test` no arranca los testhosts.

## Desired End State

- Ningún usuario puede crear un pago sobre un BL ajeno, ni sin `ClientId`.
- `forgot-password` devuelve **siempre** la misma respuesta observable y **nunca** un token.
- Los webhooks rechazan toda petición sin secreto válido (fail-closed) y validan referencia, monto, moneda e idempotencia; además pueden apagarse por configuración.
- Cualquiera puede clonar el repo y correr `dotnet test` y `ng build --configuration production` sin red ni runtime .NET 9.

## Decisiones tomadas (cerradas)

1. **Webhooks**: secreto compartido por cabecera **+ interruptor de apagado**. Fail-closed: si no hay secreto configurado, se rechaza. Se documenta que la validación real del proveedor debe montarse encima.
2. **ForgotPassword**: se **elimina por completo** el token de la respuesta. Para probar en local basta el log de `EmailService`, que ya escribe el token cuando no hay SMTP configurado.
3. **DX**: se arreglan **ambos** bloqueos (RollForward en tests + fuentes sin red).
4. **`CreatePayment` usará el claim `clientId`** (`ICurrentUserService.ClientId`) y se elimina la consulta a `Users`, que solo servía para obtener ese dato. Coherente con la Fase 6 del PR; los tokens sin el claim ya requieren re-login.
5. **Autenticación de webhooks vía abstracción en Application** (`IWebhookAuthenticator`) implementada en Infrastructure, igual que `IEmailService`/`IPaymentGatewayService`. Así los handlers son testeables y Application no toca `IConfiguration`.
6. **Protección anti-replay**: se usa el guard de estado terminal como idempotencia. Una tabla de eventos procesados (`ProcessedWebhookEvents`) es la solución completa y queda **fuera de alcance** (requiere migración); se documenta.

## What We're NOT Doing

- No se implementa integración real con Khipu ni Banco de Chile (sin credenciales ni contrato de firma del proveedor).
- No se añade tabla de eventos de webhook procesados ni campo `TransactionId` en `Payment` (evita migración).
- No se sustituye el stub de `PaymentGatewayService`.
- No se monta Mailpit/MailHog (se usa el log de `EmailService`).
- No se crea suite de tests de frontend ni se configura `ng test` (solo se documenta que el target no existe).
- No se migra el repo a `IOptions`.

## Estrategia de ramas

Se continúa en la **misma rama del PR #33**: `feature/fix-auditoria-bugs`. Un commit por fase. No commitear ni pushear sin autorización explícita del usuario.

---

## Phase 1: IDOR en CreatePayment

### Overview
Impedir que un usuario cree un pago sobre un BL de otro cliente, o sin cliente asociado.

### Changes Required

#### 1. Acotar el BL al cliente autenticado
**File**: `backend/src/HapagPortal.Application/Payments/Create/CreatePaymentCommandHandler.cs`
**Changes**: sustituir el bloque de resolución de usuario/BL (líneas ~59-78) por:

```csharp
var clientId = currentUserService.ClientId;

if (clientId is null)
    return Result<PaymentResponseDto>.Failure(
        new Error("Error.Unauthorized", "User is not associated with a client."));

// Se filtra por cliente EN LA CONSULTA: nunca se carga un BL ajeno.
var bl = await dbContext.BillsOfLading
    .Include(b => b.Client)
    .Include(b => b.LocalCharges)
    .FirstOrDefaultAsync(
        b => b.Id == request.BlId && b.ClientId == clientId.Value,
        cancellationToken);

if (bl is null)
    return Result<PaymentResponseDto>.Failure(
        DomainErrors.BillOfLading.NotFound(request.BlId));
```

- Se elimina la consulta a `Users` (solo servía para obtener `ClientId`).
- `Payment.ClientId = clientId.Value` (deja de existir el fallback `Guid.Empty`).
- La validación va **antes** de cualquier llamada a `IPaymentGatewayService` (ya lo está: el gateway se invoca al final).

### Success Criteria

#### Automated Verification:
- [ ] Compila: `dotnet build backend/HapagPortal.sln -c Release`
- [ ] Tests nuevos en `backend/tests/HapagPortal.UnitTests.Application/Payments/CreatePaymentCommandHandlerTests.cs`:
  - [ ] Un cliente crea un pago para un BL **propio** → éxito
  - [ ] Un cliente NO puede crear un pago para un BL **de otro cliente** → `BillOfLading.NotFound`
  - [ ] Un usuario **sin `ClientId`** → `Error.Unauthorized`
  - [ ] Con BL ajeno **no se llama** a `IPaymentGatewayService` (`gateway.DidNotReceive()`)
  - [ ] Con BL ajeno **no se crean** `Payment` ni `PaymentDetail` (listas vacías, `SaveChangesCallCount == 0`)
- [ ] Suite completa verde: `dotnet test backend/HapagPortal.sln -c Release`

#### Manual Verification:
- [ ] Con dos usuarios de clientes distintos, `POST /api/v1/payments` con el `blId` del otro devuelve 404 y no crea nada.

---

## Phase 2: ForgotPassword — sin token y sin enumeración

### Overview
La respuesta pública deja de contener el token y pasa a ser indistinguible exista o no el correo.

### Changes Required

#### 1. Revertir el comando a respuesta vacía
**File**: `backend/src/HapagPortal.Application/Auth/ForgotPassword/ForgotPasswordCommand.cs`
**Changes**: volver a `ICommand` sin tipo de respuesta y **eliminar** el record `ForgotPasswordResponse`.

```csharp
public sealed record ForgotPasswordCommand(string Email) : ICommand;
```

#### 2. Handler sin dependencia de entorno
**File**: `backend/src/HapagPortal.Application/Auth/ForgotPassword/ForgotPasswordCommandHandler.cs`
**Changes**: `ICommandHandler<ForgotPasswordCommand>` devolviendo `Result.Success()`; se elimina `devToken` y la inyección de `IAppEnvironment`. El token sigue generándose, guardándose y enviándose por `IEmailService` (que en local lo escribe en el log).

#### 3. Controlador
**File**: `backend/src/HapagPortal.WebApi/Controllers/V1/AuthController.cs`
**Changes**: `forgot-password` vuelve a `Ok()` sin cuerpo.

#### 4. Eliminar la abstracción que queda sin uso
**Files**: `backend/src/HapagPortal.Application/Common/Interfaces/IAppEnvironment.cs`, `backend/src/HapagPortal.Infrastructure/Services/AppEnvironment.cs`, registro en `DependencyInjection.cs`
**Changes**: borrar ambos y su `AddSingleton`. Se introdujeron solo para este atajo; dejarlos sería código muerto (el mismo tipo de deuda que la auditoría señaló con `Permissions`).

### Success Criteria

#### Automated Verification:
- [ ] Compila y suite verde: `dotnet build` + `dotnet test`
- [ ] Tests en `ForgotPasswordCommandHandlerTests.cs`:
  - [ ] Correo **existente** → `Result.Success()` sin cuerpo; el token se persiste y se envía por email
  - [ ] Correo **inexistente** → `Result.Success()` idéntico; sin `SaveChanges` ni email
  - [ ] Ninguna variante del resultado expone el token (no existe la propiedad)
- [ ] Grep: 0 ocurrencias de `IAppEnvironment` y de `ForgotPasswordResponse` en `backend/src`
- [ ] Frontend sigue compilando: `npx ng build --configuration production` (en `frontend/`)

#### Manual Verification:
- [ ] `POST /auth/forgot-password` devuelve 200 con cuerpo vacío tanto para un correo real como para uno inexistente (respuestas byte a byte equivalentes).
- [ ] En local, el token aparece en el log de la aplicación y permite completar `reset-password`.

---

## Phase 3: Autenticación de webhooks de pago

### Overview
Los dos webhooks pasan a exigir un secreto compartido (fail-closed), validar referencia/monto/moneda y ser idempotentes; además se pueden apagar por configuración.

### Changes Required

#### 1. Abstracción de autenticación
**File** (nuevo): `backend/src/HapagPortal.Application/Common/Interfaces/IWebhookAuthenticator.cs`

```csharp
public interface IWebhookAuthenticator
{
    bool WebhooksEnabled { get; }
    /// Compara en tiempo constante el secreto recibido con el configurado.
    /// Devuelve false si no hay secreto configurado (fail-closed).
    bool IsValid(string provider, string? providedSecret);
}
```

#### 2. Implementación
**File** (nuevo): `backend/src/HapagPortal.Infrastructure/Services/WebhookAuthenticator.cs`
**Changes**: lee de `IConfiguration`:
- `Payments:Webhooks:Enabled` (bool, **default false** → fail-closed)
- `Payments:Webhooks:Khipu:Secret`
- `Payments:Webhooks:BancoChile:Secret`

Comparación con `CryptographicOperations.FixedTimeEquals` sobre los bytes UTF-8. Si el secreto configurado está vacío o el recibido es nulo/vacío → `false`. **No se registra en el log el secreto ni el token completo**; solo se loguea el intento fallido con el proveedor y una marca truncada.

Registro en `DependencyInjection.cs`: `services.AddSingleton<IWebhookAuthenticator, WebhookAuthenticator>();`

#### 3. Handler de Khipu
**File**: `backend/src/HapagPortal.Application/Payments/Commands/Webhooks/KhipuWebhookCommandHandler.cs`
**Changes**: antes de tocar nada:
1. Si `!WebhooksEnabled` → `Error("Webhook.Disabled")`.
2. Si `!IsValid("Khipu", request.NotificationToken)` → `Error.Unauthorized`. **Aquí se empieza a usar `NotificationToken`**, que hoy se ignora.
3. Buscar el pago por `ExternalReference`; si no existe → error genérico que **no revela** si la referencia es válida.
4. Guard de estado terminal (`Confirmed`/`Cancelled`) → éxito sin efectos (idempotencia).
5. Solo entonces aplicar el mapeo de estado.

Comentario en el código dejando explícito que el flujo real de Khipu exige **llamar a su API** con el `notification_token` y que esta validación por secreto es una medida intermedia.

#### 4. Handler de Banco de Chile
**File**: `backend/src/HapagPortal.Application/Payments/Commands/Webhooks/BancoChileWebhookCommandHandler.cs`
**Changes**: mismos pasos 1-4, y además **usar los campos que hoy se ignoran**:
- `request.Amount` debe coincidir con `payment.TotalAmount` (comparación con tolerancia de 2 decimales); si no → no confirmar, error de validación.
- `request.TransactionId` obligatorio no vacío; se registra en `payment.ExternalReference`-adyacente vía log de auditoría (sin campo nuevo, ver "NOT doing").
- El secreto se recibe por cabecera; el comando conserva `TransactionId`/`Amount` para validación.

#### 5. Controlador: pasar la cabecera al comando
**File**: `backend/src/HapagPortal.WebApi/Controllers/V1/PaymentsController.cs`
**Changes**: las dos acciones siguen `[AllowAnonymous]` (un webhook debe serlo) pero leen `Request.Headers["X-Webhook-Secret"]` y lo inyectan en el comando (`command with { Secret = ... }`), de modo que el secreto **no viaja en el body** ni queda en logs de request body. Se añade `string? Secret` a ambos comandos.

#### 6. Configuración de ejemplo
**Files**: `appsettings.json`, `appsettings.Staging.json`
**Changes**: añadir la sección con `Enabled: false` y secretos vacíos, documentando que se rellenan por variables de entorno (`Payments__Webhooks__Khipu__Secret`).

### Success Criteria

#### Automated Verification:
- [ ] Compila y suite verde: `dotnet build` + `dotnet test`
- [ ] Tests nuevos (`KhipuWebhookCommandHandlerTests.cs`, `BancoChileWebhookCommandHandlerTests.cs`):
  - [ ] Sin secreto / secreto inválido → el pago **no se modifica** y no se setean `ConfirmedAt`/`ConfirmedBy`
  - [ ] Webhooks deshabilitados → rechazo sin efectos
  - [ ] Banco de Chile: `Amount` distinto de `TotalAmount` → no confirma
  - [ ] Banco de Chile: `TransactionId` vacío → no confirma
  - [ ] Notificación auténtica → confirma y setea `ConfirmedAt`/`ConfirmedBy`
  - [ ] Repetir la misma notificación auténtica → sin efectos adicionales (idempotente)
  - [ ] Pago `Cancelled` → permanece `Cancelled`
  - [ ] `ExternalReference` inexistente → error genérico sin filtrar información
- [ ] Test de `WebhookAuthenticator`: sin secreto configurado devuelve `false` (fail-closed)
- [ ] Grep: `NotificationToken`, `TransactionId` y `Amount` aparecen **usados** en sus handlers

#### Manual Verification:
- [ ] `POST /api/v1/payments/webhook/khipu` sin cabecera → 401, y el pago sigue en su estado.
- [ ] Con `Payments:Webhooks:Enabled=false` (default) los dos endpoints rechazan todo.
- [ ] Con el secreto correcto en la cabecera, el pago se confirma una sola vez.

---

## Phase 4: Desbloquear tests y build para quien revise

### Overview
Cerrar los dos impedimentos que el revisor reportó, para que el PR sea verificable en cualquier entorno.

### Changes Required

#### 1. Tests ejecutables con runtime .NET 10
**Files**: los 5 `.csproj` de `backend/tests/`
**Changes**: añadir `<RollForward>LatestMajor</RollForward>` en el `PropertyGroup`, para que los testhosts `net9.0` arranquen sobre el runtime instalado sin exigir exactamente 9.0.

#### 2. Build de producción sin red
**File**: `frontend/angular.json`
**Changes**: en la configuración `production`, desactivar el inlining de fuentes:
`"optimization": { "scripts": true, "styles": true, "fonts": { "inline": false } }`.
Las fuentes se siguen cargando desde el CDN en runtime (igual que hoy en dev); lo que se elimina es la **dependencia de red en tiempo de build**.

#### 3. Nota sobre `ng test`
**File**: `thoughts/shared/research/2026-08-10-auditoria-completa-codigo-y-bugs.md`
**Changes**: dejar anotado que `ng test` no tiene target configurado en `angular.json` y que `--watch` no es un argumento válido; crear la suite de frontend queda fuera de alcance.

### Success Criteria

#### Automated Verification:
- [ ] `dotnet test backend/HapagPortal.sln -c Release` corre **sin** definir `DOTNET_ROLL_FORWARD` a mano
- [ ] `npx ng build --configuration production` termina correctamente **sin acceso a Internet**
- [ ] Suite completa verde

#### Manual Verification:
- [ ] El revisor confirma que puede ejecutar tests y build de producción en su entorno.

---

## Testing Strategy

### Unit Tests
- **Fase 1**: 5 casos sobre `CreatePaymentCommandHandler` (propio / ajeno / sin ClientId / gateway no invocado / nada persistido). Se usa `MockApplicationDbContext` + `Substitute.For<IPaymentGatewayService>()` con `_currentUser.ClientId.Returns(...)`, siguiendo el patrón ya establecido en el PR.
- **Fase 2**: 2-3 casos de indistinguibilidad de la respuesta.
- **Fase 3**: 8 casos por handler según la lista del revisor, más los del autenticador.

### Integración / HTTP
Pruebas manuales con PowerShell contra Railway, con **dos usuarios de clientes distintos** para la Fase 1 y con/sin cabecera de secreto para la Fase 3.

### Manual Testing Steps
1. Registrar dos cuentas de clientes distintos; obtener sus tokens.
2. Con el token de A, intentar `POST /payments` con un `blId` de B → 404, sin pago creado.
3. `forgot-password` con correo existente y con uno inventado → respuestas idénticas.
4. Golpear los dos webhooks sin cabecera → 401; con secreto correcto → confirma una vez; repetir → sin cambios.

## Migration Notes

Ninguna migración de base de datos. La Fase 3 introduce **configuración nueva** que debe fijarse en Railway antes de habilitar webhooks:
`Payments__Webhooks__Enabled`, `Payments__Webhooks__Khipu__Secret`, `Payments__Webhooks__BancoChile__Secret`. Por defecto (`Enabled=false`) los webhooks quedan cerrados, así que **no hay regresión** si no se configuran.

## Deuda documentada (fuera de alcance, para el siguiente PR)

- Validación real contra Khipu (llamada a su API con `notification_token`) y contra Banco de Chile (HMAC/certificado oficial).
- Tabla `ProcessedWebhookEvents` + campo `TransactionId` en `Payment` para anti-replay completo con ventana temporal.
- Sustituir el stub de `PaymentGatewayService`.
- Suite de tests de frontend y target `ng test`.

## References

- Revisión del PR #33 (4 hallazgos P1) — este plan
- Auditoría original: [thoughts/shared/research/2026-08-10-auditoria-completa-codigo-y-bugs.md](../research/2026-08-10-auditoria-completa-codigo-y-bugs.md)
- Plan previo (8 fases ya implementadas): [thoughts/shared/plans/2026-08-10-solucion-bugs-auditoria.md](2026-08-10-solucion-bugs-auditoria.md)
- Patrón de propiedad por cliente a replicar: [CreateServiceOrderCommandHandler.cs:37-42](../../../backend/src/HapagPortal.Application/ServiceOrders/Commands/Create/CreateServiceOrderCommandHandler.cs#L37-L42)
