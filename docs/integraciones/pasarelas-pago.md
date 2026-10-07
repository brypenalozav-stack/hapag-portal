# Pasarelas de pago en línea

Cómo funcionan en el portal los cuatro medios de pago en línea (ficha M5-03), qué falta para encenderlos y qué está confirmado o supuesto. Todo está implementado; **solo faltan los datos de cada contrato** (credenciales, URL de producción y, en Banco de Chile, el manual técnico).

| Medio (código) | Clave del medio | Pasarela | Sección de configuración | Webhook del portal |
|---|---|---|---|---|
| Khipu (`KHIPU`) | `Khipu` | Khipu, API v3 | `Integrations:Khipu` | `POST /api/v1/payments/webhook/khipu` |
| Botón Santander (`BANK_BUTTON_SANTANDER`) | `Santander` | Getnet Chile Web Checkout (API PlacetoPay) | `Integrations:Getnet` | `POST /api/v1/payments/webhook/getnet` |
| Botón BCI (`BANK_BUTTON_BCI`) | `Bci` | Bci Pagos (ex Pago Fácil), `apis.pgf.cl` | `Integrations:BciPagos` | `POST /api/v1/payments/webhook/bci` |
| Botón Banco de Chile (`BANK_BUTTON_BCH`) | `BancoChile` | Formulario firmado configurable (manual del banco) | `Integrations:BancoChile` | `POST /api/v1/payments/webhook/banco-chile` |

La URL completa del webhook es `Payments:ApiPublicBaseUrl` (o `Payments:PublicBaseUrl`) + la ruta; por ejemplo `https://portal.hapag-lloyd.cl/api/v1/payments/webhook/getnet`.

## Reglas comunes

- **El retorno del pagador nunca confirma un pago.** Al volver a `/payments/{id}/result`, la página pide al portal `POST /api/v1/payments/{id}/verify`, que consulta el estado a la pasarela.
- **La notificación tampoco confirma por sí sola.** Se verifica la firma sobre el **cuerpo crudo** (el endpoint no hace model binding antes de verificar) y luego se consulta el estado a la pasarela. Solo se confirma si la pasarela informa la misma referencia del portal, el mismo monto (tolerancia 0,01) y la misma moneda (`PaymentStatusSync`). Firma inválida → 401 sin cambios; monto distinto → 400 sin cambios; pasarela sin respuesta → 503.
- **Idempotencia.** Un pago confirmado o anulado no cambia; una notificación duplicada se reconoce sin repetir efectos.
- **Conciliación periódica** (`PaymentReconciliationWorker`): cada `Payments:Reconciliation:IntervalSeconds` (300) consulta los pagos en línea en curso con al menos `MinAgeMinutes` (10) y hasta `MaxAgeHours` (72) de antigüedad, por el mismo camino que el webhook. Cubre las notificaciones perdidas (Getnet notifica una sola vez). Con los adaptadores Dummy no consulta nada.
- **Anulación por Finanzas.** Un pago en curso se anula primero en la pasarela (Khipu, Getnet); si la pasarela ya lo cobró, queda confirmado y no se anula.
- **Sin credenciales no falla el arranque.** Con `Mode=Real` y sin credenciales, el cobro falla con `Integration.NotConfigured` y el pago queda fallido sin cobro (el usuario ve "la plataforma no respondió").
- **Secretos.** Las credenciales se guardan cifradas en el almacén de secretos (`UpsertSecretCommand`, ámbito Global), nunca en `appsettings` ni en variables de entorno, y nunca se registran en logs.
- **Los clientes HTTP de pago no reintentan POST ni DELETE** (un reintento podría crear un segundo cobro); la conciliación cubre las consultas fallidas.
- **Modo por defecto: Dummy.** Local, desarrollo y pruebas e2e usan el adaptador simulado, que lleva al pagador al simulador de pago del portal (ver «Simulador en modo de prueba»). En Dummy la notificación simulada lleva `X-Webhook-Secret` (`Payments:Webhooks:<Clave>:Secret`) y un JSON `{ externalReference, status, transactionId?, amount? }`.
- **Interruptor general.** `Payments:Webhooks:Enabled` debe ser `true` para recibir notificaciones (también las Real).

## Configuración común

| Clave | Valor |
|---|---|
| `Payments:PublicBaseUrl` | URL pública del sitio (retorno del pagador), p. ej. `https://portal.hapag-lloyd.cl`. Obligatoria en Real. |
| `Payments:ApiPublicBaseUrl` | URL pública de la API (webhooks). Vacía = la misma del sitio. |
| `Payments:Webhooks:Enabled` | `true` para recibir notificaciones. |
| `Payments:Reconciliation:*` | `Enabled` (true), `IntervalSeconds` (300), `MinAgeMinutes` (10), `MaxAgeHours` (72), `BatchSize` (50). |

