# Integraciones del Portal 2.0 — inventario y contratos

Inventario de los sistemas con los que se integra el Portal 2.0 y contratos de integración propuestos (Fase 6a del plan `thoughts/shared/plans/2026-10-05-actualizacion-requerimientos-portal-2-0-ui-integraciones.md`).

Reglas que aplican a todo el inventario:
- **API, sin acceso directo a bases de datos** (decisión Q3 del registro `docs/requerimientos/registro-decisiones-v4.md`). Los datos de Nexus se consultan bajo demanda con caché corta; no hay sincronización de tablas.
- **Todos los contratos están en estado PROPUESTA.** Hapag-Lloyd todavía no entregó ninguno; los redactó el equipo de desarrollo Portal 2.0 y cada responsable de validación debe revisarlos.
- **Responsables de validación** según la tabla persona → área de Q5: Finanzas (Fer, Ricardo), Comercial (Kari), Customer Service (Cami, Mati), Nexus/IT (Lucho, Jorge), Arquitectura/QA (Andrés), Macros/RPX (Diego, RPX), Producto/Negocio (Katu), Área Legal y Área Seguridad TI (titular del área).
- **Puerto y modo.** Los puertos de `backend/src/HapagPortal.Application/Common/Interfaces/` y la selección `Integrations:<Sistema>:Mode` (`Dummy` o `Real`) se construyen en las fases 6b y 6c. Las columnas "Puerto" y "Modo" de esta tabla anticipan ese resultado; hoy ninguno existe.

## Inventario

Fichas: IDs de la especificación v3 más las fichas nuevas M2-10, M8-09 y M11-xx decididas en la Fase 0. Tareas: IDs de Pendientes original.

