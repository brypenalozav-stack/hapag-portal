---
title: 2026-10-05-actualizacion-requerimientos-portal-2-0-ui-integraciones
type: plan
date: 2026-10-05
status: draft
project: hapag-portal
scope: shared
author: brypenalozav-stack
ticket: null
tags: [plan, portal-2.0, requerimientos, i18n, accesibilidad, integraciones, nexus]
related: [thoughts/shared/research/2026-10-05-actualizacion-requerimientos-portal-2-0.md]
git_commit: f49ea8af9cace4140ba908d51f434e0daaf50861
branch: feature/plataforma-operativa-aduana
last_updated: 2026-10-05T12:36:56-03:00
last_updated_by: brypenalozav-stack
last_updated_note: "Iteración 1: integraciones primero, 3a+3b unidas, Gantt fuera de alcance, Fase 7 documentacion.html (Mermaid, sin bpmn-js), calendario y responsables"
---

# Plan de implementación: actualización de requerimientos Portal 2.0 (Chile/Bolivia), base UI/i18n/accesibilidad y capa de integraciones

## Overview

El plan hace cuatro cosas:

- **Documentos v4.** Actualiza 4 de los documentos depurados de `C:\Users\klaze\Desktop\katu`: especificación, Pendientes, NexusV2 y Comparación. Las versiones nuevas van a `katu\v4\` con sufijo `_v4` y los originales no se tocan. El Gantt de macros queda fuera del alcance: no se genera `Gantt_v4` y solo se conserva el hash del original en la línea base.
- **Documentos nuevos.** Crea la Guía UI/a11y/i18n, la Matriz de trazabilidad y un prototipo navegable publicado.
- **Base técnica en `hapag-portal`** (Angular 21 + .NET 9 + PostgreSQL/EF Core):
  - En el frontend: tokens aplicados a Bootstrap, i18n ES/EN en caliente con Transloco, accesibilidad WCAG 2.2 AA y verificación automática.
  - En el backend: una capa de integraciones con puertos, adaptadores Dummy, simulador HTTP, clientes Real con resiliencia y pruebas de contrato. Así el desarrollo avanza aunque los sistemas externos aún no estén disponibles.
- **Documentación del repositorio.** Actualiza `docs/documentacion.html` con lo de Portal 2.0 v4: requisitos, idioma y teclado, UI/i18n/a11y e integraciones. Los 3 diagramas BPMN pasan a Mermaid y se elimina la librería bpmn-js, que tiene una licencia no OSI.

Lo ejecuta un solo desarrollador, en este orden: 0 → 6a → 6b → 6c → 1 → 2 → 3 → 4 → 5a → 5b → 5c → 7. Empieza el **06-10-2026** y termina el **02-12-2026** (ver "Calendario y responsables").

El Portal 2.0 es este repositorio. Ningún entregable menciona al proveedor anterior.

## Current State Analysis

### Documentos

Fuente: `thoughts/shared/research/2026-10-05-actualizacion-requerimientos-portal-2-0.md`, más la verificación hecha en esta sesión.

**Especificación v3.0**
- 124 fichas: 97 RF (64 en Fase 1, 29 en Fase 2, 4 en Fase 0) y 27 NF. Cada ficha es un párrafo con estilo `Heading 3`.
- Arrastra los defectos D1–D10.
- El nombre del proveedor anterior aparece 6 veces y la palabra "proveedor" 21 veces.
- NF-23 fija solo español.
- **El índice no usa estilos de título.** Tiene 42 campos TC (`TC "<texto>" \f P \l <1|2>`, con el texto partido en varios `w:instrText`) y un `TOC \f P \h \l "1-2"`. Los títulos llevan el número escrito a mano (por ejemplo "14.   Modelo de integración– FASE 0").

**Pendientes**
- 118 tareas. Responsable, Fecha inicio y Fecha objetivo están vacíos en todas.
- Tiene una hoja `Resumen` con el gráfico `xl/charts/chart1.xml` (barChart sobre `Resumen!$A$5:$B$9`).
- La tarea 37 propone "sincronización Portal/Nexus" y las tareas 42–44 hablan de "sincronización" con FIS.
- Las tareas 102–106 son del área Macros/RPX.

**NexusV2**
- 17 filas; 5 no tienen RACI.

**Gantt**
- 8 semanas, del 07-09-2026 al 30-10-2026. Responsables: Diego, Jorge y RPX.
- Catálogo FIS de 10 pantallas e inventario de 8 macros con 11 asociaciones de servicio, sin clasificar.
- **Queda fuera del alcance de este plan**, que solo cubre nuestro desarrollo. El original se conserva y su SHA-256 entra en la línea base. No se edita ni se genera una versión `_v4`.

**Comparación**
- Mide un sitio externo. La fila 2 del `Resumen` cita su URL.
- Usa la columna "Relación con POC solicitado" y la cabecera "Dentro/relacionado con POC".

**`docs/documentacion.html`** (verificado en esta sesión)
- Se edita a mano y no tiene generador. Son 4452 líneas en `HEAD`.
- **Contenido.** Está entre las l.86 y 640 y se reparte en tres paneles `<div class="panel" data-tab="funcional|usuario|tecnica">`.
  - Cada `<section id><h2>` se agrega solo a la navegación lateral mediante `buildNav` (l.4425-4430).
  - Los diagramas usan `<div class="diagram"><pre class="mermaid">…</pre></div>`, por ejemplo `f-modulos` en las l.94-111.
- **Librerías incrustadas:**
  - Mermaid 11.16.1 (MIT), de la l.756 a la 4343;
  - bpmn-js navigated-viewer 17.11.1, de la l.4344 a la 4366. Tiene licencia bpmn.io: no es OSI y obliga a mostrar la marca de agua "Powered by bpmn.io".
- **Diagramas BPMN.** Hay 3 contenedores, en las l.135, 141 y 147: `data-bpmn="bpmn-carga|bpmn-aduana|bpmn-plazos"`, dentro de las secciones `f-bpmn-carga`, `f-bpmn-aduana` y `f-bpmn-plazos`.
  - El XML va en `<script type="application/bpmn+xml">` (l.643-753), precedido por el comentario `<!-- ===== BPMN XML embebido (con DI para render offline) ===== -->` de la l.642.
  - No hay lanes. Los pasos y las salidas de cada gateway se listan en la Fase 7 §2.
- **CSS:** la l.54 define `.bpmn { height: 360px; width: 100%; }`.
- **Inicialización** (l.4369-4450):
  - `var bpmnViewers = {};` (l.4386);
  - en `renderPanel`, el bloque `panel.querySelectorAll(".bpmn").forEach(…)` con `new BpmnJS` (l.4395-4408);
  - el comentario de las l.4382-4383 menciona "BPMN".
- **Otras ubicaciones:**
  - tabla ADR en las l.616-622;
  - `t-fuentes` en las l.626-636; la nota de la l.635 cita "demo.bpmn.io";
  - `u-pantalla` termina en la l.274, `t-seq` en la l.570 y `t-seguridad` empieza en la l.602.
- **Ramas.** `origin/develop` tiene una versión anterior de 4413 líneas, también con bpmn-js. Los commits `4ff34bb`, `abad3b7` y `f49ea8a` solo están en `feature/plataforma-operativa-aduana`.

### Código

Verificado en esta sesión.

**Backend**
- Puertos en `backend/src/HapagPortal.Application/Common/Interfaces/` (10 interfaces).
- El registro DI está en `AddInfrastructureServices`, en `backend/src/HapagPortal.Infrastructure/DependencyInjection/DependencyInjection.cs:42-52`, dentro de una clase `partial`. Ya existe la parcial `DI.Auth.Partial.cs`.
- No hay `HttpClient` en ninguna parte.
- `SecretTypes.cs` solo tiene SII_KEY y CUSTOMS_*.
- `Program.cs` no tiene HTTPS ni HSTS; `/health` está en la l.165.
- El webhook Khipu solo valida un secreto compartido (`KhipuWebhookCommandHandler.cs:24-29`).
- La clave del webhook se lee de configuración `Payments:Webhooks:{provider}:Secret` (`WebhookAuthenticator.cs`).
- No existe `backend/tools/`.
- Pruebas con xUnit 2.9.3, FluentAssertions 7.2.0, NSubstitute 5.3.0 y NetArchTest 1.3.2.
- CI: `.github/workflows/pr-tests.yml`.

**Frontend**
- `package.json` no tiene eslint ni librerías de pruebas.
- `angular.json` no tiene targets `lint` ni `test`.
- **Los overrides de Bootstrap no se aplican.** `styles.scss` declara `$primary` y otros antes de `@use 'bootstrap/scss/bootstrap'` sin `with`, y el CSS compilado emite `--bs-primary:#0d6efd`. La UI actual se ve con el azul de Bootstrap.
- Hay 100 literales hex en 22 archivos.
- `lang="es"` está fijo en `index.html:2`.
- El botón hamburguesa no tiene nombre accesible (`navbar.html:5-9`).
- No hay `<header>`.
- Hay `<table>` en 15 archivos y 24 usos de `number:'1.2-2'`.
- La sesión vive en `localStorage` (`hl_token`, `hl_user`).
- `core/models/` no tiene modelos de recibos ni de cargos.

**Entorno**
- Python 3.14 con python-docx 1.2.0 y openpyxl 3.1.5.
- Node 26 en local; CI usa Node 22.
- .NET SDK 10.0.301 en local; los proyectos son `net9.0`.
- **LibreOffice no está instalado.**
- `origin/develop` existe.
- No existe `scripts/` en la raíz del repo.

## Desired End State

1. **katu\v4.** Contiene las versiones `_v4` de especificación, Pendientes, NexusV2 y Comparación. Además: Guía, Matriz (Word), registro de decisiones, matriz de fichas de línea base y prototipo.
   - **No hay `Gantt_v4`.**
   - Los originales, incluido el Gantt, mantienen su SHA-256.
   - Ningún entregable contiene términos prohibidos.
2. **Repositorio.** `docs/requerimientos/` y `docs/integraciones/` tienen las copias Markdown, los contratos OpenAPI 3.1 (todos en estado PROPUESTA) y la hoja de ruta.
3. **Frontend:**
   - el CSS compilado usa los colores de marca corregidos;
   - `ng lint` pasa con las reglas de accesibilidad de plantillas en error;
   - axe da 0 violaciones en las pantallas principales, en ES y en EN;
   - el idioma cambia en caliente;
   - los formatos dependen del país;
   - no quedan hex sueltos.
4. **Backend:**
   - 10 puertos con adaptador Dummy, seleccionados con `Integrations:<Sistema>:Mode`;
   - simulador HTTP en `backend/tools/`;
   - clientes Real con resiliencia, probados contra el simulador;
   - pruebas de contrato en CI;
   - webhooks validados en modo Real;
   - HTTPS/HSTS.
5. **`docs/documentacion.html`:**
   - documenta Portal 2.0 v4 en las secciones `f-req-portal2`, `u-idioma-teclado`, `t-ui-i18n-a11y` y `t-integraciones`, más las filas ADR y las fuentes nuevas;
   - los 3 procesos son flowcharts Mermaid;
   - ya no contiene bpmn-js, el XML BPMN ni el código `BpmnJS`;
   - pesa menos que antes;
   - todos los diagramas se renderizan en las 3 pestañas sin "Syntax error".

### Key Discoveries:
- **Puerto con implementación simulada:** `ICustomsTransmitter.cs` + `backend/src/HapagPortal.Infrastructure/Customs/StubCustomsTransmitter.cs`. Es determinista según el payload ("REJECT"). Su fake de prueba es `backend/tests/HapagPortal.UnitTests.Application/TestHelpers/FakeCustomsTransmitter.cs`.
- **Registro parcial:** `backend/src/HapagPortal.Infrastructure/DependencyInjection/DI.Auth.Partial.cs`.
- **Pruebas de referencia:** `backend/tests/HapagPortal.UnitTests.Domain/Constants/ChargeTypesTests.cs` y `backend/tests/HapagPortal.UnitTests.Infrastructure/Services/PaymentGatewayServiceTests.cs`.
- **Errores tipados:** `backend/src/HapagPortal.Domain/Errors/DomainErrors.cs` (clases anidadas) y `Result`/`Result<T>` en `backend/src/HapagPortal.Domain/Results/`.
- **Reglas de arquitectura:** Application no depende de Infrastructure (`LayerDependencyTests.cs`); handlers y entidades son `sealed` (`SealedClassTests.cs`).
- **Validación de secretos:** `UpsertSecretCommand.cs` no valida el tipo contra una lista.
- **Webhooks:** `PaymentsController.cs:82-112`. El secreto llega en `X-Webhook-Secret` y las rutas son `/api/v1/payments/webhook/{khipu|banco-chile}`.
- **Contraste sobre blanco:**

  | Color | Contraste | Resultado AA |
  |---|---|---|
  | `#ff6600` | 2,94 | falla |
  | `#b84a00` | 5,23 (4,87 sobre `#f5f7fa`) | pasa |
  | `#009840` | 3,77 | falla |
  | `#007a33` | 5,48 | pasa |
  | `#004d6c` | 9,22 | pasa |
  | `#33424f` | 10,33 | pasa |

- **Railway termina el TLS en su proxy.** Hacen falta `ForwardedHeaders`, `HttpsPort=443` y excluir `/health` de la redirección.
- **Licencias:**
  - MediatR 12.4.1 y FluentAssertions 7.2.0 son Apache-2.0. MediatR ≥13 y FluentAssertions ≥8 son comerciales: no se actualizan.
  - bpmn-js usa la licencia bpmn.io, que no es OSI y exige la marca de agua: se elimina en la Fase 7.
- **Convención de diagramas en `documentacion.html`:** `<div class="diagram"><pre class="mermaid">` (`f-modulos`, l.94-111). Mermaid se renderiza por pestaña con `mermaid.run({ nodes })` dentro de `renderPanel`.

## What We're NOT Doing

- **Fichas funcionales en backend.** No se implementan. Los handlers de negocio no consumen los puertos nuevos, salvo la validación de webhooks de la Fase 6c.
- **Endpoints productivos.** No hay adaptadores Real contra ellos. Ningún contrato está entregado por Hapag-Lloyd: todos son PROPUESTA. Pasar a `Mode=Real` en un ambiente exige el contrato validado y el checklist Dummy→Real.
- **Sistemas solo con Dummy.** Firma, almacenamiento, Santander, BCI, Mercurio, TATC/Flagare, Navesoft, depósito y Dispute no tienen adaptador Real.
- **Servicios existentes.** No se migran `IPaymentGatewayService`/`PaymentGatewayService` ni `ICustomsTransmitter`.
- **Rediseño visual.** Solo tokens, i18n y a11y. No hay selector de tema en Angular: los tokens oscuros se declaran, pero el selector queda como tarea M11-07. La pantalla "carro por moneda" existe solo en el prototipo.
- **Localización del backend.** ProblemDetails, correos y PDF no se traducen; queda como tarea.
- **Pruebas unitarias de frontend.** No se agregan vitest ni karma; se usa Playwright.
- **Base de datos.** No hay migraciones EF. NF-27 se cubre con logs y métricas.
- **Originales.** No se modifican los de katu (incluido el Gantt). LibreOffice nunca guarda un `.docx`: solo genera PDF/PNG.
- **Licencias.** No se usan librerías de pago ni restringidas: QuestPDF, iText, PyMuPDF (AGPL), MinIO server (AGPL), ag-Grid Enterprise, MediatR ≥13, FluentAssertions ≥8 ni bpmn-js (licencia bpmn.io).
- **Gantt de macros y clasificación de macros:**
  - no se genera `Gantt_Macros_Extraccion_QA_v4.xlsx`;
  - no se clasifican las macros (reutilizar/modificar/reemplazar/eliminar);
  - no hay hoja "Cruce macros-servicios";
  - no se edita el catálogo FIS;
  - las fechas de Pendientes no salen del Gantt;
  - no hay alineación con su QA.

  Las tareas 102–106 quedan marcadas como fuera del alcance del desarrollo Portal 2.0 (área Macros/RPX).
- **`documentacion.html`.** No se crea un generador ni se reescriben secciones que no figuran en la Fase 7. Tampoco se agrega un editor BPMN.

## Implementation Approach

**Ejecución secuencial.** Un solo desarrollador ejecuta las fases en este orden: 0 → 6a → 6b → 6c → 1 → 2 → 3 → 4 → 5a → 5b → 5c → 7.
- Las integraciones van primero porque los contratos (6a) necesitan tiempo de validación de sus responsables, y porque la especificación (capítulo 15), Pendientes ("Contrato de integración" y "Validar contrato") y la Matriz (estado de contratos y código) los citan.
- Las dependencias de la tabla "Orden de ejecución" siguen valiendo: el orden secuencial las satisface todas.