En Railway se escribe con doble guion bajo: `Integrations__Getnet__Mode=Real`, `Payments__PublicBaseUrl=https://…`.

## 1. Khipu (API v3)

**Por qué:** Khipu es el medio vigente y su API v3 es pública y completa.

| Operación | Llamada |
|---|---|
| Crear | `POST /v3/payments` con `amount` (entero en CLP), `currency`, `subject`, `transaction_id` = número de pago del portal, `return_url`, `cancel_url`, `notify_url`, `notify_api_version: "3.0"`, `expires_date` (ahora + `ExpirationMinutes`), `payer_email`, `payer_name` y, si `FixPayerTaxId=true`, `fixed_payer_personal_identifier`. Se redirige a `payment_url`. |
| Consultar | `GET /v3/payments/{payment_id}`. `done` = pagado salvo `status_detail` `reversed`, `rejected-by-payer` o `marked-as-abuse` (= fallido); `verifying` = en proceso; `pending` = pendiente. |
| Anular | `DELETE /v3/payments/{payment_id}` (solo pendientes). |
| Notificación | `x-khipu-signature: t=<unix ms>,s=<base64>`; `s = Base64(HMAC-SHA256(secreto, t + "." + cuerpoCrudo))`, comparación en tiempo constante, `|ahora − t| ≤ WebhookToleranceSeconds` (300). Luego se consulta el pago. |

Autenticación: cabecera `x-api-key`. Se eliminó el flujo antiguo por `notification_token` (no existe en v3).

| Configuración | Valor |
|---|---|
| `Integrations:Khipu:Mode` | `Dummy` → `Real` |
| `Integrations:Khipu:BaseUrl` | `https://payment-api.khipu.com` (único host; no hay sandbox aparte) |
| `ExpirationMinutes`, `WebhookToleranceSeconds`, `FixPayerTaxId` | 60, 300, false |
| Secreto `KHIPU_SECRET` | API key de la cuenta de cobro |
| Secreto `KHIPU_WEBHOOK_SECRET` | Secreto de la cuenta de cobro con que Khipu firma las notificaciones |

**Pruebas:** crear una "cuenta de cobro en modo desarrollador" (solo acepta bancos de prueba como DemoBank, con su propia API key y secreto), cargar esos secretos en staging y pagar con DemoBank. Comprobar que la notificación llega y que el pago queda confirmado; repetir con un pago rechazado.

**Confirmado:** endpoints, campos, estados y algoritmo de firma (documentación pública). **Por confirmar con Khipu:** si el secreto de firma es distinto de la API key (se asume distinto), reintentos, IP de origen y límites de uso.

## 2. Botón Santander = Getnet Chile Web Checkout

**Por qué:** el botón de pago de Santander para comercios es Getnet Chile, cuya pasarela Web Checkout usa la API pública de PlacetoPay. Se integra por HTTP directo, sin SDK.

| Operación | Llamada |
|---|---|
| Autenticación | Objeto `auth` en el cuerpo de **todas** las llamadas: `login`, `tranKey = Base64(SHA-256(nonceCrudo + seed + secretKey))`, `nonce = Base64(nonceCrudo)` (16 bytes aleatorios), `seed` = ahora en ISO-8601 con zona. |
| Crear sesión | `POST /api/session` con `locale` `es_CL`, `payment { reference, description, amount { currency, total } }`, `expiration` (≥ 5 min), `returnUrl`, `cancelUrl`, `ipAddress` y `userAgent` del pagador (si no se conocen, `FallbackIpAddress`/`FallbackUserAgent`) y `buyer`. Se redirige a `processUrl`. |
| Consultar | `POST /api/session/{requestId}`: `APPROVED` = pagado (monto del intento aprobado); `REJECTED` y `PARTIAL_EXPIRED` = fallido; `PENDING` y `APPROVED_PARTIAL` = pendiente (un pago parcial nunca se aprueba). |
| Anular | `POST /api/session/{requestId}/cancel`. |
| Notificación | La URL **se registra en el panel de Getnet** (no va en la solicitud). `signature = "sha256:" + hex(SHA-256(requestId + status.status + status.date + secretKey))`, o el formato antiguo hex(SHA-1(…)); se aceptan ambos, con `status.date` tal como llega. Se envía **una sola vez**: por eso se consulta al volver el pagador y en la conciliación. |

