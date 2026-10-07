---
title: 2026-10-06-cierre-fase1-y-ux
type: plan
date: 2026-10-06
status: active
project: hapag-portal
scope: shared
author: brypenalozav-stack
ticket: null
tags: [plan, fase-1, feature-flags, ux, menu, dashboard, detalle-bl, pdf, carro]
related: [thoughts/shared/research/2026-10-06-cierre-fase1-flags-fase2-ux.md, thoughts/shared/reports/UX menú detalle BL y dashboard.md, thoughts/shared/plans/2026-10-05-implementacion-fase1-funcional.md]
---

# Plan: cierre de Fase 1, Fase 2 deshabilitada y rediseño UX

## Contexto

La investigación base está en `thoughts/shared/research/2026-10-06-cierre-fase1-flags-fase2-ux.md`. El informe de UX está en `thoughts/shared/reports/UX menú detalle BL y dashboard.md`.

La fuente de verdad es `Portal_2.0_Especificacion_Funcional_validada(1).docx`. Solo Fase 1 queda activa, con estas excepciones decididas:

- **M6-08 Carta de liberación:** activa por decisión del usuario.
- **M8-09 Counter:** activo, porque es la fuente de la carta de liberación.

## Lo que este plan NO hace

- **No borra código de Fase 2.** Lo deshabilita con flags apagados por defecto y reversibles por configuración.
- **No pasa integraciones a modo Real.** No hay contratos validados.
- **No implementa firma electrónica real (M6-01, M6-07).** El proveedor y el mecanismo los define Hapag-Lloyd. Se documenta como pendiente.
- **No inventa valores de SLA, RPO/RTO ni volumetría (NF-10, 13, 17, 18).** Se dejan plantillas para que Hapag-Lloyd los publique, según la decisión DC6.

## Fases (en orden de ejecución)

### Fase 1: Boleta y documentos PDF (pedido directo)

- **Renderizador** (`MigraDocPdfRenderer`):
  - Anchos de columna proporcionales (`PdfTable.ColumnWidths` opcional; si falta, el ancho se calcula por contenido).
  - Recuadro destacado para montos y totales.
  - Bloque de "datos clave" en dos columnas con etiqueta y valor legibles.
  - Insignia de estado.
  - Encabezado con marca (barra de color y emisor) y pie con verificación.
- **Plantilla de boleta y recibo** (`PaymentReceipt` en `ShipmentDocumentTemplates`):
  - Estados, medios y conceptos en texto, no en código: "Pendiente de verificación", "Depósito bancario", nombre del concepto.
  - Sin datos repetidos: la referencia y el RUT de facturación se muestran solo si difieren.
  - Para el depósito, la sección "Cómo pagar": monto exacto, cuenta de Hapag-Lloyd y los pasos siguientes, con los datos bancarios configurables por país (`Documents:DepositInstructions`).
- **Éxito:**
  - Prueba unitaria del PDF (contiene los textos en español y no los códigos crudos).
  - Revisión visual (render a PNG).
  - `dotnet test` en verde.

### Fase 2: Selector de medio de pago en el carro (pedido directo)

- **Tarjetas seleccionables en grilla:** radio accesible dentro de una tarjeta con logo, nombre, tipo traducido y descripción. El estado seleccionado es visible con borde y check.
- **Agrupación:** "En línea" y "Depósito".
- **Botones:** "Revisar y pagar" va a ancho completo; "Vaciar" pasa a una acción secundaria apartada.
- **Traducción:** el tipo del medio sale de i18n; el nombre viene de la configuración.
- **Éxito:**
  - Las pruebas e2e del carro y del pago siguen en verde.
  - axe en tema claro y oscuro.
  - Captura revisada.

### Fase 3: Fase 2 deshabilitada con feature flags

- **Backend:**
  - Sección `Features` en appsettings con un flag por ficha. Todos los de Fase 2 vienen apagados, salvo `ReleaseLetter` y `Counter`.
  - Atributo `[RequiresFeature("X")]` en los controladores o acciones de Fase 2: responde 404 si el flag está apagado.
  - `GET config/features` para el frontend.
  - Los publicadores internos siguen funcionando: las notificaciones por correo de M1-08 no dependen del flag de la bandeja.
- **Frontend:**
  - `FeatureService` (signal) y `featureGuard('X')` en las rutas.
  - `MenuContext.feature(...)` para el menú.
  - `@if` en el dashboard, el detalle, el panel de cargos, las facturas, la organización, el shell (guía e impersonación) y la barra superior (campana).
- **Pruebas e2e:**
  - Las especificaciones de Fase 2 activan sus flags por mock (`config/features`).
  - Se agrega una prueba que verifica que con los flags por defecto no aparece ningún punto de entrada de Fase 2.
- **Éxito:** backend y Playwright completos en verde, con la prueba "solo Fase 1" incluida.