**Documentos Office.** Se editan con scripts Python reproducibles en `scripts/requerimientos/` (nuevo):
- Leen el original de katu y escriben en `katu\v4\` con sufijo `_v4`.
- El `.docx` se guarda solo con python-docx.
- Cada documento tiene un `verificar_*.py` que termina con código 0 o 1.
- Al final de cada fase de documentos corre `verificar_originales.py`.

**Código.** Cada fase de código va en su rama desde `develop` y se revierte con `git revert` del merge.

**Cierre de fase.** Cada fase termina con su PR mezclado en `develop`. Si la revisión del PR sigue pendiente, la rama de la fase siguiente se crea desde la rama de la fase anterior y se rebasa sobre `develop` después del merge. Esto importa en dos cadenas: 6a usa `terminos.py` de la Fase 0, y la Matriz de la Fase 3 busca en `backend/src` los archivos de 6b y 6c.

**Shell.** Los criterios con sintaxis `for …; do …; done` o `$(…)` se ejecutan en Git Bash. El resto funciona igual en PowerShell.

### Ramas sugeridas (desde `develop`)

| Orden | Fase | Rama | Depende de |
|---|---|---|---|
| 1 | 0 | `feature/linea-base-requerimientos-v4` | — |
| 2 | 6a | `feature/integraciones-contratos` | 0 |
| 3 | 6b | `feature/integraciones-puertos-dummy` | 6a |
| 4 | 6c | `feature/integraciones-simulador-resiliencia` | 6b |
| 5 | 1 | `feature/especificacion-v4` | 0, 6a |
| 6 | 2 | `feature/documentos-operativos-v4` | 1, 6a |
| 7 | 3 | `feature/documentos-nuevos-v4` | 1, 2, 6c |
| 8 | 4 | `feature/prototipo-navegable` | 3 |
| 9 | 5a | `feature/frontend-lint-a11y-tokens` | 0 |
| 10 | 5b | `feature/frontend-i18n-transloco` | 5a |
| 11 | 5c | `feature/frontend-base-accesibilidad` | 5b |
| 12 | 7 | `feature/documentacion-portal-2-0` | 3, 5c, 6c (y la configuración Playwright de 4) |

- En las fases 0–4 la rama solo lleva scripts y copias Markdown, y además el prototipo HTML en la Fase 4. Los `.docx`/`.xlsx` quedan fuera del repo.
- La Fase 7 modifica `docs/documentacion.html`.

### Orden de ejecución

```
0 ─(aprobación del registro y del calendario)─> 6a ─> 6b ─> 6c ─> 1 ─> 2 ─> 3 ─> 4 ─> 5a ─> 5b ─> 5c ─> 7
```

- **Una sola línea.** Con un desarrollador, las líneas de documentos, frontend e integraciones se ejecutan una tras otra. No hay ramas en paralelo ni conflictos de rebase entre fases.
- **Fase 3 en una sola pasada.** Une la Guía y la Matriz, y corre después de 6c. La Matriz se genera **una sola vez**, con los contratos de 6a y el código de 6b/6c ya presentes, sin re-ejecuciones posteriores.
- **Fase 4** depende de 3.
- **5b → 5c.** 5c se construye sobre 5b ya mezclada: los `aria-label`, `alt` y `title` que agrega 5c se escriben directamente como claves Transloco, y `check:i18n`/`check:i18n-text` son obligatorios en 5c.
- **Fase 7** cierra el plan: documenta lo que dejaron 3, 5c y 6c, y reutiliza la configuración Playwright de `scripts/requerimientos/prototipo/` (Fase 4).

**Colores.** Los valores corregidos los fija este plan en la Fase 5a §2. La Guía (Fase 3) los documenta.

### Dependencias nuevas y licencias

| Dependencia | Dónde | Fase | Licencia |
|---|---|---|---|
| LibreOffice (herramienta, headless, solo render) | máquina local | 0 | MPL-2.0 |
| python-docx 1.2.0, openpyxl 3.1.5 (ya instalados) | `scripts/requerimientos/requirements.txt` | 0 | MIT |
| pypdfium2 | idem | 0 | Apache-2.0 / BSD-3-Clause |
| Bootstrap 5.3 / Bootstrap Icons (CDN), Inter / Montserrat | prototipo | 4 | MIT / MIT / OFL-1.1 |
| @playwright/test, @axe-core/playwright | `scripts/requerimientos/prototipo/package.json` | 4 (se reutiliza en 7) | Apache-2.0 / MPL-2.0 |
| angular-eslint **21.x** (fijado; la 22 exige Angular 22), eslint, typescript-eslint | `frontend` devDeps | 5a | MIT |
| @playwright/test, @axe-core/playwright | `frontend` devDeps | 5a | Apache-2.0 / MPL-2.0 |
| lighthouse (vía `npx`, no se instala) | verificación manual | 5c | Apache-2.0 |
| @jsverse/transloco | `frontend` deps | 5b | MIT |
| @jsverse/transloco-keys-manager | `frontend` devDeps | 5b | MIT |
| openapi-spec-validator | `docs/integraciones/requirements.txt` | 6a | Apache-2.0 |
| Microsoft.Extensions.Http.Resilience 9.x (incluye Polly 8, BSD-3-Clause) | Infrastructure | 6c | MIT |
| Microsoft.AspNetCore.Mvc.Testing 9.0.x | `HapagPortal.IntegrationTests` | 6c | MIT |
| schemathesis 4.x | `docs/integraciones/requirements.txt` | 6c | MIT |
| Mermaid 11.16.1 (existente, incrustada) | `docs/documentacion.html` | se mantiene | MIT |
| bpmn-js navigated-viewer 17.11.1 (existente, incrustada) | `docs/documentacion.html` | **7: se elimina** | bpmn.io License (no OSI, exige marca de agua) |

Cada fase que agrega una dependencia registra en `docs/requerimientos/licencias-dependencias.md` (nuevo en la Fase 0) el nombre, la **versión exacta instalada** según `package-lock.json`/`.csproj`/`pip freeze`, la licencia y la fase. Las librerías incrustadas existentes se registran en la Fase 0, en la sección "Existentes". La Fase 7 marca bpmn-js como "Eliminada (Fase 7)".

## Calendario y responsables

**Reglas del calendario**
- Se cuentan días hábiles de lunes a viernes, desde el **06-10-2026**.
- Se excluye el feriado de Chile del **12-10-2026** (lunes, Encuentro de Dos Mundos). El 31-10-2026 y el 01-11-2026 caen en fin de semana.
- Total: 41 días hábiles, con fin el **02-12-2026**.

**Responsables**
- **Ejecuta** cada fase el "Equipo de desarrollo Portal 2.0".
- **Valida** según la tabla. Los validadores son una propuesta que se confirma en la Fase 0, junto con Q5 (DC7).
- Si una fase se atrasa, las siguientes se corren la misma cantidad de días hábiles.

Las fechas también quedan como datos en `scripts/requerimientos/config.py` (`CALENDARIO`). Pendientes_v4 las toma de ahí para las tareas de nuestro desarrollo.

| Orden | Fase | Inicio | Fin | Días hábiles | Ejecuta | Valida (propuesta) |
|---|---|---|---|---|---|---|
| 1 | 0 Línea base y decisiones | 06-10-2026 | 07-10-2026 | 2 | Equipo de desarrollo Portal 2.0 | Katu, Kari |
| 2 | 6a Contratos | 08-10-2026 | 14-10-2026 | 4 | Equipo de desarrollo Portal 2.0 | Lucho (Nexus), Diego (FIS), Fer y Ricardo (pagos) |
| 3 | 6b Puertos y Dummy | 15-10-2026 | 20-10-2026 | 4 | Equipo de desarrollo Portal 2.0 | Jorge |
| 4 | 6c Simulador y resiliencia | 21-10-2026 | 27-10-2026 | 5 | Equipo de desarrollo Portal 2.0 | Jorge, Diego |
| 5 | 1 Especificación v4 | 28-10-2026 | 02-11-2026 | 4 | Equipo de desarrollo Portal 2.0 | Katu, Kari |
| 6 | 2 Documentos operativos v4 | 03-11-2026 | 05-11-2026 | 3 (un día menos: sin Gantt) | Equipo de desarrollo Portal 2.0 | Lucho, Fer |
| 7 | 3 Documentos nuevos | 06-11-2026 | 10-11-2026 | 3 | Equipo de desarrollo Portal 2.0 | Katu |
| 8 | 4 Prototipo | 11-11-2026 | 12-11-2026 | 2 | Equipo de desarrollo Portal 2.0 | Katu, Cami/Mati |
| 9 | 5a Lint, a11y y tokens | 13-11-2026 | 17-11-2026 | 3 | Equipo de desarrollo Portal 2.0 | Andrés |
| 10 | 5b i18n Transloco | 18-11-2026 | 24-11-2026 | 5 | Equipo de desarrollo Portal 2.0 | Andrés, Kari (glosario EN) |
| 11 | 5c Base de accesibilidad | 25-11-2026 | 30-11-2026 | 4 | Equipo de desarrollo Portal 2.0 | Andrés |
| 12 | 7 `documentacion.html` | 01-12-2026 | 02-12-2026 | 2 | Equipo de desarrollo Portal 2.0 | Jorge |

---

## Fase 0: Línea base y decisiones

**Rama**: `feature/linea-base-requerimientos-v4` · **Fechas**: 06-10-2026 → 07-10-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Katu, Kari

### Overview
La fase:
- instala LibreOffice, si el usuario lo aprueba;
- congela katu con hashes;
- exporta la matriz de 124 fichas;
- deja listos los scripts;
- produce el registro de las 9 decisiones más las complementarias, incluido el calendario (DC7).

**Al terminar se detiene para la aprobación del usuario.**

### Changes Required:

#### 0.0 Prerrequisito: LibreOffice
- Pedir **aprobación explícita** al usuario antes de instalar: `winget install TheDocumentFoundation.LibreOffice` (MPL-2.0).
- Sin aprobación no se ejecutan `render.py` ni los criterios de render. El resto de la fase puede avanzar.
- **Resuelto 2026-10-05: el usuario rechazó la instalación.** No se crea `render.py` ni se instala pypdfium2. Todos los criterios de render del plan se reemplazan por la revisión manual abriendo el archivo en Word o Excel.
- LibreOffice se usa **solo** para convertir a PDF y renderizar. Nunca guarda un `.docx` ni un `.xlsx`.

#### 1. Herramientas Python (`scripts/requerimientos/`, nuevo)
- **`requirements.txt`**: `python-docx==1.2.0`, `openpyxl==3.1.5`, `pypdfium2`.
- **`.gitignore`**: `__pycache__/`.
- **`extract.py`**: se copia el extractor de texto de la sesión de research a `scripts/requerimientos/extract.py`, para no depender de la carpeta temporal.
- **`config.py`**:
  - `KATU = r"C:\Users\klaze\Desktop\katu"`, `V4 = KATU + r"\v4"`;
  - mapa original → `_v4`, solo para especificación, Pendientes, NexusV2 y Comparación (el Gantt no tiene `_v4`);
  - `assert_not_original(path)`, que aborta cualquier escritura dentro de `KATU` fuera de `V4` o sin `_v4`;
  - `CALENDARIO`: diccionario `fase → (inicio, fin, validadores)`, con los valores de la sección "Calendario y responsables".
- **`terminos_prohibidos.sha256`**: hashes SHA-256 de los términos normalizados (minúsculas, sin tildes): nombre del proveedor anterior, nombre de la persona de las tareas 9–12, "supabase", "el proveedor debe" y el dominio del sitio externo. El verificador compara hashes de tokens y n-gramas (1–3), de modo que el repositorio nunca contiene los términos en claro. La lista en claro solo vive en el scratchpad local.
- **`terminos.py`**: `encontrar_prohibidos(texto) -> list[str]`, que devuelve los hashes encontrados, y `proveedor_fuera_de_permitidas(texto) -> list[str]`. Usan `terminos_prohibidos.sha256` y `proveedor_permitido.txt`, y los llaman todos los `verificar_*.py`.
- **`proveedor_permitido.txt`**: frases legítimas, como "proveedor de firma electrónica", "proveedor de pago" y "proveedor del servicio".
- **`docx_utils.py`**:
  - **Recorrido del cuerpo** en orden (párrafos y tablas), igual que `scripts/requerimientos/extract.py`.
  - **Búsqueda y edición:** `find_ficha(doc, id)`, `set_style`, `replace_in_runs` (conserva el formato), `move_block(start, end, before)` e `insert_ficha_after(anchor, id, titulo, fase, secciones)`.
  - **`insert_tc_field(paragraph, text, level)`:** inserta al inicio del párrafo la secuencia `fldChar begin` / `instrText ' TC "'` / `instrText text` / `instrText '" \f P \l {level} '` / `fldChar end`. Es la misma estructura que tienen hoy los 42 campos.
  - **`update_tc_fields(doc, old, new)`:** concatena los `instrText` entre cada `begin` y `end` de un campo TC, reemplaza el texto y reescribe los runs.
  - **`list_tc_fields(doc)`:** devuelve `[(texto, nivel)]`.
  - **`set_update_fields_on_open(docx_path)`:** solo agrega `<w:updateFields w:val="true"/>` en `word/settings.xml`.
- **`linea_base.py`**:
  - Calcula el SHA-256 de los 6 archivos de katu, incluido el Gantt, y escribe `docs/requerimientos/linea-base-katu.json` y `docs/requerimientos/linea-base-katu.md`.
  - Exporta `katu\v4\Matriz_Fichas_Linea_Base_v4.xlsx` con 124 filas. Columnas: ID, Módulo, Título, Fase según encabezado, Fase según texto, Cobertura C/P/N y Evidencia `file:line`, tomadas del research §2.1–§2.6.
- **`verificar_originales.py`**: recalcula los hashes y termina con código 1 si alguno cambió.
- **`render.py`**:
  - Copia la entrada a `<V4>\render\tmp\`, para que LibreOffice no deje archivos de bloqueo en katu ni en v4.
  - Ejecuta `soffice --headless --convert-to pdf --outdir <V4>\render <copia>`.
  - Con pypdfium2 renderiza a PNG las páginas indicadas en `<V4>\render\`.
  - Borra `tmp\`.

#### 2. Registro de decisiones
- **Archivos**: `docs/requerimientos/registro-decisiones-v4.md` (nuevo) y `katu\v4\Registro_Decisiones_v4.xlsx`.
- **Campos por decisión**: ID, pregunta, opciones, decisión propuesta, impacto y estado (Propuesta → Aprobada o Modificada).

| # | Pregunta | Decisión propuesta por defecto |
|---|---|---|
| Q1 | Fase de M3-02, M6-02 y M3-17. Dependencia de M5-07 y M6-07 sobre M7-03. | Manda el encabezado.<br>• **M3-02 = Fase 1:** se quita "en su segunda fase".<br>• **M6-02 = Fase 2:** "primera etapa" pasa a "primera entrega dentro de Fase 2, sin pago ni carro".<br>• **M3-17 = Fase 2:** se quitan la nota de opinión y "en revisión"; el WS lo desarrolla y administra Hapag-Lloyd.<br>• **D8:** M5-07 y M6-07 dependen de M4-03/M8-02 (`ICreditConditionReader`), no de M7-03. M7-03 sigue en Fase 2. |
| Q2 | M9-02 y M9-03 citadas sin ficha | Se **eliminan las referencias**. M9-01 se relaciona con M1-23 y NF-27, y NF-27 queda autocontenida. |
| Q3 | FIS/Data Lake por API o por sincronización de BD. Función "Datos" de NexusV2. | **API**, sin acceso directo a BD. Se elimina la frase de §1. Para "Datos", **el portal consulta a Nexus vía API bajo demanda, con caché corta**; no hay sincronización de tablas. |
| Q4 | Módulo "Supabase / Base de Datos" | Se reformula como **"Base de datos (PostgreSQL)"**, con EF Core y migraciones en `HapagPortal.DatabaseMigrations`. Las tareas 19–24 conservan su intención. |
| Q5 | Responsables de Pendientes | **Regla de asignación:**<br>• Función NexusV2 → R del RACI, con marca "RACI NexusV2".<br>• Tarea de desarrollo de este plan (lista cerrada en la Fase 2 §1) → `Desarrollo – Equipo de desarrollo Portal 2.0 (R); apoyo: <validadores de la fase>`, con marca "Plan Portal 2.0".<br>• Resto → área + nombre propuesto, con marca "Propuesta".<br>**Tabla persona→área propuesta:** Finanzas: Fer, Ricardo · Comercial: Kari · Customer Service: Cami, Mati · Nexus/IT: Lucho, Jorge · Arquitectura/QA: Andrés · Macros/RPX: Diego, RPX · Producto/Negocio: Katu.<br>**Legal y Seguridad** quedan como "Área Legal / Área Seguridad TI (titular del área)", con marca "Propuesta – sin nombre en fuentes".<br>**Formato de la celda:** `Área – Nombre (R); apoyo: Área – Nombre`. |
| Q6 | Cómo entran al portal los datos de FIS | **Destino:** `IShipmentSource`, contrato CT-FIS (`fis.openapi.yaml`).<br>**Entrada transitoria:** el importador existente `POST bills-of-lading/import` (`ImportBillsOfLadingCommandHandler.cs`) sigue siendo la vía de carga hasta que CT-FIS se valide y se conmute a Real.<br>**Sin clasificación de macros:** el Gantt y las macros (tareas 102–106) quedan fuera del alcance del desarrollo Portal 2.0 (área Macros/RPX). |
| Q7 | Counter, Errores de facturación y Plazos documentales | • **M8-09 "Counter Bolivia/Ultramar" (Fase 2)**: ficha nueva.<br>• **M2-10 "Consulta de plazos documentales por nave" (Fase 2)**: ficha nueva.<br>• **Errores de facturación**: fuera del alcance; sigue en Nexus.<br>• **Total:** 134 fichas. |
| Q8 | Alcance del bilingüismo, variante de inglés y glosario | **Alcance:** en Fase 1, interfaz ES/EN en caliente. En Fase 2, correos, PDF y asistente M10.<br>**Inglés:** internacional con ortografía estadounidense; los números usan el locale `en`.<br>**Fechas:** es-CL `dd-MM-yyyy`, es-BO `dd/MM/yyyy`, EN `dd MMM yyyy`. Siempre en 24 h, con el huso del país indicado (America/Santiago / America/La_Paz).<br>**Montos:** siempre con código ISO. CLP con 0 decimales; BOB, USD y EUR con 2.<br>**Glosario:** `docs/requerimientos/glosario-es-en.md`. |
| Q9 | Nivel de conformidad y evidencia | **WCAG 2.2 AA completo**: los 55 criterios A+AA de la Recomendación W3C (4.1.1, obsoleto, queda fuera). Evidencia:<br>1. `ng lint` sin errores en CI;<br>2. axe con 0 violaciones en ES y EN;<br>3. Lighthouse Accesibilidad ≥ 95;<br>4. prueba manual con teclado y NVDA según el checklist de la Guía. |

**Decisiones complementarias** (también se aprueban):
- **DC1:** M11 se inserta como capítulo 14. Integración pasa a 15, NF a 16 y Glosario a 17.
- **DC2:** valores de NF-20 y NF-21 (ver Fase 1).
- **DC3:** NF-22 calcula en UTC con calendario de negocio y presenta en el huso del país.
- **DC4:** colores corregidos (Fase 5a §2). Además deja constancia de que **hoy la UI muestra el azul por defecto de Bootstrap (`--bs-primary:#0d6efd`)** porque los overrides no se aplican. El cambio visual de la Fase 5a es mayor de lo que sugiere el código actual.
- **DC5:** portada "Preparado para: Hapag-Lloyd Chile y Bolivia".
- **DC6:** "el proveedor debe indicar…" pasa a "Hapag-Lloyd, a través del equipo de desarrollo del Portal 2.0, define y publica…".
- **DC7:** se aprueban el calendario y los validadores por fase de la sección "Calendario y responsables". Ejecuta siempre el "Equipo de desarrollo Portal 2.0", con inicio el 06-10-2026, fin el 02-12-2026 y el 12-10-2026 excluido.