| Configuración | Valor |
|---|---|
| `Integrations:Getnet:Mode` | `Dummy` → `Real` |
| `Integrations:Getnet:BaseUrl` | Pruebas `https://checkout.test.getnet.cl` (valor por defecto); producción `https://checkout.getnet.cl` |
| `Locale`, `ExpirationMinutes`, `FallbackIpAddress`, `FallbackUserAgent` | `es_CL`, 30, `127.0.0.1`, `HapagPortal/1.0` |
| Secreto `GETNET_LOGIN` | login del comercio |
| Secreto `GETNET_SECRET_KEY` | secretKey del comercio |

**Certificación:** con las credenciales de prueba, hacer las 4 transacciones de prueba con las tarjetas de prueba de Getnet y enviar el formulario de certificación. Getnet entrega entonces el login y secretKey de producción; se cambia `BaseUrl` a producción y se registra la URL de notificación en el panel.

**Confirmado:** API, autenticación, estados y firma de la notificación (documentación pública de PlacetoPay y hosts de Getnet). **Por confirmar con Getnet:** los datos del comercio y el registro de la URL de notificación.

## 3. Botón BCI = Bci Pagos (ex Pago Fácil)

**Por qué:** Bci Pagos es la pasarela de BCI para comercios, con API REST pública en `apis.pgf.cl`.

| Operación | Llamada |
|---|---|
| Crear | `POST /trxs`, sin cabecera de autorización: `x_account_id`, `x_amount` (entero CLP, mínimo 500), `x_currency`, `x_reference` (número de pago), `x_customer_email`, `x_url_complete`, `x_url_callback` (webhook del portal), `x_url_cancel`, `x_shop_country` `CL`, `x_session_id` y `x_signature`. Se redirige a la opción "gateway" de `pay_url` (o a la primera). |
| Firma | Campos escalares de primer nivel sin `x_signature`, ordenados por clave en orden ordinal; mensaje = clave + valor concatenados; `x_signature = hex minúscula(HMAC-SHA256(mensaje, token_secret))`. El monto se firma como texto entero ("53550"). |
| Consultar | `POST /users/login` (`username`, `password`) → JWT (en `token`, `access_token` o `body.data.token`), en caché hasta su `exp` (o `TokenCacheMinutes`); `GET /trxs/{id_trx}` con `Authorization: Bearer`. Ante un 401 se pide otro token una vez. |
| Estados | Pagado: `responce_code = "0"` con `auth_code`, o un estado que contiene un texto de `PaidStatusValues`; fallido: un texto de `FailedStatusValues`; el resto, pendiente. Los textos son configurables. |
| Callback | Formato no publicado. Si trae `x_signature` se verifica con el mismo HMAC; el pago se identifica por `x_id_trx`/`id_trx` o `x_reference`, y **siempre** se vuelve a consultar. |
| Anular | No hay API: el cobro pendiente vence solo. |

| Configuración | Valor |
|---|---|
| `Integrations:BciPagos:Mode` | `Dummy` → `Real` |
| `Integrations:BciPagos:BaseUrl` | Desarrollo `https://apis-dev.pgf.cl` (valor por defecto); producción `https://apis.pgf.cl` |
| `ShopCountry`, `FallbackCustomerEmail`, `TokenCacheMinutes` | `CL`, vacío (correo si el usuario no tiene), 10 |
| `PaidStatusValues`, `FailedStatusValues` | Por defecto `completed, complete, paid, aprobad, exitos` y `rechaz, reject, failed, fallid, anulad, cancel, expir` |
| Secreto `BCIPAGOS_ACCOUNT_ID` | token_service |
| Secreto `BCIPAGOS_TOKEN_SECRET` | token_secret (firma) |
| Secretos `BCIPAGOS_USERNAME`, `BCIPAGOS_PASSWORD` | usuario y clave para consultar el estado |

**Pruebas:** con credenciales de desarrollo, crear una transacción, pagarla en el ambiente de pruebas y revisar en el log del portal qué llega al callback y qué devuelve `GET /trxs/{id}`; ajustar `PaidStatusValues`/`FailedStatusValues` si los textos reales difieren.

**Confirmado:** creación `POST /trxs` y algoritmo de firma (código del SDK oficial `SignatureHelper.php`). **Supuesto:** forma exacta de `pay_url`, formato de `status` en la consulta y formato del callback (el portal los lee con tolerancia y siempre vuelve a consultar).

## 4. Botón Banco de Chile (deshabilitado hasta recibir el manual)

