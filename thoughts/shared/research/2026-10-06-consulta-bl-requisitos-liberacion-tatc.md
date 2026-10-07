---
title: 2026-10-06-consulta-bl-requisitos-liberacion-tatc
type: research
date: 2026-10-06
status: active
project: hapag-portal
scope: shared
author: brypenalozav-stack
ticket: null
tags: [research, liberacion-de-carga, tatc, flete, gate-in, carta-de-responsabilidad, carta-de-liberacion, bolivia, chile, consulta-bl]
related: [thoughts/shared/research/2026-10-05-actualizacion-requerimientos-portal-2-0.md, thoughts/shared/plans/2026-10-05-implementacion-fase1-funcional.md]
researcher: brypenalozav-stack
git_commit: 45d3bce518460de1e45dc64cb9dd93919de95da9
branch: feature/ux-ui-componentes
repository: hapag-portal
topic: "Consulta BL con requisitos de liberación (flete, Gate In, carta de responsabilidad; en Bolivia, carta de liberación), TATC automático y flujo de estado de liberación"
last_updated: 2026-10-06
last_updated_by: brypenalozav-stack
last_updated_note: "Se agrega el catálogo de servicios y las fichas del documento base Portal_2.0_Especificacion_Funcional_validada(1).docx (fuente de verdad; todo lo Fase 1 es obligatorio)"
---

# Investigación: consulta BL con requisitos de liberación y TATC