| Sistema | Propósito | Fichas | Tareas | Función NexusV2 | Protocolo conocido | Estado | Responsable de validación (Q5) | Puerto | Modo |
|---|---|---|---|---|---|---|---|---|---|
| Nexus | Exenciones, condición de crédito, FFWW autorizados, tipo de cambio y tarifas de recargos locales | M4-01, M4-02, M4-03, M4-04, M5-04, M5-05, M5-07, M7-03, M8-01, M8-02, M8-03, M2-10, M8-09 | 25–41 | Créditos, Exenciones, TC, Tarifa, Conexión Portal, API Estado de Cuenta, Datos, Counter, Plazos documentales / Feriados | Sin API hacia el portal; propuesta REST/JSON con `X-Api-Key` | Contratos PROPUESTA [CT-NEXUS](contratos/nexus.openapi.yaml) y [CT-COUNTER](contratos/counter.openapi.yaml) (Counter, Ola I) | Nexus/IT – Lucho (apoyo: Jorge) | `IExemptionReader`, `ICreditConditionReader`, `IExchangeRateProvider`, `ITariffProvider`, `ICounterRecorder` | Dummy (6b); Real contra simulador (6c); Counter Dummy o Real con el `BaseUrl` de Nexus (Ola I) |
| FIS/Data Lake | Embarques: BL, booking, ruta, nave, contenedores, partes con Match Code, ingresos, días libres y depósitos | M2-01, M2-02, M2-06, M2-07, M2-09, M3-03, M3-18 | 42–46 | — | Sin integración; propuesta REST/JSON con `X-Api-Key` (Q3) | Contrato PROPUESTA [CT-FIS](contratos/fis.openapi.yaml) | Macros/RPX – Diego (apoyo: Jorge) | `IShipmentSource` | Dummy (6b); Real contra simulador (6c) |
| Navesoft | Intercambio actual de cargos, pagos, cupones y solicitudes de cambio de almacén con el portal de pagos en producción | M3-04, M3-06 | 35 | Conexión Portal, Cambio de Almacén | CSV por FTP | Descripción del intercambio actual [CT-NAVE](navesoft.md) | Nexus/IT – Lucho | Sin puerto | — |
| Khipu | Pago en línea por transferencia simplificada | M5-01, M5-03, M7-02, NF-01, NF-04, NF-12 | 47–49, 53 | Confirmación Pago | API REST pública v3 (`x-api-key`); notificación v3 firmada (`x-khipu-signature`) a `POST /api/v1/payments/webhook/khipu` | Contrato PROPUESTA [CT-KHIPU](contratos/khipu.openapi.yaml); detalle en [pasarelas-pago.md](pasarelas-pago.md) | Finanzas – Fer | `IPaymentProvider("Khipu")` | Dummy; Real (`HttpKhipuPaymentProvider`) listo, falta cargar credenciales |
| Banco de Chile | Botón de pago bancario | M5-01, M5-03, M7-02, NF-01, NF-04, NF-12 | 47–49, 52, 55 | Confirmación Pago | Protocolo no público (manual del banco bajo contrato); formulario POST firmado y notificación a `POST /api/v1/payments/webhook/banco-chile` | Contrato PROPUESTA [CT-BCH](contratos/banco-chile.openapi.yaml); detalle en [pasarelas-pago.md](pasarelas-pago.md) | Finanzas – Ricardo | `IPaymentProvider("BancoChile")` | Dummy; Real configurable (`BancoChileFormPaymentProvider`) sin configurar hasta recibir el manual |
| Santander | Botón de pago bancario | M5-01, M5-03, M7-02 | 47–49, 52, 55 | Confirmación Pago | Getnet Chile Web Checkout (API PlacetoPay, `auth` con tranKey); notificación firmada a `POST /api/v1/payments/webhook/getnet` | Contrato PROPUESTA [CT-SANT](contratos/santander.openapi.yaml); detalle en [pasarelas-pago.md](pasarelas-pago.md) | Finanzas – Ricardo | `IPaymentProvider("Santander")` | Dummy; Real (`GetnetPaymentProvider`, `Integrations:Getnet`) listo, falta contrato y certificación |
| BCI | Pago BCI | M5-01, M5-03, M7-02 | 116–118 | Confirmación Pago | Bci Pagos (ex Pago Fácil), API REST `apis.pgf.cl` con `x_signature` HMAC-SHA256; callback a `POST /api/v1/payments/webhook/bci` | Contrato PROPUESTA [CT-BCI](contratos/bci.openapi.yaml); detalle en [pasarelas-pago.md](pasarelas-pago.md) | Finanzas – Fer | `IPaymentProvider("Bci")` | Dummy; Real (`BciPagosPaymentProvider`, `Integrations:BciPagos`) listo, falta contrato |
| Depósito | Pago por depósito o transferencia bancaria con boleta y confirmación manual | M5-03, M5-06, M7-02 | 31, 54 | Confirmación Pago | Manual: comprobante por canales externos; Nexus solo consulta | Descripción del intercambio actual [CT-DEP](deposito.md) | Nexus/IT – Lucho (apoyo: Finanzas – Fer, Ricardo) | Sin puerto | — |
| DBNet/SII | Emisión y consulta de documentos tributarios electrónicos | M3-11, M5-09, M7-01, M7-02 | 50–51 | — | API DBNet por definir (tarea 51: homologación con la API BAP); secreto `SII_KEY` existente | Contrato PROPUESTA [CT-DBNET](contratos/dbnet.openapi.yaml) | Finanzas – Fer (apoyo: Jorge) | `IInvoiceProvider` | Dummy (6b); Real contra simulador (6c) |
| Mercurio | Almacenes por puerto y almacén asignado a cada contenedor | M3-04, M3-05, M3-06 | 32 | Cambio de Almacén | API HTTP usada hoy por Nexus solo con GET | Contrato PROPUESTA [CT-MERC](contratos/mercurio.openapi.yaml), solo lectura | Nexus/IT – Lucho | Sin puerto | — |
| TATC/Flagare | Estado del TATC por contenedor y del CLD de Bolivia; generación masiva de TATC por localidad (propuesta Ola F) | M2-09, M3-04, M3-16, M6-07 | 32–34 | Generar TATC, Generar CLD, Cambio de Almacén | Servicios HTTP de Flagare consultados hoy por Nexus | Contrato PROPUESTA [CT-TATC](contratos/tatc.openapi.yaml) | Nexus/IT – Lucho | `ITatcProvider` | Dummy; Real con `BaseUrl` y caché corta (Ola F) |
| Tracking | Hitos y ubicación del embarque | M2-08 | Sin tarea | — | Sin integración | Contrato PROPUESTA [CT-TRACK](contratos/tracking.openapi.yaml) | Customer Service – Cami/Mati | `ITrackingProvider` | Dummy (6b); Real contra simulador (6c) |
| Contactos (P0060) | Registro de contactos y listas de distribución de reportes por tipo (aviso de arribo, copias, facturas, free time, demurrage, confirmación de booking) | M1-06, M1-25 | Sin tarea | — | Registro interno mantenido por gestión interna; propuesta REST/JSON con `X-Api-Key` | Contrato PROPUESTA [CT-CONTACTS](contratos/contacts.openapi.yaml) | Customer Service – Cami/Mati (apoyo: Nexus/IT – Jorge) | `IContactListProvider` | Dummy; Real con `Integrations:Contacts:BaseUrl` (Ola I) |
| Dispute | Acceso al sitio de Dispute de productos digitales de Hapag-Lloyd | M2-05 | Sin tarea | — | Enlace web, sin intercambio de datos | Descripción [CT-DISP](dispute.md) | Customer Service – Cami/Mati | Sin puerto | — |
| Firma | Firma electrónica de certificados | M6-01, M6-02, M6-07 | Sin tarea | — | Sin integración | Contrato PROPUESTA [CT-SIGN](contratos/firma.openapi.yaml) | Área Legal (titular) | `IDocumentSigner` | Solo Dummy (6b) |
| Storage | Almacenamiento de comprobantes, documentos de registro y documentos emitidos | M1-07, M5-06, M6-09, NF-16 | Sin tarea | — | Sin implementación | Contrato PROPUESTA [CT-STORAGE](storage.md) | Área Seguridad TI (titular) | `IFileStorage` | Solo Dummy (6b) |
| Correo | Correos transaccionales y notificaciones | M1-10, M1-25, M6-01, M10-05, M11-02 | Sin tarea | — | SMTP existente (`EmailService.cs`) | En operación | Nexus/IT – Jorge | `IEmailService` (existente) | Existente, sin cambio |
| IA | Asistente conversacional | M10-01 a M10-05 | Sin tarea | — | Motor de reglas propio; modelo abierto local opcional por HTTP (Ollama, `POST /api/chat`) | Sin contrato | Producto/Negocio – Katu | `IAssistantEngine` | Rules (por defecto); Ollama con `Assistant:Mode=Ollama` y `Assistant:Ollama:BaseUrl` (Ola F) |