**Por qué así:** el protocolo de "Pagos Electrónicos en Otros Sitios" **no es público**; Banco de Chile entrega el manual técnico con el contrato (soporte empresas 600 637 3838). Lo público es el flujo: el pagador va al ambiente seguro del banco con monto y orden precargados, se autentica con Banconexión y confirma; solo CLP. El cliente REST anterior (`HttpBancoChilePaymentProvider`) implementaba un contrato inventado y se eliminó.

Se implementó un adaptador configurable de **formulario firmado** (`BancoChileFormPaymentProvider`), el patrón típico de los botones bancarios chilenos:

1. **Inicio:** el portal arma un formulario POST con convenio, orden (número de pago), monto CLP, URL de retorno, notificación y cancelación, fecha y firma, y el navegador lo envía solo al banco (página `/payments/{id}/redirect`, con el botón "Continuar al banco" de respaldo).
2. **Notificación:** el banco hace POST a `/api/v1/payments/webhook/banco-chile`; el portal verifica la firma con los campos configurados, compara orden, monto y CLP con el pago y responde el texto que exija el manual.
3. **Consulta de estado:** queda sin configurar (el manual dirá si existe y su protocolo); la conciliación omite estos pagos.

| Configuración (`Integrations:BancoChile:…`) | Qué poner (del manual) |
|---|---|
| `Mode` | `Dummy`; `Real` solo con todo lo siguiente completo. No requiere `BaseUrl`. |
| `FormUrl`, `FormMethod` | URL del ambiente seguro del banco y método (POST) |
| `MerchantIdField`, `OrderField`, `AmountField`, `ReturnUrlField`, `NotifyUrlField`, `CancelUrlField`, `DateField`, `DateFormat`, `SignatureField` | Nombres de los campos (uno vacío no se envía) y formato de fecha (hora de Chile) |
| `ExtraFields` | Campos fijos adicionales (p. ej. versión) |
| `SignedFields`, `SignatureSeparator` | Campos firmados en orden y separador |
| `SignatureAlgorithm`, `SignatureEncoding` | `HMACSHA256`, `HMACSHA1` o `SHA256` (valores + llave); `Hex`, `HexUpper` o `Base64` |
| `NotificationOrderField`, `NotificationAmountField`, `NotificationStatusField`, `NotificationTransactionField`, `NotificationSignatureField`, `NotificationSignedFields` | Campos de la notificación y los que se firman (vacío = los mismos de `SignedFields`) |
| `PaidStatusValues`, `FailedStatusValues` | Valores exactos del estado pagado y rechazado |
| `NotificationAcknowledgement` | Texto de respuesta que espera el banco |
| Secretos `BANCOCHILE_MERCHANT_ID`, `BANCOCHILE_SIGNING_KEY` | Código de convenio y llave de firma |

Mientras falte el manual, con `Mode=Real` el cobro falla con `Integration.NotConfigured` (el arranque no falla). En producción conviene además **deshabilitar el medio `BANK_BUTTON_BCH`** en el mantenedor de medios de pago hasta completar la certificación con el banco.

**Confirmado:** nada del protocolo técnico. **Supuesto:** todo el formato (por eso es configurable).

## Simulador en modo de prueba

Con `Integrations:<Sistema>:Mode=Dummy` (el valor por defecto) no hay sitio de la pasarela ni del banco: el adaptador `DummyPaymentProvider` envía al pagador a la página del portal `/payments/simulator`, que imita a la pasarela del medio elegido (Khipu, Getnet/Botón Santander, Bci Pagos o Banco de Chile: nombre, logo y color) y muestra siempre el aviso «Simulador de pago — modo de prueba. No se realiza ningún cargo real.».

