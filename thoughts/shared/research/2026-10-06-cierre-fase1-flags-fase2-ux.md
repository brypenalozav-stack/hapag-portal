---
title: 2026-10-06-cierre-fase1-flags-fase2-ux
type: research
date: 2026-10-06
status: active
project: hapag-portal
scope: shared
author: brypenalozav-stack
ticket: null
tags: [research, fase-1, feature-flags, fase-2, ux, menu, dashboard, detalle-bl, pdf, carro]
related: [thoughts/shared/research/2026-10-05-actualizacion-requerimientos-portal-2-0.md, thoughts/shared/research/2026-10-06-consulta-bl-requisitos-liberacion-tatc.md, thoughts/shared/reports/UX menú detalle BL y dashboard.md, thoughts/shared/plans/2026-10-06-cierre-fase1-y-ux.md]
researcher: brypenalozav-stack
git_commit: 15846edb08497a14d76674bfa35ca20a9aed3d15
branch: develop
repository: hapag-portal
topic: "Re-análisis del documento base: brechas de Fase 1, inventario de Fase 2 para deshabilitarla, investigación UX del menú, detalle del BL y dashboard, boleta PDF y selector de medios de pago"
last_updated: 2026-10-06
last_updated_by: brypenalozav-stack
---

# Investigación: cierre de Fase 1, Fase 2 deshabilitada y UX