### Notas
- **Correo:** SMTP existente (`EmailService.cs`). La localización ES/EN de los correos es de Fase 2 (Q8).
- **IA:** el asistente de la Ola F responde con reglas y datos del portal; el modelo local (Ollama) solo clasifica la intención y redacta con los datos ya autorizados, y si falla el portal responde con reglas. No se usan servicios de IA de pago.
- **FIS:** Entrada transitoria: `POST bills-of-lading/import` (Q6). El importador existente (`ImportBillsOfLadingCommandHandler.cs`) sigue siendo la vía de carga hasta que CT-FIS se valide y se conmute a `Mode=Real`.
- **Nexus «Datos» (tarea 37):** resuelta por Q3. El portal consulta a Nexus vía API bajo demanda, con caché corta.
- **Errores de facturación:** permanece en Nexus (Q7); no tiene contrato.

## Contratos

| ID | Archivo | Operaciones | Responsable de validación (Q5) | Puerto 6b |
|---|---|---|---|---|
| CT-NEXUS | [`contratos/nexus.openapi.yaml`](contratos/nexus.openapi.yaml) | `GET /exemptions`, `GET /customers/{taxId}/conditions`, `GET /exchange-rates`, `GET /tariffs` | Nexus/IT – Lucho (apoyo: Jorge) | `IExemptionReader`, `ICreditConditionReader`, `IExchangeRateProvider`, `ITariffProvider` |
| CT-FIS | [`contratos/fis.openapi.yaml`](contratos/fis.openapi.yaml) | `GET /shipments`, `GET /shipments/{blNumber}` | Macros/RPX – Diego (apoyo: Jorge) | `IShipmentSource` |
| CT-KHIPU | [`contratos/khipu.openapi.yaml`](contratos/khipu.openapi.yaml) | `POST /v3/payments`, `GET /v3/payments/{id}`, `DELETE /v3/payments/{id}`, notificación v3 firmada | Finanzas – Fer | `IPaymentProvider("Khipu")` |
| CT-BCH | [`contratos/banco-chile.openapi.yaml`](contratos/banco-chile.openapi.yaml) | Formulario POST firmado (según manual) y notificación firmada entrante | Finanzas – Ricardo | `IPaymentProvider("BancoChile")` |
| CT-SANT | [`contratos/santander.openapi.yaml`](contratos/santander.openapi.yaml) | Getnet: `POST /api/session`, `POST /api/session/{requestId}`, `POST /api/session/{requestId}/cancel`, notificación firmada | Finanzas – Ricardo | `IPaymentProvider("Santander")` |
| CT-BCI | [`contratos/bci.openapi.yaml`](contratos/bci.openapi.yaml) | Bci Pagos: `POST /trxs`, `POST /users/login`, `GET /trxs/{id_trx}`, callback | Finanzas – Fer | `IPaymentProvider("Bci")` |
| CT-DBNET | [`contratos/dbnet.openapi.yaml`](contratos/dbnet.openapi.yaml) | `POST /documents`, `GET /documents/{folio}` | Finanzas – Fer (apoyo: Jorge) | `IInvoiceProvider` |
| CT-TRACK | [`contratos/tracking.openapi.yaml`](contratos/tracking.openapi.yaml) | `GET /tracking/{reference}/events` | Customer Service – Cami/Mati | `ITrackingProvider` |
| CT-SIGN | [`contratos/firma.openapi.yaml`](contratos/firma.openapi.yaml) | `POST /sign` | Área Legal (titular) | `IDocumentSigner` |
| CT-STORAGE | [`storage.md`](storage.md) | `Save`, `OpenRead`, `Delete` | Área Seguridad TI (titular) | `IFileStorage` |
| CT-COUNTER | [`contratos/counter.openapi.yaml`](contratos/counter.openapi.yaml) | `GET /counter/bills-of-lading/{blNumber}`, `PUT /counter/bills-of-lading/{blNumber}` (propuesta Ola I) | Nexus/IT – Lucho (apoyo: Jorge) | `ICounterRecorder` (Ola I) |
| CT-CONTACTS | [`contratos/contacts.openapi.yaml`](contratos/contacts.openapi.yaml) | `GET /contacts/{matchCode}/distribution-lists`, `PUT /contacts/{matchCode}/distribution-lists/{reportType}` (propuesta Ola I) | Customer Service – Cami/Mati (apoyo: Nexus/IT – Jorge) | `IContactListProvider` (Ola I) |
| CT-MERC | [`contratos/mercurio.openapi.yaml`](contratos/mercurio.openapi.yaml) | `GET /warehouses`, `GET /bills-of-lading/{blNumber}/warehouse` (solo lectura) | Nexus/IT – Lucho | Sin puerto |
| CT-TATC | [`contratos/tatc.openapi.yaml`](contratos/tatc.openapi.yaml) | `GET /bills-of-lading/{blNumber}/tatc`, `GET /bills-of-lading/{blNumber}/cld`, `POST /tatc/generation-requests` (propuesta Ola F) | Nexus/IT – Lucho | `ITatcProvider` (Ola F) |
| CT-NAVE | [`navesoft.md`](navesoft.md) | Descripción del intercambio actual por CSV/FTP | Nexus/IT – Lucho | Sin puerto |
| CT-DEP | [`deposito.md`](deposito.md) | Descripción del flujo actual de depósito y transferencia | Nexus/IT – Lucho (apoyo: Finanzas – Fer, Ricardo) | Sin puerto |
| CT-DISP | [`dispute.md`](dispute.md) | Enlace al sitio de Dispute | Customer Service – Cami/Mati | Sin puerto |