- **Enlace.** `/payments/simulator?provider=<Clave>&ref=<referencia del portal>&amount=<monto>&currency=<moneda>&returnUrl=<retorno>`, en el mismo origen que la URL de retorno (`/payments/{id}/result?ref=…`). La página solo vuelve a una ruta `/payments/…` del propio portal: un retorno de otro sitio (`https://…`, `//…`, `javascript:`) se rechaza y no se ofrecen los botones (sin redirección abierta). Banco de Chile usa el mismo simulador (sin formulario firmado). El depósito con boleta no cambia (no redirige).
- **Resultados.** «Pagar» (`approved` → Confirmado), «Rechazar el pago» (`rejected` → Fallido), «Dejar pendiente» (`pending` → sigue en proceso) y «Cancelar y volver al portal» (`cancelled` → Fallido, sin cobro). La página envía `POST /api/v1/payments/simulator/{referencia}` con `{ "outcome": "…" }` y vuelve a la página de resultado, que llama a `verify` como con la pasarela real.
- **Seguridad.** El endpoint existe solo si la pasarela del pago está en Dummy (el adaptador implementa `ISimulatedPaymentProvider`) y si el pago es de la organización del usuario; si no, 404. Con una pasarela Real el simulador no se puede usar.
- **Cómo se aplica.** El resultado se guarda en `IPaymentSimulatorStore` (singleton en memoria, único para las cuatro claves) y se aplica en el acto por `PaymentStatusSync`, el mismo camino que `verify` y el webhook: la consulta de estado de Dummy informa el resultado con el monto y la moneda del pago del portal, y se compara referencia, monto y moneda. Un pago con resultado final (Confirmado, Fallido o Anulado) no se vuelve a simular: el endpoint responde 409 (`PaymentSimulator.Conflict`) sin guardar ni cambiar nada, y la página lo informa y ofrece ver el estado del pago. La consulta de Dummy no depende de memoria propia: tras reiniciar la API, un pago sin resultado sigue en proceso (no se confirma solo) y el usuario puede volver a abrir el simulador. Una referencia de la pasarela con `REJECT` sigue dando Fallido.
- **Código.** Backend: `Application/Payments/Simulator/SimulatePaymentCommand.cs`, `Application/Common/Interfaces/IPaymentSimulator.cs`, `Infrastructure/Integrations/Payments/DummyPaymentProvider.cs` e `InMemoryPaymentSimulatorStore.cs`. Frontend: `features/payments/payment-simulator/`. Pruebas e2e: `e2e/funcional/simulador-pago.spec.ts`.

## Pasos para encender una pasarela (por ambiente)

1. Cargar los secretos del contrato (tabla de cada pasarela) con `UpsertSecretCommand` en ámbito Global.
2. Configurar `Payments:PublicBaseUrl` (y `ApiPublicBaseUrl` si la API tiene otro dominio) y `Payments:Webhooks:Enabled=true`.
3. Revisar `Integrations:<Sistema>:BaseUrl` (pruebas o producción) y poner `Mode=Real`.
4. Registrar el webhook donde corresponde (Getnet: panel; Khipu y BCI: va en cada cobro; Banco de Chile: según el manual).
5. Hacer el pago de prueba completo (pagado y rechazado) y verificar en el historial del pago el actor de la confirmación (`<CLAVE>_WEBHOOK`, `PAYER_RETURN_CHECK` o `PAYMENT_RECONCILIATION`).
6. Seguir `checklist-dummy-a-real.md` (alertas, vuelta atrás, aprobación).

## Código

- Puerto: `backend/src/HapagPortal.Application/Common/Interfaces/IPaymentProvider.cs` (`InitiateAsync`, `GetStatusAsync`, `CancelAsync`, `ReadNotificationAsync`).
- Sincronización única: `Application/Payments/Common/PaymentStatusSync.cs`; webhook: `Application/Payments/Commands/Webhooks/PaymentNotificationCommand.cs`; retorno y conciliación: `Application/Payments/Lifecycle/PaymentProviderSyncCommands.cs`.
- Adaptadores: `Infrastructure/Integrations/Payments/` (`HttpKhipuPaymentProvider`, `GetnetPaymentProvider`, `BciPagosPaymentProvider`, `BancoChileFormPaymentProvider`, `DummyPaymentProvider`).
- Endpoint: `WebApi/Controllers/V1/PaymentsController.cs` (`webhook/{pasarela}`, `{id}/verify`, `simulator/{referencia}` solo en Dummy); proceso: `WebApi/BackgroundServices/PaymentReconciliationWorker.cs`.
- Frontend: `core/services/payment-redirect.service.ts` y `features/payments/payment-redirect/` (formulario que se envía solo); simulador en modo de prueba: `features/payments/payment-simulator/`.

## Fuentes

- Khipu: documentación https://docs.khipu.com/portal/es/payment-api/ y referencia de la API v3 https://docs.khipu.com/apis/v3/instant-payments/openapi (crear, consultar, anular y notificación v3 con `x-khipu-signature`).
- Getnet / PlacetoPay Web Checkout: https://docs.placetopay.dev/checkout/, autenticación https://docs.placetopay.dev/checkout/authentication/, sesión https://docs.placetopay.dev/checkout/api/reference/session/ y notificación https://docs.placetopay.dev/checkout/notification/; ambiente de pruebas https://checkout.test.getnet.cl.
- Bci Pagos / Pago Fácil: API https://apis.pgf.cl (desarrollo https://apis-dev.pgf.cl) y SDK oficial de firma https://github.com/PSTPAGOFACIL/sdk-apis-php-signature (`SignatureHelper.php`).
- Banco de Chile: información comercial pública de "Pagos Electrónicos en Otros Sitios" / Recaudación por Internet; el manual técnico se entrega con el contrato.