**Fecha**: 2026-10-06T20:19:52-03:00
**Investigador**: brypenalozav-stack
**Commit**: [`45d3bce`](https://github.com/brypenalozav-stack/hapag-portal/tree/45d3bce518460de1e45dc64cb9dd93919de95da9)
**Rama**: feature/ux-ui-componentes
**Repositorio**: hapag-portal

## Pregunta

Se pidió una "consulta BL" con los requisitos básicos para liberar la carga:

- En **Chile**: flete pagado, Gate In y carta de responsabilidad presentada.
- En **Bolivia**: lo anterior más la carta de liberación.

Con todos los requisitos cumplidos se generaría un **TATC automático**, según el país. La pantalla mostraría el flujo del estado de liberación, con cada requisito como OK o pendiente.

Las referencias son la pantalla antigua "CONSULTA BL" del portal Navesoft (`CSAVCHILE.WEB_COBROSBL_Vn`) y su comprobante de TATC:

- **Encabezado por BL**: recargos BL, canje, cambio de almacén y carta de responsabilidad, con los valores AUTORIZADO / NO GENERADO.
- **Detalle por contenedor**: Gate In, EDS, demurrage, número de TATC y enlace al comprobante.
- **Avisos**: el TATC está disponible desde 72 h antes del arribo (48 h para Callao → Iquique, Angamos y Antofagasta) y no se emite TATC para contenedores SOW.

Esta investigación describe **qué existe hoy** en el código para cada pieza. No propone el diseño.

## Resumen

1. **No existe una consulta que agregue los requisitos de liberación.**
   - Cada requisito se calcula en un lugar distinto y se expone en un endpoint distinto.
   - El permiso `release-requirements.view` ("Consultar estado de los requisitos de liberación") está definido y sembrado en los perfiles, pero ningún handler ni controlador lo usa.
2. **Los cuatro requisitos tienen hoy un estado calculable**, cada uno por separado:
   - **Flete**: `"PAID"` o `"PENDING"`.
   - **Gate In, EDS y Gate Out**: estado del cargo local (`Pending`, `Paid`, `Exempt` o `CreditImputed`) y resultado de reglas.
   - **Carta de responsabilidad**: `Fulfilled` o `Missing`, y solo es requisito para FFWW.
   - **Carta de liberación**: flujo de solicitud con aprobación; solo Bolivia, importación.
3. **El TATC no lo genera el portal.**
   - El portal lo **lee** de un sistema externo (CT-TATC, port `ITatcProvider`), por BL y con detalle por contenedor.
   - Solo puede **pedir** la generación en masa, por acción manual del usuario.
   - No hay disparador automático, comprobante PDF del TATC ni reglas de 72 h, 48 h o SOW.
   - El contenedor sí tiene el dato `IsShipperOwned`.
4. **El modelo es por BL, no por contenedor.**
   - Gate In, EDS y Gate Out son cargos por BL, sin columna de contenedor.
   - Por contenedor solo existen las líneas de demurrage y el TATC, este último leído en vivo y sin guardarse.
5. **Chile y Bolivia se distinguen en pocas piezas:**
   - El certificado de libre deuda (CLD).
   - Las demoras anticipadas.
   - La carta de liberación.
   - El flete Collect como bloqueo del CLD.
   - Las reglas de cargos locales y la carta de responsabilidad no tienen rama por país.
6. **No hay un componente de pasos o flujo compartido.**
   - Existen dos de alcance local: `reinvoicing-steps` (lista `hl-steps`) y `service-request-timeline`.
   - La consulta de BL actual es el listado y el detalle de embarques. La ruta `/bills-of-lading` redirige a `/shipments`.

## Hallazgos detallados

### 1. Flete pagado

- **Dónde se guarda.** En el BL: `FreightPaidAt`, `FreightTerms` (Prepaid o Collect), `FreightAmount` y `FreightCurrency` (`backend/src/HapagPortal.Domain/Entities/BillOfLading.cs:20-21,33,37`).
- **Cuándo se marca.** `PaymentPostProcessing` llena `FreightPaidAt` al confirmarse un pago de tipo Freight (`backend/src/HapagPortal.Application/Payments/PostProcessing/PaymentPostProcessing.cs:61-65`).
- **Cómo lo expone el detalle.** El detalle del embarque calcula `"PAID"` o `"PENDING"` en línea: pagado si existe `FreightPaidAt` o un `Payment` Freight confirmado ([`GetShipmentDetailQuery.cs:65-70`](https://github.com/brypenalozav-stack/hapag-portal/blob/45d3bce518460de1e45dc64cb9dd93919de95da9/backend/src/HapagPortal.Application/Shipments/Detail/GetShipmentDetailQuery.cs#L65-L70)).
  - Solo lo devuelve si el usuario tiene `PayFreight`; si no, llega `null`.
  - Endpoint: `GET /shipments/{blNumber}`.
- **Diferencias por país.** Solo en Bolivia, el flete Collect sin pagar es un bloqueo del CLD (`PENDING_FREIGHT`, `backend/src/HapagPortal.Application/Documents/Common/NoDebtEvaluator.cs:75-81`). El cálculo del detalle no tiene condición de país.

### 2. Gate In, EDS y Gate Out

- **Modelo.** Son `LocalCharge` con `ChargeType` `GATE_IN`, `EDS` o `GATE_OUT` (`backend/src/HapagPortal.Domain/Constants/ChargeConceptCodes.cs:12-14`). Son exentables desde Nexus (`:58`). La entidad no tiene contenedor (`backend/src/HapagPortal.Domain/Entities/LocalCharge.cs:7-16`); el contenedor aparece, como mucho, en el texto de la descripción.
- **Estado del cargo.** `Pending`, `Paid`, `Exempt` o `CreditImputed` (`ChargeConceptCodes.cs:85-96`).
- **Resultado de reglas.** `ChargeRulesService.EvaluateAsync` (`backend/src/HapagPortal.Application/ChargeRules/Common/ChargeRulesService.cs:82-278`) devuelve por cargo:
  - `Outcome`: `Payable`, `PartiallyExempt`, `Exempt`, `Paid` o `CreditImputed` (`backend/src/HapagPortal.Domain/Constants/ChargeRuleConstants.cs:4-13`).
  - Una acción.
  - Un motivo de bloqueo.
  - Además entrega `RequiresPayment`, `AllApplicableExempt` y `CanProceed` (`ChargeRulesService.cs:270-274`).
- **Autorización.** No hay un estado "autorizado". Los estados que liberan son Paid, Exempt y CreditImputed. Las exenciones son por organización y concepto (`IExemptionReader`), no por BL ni por contenedor.
- **Endpoints.**
  - `GET /charges/{blNumber}`, que devuelve `ShipmentChargesDto`.
  - `POST /charges/{blNumber}/apply-rules`.
  - La lista cruda en el detalle: `GetShipmentDetailQuery.cs:72-90`.
- **Diferencias por país.** Ninguna: `ChargeRulesService` no tiene rama por país.
- **Gate Out.** Según la especificación, se lee de una fuente distinta a Gate In y EDS (`docs/requerimientos/especificacion-funcional-v4.txt:777-779`). En exportación existe el pago anticipado de Gate Out por agencia (M3-19, ola H).

### 3. Carta de responsabilidad

- **Estado.** `ResponsibilityLetterStatus.GetStatusAsync` devuelve `Fulfilled` o `Missing`; nunca `Pending` (`backend/src/HapagPortal.Application/Documents/Common/ResponsibilityLetterStatus.cs:14-25`).
  - Busca un `ShipmentDocument` de tipo `ResponsibilityLetter` en estado `Issued`, de la organización pagadora y vigente.
- **Cuándo es requisito.** Solo si Nexus indica que el pagador es FFWW: `ResponsibilityLetterRequired = isFreightForwarder` (`ChargeRulesService.cs:52,65`).
- **Dónde bloquea.**
  - Entra en `ShipmentChargesDto.Requirements` (`ChargeRulesService.cs:248-257`).
  - Impide agregar ítems al carro con `Cart.ResponsibilityLetterRequired` (`backend/src/HapagPortal.Application/Payments/Common/PayableItemResolver.cs:408-414`).
- **Endpoints.**
  - `GET /documents/{blNumber}`, con el campo `letter: {Required, Status, BlocksProcess}`.
  - `POST /documents/{blNumber}/responsibility-letter`.
  - `GET /documents/responsibility-letter/terms`.
- **Diferencias por país.** Ninguna. Los comentarios citan CL-IMP-11 y BO-IMP-10 (`backend/src/HapagPortal.Domain/Constants/DocumentConstants.cs:24`).
- **Requisitos de proceso en código.** La abstracción existente, `ProcessRequirements`, tiene solo dos códigos:
  - `RESPONSIBILITY_LETTER`.
  - `ADVANCE_DEMURRAGE`.
  - Sus estados son `Missing`, `Pending` y `Fulfilled` ([`ChargeRuleConstants.cs:40-54`](https://github.com/brypenalozav-stack/hapag-portal/blob/45d3bce518460de1e45dc64cb9dd93919de95da9/backend/src/HapagPortal.Domain/Constants/ChargeRuleConstants.cs#L40-L54)).

### 4. Carta de liberación (solo Bolivia)

- **Alcance.** Solo Bolivia, importación: `DocumentServiceRequests.IsBoliviaImport` (`backend/src/HapagPortal.Application/Documents/Common/ShipmentDocumentQueries.cs:118`).
- **Modelo.** Es una `ServiceRequest` con su fila `ReleaseLetterRequests`.
  - Estados del flujo: Draft, Submitted, PendingApproval, Approved, Rejected, PendingPayment, Paid, InProgress, Completed y Cancelled (`backend/src/HapagPortal.Domain/Constants/ServiceRequestConstants.cs:9-20`).
  - Al aprobar Customer Service, se emite un `ShipmentDocument` de tipo `ReleaseLetter` (`backend/src/HapagPortal.Application/Documents/ReleaseLetter/ReleaseLetterService.cs:138-221`).
- **Relación con el TATC.**
  - La carta consulta el TATC al enviarse y al aprobarse, y guarda una foto (`ReleaseLetterService.cs:84-124`).
  - Con `Documents:ReleaseLetterRequiresIssuedTatc` (`backend/src/HapagPortal.Application/Documents/Common/DocumentSettings.cs:42`), la aprobación exige el TATC emitido de cada contenedor (`ReleaseLetterService.cs:149-157`).
  - En el código, entonces, la carta depende del TATC, y no al revés.
- **Counter.** Canje, HBL recibido y desconsolidado viven en `CounterRecord` (`backend/src/HapagPortal.Domain/Entities/CounterRecord.cs`). Ese registro se imprime en la carta (`ReleaseLetterService.cs:281-291`).
- **Endpoints.**
  - `GET` y `POST /documents/{blNumber}/release-letter`.
  - `GET /documents/release-letter/requests/{id}`.

### 5. CLD y demoras anticipadas (solo Bolivia)

`NoDebtEvaluator` es lo más parecido a un agregado de "requisitos pendientes" que existe hoy.

- **Aplica** solo a Bolivia, importación ([`NoDebtEvaluator.cs:22-23`](https://github.com/brypenalozav-stack/hapag-portal/blob/45d3bce518460de1e45dc64cb9dd93919de95da9/backend/src/HapagPortal.Application/Documents/Common/NoDebtEvaluator.cs#L22-L23)).
- **Bloqueos** que devuelve (`NoDebtEvaluator.cs:25-100`; códigos en `DocumentConstants.cs:142-149`):
  - `PENDING_CHARGES`: cargos locales pendientes, incluidos Gate In, EDS y Gate Out.
  - `PENDING_DEMURRAGE`.
  - `PENDING_INVOICES`.
  - `PENDING_FREIGHT`: flete Collect sin pagar.
  - `ADVANCE_DEMURRAGE`.
- **Endpoints.**
  - `GET /documents/{blNumber}/no-debt-certificate` devuelve `NoDebtEligibilityDto(Applicable, Eligible, CanRequest, Blockers, …)` (`backend/src/HapagPortal.Application/Documents/NoDebt/NoDebtCertificateCommands.cs:47-77`).
  - `POST` emite el certificado, o falla con los bloqueos.
- **Demoras anticipadas.**
  - Estados: `NotRequired`, `NotRequested`, `Pending` y `Paid` (`ChargeRuleConstants.cs:74-79`).
  - Se calculan en `DemurrageStatusBuilder.EvaluateAdvanceAsync` (`backend/src/HapagPortal.Application/Demurrage/Common/DemurrageStatusBuilder.cs:195-251`).
  - Endpoint: `GET /demurrage/{blNumber}/status`.
- **No cubre** la carta de responsabilidad ni la carta de liberación.

### 6. TATC

- **Origen.** Es externo. El contrato dice: "La generación del TATC ya es automática fuera de Nexus … el portal consulta su estado" (`docs/integraciones/contratos/tatc.openapi.yaml:10-11`). El port lo documenta igual: "El TATC se genera fuera del portal" (`backend/src/HapagPortal.Application/Common/Interfaces/ITatcProvider.cs`).
- **Lectura.** `GET /shipments/{blNumber}/tatc` devuelve `ShipmentTatcDto`, con un estado agregado del BL y la lista `Containers` (`backend/src/HapagPortal.Application/Shipments/Tatc/ShipmentTatcQueries.cs:16-42`).
  - Cada contenedor trae número, número de TATC, estado, fecha de emisión, almacén y motivos pendientes.
  - Solo aplica a importación (`Tatc.NotApplicable`) y exige el permiso `tatc.download` (`:144-147`).
  - Si la fuente falla, la respuesta sigue siendo exitosa, con `Available=false` y `ErrorCode` (`:151-156`).
- **Estados.**
  - Por contenedor: NotIssued, PreTatc, Issued, Cancelled y Unknown.
  - Agregado del BL: NotRegistered, Issued, PartiallyIssued, PreTatc, Cancelled y NotIssued (`backend/src/HapagPortal.Domain/Shipments/TatcStatusMapper.cs`; `backend/src/HapagPortal.Domain/Constants/ShipmentInformationConstants.cs:97-137`).
- **Motivos pendientes.** `PAYMENT_PENDING`, `MHD_PENDING`, `DOCUMENT_PENDING` y `OTHER`.
  - Los entrega la fuente; el portal no los calcula.
  - El contrato marca el conjunto de estados como propuesto, por confirmar (`tatc.openapi.yaml:391`).
- **Generación.**
  - `POST /shipments/tatc-batches`: la pide el usuario, hasta 500 BL de una misma localidad (UN/LOCODE).
  - Se reenvía a `RequestGenerationAsync` y se guarda como `TatcBatch` (`ShipmentTatcQueries.cs:74-306`).
  - No hay disparador automático ni tarea programada.
- **Adaptadores.**
  - Dummy en `appsettings.json:61` y `appsettings.Staging.json:37`, con 10 BL fijos (`backend/src/HapagPortal.Infrastructure/Integrations/Tatc/DummyTatcProvider.cs:23-86`).
  - Real HTTP con caché de 30 s (`HttpTatcProvider.cs`, `CachedTatcProvider.cs`).
  - El simulador no tiene rutas de TATC (`backend/tools/HapagPortal.IntegrationSimulator/`).
- **Comprobante.** No hay PDF ni comprobante del TATC.
  - La acción "Descargar documento TATC" existe en la matriz de accesos (`backend/src/HapagPortal.Domain/Access/AccessMatrixBaseline.cs:51`), pero no tiene endpoint.
  - El DTO no trae puerto, nave/viaje, cliente, agente de aduanas, tipo ni depósito.
  - El único documento que incluye el TATC es la carta de liberación de Bolivia: sección "TATC de las unidades" (`backend/src/HapagPortal.Infrastructure/Documents/ShipmentDocumentTemplates.cs:448-457`).
- **Reglas del aviso antiguo.**
  - No hay lógica de 72 h ni de 48 h para Callao → norte de Chile.
  - El contenedor tiene `IsShipperOwned` (`backend/src/HapagPortal.Domain/Entities/BLContainer.cs:13-18`). Hoy solo lo usa el catálogo de servicios para excluir unidades (`backend/src/HapagPortal.Application/ServiceRequests/Common/ServiceCatalogEvaluator.cs:300`), no el TATC.
- **CLD en el contrato.** CT-TATC define `GET /bills-of-lading/{blNumber}/cld`, con los estados NOT_ISSUED, ISSUED y SENT (`tatc.openapi.yaml:161-190,497-530`). Ningún port lo implementa.

### 7. Datos por contenedor

| Dato | Por contenedor | Dónde |
|---|---|---|
| Número, tipo, sello, peso, estado (texto libre) y SOW | Sí | `BLContainer.cs:7-19`. El DTO del detalle trae solo 6 campos (`backend/src/HapagPortal.Application/Common/Dtos/BLContainerDto.cs:3-9`). |
| Gate In / EDS / Gate Out | **No** (por BL) | `LocalCharge.cs:7-16`. |
| Demurrage | Sí, una línea por contenedor | `backend/src/HapagPortal.Domain/Entities/DemurrageCharge.cs:7-23`. Estados de la línea: Pending, Invoiced y Paid. Estado del BL (`DemurrageStates`): InvoicedWithDebt, CalculatedUnpaid, NotCalculated y NoDemurrage (`ChargeRuleConstants.cs:57-70`, `DemurrageStatusBuilder.cs:21-39`). |
| TATC | Sí, leído en vivo | `ContainerTatcRecord` (`ITatcProvider.cs`), sin guardarse en la base. |
| Canje, HBL y desconsolidado | No (por BL) | `CounterRecord`. Solo lo ven los perfiles internos en el detalle (`GetShipmentDetailQuery.cs:152-154`). |
| Cambio de almacén | Sí, por solicitud | `backend/src/HapagPortal.Domain/Entities/WarehouseChange.cs:16`. Estados: Completed, Pending y Cancelled (`ChargeRuleConstants.cs:120-128`). No existe un estado "autorizado". |

- FIS (`IShipmentSource` / `ShipmentRecord`) no trae contenedores (`backend/src/HapagPortal.Application/Common/Interfaces/IShipmentSource.cs:28-49`).

### 8. Integraciones y fuentes de un estado "autorizado"

- Ningún contrato ni port entrega por BL un estado AUTORIZADO / NO GENERADO para recargos, canje, cambio de almacén o carta de responsabilidad, como la pantalla antigua.
- Los contratos están todos en estado PROPUESTA (`docs/integraciones/README.md:7`).
- Navesoft (CT-NAVE) se intercambia por CSV vía FTP, sin API ni port (`docs/integraciones/navesoft.md:4-25`). Ningún archivo del repositorio describe la pantalla "CONSULTA BL".
- Mercurio (almacén por contenedor, CT-MERC) y el CLD de CT-TATC están especificados, pero no tienen port.
- En la pantalla antigua, cada columna se puede asociar con lo que hay hoy. Es una inferencia a partir de los nombres:

| Columna antigua | Equivalente actual |
|---|---|
| Recargos BL | Cargos locales y reglas |
| Canje | Counter |
| Cambio almacén | Solicitudes de cambio de almacén |
| Carta de responsabilidad | Estado local del documento |
| Gate In / EDS por contenedor | Sin equivalente: hoy son cargos por BL |
| Demurrage por contenedor | Líneas de demurrage |
| Número de TATC | Lectura de CT-TATC |

### 9. Frontend

- **Consulta de BL actual.**
  - El listado de embarques tiene la caja "Buscar BL por número / Abrir BL", que lleva al detalle (`frontend/src/app/features/shipments/shipment-list/shipment-list.html:20-31`, `shipment-list.ts:174-180`).
  - `/bills-of-lading` redirige a `/shipments` (`frontend/src/app/app.routes.ts:67-68`).
- **Detalle del embarque** (`frontend/src/app/features/shipments/shipment-detail/shipment-detail.html`). Ya muestra en secciones separadas:
  - El estado general.
  - La emisión del BL (`app-shipment-issuance`).
  - El TATC por contenedor (`app-shipment-tatc`, solo importación con `tatc.download`, `shipment-detail.ts:122-125`).
  - El flete.
  - Los contenedores.
  - Los cargos locales.
  - El demurrage.
  - Los documentos, con el estado de la carta de responsabilidad, la carta de liberación y el CLD.
  - Tiene la barra "Ir a" (`app-section-nav`).
- **Tabla TATC** (`frontend/src/app/features/shipments/shipment-tatc/shipment-tatc.html:48-82`).
  - Columnas: contenedor, número de TATC, estado (badge), fecha de emisión, almacén y motivos pendientes.
  - Muestra un aviso cuando la fuente no responde (NF-11).
  - Enlaza a "Solicitar TATC" (`/tatc?bl=`).
- **Badges.** Hay mapas por dominio en `frontend/src/app/core/i18n/labels.ts`, como `TATC_STATUS_KEYS/_CLASS` (`:814-832`), `BL_ISSUANCE_STATUS_*` (`:780-802`) y `NO_DEBT_BLOCKER_KEYS` (`:652`). Se usan con `codeLabel`. `app-status-badge` cubre estados genéricos.
- **Pasos y línea de tiempo.**
  - `app-reinvoicing-steps` (`frontend/src/app/features/reinvoicing/reinvoicing-steps.ts`) dibuja `<ol class="hl-steps">` con modificadores `--current` y `--done`. Sus pasos están fijos en el componente.
  - `app-service-request-timeline` (`frontend/src/app/features/service-requests/shared/service-request-timeline.ts`).
  - No hay un componente genérico de pasos en `shared/components/`.
- **Menú.** El grupo "Embarques" (`frontend/src/app/shared/components/main-nav/main-nav.config.ts:56-67`) tiene Embarques, TATC (solo clientes), Cambio de almacén y Mercancías peligrosas.
- **Pruebas e2e del área.**
  - `frontend/e2e/funcional/ola-f.spec.ts:250-279`: emisión, TATC y solicitud masiva.
  - `ola-e.spec.ts:133-157`: bloqueos del CLD.
  - `ola-j.spec.ts:89-190`: carta de liberación.
  - `ola-c.spec.ts:116-151`: estado del demurrage.
  - `frontend/e2e/a11y/pantallas.a11y.spec.ts:191-208`: TATC.

### 10. Datos de demostración

- **HLCUVAL250100123** (Chile, importación). Es el BL más completo para una demostración.
  - Contenedores HLXU1234567 y HLXU7654321.
  - Demurrage calculado sin pagar (`CalculatedUnpaid`).
  - TATC: el primer contenedor está emitido (TATC-SAI-2026-004512); el segundo, no emitido por `PAYMENT_PENDING` (`DummyTatcProvider.cs:27-31`).
  - Semilla en `backend/src/HapagPortal.Infrastructure/Persistence/ApplicationDbContext.cs:5269-5301`.
- **Otros casos del Dummy de TATC:**
  - Pre-TATC: HLCUSAI260401020.
  - Documento pendiente: HLCUVAP260401130.
  - Bolivia con pago y MHD pendientes: HLCUARI260100045.
  - Cancelado: HLCUIQQ260200078.

## Mapa de requisitos pedidos frente a lo existente

| Requisito pedido | País | Estado calculable hoy | Fuente o endpoint | Diferencia con lo pedido |
|---|---|---|---|---|
| Flete pagado | CL / BO | PAID / PENDING | `GET /shipments/{bl}` | Solo con el permiso `PayFreight`. |
| Gate In | CL / BO | Pending / Paid / Exempt / CreditImputed (y Outcome) | `GET /charges/{bl}` | Por BL, no por contenedor. No existe un estado "autorizado". |
| Carta de responsabilidad | CL / BO | Fulfilled / Missing | `GET /documents/{bl}` | Solo es requisito para FFWW (Nexus). |
| Carta de liberación | BO | Estados de la solicitud y documento emitido | `GET /documents/{bl}/release-letter` | Hoy su aprobación puede exigir el TATC emitido: el orden es el inverso al pedido. |
| TATC automático | CL / BO | Lectura del estado y solicitud masiva manual | `GET /shipments/{bl}/tatc`, `POST /shipments/tatc-batches` | Lo genera el sistema externo. El portal no tiene un disparador por cumplimiento de requisitos. |
| Flujo del estado de liberación | — | — | — | No existe agregado ni componente de pasos compartido. El permiso `release-requirements.view` existe sin uso. |

## Contexto histórico (thoughts/)

- **Research del 2026-10-05** (`thoughts/shared/research/2026-10-05-actualizacion-requerimientos-portal-2-0.md:177-179`).
  - Registró "Generar TATC" como automático fuera de Nexus (Ignis WS) y lo asoció a M2-09.
  - Registró "Generar CLD" como manual en Bolivia y lo asoció a M6-07 y M3-16.
  - Registró que no hay API con Navesoft (CSV/FTP).
- **Plan de implementación** (`thoughts/shared/plans/2026-10-05-implementacion-fase1-funcional.md:35-77`). Registra como hechas:
  - Ola C: M4-04 (carta FFWW) y M3-16.
  - Ola E: carta de responsabilidad (M6-06) y CLD (M6-07).
  - Ola F: M2-09 (consulta de BL y TATC).
  - Ola J: M6-08 (carta de liberación).
  - M2-10 (plazos documentales) no está en ninguna ola.
- **Plan general** (`thoughts/shared/plans/2026-10-05-actualizacion-requerimientos-portal-2-0-ui-integraciones.md:182,422,450`). Deja TATC/Flagare y Navesoft solo con adaptador Dummy hasta validar los contratos.
- **Especificación v4.** CL-IMP-13 y BO-IMP-13 apuntan a "Consulta de BL y TATC… Flujo en M2-09" (`docs/requerimientos/especificacion-funcional-v4.txt:149,180`).
  - M2-09 (`:743-754`) pide reflejar el estado vigente del TATC y la generación masiva.
  - Deja la fuente y los estados del TATC por definir con los equipos técnicos de Hapag-Lloyd.

## Referencias de código

- `backend/src/HapagPortal.Application/Shipments/Detail/GetShipmentDetailQuery.cs:65-90,152-154`: flete, cargos locales y Counter en el detalle.
- `backend/src/HapagPortal.Application/ChargeRules/Common/ChargeRulesService.cs:52-65,82-278`: reglas de cargos y requisito de carta.
- `backend/src/HapagPortal.Application/Documents/Common/ResponsibilityLetterStatus.cs:14-25`: estado de la carta de responsabilidad.
- `backend/src/HapagPortal.Application/Documents/ReleaseLetter/ReleaseLetterService.cs:84-221`: carta de liberación y TATC.
- `backend/src/HapagPortal.Application/Documents/Common/NoDebtEvaluator.cs:22-100`: bloqueos del CLD en Bolivia.
- `backend/src/HapagPortal.Application/Shipments/Tatc/ShipmentTatcQueries.cs:16-365`: consulta y solicitud masiva de TATC.
- `backend/src/HapagPortal.Application/Common/Interfaces/ITatcProvider.cs`: port del TATC.
- `backend/src/HapagPortal.Infrastructure/Integrations/Tatc/`: adaptadores Dummy, Http y Cached.
- `backend/src/HapagPortal.Domain/Constants/ChargeRuleConstants.cs:4-128`: Outcome, ProcessRequirements, DemurrageStates y AdvanceDemurrage.
- `backend/src/HapagPortal.Domain/Constants/AccessConstants.cs:70`: permiso `release-requirements.view`, sin uso.
- `backend/src/HapagPortal.Domain/Entities/BLContainer.cs:7-19`: contenedor, incluido `IsShipperOwned`.
- `docs/integraciones/contratos/tatc.openapi.yaml:10-11,161-190,391-407,497-530`: TATC, CLD y estados propuestos.
- `frontend/src/app/features/shipments/shipment-tatc/shipment-tatc.html:48-82`: tabla del TATC por contenedor.
- `frontend/src/app/features/reinvoicing/reinvoicing-steps.ts`: único componente de pasos.
- `frontend/src/app/shared/components/main-nav/main-nav.config.ts:56-67`: grupo Embarques del menú.

## Preguntas abiertas

1. **Disparo del TATC.** El contrato CT-TATC dice que el TATC se genera solo, fuera del portal. ¿El "TATC automático" pedido es que el portal solicite la generación (`RequestGenerationAsync`) cuando se cumplan los requisitos, o que muestre el TATC que emite el sistema externo?
2. **Gate In y EDS por contenedor.** La pantalla antigua los muestra por contenedor; el modelo actual los guarda por BL. ¿Alguna fuente (Nexus, Navesoft o la facturación) entrega la autorización por contenedor?
3. **Orden entre carta de liberación y TATC en Bolivia.** El pedido pone la carta de liberación como requisito previo al TATC. El código permite que la aprobación de la carta exija el TATC emitido (configuración `ReleaseLetterRequiresIssuedTatc`).
4. **Carta de responsabilidad para clientes no FFWW.** Hoy es requisito solo para FFWW. El pedido la lista como requisito general en Chile.
5. **Reglas del aviso antiguo.** Las ventanas de 72 h y 48 h (Callao → Iquique, Angamos y Antofagasta) y la exclusión de SOW no están en el código ni en el contrato. `IsShipperOwned` existe en el contenedor.
6. **Comprobante del TATC.** Los campos del comprobante antiguo (puerto, nave/viaje, cliente, agente de aduanas, almacén, depósito, tipo) no están en el DTO del TATC ni en la ficha.

## Follow-up Research 2026-10-06T20:25:37-03:00

### Fuente de verdad

El documento que manda es **`Portal_2.0_Especificacion_Funcional_validada(1).docx`** (`C:\Users\klaze\Desktop\katu`). Todo lo marcado **Fase 1** es obligatorio.

- Su catálogo de servicios tiene 52 filas: CL-IMP, CL-EXP, BO-IMP y BO-EXP.
- Las 52 están también en `docs/requerimientos/especificacion-funcional-v4.txt`; en la versión de trabajo no falta ninguna.
- No estaban en esta investigación, y se agregan aquí con el texto del documento base.

Las convenciones del documento base son estas:

| Columna | Valores |
|---|---|
| Tipo | N = funcionalidad nueva con flujo de cobro; M = funcionalidad existente que requiere mejora; C = consulta, autogestión o gestión sin flujo de cobro asociado |
| Fase | 0 = en revisión; 1 = primera etapa; 2 = segunda etapa. Si una fila reúne componentes de distintas fases, se indican por separado |

### Catálogo del documento base: filas relacionadas con la consulta BL y la liberación

| Código | Servicio | Tipo | Fase | Descripción (texto del documento base) |
|---|---|---|---|---|
| CL-IMP-01 | Flete de importación | M | **1** | Pago de flete de importación disponible actualmente en el portal. |
| CL-IMP-02 | Gate In y EDS | M | **1** | Pago disponible actualmente. Requiere las reglas de exención descritas en M4-01 y M4-02. |
| CL-IMP-04 | Demurrage y calculadora | M | **1** / 0 | Gestión y pago de sobreestadía disponibles actualmente, incluyendo calculadora. Requiere las mejoras descritas en M3-02 y M3-03. Fase 1: MHD y comportamiento por estado del BL (M3-02 y M3-18). Fase 0: actualización de días libres de la calculadora (M3-03). |
| CL-IMP-05 | Apertura | M | **1** | Cargo local disponible actualmente. Su lógica se toma como referencia para los nuevos conceptos (ver M2-03). |
| CL-IMP-06 | Valorización | M | **1** | Cargo local disponible actualmente. Su lógica se toma como referencia para los nuevos conceptos (ver M2-03). |
| CL-IMP-07 | Certificado de transbordo | M | **1** | Se requiere que el sistema genere el certificado de forma automática y lo envíe a UMAR. Flujo en M6-01. |
| CL-IMP-08 | Corrección de BL o aclaración | N | 2 | Nuevo servicio que permite al cliente solicitar y pagar correcciones o aclaraciones sobre el BL. Flujo en M3-12. |
| CL-IMP-09 | Refacturación IAO y pérdida de IVA | N | 2 | Nuevo servicio de refacturación por IAO con cobro de pérdida de IVA y emisión de nueva factura. Flujo en M3-11. |
| CL-IMP-10 | Drop Off | N | 2 | Nuevo servicio de devolución de contenedores en SCL. Flujo en M3-09. |
| CL-IMP-11 | Carta de responsabilidad | C | **1** | Servicio existente para la gestión de la carta de responsabilidad. Para clientes FFWW es obligatoria (ver M4-04). Gestión y emisión del documento en M6-06. Gestión documental sin cobro asociado, conforme a M6-06. |
| CL-IMP-12 | Consulta de cambio de almacén | C | 2 | Consulta que permite al cliente revisar sus solicitudes de cambio de almacén y su estado. Historial en M3-06. |
| CL-IMP-13 | **Consulta de BL y TATC** | C | **1** | Consulta del estado del BL y del TATC asociado. Flujo en M2-09. |
| CL-IMP-14 | Consulta de emisión de BLs | C | **1** | Consulta del estado de emisión de BLs. Requiere la revisión de lógica descrita en M2-02. |
| CL-EXP-12 | Consulta de estado de BL (Primera fase) | C | **1** | Consulta que permite al cliente visualizar el estado de sus BLs, identificando los que tienen cargos pendientes de pago. Se presenta en el listado de M2-06 y M2-07. |
| CL-EXP-13 | Consulta de ODS generadas | C | **1** | Consulta de las órdenes de servicio (ODS) generadas, disponibles en el detalle del embarque descrito en M2-06. |
| BO-IMP-01 | Flete de importación | M | **1** | Pago de flete de importación disponible actualmente para la operación de Bolivia. |
| BO-IMP-03 | Gate In y EDS | M | **1** | Gestión y pago disponibles. Requiere las reglas de exención descritas en M4-01 y M4-02. |
| BO-IMP-06 | Demurrage y calculadora | M | **1** / 0 | Gestión y pago de sobreestadía, incluyendo calculadora. Requiere las mejoras descritas en M3-02 y M3-03. Fase 1: MHD y comportamiento por estado del BL (M3-02 y M3-18). Fase 0: actualización de días libres de la calculadora (M3-03). |
| BO-IMP-10 | Carta de responsabilidad | C | **1** | Gestión de la carta de responsabilidad disponible en el portal de Bolivia. Gestión y emisión del documento en M6-06. Gestión documental sin cobro asociado, conforme a M6-06. |
| BO-IMP-11 | **Liberación y desconsolidado** | C | **2** | Nuevo servicio para la gestión de liberación y desconsolidado de carga. Flujo en M6-08. Gestión y generación documental sin flujo de cobro definido en M6-08. |
| BO-IMP-13 | **Consulta de BL y TATC** | C | **1** | Consulta del estado del BL y del TATC asociado. Flujo en M2-09. |
| BO-IMP-14 | Consulta de emisión de BLs | C | **1** | Consulta del estado de emisión de BLs. Requiere la revisión de lógica descrita en M2-02. |
| BO-IMP-15 | Certificado de libre deuda | C | **1** | Nuevo servicio de emisión del certificado de libre deuda. Flujo en M6-07. |
| BO-IMP-16 | Demoras anticipadas | N | **1** | Pago de demoras anticipadas obligatorio para las cuentas sujetas a las reglas internas, como condición previa a la liberación del CLD. El monto se descuenta del MHD total. Flujo en M3-16. |
| BO-EXP-01 | Flete prepaid | M | **1** | Pago de flete prepaid disponible actualmente para la operación de Bolivia. |
| BO-EXP-02 | Administración de contenedor XOM | N | 2 | Nuevo cobro por administración de contenedor. Flujo en M3-10. Alcance de Fase 2 aún en revisión, conforme a M3-10. |
| BO-EXP-03 | Gate Out de unidades retiradas en Chile | M | **1** | Gestión y pago de Gate Out para unidades retiradas en Chile bajo operación boliviana. |
| BO-EXP-04 | Otros Cargos Locales | N | 2 | Nuevo servicio de otros cargos locales de exportación para Bolivia, no disponible actualmente. Habilitación de conceptos según M2-04, permitiendo incorporar cargos adicionales mediante configuración. |
| BO-EXP-05 | Matriz fuera de plazo | N | 2 | Nuevo cobro por presentación de matriz fuera de plazo, gestionado bajo modalidad on demand. Flujo en M3-14. |
| BO-EXP-06 | Correcciones de BL o aclaración | N | 2 | Nuevo servicio que permite al cliente solicitar y pagar correcciones o aclaraciones sobre el BL. Flujo en M3-12. |
| BO-EXP-07 | Transmisión de BL Hijo y fuera de plazo | N | 2 | Nuevo cobro asociado a la transmisión de BL hijo, incluyendo el caso de transmisión fuera de plazo. Flujo en M3-13. |
| BO-EXP-08 | **Consulta de estado de BL** | C | **1** | Consulta que permite al cliente visualizar el estado de sus BLs, identificando los que tienen cargos pendientes de pago. Se presenta en el listado de M2-06 y M2-07. |
| BO-EXP-09 | Consulta de ODS generadas | C | **1** | Consulta de las órdenes de servicio (ODS) generadas, disponibles en el detalle del embarque descrito en M2-06. |

El catálogo completo (52 filas) está en `docs/requerimientos/especificacion-funcional-v4.txt`.

### Fichas del documento base que gobiernan la consulta BL y la liberación

**M2-09 Consulta de BL y TATC – FASE 1** (CL-IMP-13 y BO-IMP-13)

- **Situación actual.** El estado del TATC se consulta hoy por correo a los equipos internos. El cliente no puede anticipar si su documentación permite el retiro de la carga.
- **Requerimiento.**
  - Habilitar la consulta del estado del BL y del TATC asociado, para que el cliente conozca la situación documental de su operación desde el portal.
  - La fuente de información y los estados posibles del TATC deben quedar definidos con los equipos técnicos de Hapag-Lloyd.
  - El portal debe reflejar el estado vigente, no información de una carga anterior.
  - Debe considerarse la generación masiva de TATC para clientes con alto volumen por una misma localidad (ejemplo: Delfin, por Iquique).
- **Criterios de aceptación.**
  - El cliente consulta el estado del BL y del TATC asociado.
  - Los estados posibles del TATC quedan definidos y documentados.
  - El estado presentado corresponde al del sistema de origen.
  - La solución contempla la generación masiva de TATC por localidad.
- **Dependencias.** Se apoya en el criterio de publicación de embarques no publicados (sin ficha vigente; pendiente de validación).

**M4-04 Carta de responsabilidad obligatoria para clientes FFWW – FASE 1**

- Se consulta en Nexus la condición de FFWW autorizado y la obligatoriedad de la carta.
- Para esos clientes, la carta es requisito obligatorio y bloquea el avance del proceso si no está presente.
- Requiere M8-03. Afecta a CL-IMP-11 y BO-IMP-10.

**M6-06 Gestión y emisión de la carta de responsabilidad – FASE 1**

- Gestión documental sin cobro asociado (CL-IMP-11, BO-IMP-10).

**M6-07 Certificado de libre deuda – FASE 1** (BO-IMP-15)

- Aplica a la importación de Bolivia.
- Antes de emitir el CLD, el portal verifica que no haya deuda pendiente y que esté confirmado el pago de demoras anticipadas cuando las reglas internas lo exijan (M3-16).
- El PDF se emite con firma electrónica.
- Se apoya en M7-03 para verificar la deuda.

**M3-16 Demoras anticipadas, Bolivia – FASE 1** (BO-IMP-16)

- Para las cuentas sujetas a la regla, el CLD permanece bloqueado hasta confirmar el pago.
- El monto pagado se descuenta del MHD total.

**M6-08 Carta de liberación y desconsolidado, Bolivia – FASE 2** (BO-IMP-11)

- El cliente elige el BL y las unidades, e ingresa los datos del consignatario y del transportista. El portal genera el PDF.
- Quedan por definir, con el área legal y antes de la construcción:
  - El tratamiento según el tipo de sociedad.
  - Los campos exigidos.
  - El circuito de aprobación.
  - **El vínculo con el TATC.**
- Se relaciona con M2-09.

**M2-06 Listado y detalle de embarques – FASE 1** (y CL-EXP-13, BO-EXP-09)

- Listado único de BL y bookings, con detalle por embarque.
- En exportación, el detalle muestra las ODS generadas.
- Que un BL no aparezca no significa que no tenga deuda.

**M2-07 Separación de importación y exportación – FASE 1**

- Filtro o vistas separadas, que se mantienen durante la navegación.

### Lo que cambia respecto de la investigación inicial

1. **La consulta de BL y TATC es Fase 1 y obligatoria** en Chile y en Bolivia (CL-IMP-13, BO-IMP-13; ficha M2-09). Lo mismo vale para la consulta del estado del BL con cargos pendientes en exportación (CL-EXP-12, BO-EXP-08).
   - Lo que existe hoy (secciones 6 y 9) cubre la lectura del estado del BL y del TATC en el detalle del embarque, y la generación masiva (ola F).
   - Lo que el documento base deja abierto: la fuente y los estados del TATC "deben quedar definidos con los equipos técnicos". El contrato CT-TATC sigue en PROPUESTA (`docs/integraciones/contratos/tatc.openapi.yaml:391`).
2. **La carta de liberación es Fase 2**, no Fase 1 (BO-IMP-11, M6-08).
   - El vínculo entre la carta y el TATC está **por definir con el área legal**.
   - El código actual ya tiene la carta (ola J), con la opción `Documents:ReleaseLetterRequiresIssuedTatc`. En ella, la carta depende del TATC emitido.
   - Por lo tanto, en Fase 1 los requisitos de Bolivia que el documento base marca como obligatorios son: flete (BO-IMP-01), Gate In y EDS (BO-IMP-03), carta de responsabilidad (BO-IMP-10), CLD (BO-IMP-15) y demoras anticipadas (BO-IMP-16).
3. **La carta de responsabilidad es Fase 1** en ambos países (CL-IMP-11, BO-IMP-10). Como **requisito que bloquea**, el documento base la exige solo para FFWW (M4-04). Para los demás clientes es "gestión documental sin cobro asociado" (M6-06).
4. **Gate In y EDS son Fase 1** en ambos países (CL-IMP-02, BO-IMP-03), con las reglas de exención M4-01 y M4-02. El documento base no menciona Gate In ni EDS **por contenedor**.
5. **El documento base no pide un TATC "automático" que el portal genere al cumplirse los requisitos.**
   - M2-09 pide consultar el estado vigente y contemplar la generación masiva.
   - El contrato CT-TATC indica que la generación ya es automática fuera del portal (`tatc.openapi.yaml:10-11`).
   - El documento base tampoco menciona el comprobante del TATC, las ventanas de 72 h y 48 h ni la exclusión de SOW. Esos elementos vienen de la pantalla antigua de Navesoft.

### Módulos mínimos del POC frente al portal actual

El usuario indicó que el POC (`https://portal-autohpl.netlify.app`, referencia del comparativo `Comparacion_POC_Requerimientos_Por_Fase.xlsx`) debería tener **al menos** estos módulos. La captura muestra el menú del POC y la pantalla "Carta de Liberación / Desconsolidado". La correspondencia con lo que existe hoy en el portal es la siguiente (menú en `frontend/src/app/shared/components/main-nav/main-nav.config.ts`; rutas en `frontend/src/app/app.routes.ts`):

| Grupo en el POC | Módulo del POC | En el portal actual | Ruta o ubicación |
|---|---|---|---|
| — | Inicio | Sí | `/dashboard`, grupo Inicio |
| — | Mis Embarques | Sí | `/shipments`, grupo Embarques |
| — | Agente IA (B) / Agente IA (A) | Sí, como asistente flotante (M10-01), no como entrada del menú | `app-assistant` en `frontend/src/app/app.html` |
| — | Notificaciones | Sí, como campana de la barra superior, no como entrada del menú | `/notifications` (`app.routes.ts:246`) |
| Comercio exterior | Importación / Exportación | Como filtro Importación/Exportación del listado (M2-07), no como menú separado | `/shipments?operation=` |
| Finanzas | Estado de Cuenta | Sí (solo clientes) | `/account-statement` |
| Finanzas | Mis Facturas | Sí | `/invoices` |
| Finanzas | Historial de Pagos | Sí | `/payment-history` |
| Finanzas | Disputas | Sí, enlace externo a Dispute (M2-05) | `app-dispute-link`, grupo Servicios |
| Finanzas | Devoluciones | **No hay módulo**. En el documento base, "devolución" aparece solo para unidades: Drop Off (M3-09) y Gate In por devolución (M3-15), ambos Fase 2 | — |
| Información | Dangerous Goods | Sí | `/dangerous-goods` |
| Información | FAQ | Sí | `/faq` |
| Información | Tarifas Locales | **No hay módulo**. En el POC es una página de enlaces a los tarifarios oficiales de Hapag-Lloyd (evidencia EV-27 del comparativo) | — |
| Configuración | Mi Organización | Sí | `/organization` |
| Configuración | Mi Cuenta | Sí, en el menú del usuario | `/profile` |
| Configuración | Accesos y Permisos | Sí, dentro de Mi organización (accesos a terceros) | `frontend/src/app/features/access/access-management/` |
| — | Carta de Liberación / Desconsolidado (pantalla con selector de BL, datos del BL, consignatario final y transportista) | Sí, por BL desde el detalle del embarque (ola J) | `/shipments/:blNumber/release-letter` (`app.routes.ts:56`) |
| — | Generar datos de prueba | No (es propio del POC) | — |

Diferencias de presentación respecto del POC:

- El portal no tiene una pantalla de carta de liberación con **selector de BL** propio. Hoy se entra desde el detalle de cada BL.
- Agente IA, Notificaciones, Mi Cuenta y Accesos y Permisos existen, pero no como entradas del menú principal.
- No hay grupos de menú "Comercio exterior", "Finanzas", "Información" y "Configuración". El menú actual se organiza en Inicio, Embarques, Pagos y facturación, Servicios y Mi organización.

### Preguntas abiertas (actualizadas)

1. **TATC "automático".** No está en el documento base. ¿Se agrega como mejora sobre M2-09, con el portal pidiendo la generación al cumplirse los requisitos, o basta con reflejar el estado del sistema de origen, como pide M2-09?
2. **Estados y fuente del TATC.** M2-09 exige que queden "definidos y documentados" con los equipos técnicos de Hapag-Lloyd. El contrato sigue en PROPUESTA.
3. **Carta de liberación (Bolivia).** Es Fase 2 y su vínculo con el TATC está por definir con el área legal (M6-08). ¿Se incluye como requisito en el flujo desde ya, o se agrega cuando se cierre esa definición?
4. **Gate In y EDS por contenedor.** Ni el documento base ni el modelo lo contemplan; solo aparece en la pantalla antigua.
5. **Carta de responsabilidad.** Según el documento base, bloquea solo a los FFWW. ¿El flujo la muestra como requisito para todos o solo cuando Nexus la exige?
6. **Comprobante del TATC, ventanas de 72 h y 48 h, y SOW.** Vienen de la pantalla antigua y no están en el documento base. Si se quieren, hay que definirlos como alcance adicional.
7. **"Devoluciones" y "Tarifas Locales" del POC.** No tienen ficha en el documento base. ¿Qué contenido se espera en "Devoluciones" (devolución de dinero, de garantías o de contenedores)? ¿"Tarifas Locales" basta como enlaces a los tarifarios oficiales, como en el POC?
8. **Organización del menú.** ¿Se adoptan los grupos del POC (Comercio exterior, Finanzas, Información, Configuración) con Agente IA, Notificaciones y Accesos como entradas propias?

## Follow-up Research 2026-10-06 (decisiones del usuario)

Respuestas del usuario a las preguntas abiertas, con las capturas del POC ("Devoluciones") y de la consulta antigua de Navesoft:

| # | Pregunta | Decisión |
|---|---|---|
| 1 | Devoluciones | Es solo un enlace a la página de Hapag-Lloyd donde el cliente solicita la **devolución de dinero**. Se muestra **dentro de un iframe**, para que el cliente no salga del sitio. El POC lo hace así: página "Devoluciones" con la etiqueta "Externo", el aviso "Este módulo es provisto por un sistema externo… La disponibilidad depende de los horarios de operación del proveedor", el contenido incrustado y el botón "Abrir en ventana nueva". La URL se busca en internet. |
| 2 | Tarifas Locales | Bastan los enlaces a los tarifarios oficiales de Hapag-Lloyd. |
| 3 | Reorganizar el menú con los grupos del POC | **No.** Se mantiene el menú actual. |
| 4 | Consulta BL y TATC | Mostrar el **paso a paso de los requisitos de liberación**, como la consulta antigua de Navesoft pero muy mejorado. Cuando estén cumplidos, mostrar el **TATC**. |
| 5 | Carta de liberación en Bolivia | **Entra** como requisito del flujo, aunque en el documento base es Fase 2 (BO-IMP-11, M6-08). |

### Lo que existe hoy para estas decisiones

- **Enlaces externos por país.** Disputas (M2-05) usa la sección `PortalLinks` de la configuración (`backend/src/HapagPortal.WebApi/appsettings.json:75-80`) y un parámetro global por país, `portal.dispute-url.{país}`, editable sin desplegar (`backend/src/HapagPortal.Application/PortalLinks/GetDisputeLinkQuery.cs:14-19,37`).
  - El DTO informa `Source` (Setting, AppSettings o None) y `OpensInNewTab`.
  - El frontend lo lee una vez por sesión y país (`frontend/src/app/core/services/portal-link.service.ts`).
  - Hoy el enlace se abre en una pestaña nueva, sin iframe.
- **Iframe.** No hay ningún iframe en el frontend. Incrustar un sitio de terceros depende de que ese sitio lo permita (`X-Frame-Options` o `Content-Security-Policy: frame-ancestors`). Si no lo permite, el navegador muestra el marco vacío; por eso el POC ofrece "Abrir en ventana nueva".
- **Requisitos del flujo de liberación** (secciones 1 a 6 de esta investigación):

| País | Requisito | Estado disponible hoy |
|---|---|---|
| Chile y Bolivia | Flete | PAID / PENDING |
| Chile y Bolivia | Gate In y EDS | Estado del cargo local, por BL |
| Chile y Bolivia | Carta de responsabilidad | Fulfilled / Missing; solo bloquea a FFWW |
| Chile y Bolivia | TATC | Por contenedor |
| Bolivia | CLD y demoras anticipadas | Bloqueos del `NoDebtEvaluator` |
| Bolivia | Carta de liberación | Estado de la solicitud y documento emitido |

  - No hay un agregado que reúna estos requisitos ni un componente de pasos compartido.
  - El permiso `release-requirements.view` existe sin uso.

### Enlaces externos: Devoluciones y Tarifas Locales (búsqueda web, 2026-10-06)

**Devoluciones (devolución de dinero)**

- No se encontró un formulario público de solicitud de devolución de dinero de Hapag-Lloyd Chile ni Bolivia en hapag-lloyd.com.
- La página de orientación a clientes de Chile (`https://www.hapag-lloyd.com/en/services-information/offices-localinfo/latin-america/chile/local-info/local-faq-chile.html`) describe la devolución de la **garantía de contenedor / TATC**:
  - Se solicita por correo a `Tatc_HapagLloyd@umar.cl` una vez procesado el pago.
  - El reembolso tarda como máximo 8 días hábiles desde la devolución del vacío o la facturación.
  - Esto se conoce solo por el extracto del buscador; la página responde 403 a consultas automáticas.
- El portal antiguo (Navesoft) tiene un módulo **DEVOLUCIONES** con login propio: `https://piascl.navesoft.com/pls/csavchile/login_devoluciones`.
  - Responde HTTP 200.
  - No envía `X-Frame-Options` ni `Content-Security-Policy: frame-ancestors`, así que **se puede incrustar en un iframe**.
  - No está confirmado si es la devolución de dinero o de garantías. Es el candidato más cercano al módulo "Devoluciones" del POC, cuyo aviso dice que la disponibilidad "depende de los horarios de operación del proveedor".

**Tarifas Locales**

La página del POC (evidencia EV-27, `https://portal-autohpl.netlify.app/tarifas-locales`) tenía tres tarjetas con enlace al sitio oficial:

| Tarjeta del POC | URL oficial candidata (búsqueda web) |
|---|---|
| Tarifario Inland (Chile) | URL exacta no encontrada. Guía: `https://www.hapag-lloyd.com/en/online-business/olb-user-guide/tariffs/inland-rates.html` |
| Tarifario Demurrage / Detention (LatAm) | `https://www.hapag-lloyd.com/en/online-business/quotation/detention-demurrage/latin-america.html` |
| Recargos Locales / Service Fees | `https://www.hapag-lloyd.com/en/online-business/quotation/tariffs/local-charges-service-fees.html` |

Su texto es: "Este portal no aloja los datos de tarifas directamente. Al seleccionar un tarifario serás redirigido al sitio oficial de Hapag-Lloyd, donde se publica la información vigente."

Los PDF por país en `/content/dam/website/downloads/detention_demurrage/` llevan la fecha en el nombre, cambian con cada actualización y por eso no sirven como enlace estable. Ejemplos: `BOLIVIA_LOCAL_CHARGES_08262025.pdf`, `LBO_LOCAL_CHARGES_01302026.pdf`, `11242025_Dem_Det_CHILE_Import.pdf`.

**Iframe en hapag-lloyd.com.** Todas las páginas responden 403 a consultas automáticas, también a `curl` con User-Agent de navegador. Por eso no se pudieron leer `X-Frame-Options` ni `frame-ancestors`, y queda por verificar en un navegador real.

**Patrón existente.** Los enlaces externos por país se configuran como el de Dispute: `PortalLinks` en `appsettings` más el parámetro global `portal.dispute-url.{país}` (`backend/src/HapagPortal.Application/PortalLinks/GetDisputeLinkQuery.cs:14-37`).

### Decisiones finales sobre enlaces externos (usuario, 2026-10-06)

**Devoluciones.**

- **No** se usa el módulo de Navesoft (`login_devoluciones`).
- La URL definitiva será otra, aún no disponible. Mientras no esté configurada, la pantalla muestra un estado claro para el usuario ("aún no disponible" o similar), sin un iframe vacío y sin enlaces que confundan.
- Cuando se configure, se muestra dentro de un iframe con "Abrir en ventana nueva", como en el POC.

**Tarifas Locales.** Las URLs oficiales las dio el usuario. En la tabla aparecen en el orden de las tarjetas del POC:

| Tarjeta (POC) | URL |
|---|---|
| Tarifario Inland (Chile) | `https://www.hapag-lloyd.com/en/services-information/offices-localinfo/latin-america/chile.html` |
| Tarifario Demurrage / Detention (LatAm) | `https://www.hapag-lloyd.com/es/online-business/quotation/detention-demurrage/latin-america.html` |
| Recargos Locales / Service Fees | `https://www.hapag-lloyd.com/es/online-business/quotation/tariffs/local-charges-service-fees.html` |

La captura del POC muestra estos elementos:

- El título "Tarifas Locales" y el subtítulo "Accede a los tarifarios oficiales de Hapag-Lloyd para Chile y Latinoamérica".
- El aviso "Importante: Este portal no aloja los datos de tarifas directamente…".
- Tres tarjetas con descripción y el botón "Abrir tarifario".
- Una columna lateral de accesos rápidos con la nota "Se abrirá en una nueva pestaña".
- Dos secciones plegables: "¿Cómo elegir el tarifario correcto?" (Inland, Demurrage/Detention y Recargos/Fees) y "Consideraciones importantes" (la vigencia la rige el sitio oficial; los enlaces abren en una pestaña nueva).

**Criterio de diseño (usuario).** Todo lo visto en las capturas del POC y de la consulta antigua de Navesoft se toma como **referencia funcional, potenciada a nivel UX/UI**: no se copia tal cual, se mejora. Eso aplica a la Consulta BL con el paso a paso de requisitos y el TATC, a Devoluciones, a Tarifas Locales y a la Carta de Liberación / Desconsolidado.