### Canal Web Service de clientes (M3-17, Ola J)

API entrante del portal para clientes de alto volumen, no un sistema externo del inventario: [`ws-clientes.openapi.yaml`](ws-clientes.openapi.yaml) (CT-WS, PROPUESTA – pendiente de validación con Nexus/IT – Lucho, apoyo: Área Seguridad TI). Operaciones `GET /me`, `GET /responsibility-letters/terms`, `POST /responsibility-letters`, `POST /warehouse-changes`, `POST /warehouse-changes/bulk`, `GET /requests`, `GET /requests/{id}` bajo `/api/ws/v1`, con clave por cliente (`X-Api-Key`, rotación y revocación sin despliegue), alcances, límite por minuto e `Idempotency-Key`. Sin webhooks en esta entrega: el cliente consulta el estado.

### Elementos comunes de los contratos OpenAPI
- `openapi: 3.1.0`.
- `info.x-estado` con la frase `PROPUESTA – pendiente de validación con <responsable>`, repetida al inicio de `info.description`, e `info.x-responsable`.
- Esquema `Problem` (RFC 9457, `application/problem+json`) para todos los errores.
- Cabecera `X-Correlation-Id` en la solicitud y en todas las respuestas (NF-27).
- Respuestas 200, 400, 404, 429, 500 y 503 en cada operación; 429 y 503 incluyen `Retry-After`. Los webhooks agregan 401.
- Servidor `{baseUrl}`, que corresponde a `Integrations:<Sistema>:BaseUrl`. Su valor por defecto es el simulador local de la Fase 6c (`http://localhost:5199/<sistema>`).
- Autenticación con clave de API en cabecera; la clave se guarda como secreto cifrado del portal (NF-09).

### Ciclo de estado
1. **PROPUESTA – pendiente de validación con `<responsable>`**: estado actual de todos los contratos.
2. **Validado (`<dd-mm-aaaa>`, `<responsable>`)**: el responsable aprobó el contrato. Se actualizan `info.x-estado` y la primera línea de `info.description` (en los `.md`, la fila "Estado").

Solo un contrato Validado puede pasar a `Mode=Real` en un ambiente, siguiendo el checklist Dummy → Real de la Fase 6c.

## Verificación

```bash
python -m pip install -r docs/integraciones/requirements.txt
for f in docs/integraciones/contratos/*.openapi.yaml; do python -m openapi_spec_validator "$f" || exit 1; done
python scripts/requerimientos/verificar_contratos.py
```

Hoja de ruta por sistema: [`hoja-de-ruta.md`](hoja-de-ruta.md).
