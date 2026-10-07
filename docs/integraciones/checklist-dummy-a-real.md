# Checklist Dummy → Real

Pasos para cambiar un sistema de `Integrations:<Sistema>:Mode=Dummy` a `Mode=Real` en un ambiente (staging o producción). Se completa **por sistema y por ambiente**; ninguno pasa a Real con un paso pendiente.

Sistemas con cliente Real: **Nexus**, **Fis**, **DbNet**, **Tracking**, **Tatc**, **Contacts** y las pasarelas de pago **Khipu**, **Getnet** (botón Santander), **BciPagos** (botón BCI) y **BancoChile** (formulario firmado, sin configurar hasta recibir el manual del banco). Signature y Storage solo tienen adaptador Dummy: con `Mode=Real` la API no arranca.

Las pasarelas de pago tienen su guía propia en [`pasarelas-pago.md`](pasarelas-pago.md): secretos, `Payments:PublicBaseUrl`, URL de notificación que se registra, ambiente de pruebas y certificación. En ellas las credenciales faltantes no detienen el arranque: el cobro falla con `Integration.NotConfigured`.

En Railway la configuración se escribe con doble guion bajo, por ejemplo `Integrations__Nexus__Mode=Real`.

## Pasos

| # | Paso | Cómo se verifica |
|---|---|---|
| 1 | **Contrato validado.** En `docs/integraciones/contratos/<sistema>.openapi.yaml`, `info.x-estado` pasa de `PROPUESTA – pendiente de validación con …` a `Validado (<dd-mm-aaaa>, <responsable>)`, y la misma frase abre `info.description`. Si el contrato cambia, se ajustan el cliente `Http*` y el simulador. | `python scripts/requerimientos/verificar_contratos.py` y el job `contracts` de CI en verde. |
| 2 | **Secreto cargado.** La API key se guarda cifrada con `UpsertSecretCommand` en el tipo del sistema (tabla de abajo). Sin secreto, el cliente devuelve `Integration.NotConfigured`. | `GET /api/v1/configuration/secrets?scope=Global` lista el tipo (sin mostrar el valor). |
| 3 | **`BaseUrl` y timeouts configurados.** `Integrations:<Sistema>:BaseUrl` (URL absoluta entregada por el responsable), `TimeoutSeconds` (por intento, 10 por defecto), `TotalTimeoutSeconds` (llamada completa, 30) y `RetryBaseDelayMs` (2000). `TotalTimeoutSeconds` debe ser mayor que `TimeoutSeconds`. | La API arranca; con `BaseUrl` vacía o relativa no arranca y el mensaje lo indica. |
| 4 | **Prueba en sandbox.** Con el ambiente de pruebas del sistema externo: una consulta con datos, una sin datos (404 → sin datos) y, en pagos, un pago completo con su notificación. | Registro de la prueba (fecha, datos usados, resultado) adjunto a la tarea "Validar contrato" de Pendientes. |
| 5 | **Alerta sobre `hapagportal.integrations.errors`.** Alerta por sistema (etiqueta `system`) cuando el contador sube por encima del umbral acordado; el log Warning de NF-27 trae `System`, `Operation`, `StatusCode`, `DurationMs` y `CorrelationId`. | Disparo de prueba: con `X-Sim-Scenario: error500` contra el simulador, o con una `BaseUrl` inválida en staging, la alerta llega. |
| 6 | **Vuelta atrás definida.** `Integrations__<Sistema>__Mode=Dummy` y reinicio del servicio. En pagos, además, `Payments__Webhooks__Enabled=false` si hay que cortar las notificaciones. | Ensayo en staging: el cambio a Dummy deja la API operativa. |
| 7 | **Aprobación del responsable.** El responsable de validación del contrato (Q5) aprueba el paso a Real en ese ambiente. | Aprobación registrada en la tarea de Pendientes, con fecha y ambiente. |

## Datos por sistema

| Sistema (`Integrations:<Sistema>`) | Contrato | Secreto (tipo) | Cabecera | Responsable de validación | Notas |
|---|---|---|---|---|---|
| `Nexus` | CT-NEXUS | `NEXUS_API_KEY` | `X-Api-Key` | Nexus/IT – Lucho (apoyo: Jorge) | Un cliente para exenciones, crédito, tipo de cambio y tarifas. |
| `Fis` | CT-FIS | `FIS_API_KEY` | `X-Api-Key` | Macros/RPX – Diego (apoyo: Jorge) | Mientras no pase a Real, la carga sigue por `POST bills-of-lading/import` (Q6). |
| `Khipu` | CT-KHIPU | `KHIPU_SECRET`, `KHIPU_WEBHOOK_SECRET` | `x-api-key` | Finanzas – Fer | API v3. La notificación se verifica con `x-khipu-signature` (HMAC-SHA256 del cuerpo crudo) y el pago se vuelve a consultar por `payment_id`. Pruebas con una cuenta de cobro en modo desarrollador (DemoBank). |
| `Getnet` (botón Santander) | CT-SANT | `GETNET_LOGIN`, `GETNET_SECRET_KEY` | — (objeto `auth` en el cuerpo) | Finanzas – Ricardo | `BaseUrl` de pruebas `https://checkout.test.getnet.cl`; producción tras la certificación (4 transacciones de prueba). Registrar `/api/v1/payments/webhook/getnet` en el panel de Getnet. |
| `BciPagos` (botón BCI) | CT-BCI | `BCIPAGOS_ACCOUNT_ID`, `BCIPAGOS_TOKEN_SECRET`, `BCIPAGOS_USERNAME`, `BCIPAGOS_PASSWORD` | — (`x_signature` en el cuerpo; Bearer JWT para consultar) | Finanzas – Fer | `BaseUrl` de desarrollo `https://apis-dev.pgf.cl`; producción `https://apis.pgf.cl`. Ajustar `PaidStatusValues`/`FailedStatusValues` con la prueba. |
| `BancoChile` | CT-BCH | `BANCOCHILE_MERCHANT_ID`, `BANCOCHILE_SIGNING_KEY` | — (formulario firmado) | Finanzas – Ricardo | Protocolo no público: completar `Integrations:BancoChile:*` con el manual del banco. No requiere `BaseUrl`. Sin manual, mantener `Mode=Dummy` y el medio deshabilitado en producción. |
| `DbNet` | CT-DBNET | `DBNET_API_KEY` | `X-Api-Key` | Finanzas – Fer (apoyo: Jorge) | La emisión envía `Idempotency-Key` = referencia del pago. |
| `Tracking` | CT-TRACK | `TRACKING_API_KEY` | `X-Api-Key` | Customer Service – Cami/Mati | — |

## Prueba local contra el simulador

```bash
dotnet run --project backend/tools/HapagPortal.IntegrationSimulator --urls http://localhost:5199
```

Con la API en `Integrations__<Sistema>__Mode=Real` e `Integrations__<Sistema>__BaseUrl=http://localhost:5199/<prefijo>` (`nexus`, `fis`, `khipu`, `dbnet` o `tracking`), los escenarios de falla se piden con la cabecera `X-Sim-Scenario`: `error500`, `timeout` (30 s), `lento` (3 s) y `429` (con `Retry-After`). `/contracts/<nombre>` devuelve el YAML del contrato.