**Fecha**: 2026-10-06T21:55:19-03:00
**Commit**: `15846ed` (develop, con los PR #41 y #42 mergeados)
**Fuente de verdad**: `C:\Users\klaze\Desktop\katu\Portal_2.0_Especificacion_Funcional_validada(1).docx`. Todo lo marcado Fase 1 es obligatorio.

## Pregunta

1. Volver a analizar el documento base y encontrar lo que falta de Fase 1.
2. Deshabilitar todo lo que no es Fase 1, de modo que quede activa solo la Fase 1.
3. Hacer una investigación profunda de UX/UI. Lo más urgente es el menú ("son demasiadas cosas"), el detalle del BL y el dashboard.
4. Pedidos agregados durante el trabajo:
   - Mejorar el PDF de las boletas para que se vea lo mejor posible y todos los datos se lean con claridad.
   - Mejorar la distribución del selector de medio de pago en el carro.

## Resumen

- **Fichas del documento base.** Tiene 124 fichas:

| Fase | Cantidad |
|---|---|
| Fase 1 | 92 (incluye "Fase 1 / Revisión" y "Fase 1 conexión NEXUS") |
| Fase 2 | 25 |
| Fase 2 / En revisión | 4 |
| Fase 0 (en revisión) | 3 |
| Fase 0 / En revisión | 1 |

  - En el documento base son **Fase 2** varias funciones que ya construimos:
    - Estado de cuenta (M7-03).
    - Área de administración (M8-05).
    - Bandeja de notificaciones (M1-25).
    - Comunicados (M1-26).
    - Modo guía (M1-27).
    - Empresa matriz (M1-21).
    - Reportería de transacciones (M9-01).
    - Vista como cliente (M8-08).
    - Canal Web Service (M3-17).
    - Certificado de flete (M6-02).
    - Servicios on demand (M2-03, M2-04, M3-06 a M3-15).
    - Boleta de depósito adjunta (M5-06).
    - Forma de pago por ítem con crédito (M5-10).
    - Entrega de documentos por el asistente (M10-04).
    - Listas de distribución (M1-06).
    - Transportistas pre-creados (M1-09).
    - Pago anticipado de Gate Out (M3-19).
- **No hay mecanismo de feature flags.**
  - La visibilidad se controla solo con rol y permisos: `MenuLink.visible`, guards de ruta y `[HasPermission]`.
  - Todos los controladores están siempre activos.
  - Existe una superficie autenticada de configuración para el frontend (`ConfigController`: `config/*`) y settings en base de datos (`ConfigurationSetting`). Sobre ellas se puede montar el mecanismo.
- **Fase 1 está cubierta casi entera.** Las brechas son puntuales y se listan abajo. Ningún criterio de M1 o M2 quedó como "Falta"; las faltas reales están en documentación de NF y en dos evaluaciones (M3-05 y M5-03).
- **UX.** El problema principal es la **duplicación**, no tanto el volumen:
  - 11 accesos rápidos del dashboard repiten el menú.
  - El demurrage aparece 3 veces en el dashboard.
  - El BL tiene 4 rutas paralelas: detalle, `/charges`, `/demurrage`, `/documents` y `/bl-status`.
  - Los perfiles internos ven hasta 46 enlaces en el mismo mega menú del cliente.

## 1. Brechas de Fase 1 (verificadas contra el código)

Las verificaron tres agentes, criterio por criterio, contra el texto completo del documento base.

### M1 y M2: sin "Falta", con parciales

| Ficha | Brecha |
|---|---|
| M1-10 | El correo de recuperación envía un **token en texto** y no un enlace. Además, al cerrar sesión el JWT de acceso sigue válido hasta vencer (60 min): solo se revoca el refresh token. |
| M1-04 | El país elegido no condiciona los conceptos, las monedas ni las reglas del carro y los cargos; esas decisiones siguen al país de cada BL. El selector es global, no está "al ingresar al módulo de pagos". |
| M2-07 | El filtro de importación/exportación solo existe en el listado y el dashboard. Las vistas de pago (cargos, demurrage, carro, historial, facturas) no lo tienen. |
| M2-05 | La URL de Dispute es la portada de Hapag-Lloyd. La definitiva está pendiente con Customer Service. |
| M2-06 / M1-20 | Un booking sin BL no aparece en el listado. |
| M2-09, M2-01, M2-02 | Implementadas. Solo se pueden comprobar con las integraciones en modo Real (estados del TATC y origen de la condición DIFU sin confirmar). |
| M1-13 | Los accesos por defecto se aplican solo en la importación de BL, que hoy es el único punto donde se crean BL. |

### M3, M4 y M5

| Ficha | Estado | Brecha |
|---|---|---|
| M3-05 | **Falta** | No hay documento de evaluación (alternativa y factibilidad, casos Delfin, Falabella y Bagno). La función masiva sí existe. |
| M5-03 | **Falta** | No hay constancia escrita de la futura incorporación de dólares digitales. |
| M3-01 | Parcial | Gate Out se lee de la base local, no de una API de cargos del origen. |
| M4-04 | Parcial | La carta de responsabilidad bloquea el carro, pero no `apply-rules` (el flujo de exentos). |
| M3-18 | Parcial | El estado de cuenta calcula el demurrage con consultas propias, no con `DemurrageStateEvaluator`. |
| M3-16 | Parcial | El descuento de las demoras anticipadas no se guarda como imputación consultable. |
| M5-05 | Parcial | La interfaz no muestra la vigencia del tipo de cambio ni usa `exchange-rates/transactions`. |

### M6, M7, M8, M10 y NF

- **Firma electrónica real (M6-01, M6-07).** No existe: siempre se usa `DummyDocumentSigner`.
- **Envío a UMAR (M6-01).** `UmarEmail` está vacío.
- **SMTP (afecta M6-01, M6-05 y M10-05).** Los fallos se silencian: el documento se marca entregado aunque el correo falle.
- **M6-09.** Las descargas de recibos y facturas no se registran.
- **M6-03.** El cupón asocia todas las unidades del BL, no solo las pagadas.
- **M8-03.** La lectura de FFWW no entrega razón social ni listado.
- **M8-02.** Siguen expuestos los endpoints `credit-clients` obsoletos.
- **M10-06.** La base de mercancías peligrosas es una muestra (unas 27 entradas).
- **NF en Falta:**
  - NF-10 (ventana de servicio y SLA).
  - NF-13 (respaldo, RPO y RTO).
  - NF-17 (volumetría).
  - NF-18 (tiempos de respuesta comprometidos y su medición).
  - NF-20 (aviso de navegador no soportado).
- **NF en Parcial:**
  - NF-07 (cifrado en reposo).
  - NF-09 (`Password=postgres` en `appsettings.json`).
  - NF-16 (plazo de retención por validar).
  - NF-21 (e2e solo en Chromium, sin 360 px).
  - NF-24 y NF-25 (ambiente de pruebas y procedimiento de retorno).
  - NF-26 (`/health` sin chequeos, sin alertas).

## 2. Inventario de lo que no es Fase 1 (para deshabilitarlo)

| Ficha | Puntos de entrada en el código |
|---|---|
| M1-06 Listas de distribución | Sección `<app-contact-lists>` en `/organization`; `ContactListsController` |
| M1-09 Transportistas pre-creados | `<app-carriers>` en `/organization`; `CarriersController` (lo usa la carta de liberación para elegir transportista) |
| M1-21 Empresa matriz | `<app-parent-company>`; aviso y filtro de filial en embarques; `/admin/organization-links`; `ParentCompanyController`, `AdminOrganizationLinksController` |
| M1-25 Bandeja de notificaciones | Campana de la barra superior; `/notifications`, `/notifications/preferences`. **El publicador lo usan 26 flujos** (por ejemplo, M1-08 notifica al solicitante): se oculta solo la interfaz. |
| M1-26 Comunicados | `/announcements`; `/admin/announcements`; aviso en el dashboard |
| M1-27 Modo guía | `<app-guide-host>` en el shell; `/admin/guides` |
| M2-03 / M2-04 y M3-07 a M3-15 Servicios on demand | `/service-requests/*`; `/admin/service-requests`; `/admin/service-definitions`; tarjeta "Solicitar servicio" y tabla "Mis solicitudes" del dashboard; sección "Servicios disponibles" del detalle; definiciones sembradas (SEAL_MANAGEMENT, LATE_ARRIVAL, EARLY_ARRIVAL, DROP_OFF_SCL, XOM, BL_CORRECTION, BL_HOUSE_TRANSMISSION, MATRIX_LATE, GATE_IN_RETURN) |
| ODS (CL-EXP-13, BO-EXP-09, **Fase 1**) | Se mantienen en el detalle del embarque. La página `/service-orders` está en "Servicios". |
| M3-06 Historial de cambio de almacén | `/warehouse/history`; enlace en `/warehouse` |
| M3-11 Refacturación IAO | `/reinvoicing/*` (incluye una ruta pública de aceptación); botón en `/invoices` |
| M3-17 Canal Web Service | `/admin/api-clients`; `AdminApiClientsController` y canal `/api/ws/v1` |
| M3-19 Pago anticipado de Gate Out | Bloque `gateOutAdvance` en el panel de cargos; `/admin/payments/settlements` |
| M5-06 Boleta de depósito adjunta | Sección en el detalle del historial y en el resultado del pago; `/admin/payments/deposit-proofs` |
| M5-10 Forma de pago por ítem con crédito | `/admin/credit-imputation-rules`; checkout del estado de cuenta |
| M6-02 Certificado de flete | Panel en documentos; `DocumentsController` (freight-certificate) |
| M7-03 Estado de cuenta | `/account-statement`; menú; tarjeta del dashboard |
| M8-05 Área de administración | `/admin` (inicio de administración); `AdminOverviewController`. El resto de `/admin/*` es Fase 1. |
| M8-08 Vista como cliente | Aviso en el shell; `/admin/impersonation` |
| M9-01 Reportería de transacciones | `/admin/reports/transactions` y `/admin/reports/exceptions` |
| M10-04 Documentos por el asistente | Bloque de entrega en el asistente; descarga de entregas |

**Excepciones por decisión del usuario o por dependencia:**

- **M6-08 Carta de liberación (Fase 2).** Queda activa por decisión del usuario del 2026-10-06 ("sí, entra"). Arrastra a M8-09 Counter (canje, HBL y desconsolidado), que es la fuente de la carta. M8-09 no existe en el documento base: se agregó por la decisión Q7.
- **M2-10 Plazos documentales (`/admin/deadlines`).** No existe en el documento base; también se agregó por Q7. Queda como candidato a deshabilitar.
- **Fase 0 (M1-19, M2-08, M3-03, M7-04).** No están construidas.
- **Riesgos al deshabilitar:**
  - M5-07 (Fase 1) habla de pagar "desde el estado de cuenta". El pago desde la cuenta tiene su propia pantalla (`/account-payments`), que se mantiene.
  - M5-03 (Fase 1) mantiene el depósito. Sin M5-06, el cliente no adjunta comprobante y Finanzas confirma el abono.

## 3. UX: estado actual (auditoría del código)

- **Menú del cliente.** 6 grupos y unos 20 a 21 enlaces.
- **Menú interno.** Hasta 46 enlaces en 7 grupos (Operación 12, Administración 19), en el mismo mega menú del cliente.
- **Barra superior.** 9 elementos: tema, idioma, país, carro, campana, usuario y otros.
- **Duplicaciones:**
  - El carro aparece en 5 lugares.
  - "Mi organización" está en el menú y en el menú de usuario, y el grupo y el enlace se llaman igual.
  - "Embarques" se repite como grupo y como enlace.
  - Demurrage aparece en 6 lugares.
  - TATC aparece en 4 lugares.
- **Nombres inconsistentes:**
  - Se mezclan verbos y sustantivos.
  - Términos en inglés: Dashboard, Demurrage, Counter, FAQ.
  - Mayúsculas dispares.
  - "Inicio" se llama "Dashboard".
  - "Tarifas", "Tarifas locales" y "Cargos locales" son nombres parecidos para cosas distintas.
- **Detalle del BL.** Hasta 16 secciones, con la barra "Ir a". El panel de cargos es el bloque más denso. Se solapa con `/charges/:bl`, `/demurrage/:bl`, `/shipments/:bl/documents` y `/bl-status/:bl`.
- **Dashboard:**
  - 11 accesos rápidos, todos repetidos en el menú.
  - 4 KPI.
  - La tabla de pendientes.
  - 2 tarjetas.
  - 4 tablas de indicadores.
  - Demurrage aparece 3 veces y "pagos pendientes" 2 veces.
- **Piezas disponibles:**
  - `section-nav`, `paginator`, `table-filter`, `sort-header`, `table-skeleton`, `state-message`.
  - Badges.
  - Modal y toast.
  - `.hl-card` y `.hl-stat-card`.
  - No hay un componente compartido de pestañas ni de acordeón.

## 4. UX: investigación profunda

El informe completo, con fuentes, está en `thoughts/shared/reports/UX menú detalle BL y dashboard.md`. Las notas están en `thoughts/shared/research_notes/UX menú detalle BL y dashboard/`.

- **Navieras:**
  - **CMA CGM.** Agrupa por etapa en 3 grupos: Prepare / Monitor / Manage Finance.
  - **Hapag-Lloyd Navigator 2.0:**
    - Búsqueda única: BL, booking, contenedor o factura.
    - Encabezado fijo con la ruta.
    - 4 pestañas en el detalle: Overview, Containers and Cargo, Documents, Additional Services.
    - Pendientes con prioridad: Overdue, High, Medium, Low.
  - **Maersk.** Muestra 4 requisitos de liberación en verde junto al botón "Request Delivery Order", más días libres y costo estimado de D&D.
  - **Flexport.** Usa encabezado con estado y fecha estimada, una línea de tiempo y una bandeja de tareas y excepciones. Reportó entre 30 y 40 % menos de tiempo buscando información.
- **Navegación:**
  - El menú debe ser visible y con un solo nivel de anidación.
  - Lo interno va en un espacio aparte, con un selector de aplicación (Carbon, Atlassian).
  - Las funciones que el rol nunca usa se ocultan, no se deshabilitan.
  - No se reordena por frecuencia (WCAG 3.2.3).
  - La búsqueda debe ser visible; Ctrl+K es opcional.
  - Patrón disclosure (`aria-expanded`, `aria-current`), no `role="menu"`.
- **Detalle de registro (NN/g, GOV.UK, Carbon, SAP Fiori):**
  - No usar pestañas cuando el usuario cruza información entre secciones.
  - Una sola página con encabezado fijo y anclas agrupadas en 4 a 6 grupos. Si se usan pestañas, como máximo 8.
  - Divulgación progresiva de lo poco usado.
  - Secciones por rol, armadas en el servidor.
  - `@defer on viewport` con `prefetch on idle`, sin diferir lo que está sobre el pliegue.
  - WCAG 2.4.11 con `scroll-padding`.
- **Dashboard (NN/g, GOV.UK, Flexport, Stripe):**
  - Operativo y de una sola pantalla.
  - Arriba, una bandeja "requiere acción" ordenada por urgencia.
  - Listas con los primeros N y un "ver todo".
  - Lista de tareas con verbo y estado en texto.
  - Estado vacío positivo ("Todo al día").
  - Personalización mínima: buenos valores por defecto según el rol.
- **Recomendaciones priorizadas del informe:**
  - P0: Backoffice separado; un flag por ficha que oculte todos sus puntos de entrada; menú de cliente con unas 5 entradas; dashboard con "Requiere su acción" agrupado por BL, con demurrage una sola vez y 2 a 4 accesos rápidos.
  - P1: búsqueda universal visible; detalle del BL canónico, con encabezado, próxima acción, la lista de requisitos de liberación y anclas agrupadas; `@defer` por sección.
  - P2: Ctrl+K, favoritos y recientes.

## 5. Boleta PDF y selector de medios de pago (pedidos del usuario)

- **Boleta de depósito.** Plantilla `PaymentReceipt`, en `backend/src/HapagPortal.Application/Documents/Common/ShipmentDocumentTemplates.cs:498+` y `PortalPdfs.cs`. Problemas de la captura del usuario:
  - **Códigos crudos** en lugar de texto: "PendingVerification", "DEPOSIT", "BL_FEE".
  - **Columnas sin ancho proporcional:** el BL se monta sobre el concepto ("HLCUSAI2603006 BL_FEE").
  - **Repeticiones:** la referencia repite el número de pago, y el RUT de facturación repite el del pagador.
  - **Falta lo esencial de una boleta de depósito:** monto destacado, instrucciones y datos bancarios para depositar, vencimiento y qué hacer después.
  - **Diseño plano:** sin jerarquía visual ni recuadro de total.
  - El renderizador (`MigraDocPdfRenderer`) usa tablas con ancho igual para todas las columnas.
- **Selector de medio de pago del carro.** `frontend/src/app/features/cart/cart.html:150-170`:
  - Es una lista vertical de radios con descripción suelta debajo.
  - El nombre y el tipo vienen sin traducir: "Botón de pago Santander (Online)" en la interfaz en inglés, y "Depósito bancario (boleta) (Deposit with payment slip)".
  - El botón "Empty" (vaciar) queda junto al botón de pago.
  - La recomendación es usar tarjetas seleccionables en grilla (radio accesible dentro de una tarjeta con logo, nombre y descripción), separar "en línea" de "depósito", y mover "Vaciar" a una acción secundaria apartada.