### Fase 4: Menú y barra superior

- **Backoffice separado:**
  - Los perfiles internos entran a un espacio "Backoffice" (Operación y Administración) mediante un selector de espacio en la barra superior.
  - El menú del cliente no mezcla funciones internas.
- **Menú del cliente por tareas, con unas 5 entradas:** Inicio · Embarques · Pagos · Documentos y trámites · Ayuda.
  - "Mi organización" y los accesos pasan al menú de usuario.
  - Nombres consistentes, en español, sin duplicar grupo y enlace.
- **Búsqueda universal visible** (BL, booking o contenedor), que lleva al detalle.
- **Barra superior más liviana:** tema e idioma dentro del menú de usuario o de preferencias; el país, solo para quien opera en ambos.
- **Éxito:**
  - Pruebas e2e de navegación ajustadas y en verde.
  - axe en tema claro y oscuro.
  - Teclado (disclosure).

### Fase 5: Dashboard orientado a acción

- **Arriba:** "Requiere su acción", agrupado por BL y ordenado por urgencia (vencido, por vencer). Cada fila tiene su acción y la lista muestra los primeros N con "Ver todo".
- **KPI:** 3 o 4, sin repetir el demurrage.
- **Accesos rápidos:** como máximo 4, distintos del menú.
- **Indicadores** colapsados o a demanda, con `@defer`.
- **Estado vacío positivo:** "Todo al día".
- **Éxito:** pruebas e2e del dashboard ajustadas, axe, captura revisada.

### Fase 6: Detalle del BL canónico

- **Encabezado fijo:**
  - BL, estado, nave y viaje, ETA.
  - "Próxima acción", con la lista de requisitos de liberación (resumen de `/bl-status`).
- **Secciones agrupadas en el índice:**
  - Resumen.
  - Contenedores.
  - Cargos y pagos (flete, cargos locales, demurrage).
  - Documentos.
  - Accesos y servicios.
  - Interno: DIFU y Counter, solo para perfiles internos.
- **Carga diferida:** `@defer (on viewport; prefetch on idle)` en las secciones bajo el pliegue.
- **Rutas paralelas:** `/charges/:bl`, `/demurrage/:bl` y `/shipments/:bl/documents` se mantienen como vistas enfocadas que enlazan al detalle; el índice deja de repetirlas.
- **Éxito:** pruebas e2e del detalle y axe; métrica de carga inicial menor.

### Fase 7: Brechas de Fase 1 en el código

- **M1-10:** correo con enlace de un solo uso a `/reset-password?token=`; revocación del JWT al cerrar sesión con un sello de seguridad por usuario validado en cada petición.
- **M2-07:** filtro de importación/exportación compartido en cargos, demurrage, historial y facturas.
- **M4-04:** `apply-rules` exige la carta de responsabilidad cuando corresponde.
- **M3-18:** el estado de cuenta usa `DemurrageStateEvaluator`.
- **M3-16:** imputación del descuento de las demoras anticipadas guardada y consultable.
- **M5-05:** vigencia del tipo de cambio visible en el carro y el historial.
- **M6-09:** registro de descargas de recibos y facturas.
- **M6-03:** el cupón asocia solo las unidades pagadas.
- **M8-02:** retirar los endpoints `credit-clients` obsoletos.
- **NF-20:** aviso de navegador no soportado.
- **NF-09:** quitar la contraseña de ejemplo de appsettings (usar una variable de entorno).
- **NF-26:** `/health` con chequeos de base de datos y de integraciones.
- **SMTP:** no marcar como entregado un correo que falló, y mostrar el error.

### Fase 8: Documentación de Fase 1

- **M3-05:** evaluación de solicitudes masivas (alternativa, factibilidad, casos Delfin, Falabella y Bagno).
- **M5-03:** constancia de la futura incorporación de dólares digitales.
- **NF-10, 13, 17, 18, 24 y 25:** plantillas para que Hapag-Lloyd publique los valores y los procedimientos (decisión DC6).

## Estado

| Fase | Estado |
|---|---|
| 1. Boleta y PDF | Hecho (sin commit): renderizador, boleta y comprobante, `Documents:DepositInstructions` |
| 2. Selector de medios de pago | Hecho (sin commit): `payment-method-picker` en carro, pagos y estado de cuenta |
| 3. Flags Fase 2 | Hecho (sin commit): `Features` + `[RequiresFeature]` + `config/features`; `FeatureService`/`featureGuard`; `e2e/ux/solo-fase1.spec.ts`. Pendiente aparte: vínculos matriz–filial ya activos siguen dando visibilidad (lógica de acceso central, no se tocó) |
| 4. Menú y barra superior | Pendiente |
| 5. Dashboard | Pendiente |
| 6. Detalle del BL | Pendiente |
| 7. Brechas Fase 1 (código) | Pendiente |
| 8. Documentación Fase 1 | Pendiente |