#### 3. Precondición de repositorio
- Se abre y mezcla el PR `feature/plataforma-operativa-aduana → develop`. Contiene los commits `4ff34bb`, `abad3b7` y `f49ea8a`, que dejan `docs/documentacion.html` en la versión de 4452 líneas que la Fase 7 edita por marcadores.
- Todas las ramas del plan parten de ese `develop`.

#### 4. Glosario y licencias
- **`docs/requerimientos/glosario-es-en.md`** (nuevo): BL, SWB/EBL, demurrage, detention, free time, Gate In/Out, EDS, MHD, TATC, CLD, DIFU, customs broker, freight forwarder (FFWW), consignee, Match Code, receipt, invoice, statement of account, cart, canje, desconsolidado, depot.
- **`docs/requerimientos/licencias-dependencias.md`** (nuevo). Incluye la sección "Existentes" con Mermaid 11.16.1 (MIT) y bpmn-js navigated-viewer 17.11.1 (bpmn.io License, no OSI, exige marca de agua), ambas incrustadas en `docs/documentacion.html`.

### Success Criteria:

#### Automated Verification:
- [ ] Dependencias: `python -m pip install -r scripts/requerimientos/requirements.txt`
- [ ] Línea base: `python scripts/requerimientos/linea_base.py`; existen `docs/requerimientos/linea-base-katu.json` y `C:\Users\klaze\Desktop\katu\v4\Matriz_Fichas_Linea_Base_v4.xlsx`
- [ ] 124 filas: `python -c "import openpyxl;ws=openpyxl.load_workbook(r'C:\Users\klaze\Desktop\katu\v4\Matriz_Fichas_Linea_Base_v4.xlsx').active;assert ws.max_row-1==124"`
- [ ] Campos TC legibles: `python -c "import sys;sys.path.insert(0,'scripts/requerimientos');import docx,docx_utils as u;assert len(u.list_tc_fields(docx.Document(r'C:\Users\klaze\Desktop\katu\Portal_2.0_Especificacion_Funcional_validada.docx')))==42"`
- [ ] Originales intactos: `python scripts/requerimientos/verificar_originales.py`
- [ ] Render (solo con LibreOffice aprobado e instalado): `python scripts/requerimientos/render.py "C:\Users\klaze\Desktop\katu\Portal_2.0_Especificacion_Funcional_validada.docx" --pages 1-3` genera 3 PNG y no deja `.~lock*` en `C:\Users\klaze\Desktop\katu\`
- [ ] Precondición de repositorio: `git fetch origin && git merge-base --is-ancestor f49ea8a origin/develop`
- [ ] Registro completo: `python -c "t=open('docs/requerimientos/registro-decisiones-v4.md',encoding='utf-8').read();assert all(f'Q{i}' in t for i in range(1,10)) and all(f'DC{i}' in t for i in range(1,8))"`
- [ ] Calendario cargado: `python -c "import sys;sys.path.insert(0,'scripts/requerimientos');import config as c;assert len(c.CALENDARIO)==12 and c.CALENDARIO['0'][0]=='2026-10-06' and c.CALENDARIO['7'][1]=='2026-12-02'"`

#### Manual Verification:
- [ ] El usuario autoriza o rechaza la instalación de LibreOffice (paso 0.0)
- [ ] El usuario aprueba o modifica cada decisión Q1–Q9 y DC1–DC7
- [ ] El usuario confirma la tabla persona→área de Q5 y, con ella, el calendario y los validadores (DC7)
- [ ] Los PNG de la especificación original se ven correctos

**Implementation Note**: fase de compuerta. Ninguna fase posterior, empezando por la 6a, se inicia sin aprobación. Si una decisión cambia, se actualizan las secciones afectadas del plan y `config.CALENDARIO`.

---

## Fase 6a: Inventario y contratos de integración

**Rama**: `feature/integraciones-contratos` (depende de 0) · **Fechas**: 08-10-2026 → 14-10-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Lucho (Nexus), Diego (FIS), Fer y Ricardo (pagos)

### Changes Required:

#### 1. Inventario
**File**: `docs/integraciones/README.md` (nuevo).
- **Sistemas:** Nexus, FIS/Data Lake, Navesoft, Khipu, Banco de Chile, Santander, BCI, depósito, DBNet/SII, Mercurio, TATC/Flagare, tracking, Dispute, firma, storage, correo e IA.
- **Columnas:** propósito, fichas (IDs v3 más los nuevos M2-10, M8-09 y M11-xx decididos en la Fase 0), tareas (IDs de Pendientes original), función NexusV2, protocolo conocido, estado, responsable de validación (Q5), puerto y modo.
- **Notas:**
  - Correo: "SMTP existente (`EmailService.cs`)".
  - IA: "Fase 2, sin contrato".
  - FIS: "Entrada transitoria: `POST bills-of-lading/import` (Q6)".

#### 2. Contratos
`docs/integraciones/contratos/` (nuevo):
- **Todos están en estado PROPUESTA**: Hapag-Lloyd todavía no entregó ninguno.
- Cada YAML declara `openapi: 3.1.0`, `info.x-estado: "PROPUESTA – pendiente de validación con <responsable>"` (la misma frase en `description`) e `info.x-responsable`.
- Todos comparten:
  - `Problem` (RFC 9457);
  - la cabecera `X-Correlation-Id`;
  - las respuestas 200, 400, 404, 429, 500 y 503.

| ID | Archivo | Operaciones mínimas | Responsable de validación (Q5) | Puerto 6b |
|---|---|---|---|---|
| CT-NEXUS | `nexus.openapi.yaml` | `GET /exemptions?taxId&matchCode&at`, `GET /customers/{taxId}/conditions`, `GET /exchange-rates?from&to&date`, `GET /tariffs?country&concept&at` | Nexus/IT – Lucho (apoyo: Jorge) | IExemptionReader, ICreditConditionReader, IExchangeRateProvider, ITariffProvider |
| CT-FIS | `fis.openapi.yaml` | `GET /shipments?updatedSince&type`, `GET /shipments/{blNumber}` | Macros/RPX – Diego (apoyo: Jorge) | IShipmentSource |
| CT-KHIPU | `khipu.openapi.yaml` | Reproduce la API pública de Khipu: creación de pago y consulta del pago por `notification_token` (rutas y nombres de campo tal como los publica Khipu) | Finanzas – Fer | IPaymentProvider("Khipu") |
| CT-BCH | `banco-chile.openapi.yaml` | `POST /payments`, `GET /payments/{reference}`, webhook con `X-Signature` (HMAC-SHA256 del cuerpo) | Finanzas – Ricardo | IPaymentProvider("BancoChile") |
| CT-SANT | `santander.openapi.yaml` | Igual a CT-BCH | Finanzas – Ricardo | IPaymentProvider("Santander") |
| CT-BCI | `bci.openapi.yaml` | Igual a CT-BCH (tareas 116–118) | Finanzas – Fer | IPaymentProvider("Bci") |
| CT-DBNET | `dbnet.openapi.yaml` | `POST /documents`, `GET /documents/{folio}` | Finanzas – Fer (apoyo: Jorge) | IInvoiceProvider |
| CT-TRACK | `tracking.openapi.yaml` | `GET /tracking/{reference}/events` | Customer Service – Cami/Mati | ITrackingProvider |
| CT-SIGN | `firma.openapi.yaml` | `POST /sign` | Área Legal (titular) | IDocumentSigner |
| CT-STORAGE | `storage.md` | `Save/OpenRead/Delete` | Área Seguridad TI (titular) | IFileStorage |
| CT-MERC, CT-TATC, CT-NAVE, CT-DEP, CT-DISP | `mercurio.openapi.yaml`, `tatc.openapi.yaml`, `navesoft.md`, `deposito.md`, `dispute.md` | Solo lectura, o descripción del intercambio actual (CSV/FTP en el caso de Navesoft) | Nexus/IT – Lucho · Customer Service – Cami/Mati | sin puerto |

#### 3. Hoja de ruta
**File**: `docs/integraciones/hoja-de-ruta.md` (nuevo).
- Hitos por sistema, alineados **solo** con las tareas Nexus 25–41, FIS 42–46, Pagos 47–55 y 116–118, y con el calendario del plan (sección "Calendario y responsables").
- No usa el Gantt de macros.

#### 4. Validación
- **File**: `docs/integraciones/requirements.txt` (nuevo): `openapi-spec-validator`.
- **File**: `scripts/requerimientos/verificar_contratos.py` (nuevo). Comprueba:
  - cada YAML declara `openapi: 3.1.0` y `x-estado` es `PROPUESTA – …` o `Validado (<fecha>, <responsable>)`; hoy todos son PROPUESTA;
  - `x-responsable` no está vacío;
  - el README cubre todos los sistemas;
  - 0 términos prohibidos (`terminos.py`).

### Success Criteria:

#### Automated Verification:
- [ ] `python -m pip install -r docs/integraciones/requirements.txt && for f in docs/integraciones/contratos/*.openapi.yaml; do openapi-spec-validator "$f" || exit 1; done`
- [ ] `python scripts/requerimientos/verificar_contratos.py`

#### Manual Verification:
- [ ] Cada responsable recibe su contrato. Las tareas "Validar contrato" se registran en Pendientes_v4 durante la Fase 2.
- [ ] CT-FIS cubre los campos del catálogo FIS del research §1.4. El catálogo solo se lee: no se edita.
- [ ] CT-KHIPU coincide con la documentación pública de Khipu

### Rollback
Solo documentación: `git revert`.

**Implementation Note**: pausar para confirmación manual.

---

## Fase 6b: Puertos y adaptadores Dummy

**Rama**: `feature/integraciones-puertos-dummy` (depende de 6a) · **Fechas**: 15-10-2026 → 20-10-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Jorge

### Changes Required:

#### 1. Puertos
En `backend/src/HapagPortal.Application/Common/Interfaces/`, un archivo por interfaz con sus records, como en `IPaymentGatewayService.cs`:
- Todos devuelven `Result<T>`.
- Sin datos: `Success(null)` o una lista vacía.
- Falla: `Failure(DomainErrors.Integration.*)`.

| Archivo (nuevo) | Contrato |
|---|---|
| `IExemptionReader.cs` | `Task<Result<IReadOnlyList<ExemptionInfo>>> GetExemptionsAsync(string taxId, string? matchCode, DateOnly at, CancellationToken ct = default)`; `ExemptionInfo(string Concept, decimal? Amount, string? Currency, DateOnly ValidFrom, DateOnly? ValidTo)` |
| `ICreditConditionReader.cs` | `GetConditionsAsync(string taxId, string? matchCode, …)` → `Result<CustomerConditions?>`; `CustomerConditions(string TaxId, string? MatchCode, bool IsFreightForwarder, CreditCondition? Credit)`; `CreditCondition(IReadOnlyList<string> Concepts, int CreditDays, DateOnly ValidFrom, DateOnly? ValidTo)` |
| `IExchangeRateProvider.cs` | `GetRateAsync(string fromCurrency, string toCurrency, DateOnly date, …)` → `Result<ExchangeRateQuote?>`; `ExchangeRateQuote(string FromCurrency, string ToCurrency, decimal Rate, DateOnly EffectiveDate, string Source, bool Approved)` |
| `ITariffProvider.cs` | `GetTariffsAsync(string countryCode, string concept, DateOnly at, …)` → `Result<IReadOnlyList<TariffItem>>`; `TariffItem(string Concept, string? ContainerType, decimal Amount, string Currency, DateOnly ValidFrom, DateOnly? ValidTo)` |
| `IShipmentSource.cs` | `GetByBlNumberAsync(string blNumber, …)` y `GetUpdatedSinceAsync(DateTime sinceUtc, string? shipmentType, …)`; `ShipmentRecord(string BlNumber, string? BookingNumber, string ShipmentType, string? Vessel, string? Voyage, DateTime? Etd, DateTime? Eta, int? FreeDays, string? DepotImport, string? DepotExport, string? MatchCode, string? TaxId)` |
| `IInvoiceProvider.cs` | `IssueAsync(InvoiceIssueRequest)`, `GetAsync(string folio)` |
| `IPaymentProvider.cs` | `string ProviderCode { get; }`; `bool VerifiesNotifications { get; }` (Dummy = false, Real = true); `InitiateAsync(PaymentInitiationRequest)`; `VerifyNotificationAsync(string notificationToken, string externalReference, …)` → `Result<PaymentVerification>`; `PaymentVerification(string ExternalReference, string Status, decimal Amount, string Currency, string? TransactionId)` |
| `IDocumentSigner.cs` | `SignAsync(SignDocumentRequest(byte[] Content, string DocumentType, string SignerProfile))` |
| `IFileStorage.cs` | `SaveAsync(Stream, string fileName, string contentType, string container)`, `OpenReadAsync(string key)`, `DeleteAsync(string key)` |
| `ITrackingProvider.cs` | `GetEventsAsync(string reference)` |

#### 2. Errores y secretos
- **File**: `backend/src/HapagPortal.Domain/Errors/DomainErrors.cs`. Clase anidada `Integration` con `Unavailable`, `Timeout`, `InvalidResponse` y `NotConfigured`, todas con parámetro `string system`.
- **File**: `backend/src/HapagPortal.Domain/Constants/SecretTypes.cs`. Agrega:
  - `NEXUS_API_KEY`, `FIS_API_KEY`
  - `KHIPU_RECEIVER_ID`, `KHIPU_SECRET`
  - `BANCOCHILE_API_KEY`, `SANTANDER_API_KEY`, `BCI_API_KEY`
  - `DBNET_API_KEY`, `TRACKING_API_KEY`
  - `SIGNER_CERTIFICATE`, `STORAGE_ACCESS_KEY`

  La clave de firma del webhook de Banco de Chile **no** es un SecretType: va en configuración (Fase 6c).

#### 3. Adaptadores Dummy
`backend/src/HapagPortal.Infrastructure/Integrations/<Sistema>/Dummy*.cs` (nuevos), `sealed` y deterministas, con el modelo de `StubCustomsTransmitter.cs`:
- `Nexus/DummyExemptionReader.cs`
- `Nexus/DummyCreditConditionReader.cs`
- `Nexus/DummyExchangeRateProvider.cs`
- `Nexus/DummyTariffProvider.cs`
- `Fis/DummyShipmentSource.cs`
- `DbNet/DummyInvoiceProvider.cs`
- `Payments/DummyPaymentProvider.cs`
- `Signature/DummyDocumentSigner.cs`
- `Storage/DummyFileStorage.cs` (en memoria)
- `Tracking/DummyTrackingProvider.cs`

**Escenarios por RUT:**

| RUT | Resultado |
|---|---|
| `76000001-1` | exento de Gate In y EDS |
| `76000002-2` | crédito a 30 días |
| `76000003-3` | FFWW |
| cualquier otro | sin condiciones |

**Tipos de cambio:** CLP 950, BOB 6,91 y EUR.

**`DummyPaymentProvider`:**
- se registra con clave para `Khipu`, `BancoChile`, `Santander` y `Bci`;
- `VerifiesNotifications = false`.

#### 4. Selección por configuración
- **File**: `backend/src/HapagPortal.Infrastructure/DependencyInjection/DI.Integrations.Partial.cs` (nuevo), con el modelo de `DI.Auth.Partial.cs`. Contiene `public static IServiceCollection AddIntegrations(this IServiceCollection services, IConfiguration configuration)`:
  - lee `Integrations:<Sistema>:Mode`; si falta, usa `Dummy`;
  - en esta fase, `Mode=Real` lanza `InvalidOperationException("Integrations:<Sistema>:Mode=Real no tiene adaptador disponible")`.
- **File**: `DependencyInjection.cs`. Agregar `services.AddIntegrations(configuration);` dentro de `AddInfrastructureServices`, junto a los registros de las l.42-52.
- **Files**: `appsettings.json` y `appsettings.Staging.json`. Sección `"Integrations"` por sistema: `{ "Mode": "Dummy", "BaseUrl": "", "TimeoutSeconds": 10, "TotalTimeoutSeconds": 30, "RetryBaseDelayMs": 2000 }`.

#### 5. Pruebas
- **`backend/tests/HapagPortal.UnitTests.Infrastructure/Integrations/Dummy*Tests.cs`** (nuevos), con el modelo de `PaymentGatewayServiceTests.cs`.
- **`…/Integrations/IntegrationsRegistrationTests.cs`** (nuevo). Comprueba Dummy por defecto, la excepción con `Mode=Real` y la excepción con un modo inválido.
- **`backend/tests/HapagPortal.UnitTests.Domain/Constants/SecretTypesTests.cs`** (nuevo), con el modelo de `ChargeTypesTests.cs`.
- **`backend/tests/HapagPortal.ArchitectureTests/SealedClassTests.cs`**. Caso nuevo: las clases de `HapagPortal.Infrastructure.Integrations` deben ser `sealed`.

### Success Criteria:

#### Automated Verification:
- [ ] `dotnet build backend/HapagPortal.sln --configuration Release`
- [ ] `dotnet test backend/HapagPortal.sln --configuration Release`
- [ ] `dotnet test backend/HapagPortal.sln --filter "FullyQualifiedName~Integrations|FullyQualifiedName~SecretTypesTests"`
- [ ] `dotnet test backend/tests/HapagPortal.ArchitectureTests/HapagPortal.ArchitectureTests.csproj`

#### Manual Verification:
- [ ] La API arranca y `/health` responde 200
- [ ] Con `Integrations__Nexus__Mode=Real` la API no arranca y el mensaje de error es claro

### Rollback
`git revert` del merge. Las claves `Integrations:*` se ignoran y los `SecretCredential` con tipos nuevos quedan inertes.

**Implementation Note**: pausar para confirmación manual.

---

## Fase 6c: Simulador HTTP + resiliencia

**Rama**: `feature/integraciones-simulador-resiliencia` (depende de 6b) · **Fechas**: 21-10-2026 → 27-10-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Jorge, Diego

### Changes Required:

#### 1. Simulador
- **File**: `backend/tools/HapagPortal.IntegrationSimulator/HapagPortal.IntegrationSimulator.csproj` (nuevo): `Microsoft.NET.Sdk.Web`, `net9.0`, sin paquetes externos.
- **File**: `backend/tools/HapagPortal.IntegrationSimulator/Program.cs` (nuevo). Minimal API:
  - **Rutas:** CT-NEXUS, CT-FIS, CT-KHIPU, CT-BCH, CT-DBNET y CT-TRACK, bajo `/nexus`, `/fis`, `/khipu`, `/banco-chile`, `/dbnet` y `/tracking`.
  - **Escenarios de datos:** los mismos que los Dummy.
  - **Escenarios de falla** con la cabecera `X-Sim-Scenario`:
    - `error500`: responde 500 con `Problem`;
    - `timeout`: tarda 30 s;
    - `lento`: tarda 3 s;
    - `429`: responde con `Retry-After`.
  - **Contratos:** `/contracts/{nombre}` sirve los YAML desde una ruta explícita, `Simulator:ContractsPath`. Su valor por defecto se calcula a partir de `AppContext.BaseDirectory`, subiendo hasta la raíz del repo y bajando a `docs/integraciones/contratos`. Se puede sobrescribir con una variable de entorno.
  - **Al final del archivo:** `public partial class Program;`, para `WebApplicationFactory<Program>`.
- **File**: `backend/HapagPortal.sln`. Agregar el proyecto en una carpeta de solución nueva `tools`. El Dockerfile no se ve afectado.

#### 2. Clientes Real
- **File**: `backend/src/HapagPortal.Infrastructure/HapagPortal.Infrastructure.csproj`. Agregar `Microsoft.Extensions.Http.Resilience` 9.x.
- **Clientes nuevos**, en `backend/src/HapagPortal.Infrastructure/Integrations/`:
  - `Nexus/HttpNexusClient.cs`: implementa los 4 puertos Nexus;
  - `Fis/HttpShipmentSource.cs`;
  - `Payments/HttpKhipuPaymentProvider.cs` y `Payments/HttpBancoChilePaymentProvider.cs`: `VerifiesNotifications = true`;
  - `DbNet/HttpInvoiceProvider.cs`;
  - `Tracking/HttpTrackingProvider.cs`.
- **Comportamiento común:**
  - respuestas 5xx, timeout y circuito abierto se traducen a `DomainErrors.Integration.*`;
  - 404 se traduce a `Success(null)`;
  - la API key sale de `ISecretResolver`.
- **Registro con `Mode=Real`** (en `DI.Integrations.Partial.cs`):
  ```csharp
  services.AddHttpClient<HttpNexusClient>(c => c.BaseAddress = new Uri(baseUrl))
      .AddHttpMessageHandler<IntegrationLoggingHandler>()
      .AddStandardResilienceHandler(o =>
      {
          o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);           // Integrations:<S>:TimeoutSeconds (10)
          o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(totalTimeoutSeconds); // Integrations:<S>:TotalTimeoutSeconds (30)
          o.Retry.MaxRetryAttempts = 3;
          o.Retry.BackoffType = DelayBackoffType.Exponential;
          o.CircuitBreaker.MinimumThroughput = 5;
          o.CircuitBreaker.FailureRatio = 0.5;
          o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(30, 2 * timeoutSeconds));
          o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
      });
  ```
  - El patrón se repite para cada cliente. `o.Retry.Delay = TimeSpan.FromMilliseconds(retryBaseDelayMs)` con la clave `Integrations:<S>:RetryBaseDelayMs` (2000 por defecto; 50 en `HapagPortal.IntegrationTests`).
  - `BaseUrl` se normaliza para terminar en `/` y los clientes usan rutas relativas sin `/` inicial (si no, `http://localhost/nexus` + `/exemptions` pierde el prefijo).
  - Los proveedores de pago Real se exponen como servicios con clave: `services.AddKeyedTransient<IPaymentProvider>("Khipu", (sp, _) => sp.GetRequiredService<HttpKhipuPaymentProvider>())`, y lo mismo para `"BancoChile"`, para que `[FromKeyedServices]` resuelva en `Mode=Real`.
  - Santander, Bci, Signature y Storage mantienen el fail-fast.
  - `IntegrationsRegistrationTests.cs` (de 6b) se actualiza: con `Mode=Real`, Nexus, Fis, Khipu, BancoChile, DbNet y Tracking resuelven su cliente Real; Santander, Bci, Signature y Storage siguen lanzando la excepción de fail-fast.
- **File**: `backend/src/HapagPortal.Infrastructure/Integrations/IntegrationLoggingHandler.cs` (nuevo, NF-27). Es un `DelegatingHandler` que:
  - propaga o genera `X-Correlation-Id`;
  - registra en Warning los campos `System`, `Operation`, `StatusCode`, `DurationMs` y `CorrelationId` cuando la respuesta es ≥ 400 o hay excepción;
  - incrementa el contador `hapagportal.integrations.errors` (`System.Diagnostics.Metrics`) con la etiqueta `system`.

#### 3. Webhooks

**Khipu** (`KhipuWebhookCommandHandler.cs`)
- El handler recibe `[FromKeyedServices("Khipu")] IPaymentProvider`.
- **Con `VerifiesNotifications == false`** (Dummy, es decir `Integrations:Khipu:Mode=Dummy`), el comportamiento es **exactamente el de hoy**: secreto compartido, estado tomado del cuerpo e idempotencia.
- **Con `true`** (Real), después del chequeo de secreto:
  1. llama a `VerifyNotificationAsync(request.NotificationToken, request.ExternalReference)`;
  2. si falla, devuelve `Error.Unauthorized` sin cambios;
  3. exige que `ExternalReference` coincida y que el monto sea igual a `payment.Amount`; si no, devuelve `Error.Unauthorized` sin cambios;
  4. el nuevo estado se toma de la verificación, no del cuerpo.
- **Pruebas:**
  - `TestHelpers/FakePaymentProvider.cs` (nuevo), con el modelo de `FakeCustomsTransmitter.cs`. Permite configurar `VerifiesNotifications` y la verificación.
  - Casos nuevos en `KhipuWebhookCommandHandlerTests.cs`:
    - Dummy con `done`: el pago queda `Confirmed`;
    - Dummy con `rejected`: el pago queda `Failed` y **no** se confirma;
    - Real con monto distinto: `Unauthorized` y el pago no cambia.
  - Los casos existentes se mantienen, usando el fake en modo Dummy.

**Banco de Chile**
- `IWebhookAuthenticator.cs` agrega `bool IsValidSignature(string provider, string rawBody, string? signature)`.
- `WebhookAuthenticator.cs` lo implementa con HMAC-SHA256 y `FixedTimeEquals`. La clave sale de la configuración `Payments:Webhooks:{provider}:SigningKey`, igual que hoy `Secret`. Si está vacía, rechaza.
- En `Program.cs`, un middleware previo a `MapControllers` llama a `Request.EnableBuffering()` cuando la ruta empieza por `/api/v1/payments/webhook`.
- `PaymentsController.cs` (l.97-112) lee el cuerpo crudo y `X-Signature` y los pasa en el comando.
- `BancoChileWebhookCommandHandler.cs` exige el secreto y la firma.
- Se actualizan las pruebas.
- **Configuración:** `appsettings*.json` agrega `"Payments": { "Webhooks": { "BancoChile": { "SigningKey": "" } } }`.

#### 4. HTTPS/HSTS
En `backend/src/HapagPortal.WebApi/Program.cs`:
- `builder.Services.Configure<ForwardedHeadersOptions>(o => { o.ForwardedHeaders = XForwardedFor | XForwardedProto; o.KnownNetworks.Clear(); o.KnownProxies.Clear(); })`.
  - Limpiar las listas significa confiar en cualquier proxy que envíe `X-Forwarded-*`. Es aceptable porque el contenedor solo es accesible a través del proxy de Railway.
  - Debe revisarse si el servicio pasa a estar expuesto directamente.
- `builder.Services.Configure<HttpsRedirectionOptions>(o => o.HttpsPort = 443)`.
- `app.UseForwardedHeaders()` como primer middleware.
- Si `!IsDevelopment()` y `Security:EnforceHttps` (por defecto `true`):
  - `app.UseHsts()`;
  - `app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/health"), b => b.UseHttpsRedirection())`.

#### 5. Pruebas de integración y de contrato

**Proyecto nuevo:** `backend/tests/HapagPortal.IntegrationTests/HapagPortal.IntegrationTests.csproj`.
- Paquetes: xUnit, FluentAssertions 7.2.0 y NSubstitute, con las mismas versiones que el resto, más `Microsoft.AspNetCore.Mvc.Testing` 9.0.x.
- Referencias: Infrastructure y el simulador.
- Se agrega a `HapagPortal.sln`.

**Construcción de cada prueba**
- Se crea una `ServiceCollection` real con `AddLogging()` y `AddIntegrations(config)`, con `Integrations:<S>:Mode=Real`, `BaseUrl=http://localhost/<prefijo>`, y `TimeoutSeconds=1`/`TotalTimeoutSeconds=5` en las pruebas de tiempo.
- `ISecretResolver` se reemplaza con NSubstitute.
- Para cada cliente: `services.AddHttpClient(typeof(HttpNexusClient).Name).ConfigurePrimaryHttpMessageHandler(() => factory.Server.CreateHandler())`, donde `factory` es el `WebApplicationFactory<Program>` del simulador. Así la tubería real (logging + resiliencia) apunta al simulador en memoria.

**Casos:**
- datos: exento, crédito, FFWW y tipo de cambio;
- `error500`: después de 3 reintentos, `Integration.Unavailable`;
- `timeout`: `Integration.Timeout`, terminando dentro de `TotalRequestTimeout` (5 s) más un margen de 2 s;
- 5 fallas en la ventana abren el circuito y la llamada siguiente falla sin llegar al simulador;
- `lento`: éxito;
- `429`: se reintenta;
- se emite el log de NF-27.

**CI:** job nuevo `contracts` en `.github/workflows/pr-tests.yml`:
- checkout, `setup-dotnet` 9.0.x y `setup-python` 3.12;
- `pip install -r docs/integraciones/requirements.txt`, que ahora incluye `schemathesis` 4.x;
- `python scripts/requerimientos/verificar_contratos.py`;
- `openapi-spec-validator` sobre cada contrato;
- `dotnet run --project backend/tools/HapagPortal.IntegrationSimulator --urls http://localhost:5199 &`, esperando a que responda `/contracts/nexus`;
- para los 6 contratos implementados: `schemathesis run docs/integraciones/contratos/<x>.openapi.yaml --url http://localhost:5199/<prefijo> --checks not_a_server_error,status_code_conformance,content_type_conformance,response_schema_conformance`.

#### 6. Checklist
**File**: `docs/integraciones/checklist-dummy-a-real.md` (nuevo). Pasos por sistema:
1. contrato validado: `x-estado` pasa de PROPUESTA a Validado;
2. secreto cargado;
3. `BaseUrl` y timeouts configurados;
4. prueba en sandbox;
5. alerta sobre `hapagportal.integrations.errors`;
6. vuelta atrás definida (`Mode=Dummy`);
7. aprobación del responsable.

### Success Criteria:

#### Automated Verification:
- [ ] `dotnet build backend/HapagPortal.sln --configuration Release`
- [ ] `dotnet test backend/HapagPortal.sln --configuration Release` (incluye `HapagPortal.IntegrationTests` y los casos de webhook)
- [ ] `python scripts/requerimientos/verificar_contratos.py`
- [ ] `for f in docs/integraciones/contratos/*.openapi.yaml; do openapi-spec-validator "$f" || exit 1; done`
- [ ] Con el simulador corriendo: `schemathesis run docs/integraciones/contratos/nexus.openapi.yaml --url http://localhost:5199/nexus --checks not_a_server_error,status_code_conformance,content_type_conformance,response_schema_conformance`, y lo mismo para fis, khipu, banco-chile, dbnet y tracking
- [ ] `dotnet test backend/tests/HapagPortal.ArchitectureTests/HapagPortal.ArchitectureTests.csproj`

#### Manual Verification:
- [ ] En staging, `/health` responde 200 sin bucle de redirección y las respuestas HTTPS traen `Strict-Transport-Security`
- [ ] Con `Mode=Real` contra el simulador local y `X-Sim-Scenario: error500`, aparece el log estructurado de NF-27
- [ ] Webhook de Banco de Chile: firma incorrecta da 401 y firma correcta da 200
- [ ] Webhook de Khipu en staging (Dummy): el comportamiento no cambia respecto de hoy

### Rollback
- **HTTPS:** `Security__EnforceHttps=false` lo desactiva sin redesplegar. Los navegadores conservan HSTS durante su `max-age` (30 días por defecto), así que conviene usar uno corto antes de producción.
- **Integraciones:** `Integrations__<S>__Mode=Dummy`.
- **Webhooks:** revertir el commit, o usar `Payments:Webhooks:Enabled=false`.
- **Todo:** `git revert` del merge.

**Implementation Note**: pausar para confirmación manual.

---

## Fase 1: Especificación v4.0

**Rama**: `feature/especificacion-v4` (depende de 0 y 6a) · **Fechas**: 28-10-2026 → 02-11-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Katu, Kari · **Salida**: `katu\v4\Portal_2.0_Especificacion_Funcional_v4.docx`

### Changes Required:

#### 1. Script de edición
**File**: `scripts/requerimientos/editar_spec_v4.py` (nuevo). Parte de `…validada.docx`; la copia `(1)` es idéntica. Pasos en orden:

**Portada y defectos (pasos 1–10)**
1. **Portada:** DC5. La versión pasa a 4.0.
2. **D1:** los 12 `Heading 2` de texto corrido de los capítulos 1–2 pasan a `Normal`. Las viñetas "- …" pasan a `List Paragraph`. Antes se comprueba con `list_tc_fields` que ninguno tenga campo TC; si alguno lo tiene, se elimina ese campo.
3. **D2:** "4.2 Registro y administración de la organización" pasa a `Heading 2`, y se le inserta `insert_tc_field(p, "4.2  Registro y administración de la organización", 2)` si no tiene campo.
4. **D3:** el bloque de M8-08 se mueve al final de M8, antes de "M9. Reportería y trazabilidad".
5. **D4:** se aplica Q2. Se quitan M9-02, M9-03 y las tres marcas "[Validación pendiente…]".
6. **D5:** en el cierre de M7, "M7-03 Historial de pagos y boletas" pasa a "M7-02".
7. **D6:** se eliminan la nota "Agregar Funcionaliad…" del inicio de M8 y la nota de opinión de M3-17.
8. **D7 y D8:** se aplica Q1 en M3-02, M6-02, M3-17, M5-07 y M6-07.
9. **D9:** se eliminan las leyendas sin imagen. Una leyenda es huérfana si es un párrafo `Normal` al cierre de una ficha sin `w:drawing` adyacente. El script imprime la lista.
10. **D10:** en M8-05 se mueve el contenido de "Situación actual" a "Requerimiento". La situación actual se reescribe según `frontend/src/app/app.routes.ts:118-164`.

**Proveedor anterior (paso 11)**

11. Cambios por ubicación:
    - §1 "Dependencias relevantes": "…validación técnica del equipo de desarrollo del Portal 2.0 de Hapag-Lloyd".
    - M3-17: el WS lo desarrolla y administra Hapag-Lloyd.
    - M7-04: "validación técnica del equipo de desarrollo del Portal 2.0".
    - Las demás apariciones de "proveedor" se tratan según DC6, salvo las frases permitidas.

**Contenido nuevo (pasos 12–13)**

12. **Capítulo M11 (DC1).** Nuevo `Heading 1` con el texto literal "14.   M11. Interfaz, accesibilidad e idiomas – FASE 1" y `insert_tc_field(…, 1)`. Las fichas son `Heading 3`, sin campo TC, porque hoy las fichas no lo tienen (el TOC solo cubre niveles 1–2). Siguen la estructura de §2.2:
    - M11-01 Sistema visual y tokens de diseño;
    - M11-02 Selector de idioma ES/EN en caliente;
    - M11-03 Formatos locales por país;
    - M11-04 Conformidad WCAG 2.2 AA;
    - M11-05 Operación por teclado y foco visible;
    - M11-06 Anuncios dinámicos (`aria-live`) en pagos, cargas y errores;
    - M11-07 Tema claro/oscuro y reducción de movimiento;
    - M11-08 Navegadores y dispositivos soportados.
13. **Fichas Nexus (Q7):**
    - M2-10 "Consulta de plazos documentales por nave – FASE 2", después de M2-09;
    - M8-09 "Counter Bolivia/Ultramar – FASE 2", después de M8-08;
    - en el capítulo de integración, la frase "Errores de facturación permanece en Nexus".

**Renumeración (paso 14)**

14. **DC1.**
    - Se reescribe el número literal de los títulos: "14." pasa a "15.", "15." a "16.", "16." a "17.", y las subsecciones "15.x" pasan a "16.x".
    - Con `update_tc_fields` se reescribe el texto de los campos TC afectados.
    - Se reemplazan las referencias de texto: "capítulo 14" pasa a "capítulo 15", y "capítulos 4 a 13" pasa a "4 a 14".

**Reescritura (pasos 15–16)**

15. **NF reescritos:**
    - **NF-20:** las 2 últimas versiones mayores de Chrome, Edge, Firefox, Safari (macOS), Safari iOS y Chrome Android. La Fase 5a copia este listado a `.browserslistrc`. Se muestra un aviso en navegadores no soportados.
    - **NF-21:** anchos de 360, 768, 1024 y ≥1280 px. Consulta y pago funcionan en móvil y hay reflow a 320 px (WCAG 1.4.10).
    - **NF-22:** DC3.
    - **NF-23:** ES/EN según M11-02 y M11-03.
16. **Capítulo 15, integración**, reescrito:
    - APIs de Hapag-Lloyd, sin acceso directo a BD, con FIS y Data Lake por API (Q3);
    - entrada transitoria de FIS por `POST bills-of-lading/import` (Q6);
    - inventario en `docs/integraciones/`, ya publicado en la Fase 6a;
    - puertos, Dummy, simulador y Real, con `Integrations:<Sistema>:Mode`;
    - resiliencia;
    - NF-27.

**Cierre (pasos 17–20)**

17. **Cifras:**
    - 107 RF en 11 módulos: Fase 1 72, Fase 2 31, Fase 0 4;
    - 27 NF;
    - 134 fichas en total;
    - "diez módulos" pasa a "once".
18. **Anexo A:** las fichas M11-xx, M2-10 y M8-09 dicen "Sin US: requerimiento adicional (v4)".
19. **Historial de cambios:** subsección nueva en §2, "2.4  Historial de cambios", con su campo TC de nivel 2. Tiene una fila por cada defecto, M11, ficha Nexus y decisión.
20. **Guardado:** se guarda con python-docx como `_v4`. Después, `set_update_fields_on_open()`. **No se pasa por LibreOffice.**

#### 2. Verificador
**File**: `scripts/requerimientos/verificar_spec.py` (nuevo). Comprueba:
- **Fichas:**
  - `Heading 3` == 134;
  - por prefijo: M1 27, M2 10, M3 19, M4 4, M5 10, M6 9, M7 4, M8 9, M9 1, M10 6, M11 8, NF 27;
  - en RF: F1 72, F2 31, F0 4.
- **Estructura:**
  - ningún `Heading 2` supera los 120 caracteres;
  - "4.2 Registro…" es `Heading 2`;
  - M8-08 está dentro de M8.
- **Texto:**
  - 0 apariciones de "M9-02", "M9-03" y "[Validación pendiente";
  - 0 términos prohibidos;
  - "proveedor" solo aparece en las frases permitidas.
- **Campos TC** (con `list_tc_fields`):
  - nivel 1 en orden "1." … "17." más "Anexo A";
  - el texto de cada campo coincide con el texto literal de su título;
  - existen los campos de "14.   M11…", "4.2" y "2.4".
- **settings.xml** contiene `w:updateFields w:val="true"`.

### Success Criteria:

#### Automated Verification:
- [ ] Generación: `python scripts/requerimientos/editar_spec_v4.py`; existe `C:\Users\klaze\Desktop\katu\v4\Portal_2.0_Especificacion_Funcional_v4.docx`
- [ ] Estructura, TC y términos: `python scripts/requerimientos/verificar_spec.py "C:\Users\klaze\Desktop\katu\v4\Portal_2.0_Especificacion_Funcional_v4.docx"`
- [ ] Originales intactos: `python scripts/requerimientos/verificar_originales.py`
- [ ] Render: `python scripts/requerimientos/render.py "C:\Users\klaze\Desktop\katu\v4\Portal_2.0_Especificacion_Funcional_v4.docx"`

#### Manual Verification:
- [ ] Al abrir en Word aparece el aviso de actualizar campos; tras aceptar, el índice muestra 17 capítulos más el Anexo A, sin aviso de reparación
- [ ] Los PNG de portada, índice y una ficha M11 se ven correctos
- [ ] Katu y Kari confirman que los textos reescritos de M3-02, M6-02, M3-17, M5-07, M6-07, M8-05, NF-20/21/22/23 y el capítulo 15 son coherentes

**Implementation Note**: pausar para confirmación manual.

---

## Fase 2: Documentos operativos v4

**Rama**: `feature/documentos-operativos-v4` (depende de 1 y 6a) · **Fechas**: 03-11-2026 → 05-11-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Lucho, Fer

**Salidas** en `katu\v4\` (3 archivos; no hay `Gantt_v4`):
- `Portal_2_0_Pendientes_v4.xlsx`
- `Funcionalidades NexusV2_v4.xlsx`
- `Comparacion_Requerimientos_Por_Fase_v4.xlsx`

### Changes Required:

#### 1. Pendientes_v4
**File**: `scripts/requerimientos/editar_operativos_v4.py` (nuevo), función `pendientes()`.

**Columnas nuevas:**
- "Área responsable";
- "Origen responsable": `RACI NexusV2` | `Plan Portal 2.0` | `Propuesta` | `Propuesta – sin nombre en fuentes`;
- "Ficha spec v4";
- "Contrato de integración".

**Responsables:** las 118 filas y las nuevas se llenan con la regla Q5.

**Tareas de nuestro desarrollo.** Es la lista cerrada `TAREAS_PLAN` del script.
- Solo estas tareas llevan "Fecha inicio" y "Fecha objetivo", y las toman de `config.CALENDARIO`.
- Llevan origen "Plan Portal 2.0".
- Las tareas "Validar contrato <ID> con <responsable>" **no** están en `TAREAS_PLAN`: las ejecutan los responsables de Hapag-Lloyd de la tabla de la Fase 6a (origen "Propuesta") y quedan "Sin fecha en fuentes", porque su plazo no depende del equipo de desarrollo. Al crearse, todos los contratos siguen en PROPUESTA.

| Tarea | Fase del plan | Fechas |
|---|---|---|
| 28 (API Dummy de Nexus) | 6b + 6c | 15-10-2026 → 27-10-2026 |
| Fichas M2-10 y M8-09 (redacción en la especificación v4) | 1 | 28-10-2026 → 02-11-2026 |
| M11-01 y M11-08 | 5a | 13-11-2026 → 17-11-2026 |
| M11-02 y M11-03 | 5b | 18-11-2026 → 24-11-2026 |
| M11-04, M11-05 y M11-06 | 5c | 25-11-2026 → 30-11-2026 |

**Fechas del resto:** quedan vacías, con el comentario "Sin fecha en fuentes". No se toma ninguna fecha del Gantt. Esto incluye:
- M11-07, con la nota "reducción de movimiento en 5c; selector de tema pendiente";
- la localización de correos, PDF y asistente;
- "Conmutar <sistema> Dummy→Real";
- las brechas N;
- el RACI pendiente.

**Tareas 102–106 (Macros):**
- responsable: `Macros/RPX – Diego (R); apoyo: Macros/RPX – RPX`, con origen "Propuesta";
- fechas vacías;
- comentario "Fuera del alcance del desarrollo Portal 2.0 (área Macros/RPX)".

**Reformulación:**
- **1–4:** el módulo pasa a "Transición y línea base".
- **9–12:** "Inventariar y migrar herramientas de desarrollo heredadas", sin nombres propios.
- **19–24:** "Base de datos (PostgreSQL)" (Q4).
- **37** (Q3): "Datos – Definir el modelo de consulta bajo demanda Portal→Nexus vía API (con caché), sin sincronización de tablas".
- **42–44** (Q3, Q6): "Validar la API FIS de organizaciones / datos / documentos (consulta bajo demanda vía `IShipmentSource`; entrada transitoria por `POST bills-of-lading/import`)".
- **28** (API Dummy de Nexus): se vincula a las fases 6b y 6c en la columna "Contrato de integración" (`CT-NEXUS`), con el comentario "Cubierto por adaptadores Dummy (6b) y simulador HTTP (6c)".

**Tareas nuevas**, con ID desde 119:
- M11-01…08, el selector de tema y la localización de correos, PDF y asistente;
- brechas de cobertura de Fase 1 con estado N;
- RACI pendiente en NexusV2;
- fichas M2-10 y M8-09;
- "Validar contrato <ID> con <responsable>", una por contrato de la Fase 6a;
- "Conmutar <sistema> Dummy→Real", una por sistema.

**Hoja `Resumen`:**
- Se recalculan los totales.
- Se agregan conteos por área y por origen.
- El gráfico se **recrea** con `openpyxl.chart.BarChart`. Usa las mismas categorías de estado de `Resumen!$A$5:$A$9` y los valores recalculados de `$B$5:$B$9`, con título y ubicación equivalentes, porque openpyxl no conserva gráficos existentes al guardar.

#### 2. NexusV2_v4
- **RACI de las 5 filas vacías**, todas con marca "Propuesta" y con S Jorge, C Andrés, I Katu/Kari:

  | Función | R | A |
  |---|---|---|
  | Datos | Lucho | Jorge |
  | Cuenta Admin | Lucho | Jorge |
  | Cuenta CS | Cami/Mati | Kari |
  | Errores de facturación | Lucho | Fer |
  | Plazos documentales | Lucho | Cami/Mati |

- **Columnas nuevas:** "Criticidad Fase 1", "Ficha spec v4", "Estado en hapag-portal" (C/P/N con `file:line`) y "Contrato de integración".
- **Fila "Datos":** se aplica Q3.

#### 3. Comparacion_v4
- **Hojas eliminadas:** "Alcance POC", "Texto fuente" y "Evidencias" del sitio externo.
- **Títulos de hoja y de tabla:** "Requerimientos vs POC" pasa a "Requerimientos vs hapag-portal". Se reescriben las filas de nota 1–2 de cada hoja.
- **Resumen:**
  - la fila 2 cita `hapag-portal`, el commit de línea base y la fecha;
  - se elimina la URL externa;
  - la cabecera "Dentro/relacionado con POC" pasa a "Relacionado con código existente".
- **Columnas:**
  - "Existe en POC" pasa a "Cobertura hapag-portal (C/P/N)";
  - "Relación con POC solicitado" pasa a "Código relacionado";
  - "Pantalla revisada" pasa a "Archivo/pantalla en hapag-portal".
- **Filas** reconstruidas desde `Matriz_Fichas_Linea_Base_v4.xlsx` más las fichas nuevas (N).
- **Evidencias nuevas** con `file:line`.
- **Conteos y alertas** recalculados según Q1.

#### 4. Verificador
**File**: `scripts/requerimientos/verificar_operativos.py` (nuevo). Comprueba:
- **Pendientes_v4:**
  - IDs 1–118 conservados y al menos 118 filas;
  - 0 celdas vacías en "Responsable / apoyo" y en "Origen responsable";
  - "Origen responsable" solo toma los 4 valores permitidos; ninguno es "Gantt";
  - las tareas 37 y 42–44 no contienen "sincroniz";
  - las tareas 102–106 contienen "Fuera del alcance del desarrollo Portal 2.0 (área Macros/RPX)" y no tienen fechas;
  - las filas con fecha son exactamente las de `TAREAS_PLAN`, con fechas iguales a `config.CALENDARIO`; las demás dicen "Sin fecha en fuentes";
  - el zip contiene `xl/charts/`;
  - la hoja `Resumen` tiene un gráfico (`ws._charts`), que es la forma de comprobarlo al cargar con openpyxl.
- **NexusV2_v4:** 17 filas con R y A.
- **Comparacion_v4:**
  - no tiene las hojas eliminadas;
  - tiene 72 filas de Fase 1;
  - la regex `\bPOC\b` (sensible a mayúsculas, palabra completa) da 0 coincidencias en todas las celdas;
  - no aparece la URL externa.
- **No existe** `C:\Users\klaze\Desktop\katu\v4\Gantt_Macros_Extraccion_QA_v4.xlsx`.
- **Los 3 archivos:** 0 términos prohibidos.

### Success Criteria:

#### Automated Verification:
- [ ] Generación: `python scripts/requerimientos/editar_operativos_v4.py`; existen los 3 archivos `_v4`
- [ ] Verificación: `python scripts/requerimientos/verificar_operativos.py`
- [ ] Originales intactos: `python scripts/requerimientos/verificar_originales.py`

#### Manual Verification:
- [ ] Lucho y Fer revisan 15 responsables propuestos y la hoja Resumen con su gráfico recreado
- [ ] Las fechas de las tareas de `TAREAS_PLAN` coinciden con la sección "Calendario y responsables"
- [ ] 5 evidencias `file:line` al azar en Comparacion_v4 apuntan al código correcto

**Implementation Note**: pausar para confirmación manual.

---

## Fase 3: Documentos nuevos (Guía UI/a11y/i18n + Matriz de trazabilidad)

**Rama**: `feature/documentos-nuevos-v4` (depende de 1, 2 y 6c) · **Fechas**: 06-11-2026 → 10-11-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Katu

**Salidas**:
- `katu\v4\Guia_UI_Accesibilidad_i18n_v4.docx`
- `docs/requerimientos/guia-ui-a11y-i18n.md` (nuevo)
- `katu\v4\Matriz_Trazabilidad_v4.docx` (Word, sin xlsx)
- `docs/requerimientos/matriz-trazabilidad.md` (nuevo)

### Changes Required:

#### 1. Guía UI/a11y/i18n
- **File**: `scripts/requerimientos/generar_guia.py` (nuevo). Genera el Markdown y el Word con python-docx. Secciones:
  1. **Tokens:** los valores de la Fase 5a §2, con su contraste y la variante oscura. `#ff6600` se reserva para logo y acentos no textuales.
  2. **Tipografía:** Inter y Montserrat.
  3. **Componentes y patrones:**
     - tablas con `scope` y `caption`;
     - formularios con `label for`, `aria-describedby` y resumen de errores;
     - feedback con `aria-live`;
     - estados NF-11: "No hay datos" con `role="status"` y "Servicio temporalmente no disponible" con `role="alert"` y Reintentar.
  4. **Checklist WCAG 2.2 AA:** una fila por cada uno de los 55 criterios A+AA. Columnas: ID, nombre, aplica, método (eslint / axe / teclado / NVDA / Lighthouse / revisión visual) y pantalla.
  5. **Estrategia i18n con Transloco:**
     - claves `modulo.componente.elemento`;
     - archivos `public/i18n/{es,en}.json`;
     - formatos de Q8;
     - textos sin concatenar;
     - plurales con ICU;
     - paridad de claves en CI.
  6. **Glosario.**
- **File**: `scripts/requerimientos/wcag22_a_aa.json` (nuevo). Es la lista canónica de criterios: los 55 criterios A y AA transcritos de la Recomendación W3C *Web Content Accessibility Guidelines (WCAG) 2.2* (https://www.w3.org/TR/WCAG22/), sin 4.1.1. Campos: id, nombre, nivel.
- **File**: `scripts/requerimientos/verificar_guia.py` (nuevo). Comprueba:
  - las 6 secciones;
  - que el checklist cubre exactamente los ids de `wcag22_a_aa.json` (55) y que ningún método está vacío;
  - que los tokens coinciden con `frontend/src/styles/_tokens.scss` si existe, o con la tabla de la Fase 5a (en este orden, el archivo aún no existe);
  - que el `.md` y el `.docx` tienen los mismos títulos;
  - 0 términos prohibidos.

#### 2. Matriz de trazabilidad
Se genera una sola vez; los contratos y el código de 6a–6c ya existen.

- **File**: `scripts/requerimientos/generar_matriz.py` (nuevo).
  - **Fuentes:**
    - la especificación v4 (134 fichas);
    - Pendientes_v4 y NexusV2_v4;
    - los contratos de `docs/integraciones/contratos/`;
    - `backend/src`.

    No usa el Gantt.
  - **Formato:** el Word va en sección horizontal, con una tabla de 134 filas y fila de cabecera repetida.
  - **Columnas:**
    - Ficha;
    - Fase;
    - Tareas Pendientes_v4;
    - Función NexusV2;
    - Contrato (ID de la Fase 6a);
    - Estado contrato, leído de `info.x-estado` del contrato, o "Sin contrato";
    - Código hapag-portal, buscando en `backend/src` el puerto, el `Dummy*.cs` y el `Http*.cs` del contrato, o "—";
    - Cobertura.
  - Incluye una sección "Fecha y commit de generación".
- **File**: `scripts/requerimientos/verificar_matriz.py` (nuevo). Comprueba:
  - 134 filas;
  - cada ficha de Fase 1 tiene al menos una tarea;
  - los contratos citados existen en la tabla de la Fase 6a;
  - en las filas con contrato, "Estado contrato" empieza por "PROPUESTA" o por "Validado";
  - en las filas con contrato que tiene puerto, "Código" cita un `Dummy*.cs`. Si el contrato es CT-NEXUS, CT-FIS, CT-KHIPU, CT-BCH, CT-DBNET o CT-TRACK, cita además un `Http*.cs`;
  - el `.md` y el `.docx` tienen el mismo número de filas;
  - 0 términos prohibidos.

### Success Criteria:

#### Automated Verification:
- [ ] Generación de la Guía: `python scripts/requerimientos/generar_guia.py`; existen `C:\Users\klaze\Desktop\katu\v4\Guia_UI_Accesibilidad_i18n_v4.docx` y `docs/requerimientos/guia-ui-a11y-i18n.md`
- [ ] Verificación de la Guía: `python scripts/requerimientos/verificar_guia.py`
- [ ] Generación de la Matriz: `python scripts/requerimientos/generar_matriz.py`; existen `C:\Users\klaze\Desktop\katu\v4\Matriz_Trazabilidad_v4.docx` y `docs/requerimientos/matriz-trazabilidad.md`
- [ ] Verificación de la Matriz: `python scripts/requerimientos/verificar_matriz.py`
- [ ] Originales intactos: `python scripts/requerimientos/verificar_originales.py`
- [ ] Render: `python scripts/requerimientos/render.py "C:\Users\klaze\Desktop\katu\v4\Guia_UI_Accesibilidad_i18n_v4.docx"` y lo mismo para `Matriz_Trazabilidad_v4.docx`

#### Manual Verification:
- [ ] Katu revisa la legibilidad de la Guía
- [ ] Katu confirma que la trazabilidad de 5 fichas de Fase 1 está completa

**Implementation Note**: pausar para confirmación manual.

---

## Fase 4: Prototipo navegable

**Rama**: `feature/prototipo-navegable` (depende de 3) · **Fechas**: 11-11-2026 → 12-11-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Katu, Cami/Mati

**Salidas**:
- `docs/prototipo/portal-2.0-prototipo.html` (nuevo)
- `katu\v4\Prototipo_Portal_2.0_v4.html`

El Artifact lo publica la sesión que orquesta el plan. La URL se anota en `docs/requerimientos/registro-decisiones-v4.md` (sección "Entregables publicados") y en la descripción del PR de la Fase 4. La Guía de la Fase 3 no se vuelve a generar.

### Changes Required:
- **File**: `docs/prototipo/portal-2.0-prototipo.html` (nuevo). Un solo archivo:
  - **Base:** Bootstrap 5.3 y Bootstrap Icons por CDN. Los tokens son variables CSS con la lista cerrada de nombres que fija este plan (Fase 5a §2): `--hl-dark`, `--hl-orange`, `--hl-orange-700`, `--hl-green`, `--hl-green-700`, `--hl-blue`, `--hl-light-gray`, `--hl-border`, `--hl-body-color`, `--hl-white`, `--hl-dark-bg`, `--hl-dark-text`, `--hl-surface-muted`, `--hl-text-muted` y `--hl-focus`. Las clases usan el prefijo `hl-`.
  - **Pantallas:** Dashboard, Listado de embarques, Detalle de BL y Carro por moneda (CLP/USD/BOB/EUR, con el pago anunciado por `aria-live`).
  - **Idioma:** ES/EN en caliente; actualiza `lang`.
  - **Tema:** `data-bs-theme` según `prefers-color-scheme`, con selector persistente.
  - **Accesibilidad:** skip-link, landmarks, `:focus-visible`, `prefers-reduced-motion` y estados de NF-11.
  - **Datos:** ficticios.
- **Files**: `scripts/requerimientos/prototipo/package.json`, `playwright.config.ts` (con `testDir: '.'`, para que la Fase 7 agregue su spec en la misma carpeta) y `prototipo.a11y.spec.ts` (nuevos). Recorren 4 pantallas × ES/EN × claro/oscuro y comprueban:
  - axe con los tags `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa` y `wcag22aa` da 0 violaciones;
  - el primer Tab enfoca el skip-link;
  - el cambio de idioma no recarga la página.
- **File**: `scripts/requerimientos/prototipo/.gitignore` (nuevo): `node_modules/`, `test-results/` y `playwright-report/`. El `package-lock.json` sí se versiona. `package.json` no declara `"type": "module"`, porque la spec de la Fase 7 usa `__dirname`.
- **File**: `scripts/requerimientos/verificar_prototipo.py` (nuevo). Comprueba las marcas obligatorias, la paridad del diccionario es/en y 0 términos prohibidos.

### Success Criteria:

#### Automated Verification:
- [ ] Existen `docs/prototipo/portal-2.0-prototipo.html` y `C:\Users\klaze\Desktop\katu\v4\Prototipo_Portal_2.0_v4.html`
- [ ] `python scripts/requerimientos/verificar_prototipo.py`
- [ ] `cd scripts/requerimientos/prototipo && npm install && npx playwright install chromium && npx playwright test`
- [ ] `git status --porcelain scripts/requerimientos/prototipo` no lista `node_modules/`, `test-results/` ni `playwright-report/`

#### Manual Verification:
- [ ] El Artifact publicado navega por las 4 pantallas
- [ ] Recorrido completo solo con teclado
- [ ] El tema oscuro es legible y no hay animaciones con reducción de movimiento activa
- [ ] Lighthouse ≥ 95 en las 4 pantallas: `npx lighthouse file:///C:/source/hapag-portal/docs/prototipo/portal-2.0-prototipo.html --only-categories=accessibility --chrome-path="<ruta de Chromium de Playwright>"`
- [ ] Katu y Cami/Mati validan el recorrido de Customer Service

**Implementation Note**: pausar para confirmación manual.

---

## Fase 5a: Herramientas de verificación + tokens frontend

**Rama**: `feature/frontend-lint-a11y-tokens` (depende de 0) · **Fechas**: 13-11-2026 → 17-11-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Andrés

### Changes Required:

#### 1. angular-eslint
- **Comando**: `cd frontend && npx ng add angular-eslint@21`. Se fija la 21 porque la 22 exige Angular 22. La versión exacta instalada se registra en `licencias-dependencias.md`.
  - Crea `frontend/eslint.config.js` (nuevo).
  - Agrega el target `lint` en `angular.json`.
- **File**: `frontend/eslint.config.js`:
  - `*.ts`: `angular.configs.tsRecommended`;
  - `*.html`: `angular.configs.templateRecommended` y `angular.configs.templateAccessibility`.
- **Severidad:** quedan en **warn** solo las reglas que hoy fallan, listadas explícitamente (por ejemplo `label-has-associated-control`, `elements-content`, `interactive-supports-focus`, `click-events-have-key-events`). **`table-scope` no va en la lista**: el `scope` de `th` lo verifican axe y la búsqueda de la Fase 5c. El resto queda en error.
- **Script**: `"lint": "ng lint"`.

#### 2. Tokens (DC4)
- **File**: `frontend/src/styles/_tokens.scss` (nuevo). **Solo variables Sass**, sin CSS emitido. Es el único archivo con hex:
  - `$hl-dark: #33424f`
  - `$hl-orange: #ff6600` (logo y acentos)
  - `$hl-orange-700: #b84a00`
  - `$hl-green: #009840`
  - `$hl-green-700: #007a33`
  - `$hl-blue: #004d6c`
  - `$hl-light-gray: #f5f7fa`
  - `$hl-border: #e2e8f0`
  - `$hl-body-color: #2d3748`
  - `$hl-white: #ffffff`
  - oscuros: `$hl-dark-bg: #1a202c`, `$hl-dark-text: #e2e8f0`
  - tonos semánticos: `$hl-surface-muted: #f8fafc` y `$hl-text-muted: #4a5568` (contraste 7,5:1 sobre blanco; reemplaza el `#718096`, que da 4,0:1).
  - Esta es la lista cerrada de nombres, la misma que usa el prototipo de la Fase 4. Los demás hex del inventario de los 22 archivos se mapean a uno de estos tokens; si alguno no encaja, se agrega aquí y en el prototipo en el mismo PR.
- **File**: `frontend/src/styles.scss`. Las líneas 1-37 se reemplazan por:
  ```scss
  @use 'styles/tokens' as t;
  @use 'bootstrap/scss/bootstrap' with (
    $primary: t.$hl-orange-700, $secondary: t.$hl-dark, $success: t.$hl-green-700, $info: t.$hl-blue,
    $body-bg: t.$hl-light-gray, $body-color: t.$hl-body-color,
    $font-family-sans-serif: ('Inter', system-ui, -apple-system, sans-serif),
    $headings-font-family: ('Montserrat', 'Inter', system-ui, sans-serif), $headings-font-weight: 600,
    $border-radius: 0.5rem, $border-radius-sm: 0.375rem, $border-radius-lg: 0.75rem,
    $card-border-radius: 0.75rem, $input-border-color: t.$hl-border,
    $input-focus-border-color: t.$hl-orange-700
  );
  ```
  - Luego se agrega `:root { --hl-*: #{t.$…}; --hl-focus: #{t.$hl-blue}; … }`, `.hl-navbar, .hl-sidebar { --hl-focus: #{t.$hl-white}; }` y `[data-bs-theme="dark"] { --hl-*: … }`.
  - Los 43 hex que quedan en `styles.scss` se reemplazan por `t.$…` o `var(--hl-…)`.
  - `$card-box-shadow` y `$input-focus-box-shadow` se pasan en el mismo `with`, usando `rgba(t.$…, …)`.
- **Plantillas y componentes con hex:**
  - `register.html`, `register.scss`
  - `country-badge.ts`
  - `login.scss`, `login.html`
  - `navbar.html`
  - `faq.scss`, `faq.html`
  - `dashboard.html`
  - `reset-password.ts`, `forgot-password.ts`
  - `local-charges.html`
  - `demurrage.html`
  - `warehouse.ts`
  - `service-orders.html`
  - `receipts.html`
  - `payment-list.html`, `payment-form.html`
  - `bl-list.html`, `bl-detail.scss`

  En los SVG del logo se usa la clase `.hl-logo__mark { fill: var(--hl-orange) }`. Excepción: `index.html` (`meta theme-color`).
- **File**: `frontend/scripts/check-hex.mjs` (nuevo). Busca `#[0-9a-fA-F]{3,8}\b` en `src/` (`.scss`/`.html`/`.ts`), excluye `_tokens.scss` e `index.html`, y termina con código 1 si encuentra alguno. Script `"check:hex"`.
- **File**: `frontend/scripts/check-bootstrap-theme.mjs` (nuevo). Lee los `dist/hapag-portal/browser/styles-*.css` y falla si no hay coincidencia con la regex `/--bs-primary:\s*#b84a00\b/i` o si la hay con `/--bs-primary:\s*#0d6efd\b/i` (el CSS compilado lleva espacio tras los dos puntos). Script `"check:theme"`.
- **File**: `frontend/.browserslistrc` (nuevo). Igual a NF-20:
  - `last 2 Chrome versions`
  - `last 2 Edge versions`
  - `last 2 Firefox versions`
  - `last 2 Safari major versions`
  - `last 2 iOS major versions`
  - `last 2 ChromeAndroid versions`

#### 3. Playwright + axe
- **Comando**: `npm i -D @playwright/test @axe-core/playwright`.
- **File**: `frontend/playwright.config.ts` (nuevo):
  - `webServer: { command: 'npx ng serve --configuration development --port 4300 --open=false', url: 'http://localhost:4300', reuseExistingServer: !process.env.CI }`;
  - proyecto chromium;
  - `testDir: 'e2e'`.
- **File**: `frontend/e2e/fixtures/session.ts` (nuevo). Siembra `hl_token` y `hl_user` (rol USER, país CL), y opcionalmente `hl_lang`.
- **File**: `frontend/e2e/fixtures/api-mocks.ts` (nuevo). `page.route('**/api/v1/**')`:
  - BL, pagos, notificaciones y cliente se tipan con los modelos que existen (`bl.model.ts`, `payment.model.ts`, `notification.model.ts`, `client.model.ts`).
  - Recibos y cargos no tienen modelo en `core/models/`. Su forma se copia de la interfaz que declara el servicio o componente que los consume (`receipt.service.ts`, `features/local-charges`).
  - Cualquier otra ruta responde `[]`.
- **File**: `frontend/e2e/a11y/pantallas.a11y.spec.ts` (nuevo). Recorre `/login`, `/dashboard`, `/bills-of-lading`, `/bills-of-lading/HLCU0000001`, `/payments` y `/payments/new/<id>`, y aplica `AxeBuilder` con los tags WCAG 2.0/2.1/2.2 A/AA.
- **File**: `frontend/e2e/a11y/axe-baseline.json` (nuevo). Contiene los `rule.id` que hoy fallan, por pantalla. La prueba falla solo con reglas nuevas.
- **Script**: `"test:a11y": "playwright test"`.
- **File**: `frontend/.gitignore`: agregar `test-results/` y `playwright-report/`.

#### 4. CI
**File**: `.github/workflows/pr-tests.yml`, job `frontend`:
- `npx ng lint`
- `npm run check:hex`
- después del build: `npm run check:theme`, `npx playwright install --with-deps chromium` y `npx playwright test`
- en caso de fallo, publicar el artefacto `playwright-report`

### Success Criteria:

#### Automated Verification:
- [ ] `cd frontend && npx ng lint`
- [ ] `cd frontend && node scripts/check-hex.mjs`
- [ ] `cd frontend && npx ng build --configuration production`, sin pasar los budgets
- [ ] Overrides aplicados: `cd frontend && node scripts/check-bootstrap-theme.mjs`
- [ ] `cd frontend && npx playwright test`
- [ ] `dotnet build backend/HapagPortal.sln --configuration Release`

#### Manual Verification:
- [ ] Capturas antes y después de login, dashboard, listado de BL, detalle de BL y formulario de pago. Cambios esperados y anotados:
  - botones y enlaces primarios: del azul `#0d6efd` a `#b84a00`;
  - estados de éxito: a `#007a33`;
  - `secondary` e `info`: a `#33424f` y `#004d6c`;
  - radios de 0,5 rem (tarjetas 0,75 rem);
  - Inter en el cuerpo y Montserrat en los títulos;
  - fondo `#f5f7fa`;
  - anillo de foco de inputs `#b84a00`.

  Cualquier otra diferencia es una regresión.
- [ ] El job de CI muestra los pasos nuevos en verde
- [ ] Andrés valida el set de capturas

### Rollback
`git revert` del merge; no hay estado persistente. Si solo falla Playwright en CI, se puede quitar ese paso en un commit aparte.

**Implementation Note**: pausar para confirmación manual.

---

## Fase 5b: Capa i18n (Transloco)

**Rama**: `feature/frontend-i18n-transloco` (depende de 5a) · **Fechas**: 18-11-2026 → 24-11-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Andrés, Kari (glosario EN)

### Changes Required:

#### 1. Transloco
- **Comandos**: `npm i @jsverse/transloco` (versión cuyos `peerDependencies` acepten Angular 21) y `npm i -D @jsverse/transloco-keys-manager`.
- **Archivos nuevos**:
  - `frontend/src/app/core/i18n/transloco-loader.ts`: lee `/i18n/${lang}.json`.
  - `frontend/public/i18n/es.json` y `frontend/public/i18n/en.json`.
  - `frontend/transloco.config.ts`: `rootTranslationsPath: 'public/i18n/'`, `langs: ['es','en']`, `keysManager: { input: ['src/app'], output: 'public/i18n' }`. Lo usa keys-manager.
- **File**: `frontend/src/app/app.config.ts`:
  - `provideTransloco({ config: { availableLangs: ['es','en'], defaultLang: 'es', fallbackLang: 'es', reRenderOnLangChange: true, prodMode: !isDevMode() }, loader: TranslocoHttpLoader })`;
  - `{ provide: LOCALE_ID, useValue: 'es-CL' }`.
- **File**: `frontend/src/main.ts`: `registerLocaleData` para `es-CL` y `es-BO`.

#### 2. Idioma y locale
**File**: `frontend/src/app/core/services/locale.service.ts` (nuevo):
- signal `lang`, persistida en `hl_lang`;
- computed `locale`: `es-CL` | `es-BO` | `en`;
- `timeZone`;
- `setLang()`: llama a `setActiveLang` y actualiza `document.documentElement.lang`, sin recargar.

#### 3. Pipes de formato
`frontend/src/app/shared/pipes/hl-date.pipe.ts`, `hl-number.pipe.ts` y `hl-currency.pipe.ts` (nuevos):
- `pure: false`;
- usan `Intl.*`, porque admite husos IANA;
- guardan en caché los formateadores por `(locale, opciones)`;
- las fechas siguen Q8 y llevan `timeZoneName: 'short'`;
- los montos usan `currencyDisplay:'code'` y `getNumberOfCurrencyDigits`.

Se reemplazan los **24** usos de `number:'1.2-2'` y los patrones fijos de `date`.

#### 4. Extracción de textos
- Las plantillas de `features/**` y `shared/components/**` pasan a `transloco`, incluidos `placeholder`, `aria-label`, `title` y `alt`.
- `STATUS_LABEL_MAP` (`status-badge.ts:20-32`) pasa a `status.<code>`.
- Los mensajes en TS usan `translate()`.
- Los términos salen del glosario.
- Para extraer: `npx transloco-keys-manager extract`.
- **Selector ES/EN** en `navbar.html`: grupo de botones con `aria-pressed`.

#### 5. Verificadores y pruebas
- **File**: `frontend/scripts/check-i18n.mjs` (nuevo). Paridad de claves, sin valores vacíos y sin claves usadas que no existan. Script `"check:i18n"`.
- **File**: `frontend/scripts/check-hardcoded-text.mjs` (nuevo). No quedan textos ni atributos accesibles literales con letras. Las excepciones van en el mismo script. Script `"check:i18n-text"`.
- **File**: `frontend/e2e/a11y/pantallas.a11y.spec.ts` (modificado). Se **parametriza con `['es','en']`**: siembra `hl_lang` antes de navegar y comprueba `html[lang]` en cada pantalla.
- **File**: `frontend/e2e/fixtures/session.ts` (modificado). Recibe `lang`.
- **File**: `frontend/e2e/i18n/cambio-idioma.spec.ts` (nuevo). Comprueba:
  - el título cambia;
  - `html[lang="en"]`;
  - la marca `window.__noReload` se mantiene, es decir, no hubo recarga;
  - CLP se muestra sin decimales y USD con 2.
- **CI**: agregar `npm run check:i18n` y `npm run check:i18n-text`.
- **Licencias**: registrar las versiones en `licencias-dependencias.md`.

### Success Criteria:

#### Automated Verification:
- [ ] `cd frontend && npx ng build --configuration production`
- [ ] `cd frontend && npx ng lint`
- [ ] `cd frontend && node scripts/check-i18n.mjs`
- [ ] `cd frontend && node scripts/check-hardcoded-text.mjs`
- [ ] `cd frontend && npx playwright test` (axe en ES y EN, más el cambio de idioma)
- [ ] `cd frontend && node scripts/check-hex.mjs`

#### Manual Verification:
- [ ] La aplicación completa en EN no muestra textos en español
- [ ] Un usuario BO ve `dd/MM/yyyy`, BOB con 2 decimales y el huso de La Paz
- [ ] Kari valida que la terminología EN coincide con el glosario

### Rollback
`git revert` del merge; `hl_lang` se ignora. Mitigación temporal: `availableLangs: ['es']`.

**Implementation Note**: pausar para confirmación manual.

---

## Fase 5c: Base de accesibilidad

**Rama**: `feature/frontend-base-accesibilidad` (depende de 5b) · **Fechas**: 25-11-2026 → 30-11-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Andrés

Se construye sobre 5b ya mezclada. Todo texto o atributo accesible nuevo (`aria-label`, `alt`, `title`, captions, mensajes de estado) se escribe directamente como clave Transloco en `es.json` y `en.json`.

### Changes Required:

#### 1. Shell
- **File**: `frontend/src/app/app.html`:
  - skip-link `<a class="hl-skip-link" href="#contenido-principal">`;
  - `<header>` que envuelve `<app-navbar>`;
  - `<main id="contenido-principal" tabindex="-1">` en las dos ramas;
  - regiones `aria-live` polite y assertive con `visually-hidden`.
- **File**: `frontend/src/app/shared/components/navbar/navbar.html`:
  - hamburguesa con `type="button"`, nombre accesible, `aria-controls="hl-sidebar"` y `[attr.aria-expanded]`;
  - SVG decorativos con `aria-hidden="true"` y `focusable="false"`;
  - enlace del logo con nombre;
  - campana con nombre que incluye el conteo.
- **File**: `frontend/src/app/shared/components/sidebar/sidebar.html`: `id="hl-sidebar"`, `aria-label` en el `nav` y `ariaCurrentWhenActive="page"`.

#### 2. Anuncios
**File**: `frontend/src/app/core/services/live-announcer.service.ts` (nuevo, sin CDK). Se usa en `payment-form`, en la importación de BL (`features/admin`) y en los errores de envío.

#### 3. Estilos
En `frontend/src/styles.scss`:
- `.hl-skip-link`;
- `:focus-visible { outline: 3px solid var(--hl-focus); outline-offset: 2px; }`;
- `@media (prefers-reduced-motion: reduce)` con animaciones y transiciones a 0,01 ms y `scroll-behavior: auto`;
- tamaño mínimo de 24×24 px en botones de ícono (2.5.8).

#### 4. Plantillas
- **Tablas** (15 archivos con `<table>`): `scope="col"` en los `th` de cabecera y `<caption class="visually-hidden">`.
- **Formularios:** todos los `label` asociados con `for`/`id`, más `aria-describedby` y `aria-invalid`.
- **Íconos:** decorativos con `aria-hidden`; los botones de solo ícono con `aria-label`.

#### 5. NF-11
**File**: `frontend/src/app/shared/components/state-message/state-message.ts` (nuevo):
- `kind: 'empty' | 'error'`;
- `empty` usa `role="status"`;
- `error` usa `role="alert"` y botón Reintentar.

Se aplica en `dashboard`, `bl-list`, `bl-detail`, `payment-list` y `receipts`. HTTP 5xx o 0 muestra `error`; 200 con lista vacía muestra `empty`.

#### 6. Cierre de verificación
- **`frontend/eslint.config.js`:** las reglas en warn pasan a error.
- **`frontend/e2e/a11y/axe-baseline.json`:** queda `{}`.
- **`frontend/e2e/a11y/teclado.spec.ts`** (nuevo):
  - el skip-link es el primer foco;
  - Enter lleva a `main`;
  - la hamburguesa a 375 px alterna `aria-expanded`;
  - cada `th` de cabecera de las tablas visitadas tiene `scope`.
- **`frontend/scripts/check-table-scope.mjs`** (nuevo): en las plantillas, cada `<th` dentro de `<thead>` tiene `scope=`. Script `"check:table-scope"`, agregado a CI.

### Success Criteria:

#### Automated Verification:
- [ ] `cd frontend && npx ng lint` (accesibilidad en error)
- [ ] `cd frontend && node scripts/check-table-scope.mjs`
- [ ] `cd frontend && npx playwright test` (línea base vacía y pruebas de teclado)
- [ ] `cd frontend && npx ng build --configuration production`
- [ ] `cd frontend && node scripts/check-hex.mjs`
- [ ] `cd frontend && node scripts/check-i18n.mjs && node scripts/check-hardcoded-text.mjs`

#### Manual Verification:
- [ ] Recorrido con teclado y NVDA de login → listado → detalle → pago, según el checklist
- [ ] Lighthouse ≥ 95 en las 6 pantallas, con `ng serve` y el backend local con datos demo:
  - obtener la ruta de Chromium con `node -e "console.log(require('@playwright/test').chromium.executablePath())"`;
  - ejecutar `npx lighthouse http://localhost:4300/<ruta> --only-categories=accessibility --chrome-path="<ruta>"`;
  - en las rutas autenticadas, iniciar sesión antes en el mismo perfil, con `--chrome-flags="--user-data-dir=<perfil>"`.
- [ ] Con reducción de movimiento activa, el sidebar no se anima
- [ ] Con el backend detenido, el dashboard muestra el estado de error con Reintentar
- [ ] Andrés valida la evidencia de conformidad (Q9)

### Rollback
`git revert` del merge, o solo del commit que pasa las reglas a error.

**Implementation Note**: pausar para confirmación manual.

---

## Fase 7: Actualización de `docs/documentacion.html`

**Rama**: `feature/documentacion-portal-2-0` (depende de 3, 5c y 6c; reutiliza `scripts/requerimientos/prototipo/` de la Fase 4) · **Fechas**: 01-12-2026 → 02-12-2026 · **Ejecuta**: Equipo de desarrollo Portal 2.0 · **Valida**: Jorge

**Precondición.** `feature/plataforma-operativa-aduana` debe estar mezclada en `develop`, con los commits `4ff34bb`, `abad3b7` y `f49ea8a`.
- Al 05-10-2026, `origin/develop` tiene una versión anterior del archivo, de 4413 líneas.
- Si la versión que se edita no es la de 4452 líneas, el script falla por marcadores.

### Changes Required:

#### 1. Script de edición
**File**: `scripts/requerimientos/actualizar_documentacion.py` (nuevo). Lee y escribe `docs/documentacion.html` en UTF-8, sin cambiar los finales de línea.
- **Marcadores.** Trabaja **solo** con los textos exactos que se listan abajo, nunca con números de línea. Hay dos tipos de marcador:
  - **anclas únicas** (por ejemplo `id="t-adr"`, `data-bpmn="bpmn-carga"`, el comentario que precede al XML BPMN, el banner `/*! bpmn-js`, `function renderPanel`): deben aparecer exactamente una vez; si no, el script termina con código 1 sin escribir nada;
  - **terminadores relativos** (`</tbody>`, `</ul>`, `</script>`, `    });`): se buscan como la primera coincidencia después de su ancla, o dentro de la sección `t-adr`/`t-fuentes`, nunca en todo el archivo.
- **Idempotencia.** Si ya existe `id="t-integraciones"` y no queda ningún texto `data-bpmn`, imprime "ya aplicado" y termina con código 0.
- **Alcance.** Solo toca:
  - (a) la región de contenido, de `<main>` a `</main>`;
  - (b) los bloques BPMN y la librería bpmn-js;
  - (c) el código BPMN de la inicialización;
  - (d) la regla CSS `.bpmn`.

  El `<script>` de Mermaid (desde `<script>"use strict";var __esbuild_esm_mermaid_nm`) queda byte a byte igual.

**Eliminaciones exactas:**
1. **XML BPMN:** desde la línea `<!-- ===== BPMN XML embebido (con DI para render offline) ===== -->` hasta el tercer `</script>` siguiente inclusive. Es decir, los 3 `<script type="application/bpmn+xml">`, justo antes de `<!-- ===== Librerías inline (offline) ===== -->`.
2. **Librería bpmn-js:** desde la línea que empieza por `<script>/*! bpmn-js - bpmn-navigated-viewer v17.11.1` hasta su primer `</script>` inclusive.
3. **Inicialización:**
   - la línea `  var bpmnViewers = {};`;
   - en `renderPanel`, desde la línea que contiene `panel.querySelectorAll(".bpmn").forEach(function (el) {` hasta la primera línea `    });` posterior inclusive;
   - en el comentario `// Render perezoso por pestaña: flowchart/ER/BPMN necesitan…`, se reemplaza `flowchart/ER/BPMN` por `flowchart/ER`.
4. **CSS:** la línea `  .bpmn { height: 360px; width: 100%; }`.

#### 2. BPMN → Mermaid (panel Funcional)
- Las secciones se renombran: `f-bpmn-carga` → `f-flujo-carga`, `f-bpmn-aduana` → `f-flujo-aduana` y `f-bpmn-plazos` → `f-flujo-plazos`. Ningún enlace `#f-bpmn-*` apunta a ellas.
- Los `<h2>` "Proceso BPMN — …" pasan a "Proceso — …" y el `<p>` de cada sección se conserva.
- Cada `<div class="diagram"><div class="bpmn" data-bpmn="…"></div></div>` se reemplaza por `<div class="diagram"><pre class="mermaid">…</pre></div>`, según la convención de `f-modulos`.
- Los diagramas tienen los mismos pasos y salidas que el XML actual, que no tiene lanes:

```
flowchart LR
  s1(("BL recibido")) --> t1["Subir filas"] --> t2["Validar por fila"] --> g1{"¿Errores?"}
  g1 -- "No" --> t3["Confirmar carga"] --> e1(("Cargado"))
  g1 -- "Sí" --> t4["Corregir datos"] --> e2(("Rechazado"))
```
```
flowchart LR
  a_s(("Manifiesto listo")) --> a_h["Transmitir encabezado"] --> a_g1{"¿Aceptado?"}
  a_g1 -- "Sí" --> a_bl["Transmitir B/L"] --> a_e(("Manifiesto aceptado"))
  a_g1 -- "No" --> a_retry["Reintentar / Aclarar"] --> a_h
```
```
flowchart LR
  p_s(("Evento base")) --> p_calc["Calcular plazo"] --> p_g{"Estado"}
  p_g -- "OK" --> p_ok(("En plazo"))
  p_g -- "Riesgo" --> p_alert["Alertar (en riesgo)"] --> p_e2(("Notificado"))
  p_g -- "Vencido" --> p_esc["Escalar (vencido)"] --> p_e2
```

#### 3. Secciones nuevas
Todas son `<section id><h2>`. `buildNav` las agrega solas a la navegación lateral.

- **Funcional — `f-req-portal2`** "Requerimientos Portal 2.0 (v4)". Se inserta antes de `<section id="f-modulos">`. Contiene:
  - un resumen de la especificación v4: 134 fichas, 107 RF en 11 módulos (Fase 1 72, Fase 2 31, Fase 0 4) y 27 NF;
  - una tabla de módulos M1–M11, con M11 "Interfaz, accesibilidad e idiomas";
  - las fases;
  - los documentos v4 citados **por nombre de archivo, sin ruta local**: `Portal_2.0_Especificacion_Funcional_v4.docx`, `Portal_2_0_Pendientes_v4.xlsx`, `Funcionalidades NexusV2_v4.xlsx`, `Comparacion_Requerimientos_Por_Fase_v4.xlsx`, `Guia_UI_Accesibilidad_i18n_v4.docx`, `Matriz_Trazabilidad_v4.docx` y `Registro_Decisiones_v4.xlsx`;
  - enlaces relativos a `requerimientos/guia-ui-a11y-i18n.md` y `requerimientos/matriz-trazabilidad.md`.
- **Usuario — `u-idioma-teclado`** "Idioma y uso con teclado". Se inserta antes de `<section id="u-intro">`, es decir, después de `u-pantalla`. Explica:
  - el selector ES/EN de la barra superior: cambia sin recargar y recuerda la elección;
  - los formatos por país (fechas y montos según Q8);
  - el uso con teclado: Tab/Shift+Tab, el enlace "Saltar al contenido" como primer foco, Enter/Espacio, el menú hamburguesa y el foco visible.
- **Técnica — `t-integraciones`** "Integraciones: puertos, Dummy y Real". Se inserta antes de `<section id="t-api">`, después de `t-seq`. Contiene:
  - la tabla de los 10 puertos con sistema y contrato (CT-*);
  - el modo `Integrations:<Sistema>:Mode` (Dummy/Real) y el fail-fast;
  - el simulador `backend/tools/HapagPortal.IntegrationSimulator` con `X-Sim-Scenario`;
  - los parámetros de resiliencia;
  - el log y la métrica de NF-27;
  - enlaces relativos a `integraciones/README.md` y `integraciones/checklist-dummy-a-real.md`;
  - un diagrama `<pre class="mermaid">` `flowchart LR`: Handlers → Puertos → {Dummy, Clientes Real} → Simulador / sistema externo con contrato validado.
- **Técnica — `t-ui-i18n-a11y`** "UI, idiomas y accesibilidad". Se inserta antes de `<section id="t-seguridad">`. Contiene:
  - los tokens (`_tokens.scss`, colores corregidos DC4);
  - Transloco (`public/i18n/{es,en}.json`, `LocaleService`, pipes `hl-*`);
  - WCAG 2.2 AA y su evidencia (`ng lint`, axe en Playwright, `check:*`, Lighthouse);
  - un enlace a la Guía.

#### 4. ADR y fuentes
- **`t-adr`:** 3 filas nuevas antes de `</tbody>`:
  - Transloco para i18n en caliente;
  - adaptadores Dummy y simulador HTTP antes de los clientes Real;
  - diagramas solo con Mermaid (MIT), de código abierto; se retira el visor BPMN anterior por su licencia no OSI (el texto de la fila no nombra la librería).
- **`t-fuentes`:**
  - `<li>` nuevos antes de `</ul>`: WCAG 2.2 (https://www.w3.org/TR/WCAG22/), Transloco (https://jsverse.gitbook.io/transloco), resiliencia HTTP en .NET (https://learn.microsoft.com/dotnet/core/resilience/http-resilience), OpenAPI 3.1 (https://spec.openapis.org/oas/v3.1.0) y RFC 9457 (https://www.rfc-editor.org/rfc/rfc9457);
  - la nota `<div class="note">Diagramas Mermaid y BPMN incrustados…` se reemplaza por "Diagramas Mermaid 11.16.1 (MIT) incrustados <strong>inline</strong> (sin CDN) para que este archivo funcione con doble clic y sin conexión.".

#### 5. Verificador
**File**: `scripts/requerimientos/verificar_documentacion.py` (nuevo). Argumento `--antes <ref>` (por defecto `origin/develop`): lee la versión anterior con `git show <ref>:docs/documentacion.html`. Comprueba:
- 0 apariciones (sin distinguir mayúsculas) de `bpmn-js`, `bpmn.io`, `bpmnjs`, `data-bpmn`, `application/bpmn+xml` y `class="bpmn"` en todo el archivo (por eso la fila ADR y la nota de fuentes no nombran la librería retirada);
- que `f-req-portal2`, `u-idioma-teclado`, `t-ui-i18n-a11y` y `t-integraciones` existen una vez cada una, dentro del panel correcto (funcional, usuario, tecnica y tecnica), según `html.parser`;
- que `f-flujo-carga`, `f-flujo-aduana` y `f-flujo-plazos` contienen un `pre.mermaid` cada una;
- que el número de `<pre class="mermaid">` es el de la versión anterior + 4;
- que el SHA-256 del `<script>` de Mermaid es igual al de la versión anterior;
- que el tamaño en bytes es menor que el de la versión anterior;
- 0 términos prohibidos (`terminos.py`) en el texto de `<main>`.

#### 6. Prueba de render
**File**: `scripts/requerimientos/prototipo/documentacion.spec.ts` (nuevo), en la carpeta y con la configuración de la Fase 4:
- abre `pathToFileURL(path.resolve(__dirname, '../../../docs/documentacion.html')).href`;
- registra los `console` de tipo error y los `page.on('pageerror')`, porque `mermaid.run` devuelve una Promise y sus fallos llegan como rechazos no capturados;
- para cada pestaña `funcional`, `usuario` y `tecnica`:
  - hace clic en `.tabs button[data-tab=<tab>]`;
  - espera `.panel[data-tab=<tab>].active`;
  - con `expect.poll`, comprueba que cada `.panel[data-tab=<tab>] .mermaid` contiene un `svg`;
  - comprueba que ningún elemento dentro de `main` contiene "Syntax error" (el bundle de Mermaid trae ese texto en su código, así que no se busca en toda la página);
  - comprueba que `#sidenav` tiene un enlace a cada sección nueva de esa pestaña;
- al final, comprueba que no hubo errores de consola que empiecen por "Mermaid:" ni ningún `pageerror`.

#### 7. Licencias
`docs/requerimientos/licencias-dependencias.md`: en "Existentes", bpmn-js navigated-viewer 17.11.1 pasa a "Eliminada (Fase 7): reemplazada por Mermaid por licencia bpmn.io no OSI".

### Success Criteria:

#### Automated Verification:
- [ ] Edición: `python scripts/requerimientos/actualizar_documentacion.py`
- [ ] Idempotencia: una segunda ejecución de `python scripts/requerimientos/actualizar_documentacion.py` imprime "ya aplicado" y deja `git diff --stat` sin cambios adicionales
- [ ] Verificación: `python scripts/requerimientos/verificar_documentacion.py --antes $(git merge-base HEAD origin/develop)` (Git Bash; la versión de partida es la de `develop` al crear la rama, que la precondición de la Fase 0 garantiza en 4452 líneas)
- [ ] Render: `cd scripts/requerimientos/prototipo && npm ci && npx playwright install chromium && npx playwright test documentacion.spec.ts`
- [ ] Licencias: `python -c "t=open('docs/requerimientos/licencias-dependencias.md',encoding='utf-8').read();assert 'Eliminada (Fase 7)' in t"`

#### Manual Verification:
- [ ] Sin conexión y con doble clic en Edge o Chrome: las 3 pestañas muestran todos los diagramas y los 3 flowcharts nuevos se leen en tema claro y oscuro
- [ ] La navegación lateral lista las secciones nuevas y la búsqueda encuentra "Transloco" en la pestaña Técnica
- [ ] Jorge valida el contenido de `t-integraciones` y `t-ui-i18n-a11y`

### Rollback
`git revert` del merge. El archivo vuelve a la versión con bpmn-js; no hay otro estado.

**Implementation Note**: pausar para confirmación manual.

---

## Testing Strategy

### Unit Tests:
- **Dummy:** determinismo y escenarios por clave.
- **Registro:** Dummy por defecto, fail-fast y modo inválido.
- **`SecretTypesTests`.**
- **`IsValidSignature`:** firma válida, inválida, vacía y sin clave.
- **Khipu:**
  - Dummy con `done` confirma;
  - Dummy con `rejected` no confirma;
  - Real con verificación fallida o monto distinto da `Unauthorized` sin cambios;
  - estado terminal idempotente.
- **Banco de Chile:** secreto y firma.

### Integration Tests:
- **`HapagPortal.IntegrationTests`:** `AddIntegrations` real en modo Real contra el simulador en memoria (datos, 500, timeout, lento, 429, circuito) y log de NF-27.
- **Contratos:** schemathesis con los 4 checks, openapi-spec-validator y `verificar_contratos.py`.
- **Frontend (Playwright):** axe en 6 pantallas × ES/EN, cambio de idioma, teclado y `scope`.
- **Documentos:**
  - `verificar_*.py` y `verificar_originales.py`;
  - `verificar_operativos.py` comprueba que no existe `Gantt_v4` y que las fechas de Pendientes salen del calendario.
- **`documentacion.html`:** `verificar_documentacion.py` y `documentacion.spec.ts` (render Mermaid en las 3 pestañas, sin "Syntax error").

### Manual Testing Steps:
1. Recorrido con teclado y NVDA, de login a pago, en ES y EN.
2. Lighthouse Accesibilidad ≥ 95 en las 6 pantallas, con `npx lighthouse` sobre el Chromium de Playwright.
3. Con el backend detenido, verificar el estado de error distinto del de "sin datos".
4. Abrir la especificación v4 en Word: aceptar la actualización de campos y revisar el índice y las 134 fichas.
5. Revisar el prototipo en tema claro y oscuro, y con reducción de movimiento.
6. En staging: HSTS, `/health`, conmutación de modo y webhook Khipu sin cambios.
7. Revisar el set de capturas antes y después de la Fase 5a.
8. Abrir `docs/documentacion.html` sin conexión y recorrer las 3 pestañas en tema claro y oscuro.

## Performance Considerations

- **Pipes:** formateadores `Intl` en caché por `(locale, opciones)`, porque los pipes son `pure: false`.
- **Transloco:** solo carga el idioma activo.
- **Resiliencia:**
  - cada intento tiene un tope de `TimeoutSeconds` (10 s) y la llamada completa un tope de `TotalRequestTimeout` (30 s);
  - el circuit breaker usa `MinimumThroughput=5`, `FailureRatio=0.5`, `SamplingDuration=max(30 s, 2×AttemptTimeout)` (lo exige la validación del handler estándar) y `BreakDuration=15 s`.
- **Budgets:** los de `angular.json` se verifican en cada build de producción.
- **`documentacion.html`:** quitar bpmn-js (unos 194 KB) y el XML BPMN (unos 11 KB) reduce el archivo, que hoy pesa unos 3,8 MB.

## Migration Notes

**Base de datos**
- Sin migraciones EF.

**Configuración nueva**
- `Integrations:<Sistema>:{Mode,BaseUrl,TimeoutSeconds,TotalTimeoutSeconds,RetryBaseDelayMs}` para Nexus, Fis, Khipu, BancoChile, Santander, Bci, DbNet, Signature, Storage y Tracking. El valor por defecto de `Mode` es `Dummy`.
- `Payments:Webhooks:BancoChile:SigningKey`.
- `Security:EnforceHttps`.
- `Simulator:ContractsPath`, solo para el simulador.
- En Railway se configuran con doble guion bajo, por ejemplo `Integrations__Nexus__Mode`.

**SecretTypes nuevos**
- `NEXUS_API_KEY`, `FIS_API_KEY`, `KHIPU_RECEIVER_ID`, `KHIPU_SECRET`, `BANCOCHILE_API_KEY`, `SANTANDER_API_KEY`, `BCI_API_KEY`, `DBNET_API_KEY`, `TRACKING_API_KEY`, `SIGNER_CERTIFICATE` y `STORAGE_ACCESS_KEY`.
- Se cargan con `UpsertSecretCommand`; no cambia el esquema.

**Webhooks**
- **Khipu:** en Dummy no cambia nada. En Real verifica contra el proveedor el token, la referencia y el monto.
- **Banco de Chile:** desde la Fase 6c exige `X-Signature`. Hay que coordinar con el banco antes de habilitarlo.

**Cambio visual (DC4)**
- Hoy la UI se ve con el azul de Bootstrap. Después de la Fase 5a aplica la marca corregida. Conviene avisar a los usuarios de staging.

**CI**
- **Job nuevo `contracts`** (6c, el primero en entrar): `verificar_contratos.py`, openapi-spec-validator, simulador y schemathesis.
- **Job frontend:**
  - en 5a: `ng lint`, `check:hex`, `check:theme` y Playwright;
  - en 5b: `check:i18n` y `check:i18n-text`;
  - en 5c: `check:table-scope`.
- **Job backend:** compila y prueba los proyectos nuevos, porque están en la solución.
- La prueba `documentacion.spec.ts` (Fase 7) corre en local; no se agrega a CI.

**Frontend**
- Claves nuevas en `localStorage`: `hl_lang` y, solo en el prototipo, `hl_theme`.

**Documentos**
- Los originales de katu no cambian, incluido el Gantt, cuyo hash sigue en la línea base.
- Lo nuevo va en `katu\v4\` con sufijo `_v4`. No hay `Gantt_v4`.
- El `.docx` solo se guarda con python-docx.
- LibreOffice trabaja sobre copias en `v4\render\tmp\`.

**`docs/documentacion.html`**
- Pierde bpmn-js, el XML BPMN y el código `BpmnJS`, y los 3 procesos pasan a Mermaid.
- No hay estado persistente: la vuelta atrás es `git revert` del merge de la Fase 7.

## References

- **Research:** `C:\source\hapag-portal\thoughts\shared\research\2026-10-05-actualizacion-requerimientos-portal-2-0.md`
- **Fuente:** `C:\Users\klaze\Desktop\katu\`. El Gantt solo forma parte de la línea base de hashes.
- **Extractos:** `C:\Users\klaze\AppData\Local\Temp\claude\c--source-hapag-portal\64cf0a9c-73b3-4cd3-8b17-a01407f8e537\scratchpad\docs\*.txt`, `fichas.tsv` (sesión de research; el extractor se versiona como `scripts/requerimientos/extract.py` en la Fase 0)
- **Histórico:**
  - Research del 2026-08-22 (eliminado; resumido en el research del 2026-10-05, sección Historical Context)
  - `thoughts/shared/plans/2026-08-10-habilitar-terceros.md`
- **Lista WCAG:** W3C *WCAG 2.2*, https://www.w3.org/TR/WCAG22/ (transcrita en `scripts/requerimientos/wcag22_a_aa.json`)
- **Implementaciones de referencia:**
  - `backend/src/HapagPortal.Infrastructure/Customs/StubCustomsTransmitter.cs`
  - `backend/src/HapagPortal.Application/Common/Interfaces/IPaymentGatewayService.cs`
  - `backend/src/HapagPortal.Infrastructure/DependencyInjection/DI.Auth.Partial.cs`
  - `backend/src/HapagPortal.Infrastructure/DependencyInjection/DependencyInjection.cs:42-52`
  - `backend/src/HapagPortal.Infrastructure/Services/WebhookAuthenticator.cs`
  - `backend/src/HapagPortal.Application/Payments/Commands/Webhooks/KhipuWebhookCommandHandler.cs`
  - `backend/src/HapagPortal.WebApi/Controllers/V1/PaymentsController.cs:82-112`
  - `backend/tests/HapagPortal.UnitTests.Application/TestHelpers/FakeCustomsTransmitter.cs`
  - `backend/tests/HapagPortal.UnitTests.Domain/Constants/ChargeTypesTests.cs`
  - `backend/tests/HapagPortal.UnitTests.Infrastructure/Services/PaymentGatewayServiceTests.cs`
  - `frontend/src/styles.scss:1-37`
  - `frontend/src/app/app.html`
  - `frontend/src/app/shared/components/navbar/navbar.html:5-9`
  - `.github/workflows/pr-tests.yml`
- **`docs/documentacion.html`** (líneas en `HEAD` `f49ea8a`):
  - CSS `.bpmn` en la l.54;
  - convención Mermaid en `f-modulos` (l.94-111);
  - BPMN en las l.135, 141 y 147;
  - ADR en las l.616-622 y `t-fuentes` en las l.626-636;
  - XML BPMN en las l.642-753;
  - Mermaid en las l.756-4343 y bpmn-js en las l.4344-4366;
  - inicialización en las l.4369-4450 (`renderPanel` l.4388-4409, `buildNav` l.4425-4430).
- **Configuración Playwright reutilizada:** `scripts/requerimientos/prototipo/` (Fase 4).