---
title: 2026-10-05-actualizacion-requerimientos-portal-2-0
type: research
date: 2026-10-05
status: active
project: hapag-portal
scope: shared
author: brypenalozav-stack
ticket: null
tags: [research, portal-2.0, requerimientos, nexus, cobertura, i18n, accesibilidad]
related: [thoughts/shared/plans/2026-10-05-actualizacion-requerimientos-portal-2-0-ui-integraciones.md]
researcher: brypenalozav-stack
git_commit: f49ea8af9cace4140ba908d51f434e0daaf50861
branch: feature/plataforma-operativa-aduana
repository: hapag-portal
topic: "Actualización de requerimientos Portal 2.0 (Chile/Bolivia): 5 documentos depurados vs. código de hapag-portal"
last_updated: 2026-10-05
last_updated_by: brypenalozav-stack
---

# Research: Actualización de requerimientos Portal 2.0 — documentos depurados vs. código

**Git Commit**: f49ea8af9cace4140ba908d51f434e0daaf50861
**Branch**: feature/plataforma-operativa-aduana

## Research Question

Analizar los 5 documentos depurados del requerimiento ubicados en `C:\Users\klaze\Desktop\katu`:

- Especificación Funcional validada
- Pendientes
- Funcionalidades NexusV2
- Gantt de Macros (extracción/QA)
- Comparación con la prueba de concepto por fase

Además, cruzar las 124 fichas de la especificación contra el código real de `hapag-portal` (Angular 21 + .NET 9) para saber qué está construido. El portal nuevo es este repositorio, independiente de cualquier proveedor anterior. Hay requerimientos adicionales: interfaz, accesibilidad WCAG 2.2 AA y operación bilingüe ES/EN.

Este documento sirve de insumo para el plan que vendrá después (`/create_plan`). Esa etapa actualizará las fuentes existentes y creará los documentos nuevos.

## Summary

- **Fuente de verdad.** Son los 5 archivos de `Desktop\katu`, con fecha de fines de septiembre e inicios de octubre de 2026. Reemplazan a la versión v2.0 de `docs/` que analizó el research del 2026-08-22.
  - Las dos copias de la especificación validada que hay en la carpeta (`…validada.docx` y `…validada(1).docx`) tienen texto idéntico.
- **Especificación validada (v3.0, octubre 2026).**
  - Tiene 124 fichas: 97 funcionales y 27 no funcionales.
  - Las funcionales se reparten en 64 de Fase 1, 29 de Fase 2 y 4 de Fase 0. Hay 10 módulos funcionales (M1–M10), además del capítulo 14 (integración) y del capítulo 15 (no funcionales).
  - La portada dice "Preparado para Flowcraft" y el proveedor se nombra en 6 lugares.
  - El documento arrastra defectos de estructura y referencias rotas (ver la sección 1.1).
  - NF-23 fija "el portal debe presentarse en español". Ninguna ficha trata accesibilidad ni bilingüismo.
- **Cobertura del código.** Ninguna ficha funcional está construida completa.

  | Grupo | Construido | Parcial | No existe | Total |
  |---|---|---|---|---|
  | Fase 1 | 0 | 23 | 41 | 64 |
  | Fase 2 | 0 | 8 | 21 | 29 |
  | Fase 0 | 0 | 0 | 4 | 4 |
  | No funcionales | 1 (NF-08) | 18 | 8 | 27 |

  Lo construido corresponde a un portal de pagos por BL y a una consola interna de Aduana y plazos:
  - RBAC interno, pagos simulados con webhooks, cargos y demurrage almacenados, cambio de almacén y ODS.
  - Importación masiva de BL, transmisión a Aduana con stub, reglas de plazo, notificaciones, auditoría de solo lectura y reportes CSV.
- **Integraciones.** No existe ningún cliente HTTP hacia sistemas de Hapag-Lloyd, ni hacia Nexus, FIS, Data Lake, Navesoft, DBNet, Mercurio o TATC/Flagare. Tampoco hay una API dummy.
  - Los BL entran solo por importación de filas.
  - La pasarela de pago y el transmisor de Aduana son simulaciones en proceso.
- **Interfaz.**
  - Usa Bootstrap 5.3 con tokens SCSS propios.
  - No tiene capa de i18n (`lang="es"` fijo, 0 usos de `$localize`/`LOCALE_ID`) ni tema oscuro.
  - Su base de accesibilidad es mínima: 3 `aria-label`, 0 `aria-live`, sin skip-link, sin `scope` en tablas y sin `prefers-reduced-motion`.
- **Documentos operativos.**
  - **Pendientes**: 118 tareas en 20 módulos. La columna *Responsable / apoyo* está vacía en las 118.
  - **NexusV2**: 17 funciones con análisis de brecha. 5 filas no tienen RACI.
  - **Gantt**: 8 semanas desde el 07-09-2026, un catálogo FIS de 10 pantallas y un inventario de 8 archivos de macro con 11 servicios.
  - **Comparación**: mide contra un sitio externo de prueba de concepto, no contra este repositorio.

## Detailed Findings

### 1. Los 5 documentos depurados (`Desktop\katu`)

#### 1.1 Especificación Funcional validada (`Portal_2.0_Especificacion_Funcional_validada.docx`)

**Estructura.**
- Capítulos 1–3: resumen, cómo leer, catálogo de 52 servicios en 4 operaciones (CL-EXP 13, CL-IMP 14, BO-EXP 9, BO-IMP 16).
- Capítulos 4–13: módulos M1–M10.
- Capítulo 14: modelo de integración (Fase 0).
- Capítulo 15: 27 NF.
- Capítulo 16: glosario.
- Anexo A: trazabilidad de US-01 a US-90.

**Fichas por módulo:** M1 27, M2 9, M3 19, M4 4, M5 10, M6 9, M7 4, M8 8, M9 1, M10 6 y NF 27.

**Encabezados de fase tal como están escritos.** Como encabezado de ficha, "FASE 1" aparece 87 veces, contando las NF.

| Encabezado | Fichas |
|---|---|
| FASE 1 / REVISION | M5-02, M5-03, M6-03 |
| FASE 1 conexión NEXUS | M8-01 |
| FASE 2 / EN REVISIÓN | M3-10, M3-17, M3-19, M8-08 |
| FASE 0 / EN REVISIÓN | M7-04 |

**Defectos de documento:**

| # | Defecto | Ubicación |
|---|---|---|
| D1 | 12 párrafos de texto corrido con estilo *Heading 2*, que ensucian el índice | Cap. 1 "Qué problema resuelve" y "Qué contempla el alcance" (10 párrafos); cap. 2, párrafos introductorios (2) |
| D2 | El título "4.2 Registro y administración de la organización" perdió su nivel; quedó como *Normal* | Inicio de M1-07 |
| D3 | La ficha **M8-08** (Impersonación) quedó dentro del capítulo 12 (M9), después de M9-01 | Cap. 12 |
| D4 | Se citan **M9-02 y M9-03**, que no tienen ficha; quedaron marcadas "[Validación pendiente]" | M9-01 (dependencias), NF-27 (requerimiento y criterio) |
| D5 | En la tabla de cierre de M7 dice "M7-03 Historial de pagos y boletas"; ese contenido corresponde a **M7-02** | Final de M7-04 |
| D6 | Notas sueltas en el cuerpo: "Agregar Funcionaliad para bloquear pagos por ciertos horarios" (inicio de M8) y la nota de opinión sobre la fase de M3-17 | Cap. 11; M3-17 |
| D7 | Inconsistencias de fase señaladas en el propio texto | M3-02 (encabezado Fase 1, texto "en su segunda fase"); M6-02 (Fase 2, texto "primera etapa sin pago"); M3-17 (Fase 2/en revisión vs. nota que propone Fase 0) |
| D8 | Dependencia de fase invertida | M5-07 y M6-07 (Fase 1) dependen de M7-03 (Fase 2) |
| D9 | Leyendas de imagen sin imagen asociada | Por ejemplo "con condición de crédito gestionan el pago de sus facturas." al cierre de M7-03 |
| D10 | Contenido de "Situación actual" ubicado fuera de lugar: describe el requerimiento | M8-05 |

**Referencias a un proveedor externo:**
- La portada: "Preparado para / Flowcraft".
- §1 "Dependencias relevantes" ("propuesta técnica de Flowcraft").
- M3-17 (3 menciones).
- M7-04 (dependencias).
- Además, la palabra "proveedor" aparece 21 veces, por ejemplo en NF-10, NF-13, NF-17, NF-18, NF-20 y NF-22 ("el proveedor debe indicar…") y en M6-01, M6-02 y M6-07 (firma electrónica).

**Idioma y accesibilidad:**
- NF-23 dice: "El portal debe presentarse en español, con formatos de fecha, número y moneda correspondientes al país seleccionado según M1-04".
- No hay ninguna mención de WCAG, accesibilidad, inglés ni idiomas.
- NF-20 (navegadores) y NF-21 (dispositivos) delegan los valores al proveedor.
- M1-01 pide consistencia visual sin criterios medibles.

**Capítulo 14:** la integración será mediante APIs provistas por Hapag-Lloyd, sin acceso directo a bases de datos. En cambio, §1 dice que para FIS y Data Lake "se contempla una sincronización entre bases de datos, sin uso de APIs". Las dos frases conviven en el documento.

#### 1.2 Pendientes (`Portal_2_0_Pendientes.xlsx`)

**Contenido.**
- Hoja "Pendientes Portal 2.0", consolidado al 29-09-2026, con 118 tareas.
- Columnas: ID, Módulo, Tarea, Responsable / apoyo, Dependencia, Estado, Tipo de pendiente, Prioridad, Fecha inicio, Fecha objetivo, Comentarios.
- Estado: 114 Pending, 2 In Progress (1 y 56) y 2 In Analysis (51 y 66).
- **La columna Responsable / apoyo está vacía en las 118 filas.** Fecha inicio y Fecha objetivo también están vacías.

**Módulos (20), con cantidad de tareas:**

| Módulo | Tareas | Módulo | Tareas |
|---|---|---|---|
| Nexus | 17 | Registro Clientes Bolivia | 14 |
| Pagos/DBNet | 9 | Notificaciones | 8 |
| Gate Out | 6 | Modelo Operativo | 6 |
| Supabase/Base de Datos | 6 | Integración FIS | 5 |
| Macros | 5 | Agencias de Aduana/FFW | 5 |
| Seguridad | 5 | Registro Clientes CL/BO | 5 |
| Arquitectura/Código | 4 | Herramientas Desarrollo | 4 |
| Credenciales/Accesos | 4 | POC/Flowcraft | 4 |
| Canje | 3 | Pagos/BCI | 3 |
| Registro Clientes Chile | 3 | Gate Out/EDS | 2 |

**Valores de Tipo de pendiente:** Definición, Hapag, Arquitectura, Desarrollo, Integración, Finanzas, Legal, Testing, Planificación y Seguridad. Hay tipos (Finanzas, Legal, Hapag) que apuntan a un área responsable, pero no hay nombres.

**Relación con el código:**
- El módulo "Supabase / Base de Datos" (tareas 19–24) parte del supuesto de que se usa Supabase. El backend usa PostgreSQL con EF Core (ver §2.6).
- El módulo "POC / Flowcraft" (1–4) y las tareas 9–12 ("herramientas utilizadas por Vlad") se refieren al proveedor anterior.
- Las tareas 102–106 (Macros) piden clasificar las macros en reutilizar / modificar / reemplazar / eliminar. Esa clasificación no existe en el Gantt.
- Faltan tareas para:
  - interfaz/UX, accesibilidad e i18n;
  - las funciones Nexus "Errores de facturación" y "Plazos documentales / Feriados", que NexusV2 sí lista;
  - las 3 áreas responsables que pide el usuario (Finanzas, Comercial, Customer Service) más RPX, que solo aparece en el Gantt.

#### 1.3 Funcionalidades NexusV2 (`Funcionalidades NexusV2.xlsx`)

**Contenido.**
- 17 filas: 16 funciones más "Plazos documentales / Feriados".
- Columnas: Función, Descripción, Información, Tabla, RACI (R/A/S/C/I), Comments, "Nexus hoy", "Objetos BD / integraciones actuales" y "Brecha vs. lo pedido".
- Personas que aparecen en el RACI: Lucho, Fer, Ricardo, Kari, Jorge, Andrés, Cami/Mati, Mati, Katu.

| Función Nexus | ¿Existe en Nexus? (según el documento) | RACI | Ficha de la especificación relacionada | ¿Hay código en hapag-portal? |
|---|---|---|---|---|
| Créditos | No existe (tabla nueva `credits`) | Completo | M4-03, M5-07, M8-02 | Entidad local `CreditClient` con CRUD de admin; sin lectura Nexus |
| Exenciones | Sí ("Exonerados"), solo Gate In y EDS | Completo | M4-01, M4-02, M3-01, M3-15 | Entidad local `DemurrageExemption` con CRUD; ningún flujo la consume |
| Confirmación Pago | Solo lectura (Consultas) | Completo | M5-06, NF-04 | Webhooks Khipu y Banco Chile; confirmación manual de Admin |
| Cambio de Almacén | Parcial (TATC + Plazos; Mercurio solo GET; sin PDF CLVAP) | Completo | M3-04, M3-05, M3-06 | Creación individual con monto; sin adjunto |
| Generar TATC | Automático fuera de Nexus (Ignis WS) | Completo | M2-09 | No existe |
| Generar CLD | Manual (TATC Bolivia) | Completo | M6-07, M3-16 | No existe |
| Conexión Portal | No hay API; hoy CSV por FTP hacia Navesoft | Completo | Cap. 14 | No existe |
| API Estado de Cuenta | No existe (solo vista por BL) | Completo | M7-03 | No existe |
| Datos | Decisión pendiente (push vs. consulta) | **Vacío** (solo I) | Cap. 14 | — |
| Cuenta Admin | Solo "Forzar TATC" | **Vacío** (solo I) | M8-06 | Rol Admin con todos los permisos (`PermissionResolver`) |
| Cuenta CS | Módulos de uso diario | **Vacío** | M8-05 | Roles internos Coordinador/Supervisor |
| TC | No está en Nexus; BOB automático en Facturación desde 2026-09-22 | Parcial (sin S/C) | M5-05 | `Currency.ExchangeRateToUSD` sembrado; sin lectura |
| Tarifa | Sin mantenedor; tarifas hardcodeadas en SP | Parcial | M8-01 | No existe |
| Counter (Bolivia/Ultramar) | No existe | Parcial | (sin ficha) | No existe |
| Errores de facturación | Existe (módulo Facturación) | **Vacío** | (sin ficha) | No existe |
| Plazos documentales / Feriados | Existe | **Vacío** | (sin ficha comercial) | Reglas de plazo de Aduana (`DeadlineRule`), no plazos documentales por nave |

Tres funciones de Nexus no tienen ficha en la especificación: Counter, Errores de facturación y Plazos documentales/Feriados. En Pendientes hay tarea para Counter (41), pero no para las otras dos.

#### 1.4 Gantt de Macros (`Gantt_Macros_Extraccion_QA.xlsx`)

**Hoja "Gantt".**
- 6 semanas de desarrollo más 2 de QA, del 07-09-2026 al 30-10-2026.
- Responsables: Diego, Jorge y RPX.
- Etapas: Análisis (S1), Desarrollo en 6 bandas (S2–S5), Integración (S6), QA (S7) y Cierre (S8).
- Las ventanas core son S8100, D1040, I3000, D1000 e I3100.

**Hoja "Catálogo FIS".** Lista 10 pantallas FIS2 con sus campos y output: D1040, I3000, S8100, S7230, D1000, I3100, E4842, H0020, V3100 y F3010. La columna "Libro" está vacía.

**Hoja "Inventario macros".** Tiene 8 archivos `.xlsm` y 11 asociaciones de servicio:
- Cambio Almacén, Collect, GateIn-EDS, IPO, Logistics Fee, On Demand Charges y Cambio Depósito Devolución.
- Portal Invoice, en 4 variantes: Comentarios, Exportación, MHD y Otros Cargos.

**Qué no tiene el documento.** No cruza macros contra servicios de Fase 1 ni las clasifica en reutilizar / modificar / reemplazar / eliminar. Tampoco asigna pantallas FIS a archivos de macro.

**Relación con el código.** No hay código en hapag-portal que consuma la salida de las macros ni datos de pantallas FIS (ver §2.6). Los campos FIS se pueden comparar con la entidad `BillOfLading` (ver §2.2):

| Campo FIS | Pantalla | ¿Está en `BillOfLading`? |
|---|---|---|
| `booking_number` | S8100/D1040 | No |
| `free_days` | F3010 | No; `FreeDays` existe en `DemurrageCharge` |
| `matchcode`, `tax_id` | H0020 | `Client.TaxId` sí; Match Code no existe |
| `depot_import/export` | E4842 | No |
| `revenue_*` | S7230 | No; hay `LocalCharge.ChargeType` como string libre |

#### 1.5 Comparación por fase (`Comparacion_POC_Requerimientos_Por_Fase.xlsx`)

**Hojas.** Resumen, Primera fase (64), Segunda fase (29), Fase 0 (4), No funcionales (27), Alcance POC (16 tareas en 4 áreas), Catálogo servicios (52), Texto fuente y Evidencias (34 EV).

**Contra qué mide.** Contra el sitio externo de la prueba de concepto (React + Tailwind), revisado el 03-10-2026 con perfil CU. Primera fase: 1 Sí, 40 Parcial, 5 No observado y 18 No verificable.

**Columnas.** ID, Módulo, Requerimiento, Existe en POC, Relación con POC, Qué existe, Brecha, Pantalla revisada, Evidencia, Fase, Alerta y Referencia.

**Qué sirve para el nuevo portal.** La estructura del archivo y las "alertas de fase" del Resumen, que coinciden con D7 y D8 de §1.1. Los resultados "Existe en POC" no describen hapag-portal. La sección 3 de este documento entrega la cobertura equivalente medida contra este repositorio.

### 2. Cobertura de las fichas en el código de hapag-portal

Leyenda:
- **C** = Construido: todos los criterios tienen código.
- **P** = Parcial: existe código relacionado y faltan criterios.
- **N** = No existe.

Las rutas son relativas a la raíz del repositorio. Los controladores están en [`backend/src/HapagPortal.WebApi/Controllers/V1/`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.WebApi/Controllers/V1/).

#### 2.1 M1 — Acceso, usuarios y experiencia (27 fichas: 0 C, 9 P, 18 N)

**Modelo actual.**
- JWT HS256 con refresh token rotativo ([`backend/src/HapagPortal.Infrastructure/Authentication/JwtTokenService.cs:20-73`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Authentication/JwtTokenService.cs#L20-L73)). Los claims son `sub`, `email`, `country`, `clientId`, roles y un claim `permission` por permiso.
- RBAC con 8 roles y 14 permisos sembrados ([`backend/src/HapagPortal.Infrastructure/Persistence/ApplicationDbContext.cs:108-165`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Persistence/ApplicationDbContext.cs#L108-L165)), resueltos por [`backend/src/HapagPortal.Infrastructure/Authentication/PermissionResolver.cs:10-31`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Authentication/PermissionResolver.cs#L10-L31). Se aplican en el servidor con `[HasPermission]` ([`backend/src/HapagPortal.Infrastructure/Authentication/HasPermissionAttribute.cs:5-52`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Authentication/HasPermissionAttribute.cs#L5-L52)).
- Los roles son internos y de cliente (Administrador, Coordinador, Supervisor, ExternalApi, Client, CustomsAgent, AdminBA y SuperAdmin; [`backend/src/HapagPortal.Domain/Constants/RoleCodes.cs:9-17`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Constants/RoleCodes.cs#L9-L17)). No existen los roles customer, shipper, consignee ni tercero.
- `User.ClientId` admite un solo cliente por usuario ([`backend/src/HapagPortal.Domain/Entities/User.cs:26`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Entities/User.cs#L26)).
- No hay entidades de acceso por BL, mandato, organización matriz, booking ni Match Code.

| Ficha | Fase | Código relacionado | Cob. |
|---|---|---|---|
| M1-01 Experiencia de uso | 1 | Shell navbar + sidebar + footer ([`frontend/src/app/app.html:1-17`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/app.html#L1-L17)), tokens [`frontend/src/styles.scss:2-40`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/styles.scss#L2-L40) | P |
| M1-02 Usuarios, perfiles y permisos | 1 | `UsersController.cs:17-59`. Los endpoints son globales con `users.manage` y no están acotados al cliente. `CreateUserCommandHandler` no asigna `ClientId`. La UI no llama a update ni a set-active ([`frontend/src/app/core/services/admin-user.service.ts:19-35`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/core/services/admin-user.service.ts#L19-L35)) | P |
| M1-03 Delegación y mandatos | 1 | — | N |
| M1-04 Selección de país | 1 | El país queda fijado por el cliente al registrarse ([`backend/src/HapagPortal.Domain/Entities/Client.cs:10`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Entities/Client.cs#L10)). La navbar lo muestra en solo lectura ([`frontend/src/app/shared/components/navbar/navbar.html:25-30`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/shared/components/navbar/navbar.html#L25-L30)) y no hay selector | P |
| M1-05 Dashboard | 1 | 3 contadores ([`frontend/src/app/features/dashboard/dashboard.ts:30-47`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/features/dashboard/dashboard.ts#L30-L47)); el panel de país es texto fijo ([`frontend/src/app/features/dashboard/dashboard.html:105-192`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/features/dashboard/dashboard.html#L105-L192)) | P |
| M1-06 Lista de distribución | 2 | — | N |
| M1-07 Cuenta de organización | 1 | `RegisterCommandHandler.cs:20-117`. Solo admite los tipos Client y CustomsAgent. La organización queda activa sin aprobación y no se pueden adjuntar documentos | P |
| M1-08 Aprobación de registro | 1 | — | N |
| M1-09 Pre-creación transportistas | 2 | — | N |
| M1-10 Recuperación y cierre de sesión | 1 | La recuperación está completa: respuesta uniforme, token de 1 h invalidado al usarse, y una cuenta inactiva no puede recuperar (`ForgotPasswordCommandHandler.cs:14-43`, `ResetPasswordCommandHandler.cs:16-55`). El logout es solo local ([`frontend/src/app/core/services/auth.service.ts:81-87`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/core/services/auth.service.ts#L81-L87)) y no hay endpoint de logout | P |
| M1-11 Modelo de accesos por rol | 1 | RBAC en el servidor. No hay matriz por tipo de información ni por acción sobre BL. Nada en el código usa `roles.manage` ni administra los permisos base | P |
| M1-12 a M1-22, M1-24 | 1/0/2 | Sin código de acceso por BL. `GetBLByNumberQueryHandler.cs:26` restringe al cliente propietario | N |
| M1-23 Auditoría de accesos | 1 | `AuditController.cs:15-23` y `SearchAuditQuery.cs:19-65` son de solo lectura. Las únicas filas de `AuditLogs` son las semilla (`ApplicationDbContext.cs:1125-1129`); ningún código las escribe en ejecución | P |
| M1-25 Bandeja de notificaciones | 2 | `Notification` + `NotificationsController` + campana ([`frontend/src/app/shared/components/navbar/navbar.html:33-40`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/shared/components/navbar/navbar.html#L33-L40)). Los únicos emisores son plazos y transmisiones de Aduana | P |
| M1-26 Comunicados masivos | 2 | — | N |
| M1-27 Modo guía | 2 | — | N |

#### 2.2 M2 — Disponibilidad de embarques (9: 0 C, 3 P, 6 N)

**Fuente de datos.** Los BL vienen de la base de datos propia. El único camino de carga es `POST bills-of-lading/import` con el permiso `bl.upload` ([`backend/src/HapagPortal.Application/BillsOfLading/Import/ImportBillsOfLadingCommandHandler.cs:25-119`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Application/BillsOfLading/Import/ImportBillsOfLadingCommandHandler.cs#L25-L119)).

**Entidad.** `BillOfLading` ([`backend/src/HapagPortal.Domain/Entities/BillOfLading.cs:5-39`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Entities/BillOfLading.cs#L5-L39)) no tiene booking, TATC, DIFU ni hitos.

| Ficha | Fase | Código relacionado | Cob. |
|---|---|---|---|
| M2-01 Publicación DIFU | 1 | — | N |
| M2-02 Emisión de BL (SWB/EBL) | 1 | — | N |
| M2-03 / M2-04 Conceptos de cobro | 2 | `ChargeTypes` ([`backend/src/HapagPortal.Domain/Constants/ChargeTypes.cs:3-12`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Constants/ChargeTypes.cs#L3-L12)) existe sin usos; no hay catálogo de conceptos | N |
| M2-05 Dispute | 1 | — | N |
| M2-06 Listado y detalle | 1 | [`frontend/src/app/features/bill-of-lading/bl-list/bl-list.ts:34-73`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/features/bill-of-lading/bl-list/bl-list.ts#L34-L73), [`frontend/src/app/features/bill-of-lading/bl-detail/bl-detail.ts:108-142`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/features/bill-of-lading/bl-detail/bl-detail.ts#L108-L142). Solo busca por número exacto, sin booking ni filtros por columna. Solo incluye los BL del propio `ClientId` (`GetMyBLsQueryHandler`) | P |
| M2-07 Separación importación/exportación | 1 | `ShipmentType` se muestra como columna "Tipo"; no hay filtro ni persistencia | P |
| M2-08 Seguimiento | 0 | Solo los campos estáticos Vessel/Voyage/ETD/ETA | N |
| M2-09 Consulta BL y TATC | 1 | Consulta de BL por número; no hay TATC | P |

#### 2.3 M3 y M4 — Flujos de servicio y reglas de negocio (23: 0 C, 8 P, 15 N)

**Cargos.**
- Se leen como están almacenados; el backend no los calcula. `LocalCharge` trae el impuesto precalculado ([`backend/src/HapagPortal.Domain/Entities/LocalCharge.cs:5-19`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Entities/LocalCharge.cs#L5-L19)).
- Las semillas usan los códigos THC, BL_FEE, ISPS, THC_RF y TRANSIT_FEE (`ApplicationDbContext.cs:731-741`), que no coinciden con `ChargeTypes`.
- Ningún servicio del catálogo del capítulo 3 tiene representación propia en el código.

**Exenciones y crédito.** `DemurrageExemption` y `CreditClient` tienen CRUD de admin (`backend/src/HapagPortal.Application/Admin/*`), pero ningún handler de cargos o pagos los lee.

| Ficha | Fase | Código relacionado | Cob. |
|---|---|---|---|
| M3-01 Gate Out | 1 | Lectura genérica de cargos ([`backend/src/HapagPortal.Application/LocalCharges/Read/GetByBL/GetLocalChargesByBLQueryHandler.cs:19-43`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Application/LocalCharges/Read/GetByBL/GetLocalChargesByBLQueryHandler.cs#L19-L43)) | P |
| M3-02 MHD | 1 | — | N |
| M3-03 Calculadora de días libres | 0 | `FreeDays` se almacena, pero no hay cálculo | N |
| M3-04 Cambio de almacén gratuito | 1 | `CreateWarehouseChangeCommandHandler.cs:21-55` cobra siempre el monto del request; no hay rama gratuita. El formulario no envía motivo, contenedor ni teléfono ([`frontend/src/app/features/warehouse/warehouse.ts:153-159`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/features/warehouse/warehouse.ts#L153-L159)) | P |
| M3-05 Solicitudes masivas | 1 | Solo hay formulario individual | N |
| M3-06 Historial de cambio de almacén | 2 | Existen `GET warehouse-changes/my` y `GET warehouse-changes/{id}`; no hay historial de estados ni RUT del pagador | P |
| M3-07 a M3-11 | 2 | — | N |
| M3-12 Correcciones de BL | 2 | Solo la rectificación de manifiesto de Aduana (`SubmitManifestAmendmentCommand`); el cliente no puede solicitarla ni pagarla | P |
| M3-13 BL hijo | 2 | `TransmitBLCommand` exige que el padre esté aceptado; la regla `BL_HOUSE_IN` existe; no hay cobro | P |
| M3-14 Matriz fuera de plazo | 2 | `DeadlineCalculator` produce el estado `Overdue` ([`backend/src/HapagPortal.Domain/Validation/DeadlineCalculator.cs:9-36`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Validation/DeadlineCalculator.cs#L9-L36)); no hay cobro | P |
| M3-15, M3-16, M3-17, M3-18, M3-19 | 2/1/2/1/2 | — | N |
| M4-01 Exentos desde Nexus | 1 | `DemurrageExemption` local, sin consumidor | P |
| M4-02 Gate In/EDS exentos sin carro | 1 | — | N |
| M4-03 Exclusión de IPO con crédito | 1 | `CreditClient` local, sin consumidor; no hay recargo IPO | P |
| M4-04 Carta de responsabilidad FFWW | 1 | — | N |

**Plazos y Aduana (construido; fuera del alcance de las fichas).**
- `DeadlineRule` es configurable y se siembran 8 reglas (`ApplicationDbContext.cs:60-92`).
- Ciclo de transmisión `Draft→Queued→Sent→Accepted|Rejected|Error` ([`backend/src/HapagPortal.Domain/Constants/CustomsConstants.cs:85-98`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Constants/CustomsConstants.cs#L85-L98)).
- Transmisor stub ([`backend/src/HapagPortal.Infrastructure/Customs/StubCustomsTransmitter.cs`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Customs/StubCustomsTransmitter.cs)), registrado en [`backend/src/HapagPortal.Infrastructure/DependencyInjection/DependencyInjection.cs:50`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/DependencyInjection/DependencyInjection.cs#L50).
- Este módulo no tiene ficha en la especificación validada.

#### 2.4 M5, M7, M9 — Pagos, facturación y reportería (15: 0 C, 7 P, 8 N)

**Modelo actual.**
- Hay un `Payment` por solicitud y BL. La moneda es la del BL ([`backend/src/HapagPortal.Application/Payments/Create/CreatePaymentCommandHandler.cs:82-84`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Application/Payments/Create/CreatePaymentCommandHandler.cs#L82-L84)).
- Estados posibles: Pending, Processing, Confirmed, Failed, Cancelled y PendingVerification, este último sin uso ([`backend/src/HapagPortal.Domain/Constants/PaymentStatus.cs:5-10`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Constants/PaymentStatus.cs#L5-L10)).
- La pasarela es simulada: devuelve `SIM-…` y el estado siempre es "Completed" ([`backend/src/HapagPortal.Infrastructure/Services/PaymentGatewayService.cs:6-43`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Services/PaymentGatewayService.cs#L6-L43)).
- Los webhooks de Khipu y Banco Chile autentican con secreto compartido ([`backend/src/HapagPortal.Application/Payments/Commands/Webhooks/`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Application/Payments/Commands/Webhooks/), [`backend/src/HapagPortal.Infrastructure/Services/WebhookAuthenticator.cs`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Services/WebhookAuthenticator.cs)).
- El recibo es el campo `Payment.ReceiptNumber`; su PDF es el literal `"%PDF-1.4 placeholder"` ([`backend/src/HapagPortal.Application/Receipts/Read/GetPdf/GetReceiptPdfQueryHandler.cs:31`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Application/Receipts/Read/GetPdf/GetReceiptPdfQueryHandler.cs#L31)).
- No hay carro, facturas, estado de cuenta ni RUT de facturación.

| Ficha | Fase | Código relacionado | Cob. |
|---|---|---|---|
| M5-01 Carro unificado | 1 | El comando acepta varios `Details`, pero el handler no usa `ChargeIds`; no hay entidad ni vista de carro | P |
| M5-02 Anulación de boletas | 1/rev | `CancelPaymentCommandHandler` cancela pagos no confirmados | P |
| M5-03 Medios de pago | 1/rev | Las constantes incluyen Khipu, BankButton, Deposit y BCI. `GET config/payment-methods/{country}` no se consume: el frontend usa una lista fija ([`frontend/src/app/features/payments/payment-form/payment-form.ts:44-58`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/features/payments/payment-form/payment-form.ts#L44-L58)) | P |
| M5-04 Monedas por recargo | 1 | Existen la entidad `Currency` y el endpoint de monedas; el pago usa una sola moneda | P |
| M5-05 Tipo de cambio Nexus | 1 | `Currency.ExchangeRateToUSD` está sembrado (CLP 950, BOB 6.91); ningún código asigna `Payment.ExchangeRate`; no hay EUR | N |
| M5-06 Adjuntar comprobante de depósito | 2 | Existe el campo `Payment.DepositProofUrl`, pero no hay endpoint ni UI de carga | P |
| M5-07 Pago con crédito | 1 | El método `CreditLine` existe como string; no se valida contra `CreditClient` | P |
| M5-08, M5-09, M5-10 | 1/1/2 | — | N |
| M7-01 Facturas | 1 | — | N |
| M7-02 Historial de pagos y boletas | 1 | `GET payments/my` y `GET receipts/my` con sus pantallas; el PDF es placeholder y no hay RUT del pagador | P |
| M7-03 Estado de cuenta | 2 | — | N |
| M7-04 Comprobantes Collect | 0 | Solo existe `FreightTerms` en el BL | N |
| M9-01 Reportería de transacciones | 2 | Hay reportes CSV de transmisiones y plazos (`ReportsController.cs`), ninguno de pagos | N |

#### 2.5 M6, M8, M10 — Documentos, administración y asistente (23: 0 C, 4 P, 19 N)

- **Documentos.** No hay biblioteca PDF en los `.csproj` ni plantillas. Los dos endpoints PDF (recibo y ODS) devuelven placeholder ([`backend/src/HapagPortal.Application/ServiceOrders/Read/GetPdf/GetServiceOrderPdfQueryHandler.cs:31`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Application/ServiceOrders/Read/GetPdf/GetServiceOrderPdfQueryHandler.cs#L31)).
- **Archivos.** No existe almacenamiento de archivos.

| Ficha | Fase | Código relacionado | Cob. |
|---|---|---|---|
| M6-01 a M6-07, M6-09 | 1/2 | — | N |
| M6-08 Carta de liberación y desconsolidado | 2 | Solo los tipos de ODS `RELEASE` y `DECONSOLIDATION` ([`frontend/src/app/features/service-orders/service-orders.ts:38-46`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/features/service-orders/service-orders.ts#L38-L46)) | P |
| M8-01 Mantenedor de tarifas | 1 | — | N |
| M8-02 Clientes con crédito desde Nexus | 1 | `CreditClient` se mantiene a mano; no hay lectura de Nexus | N |
| M8-03 FFWW desde Nexus | 1 | — | N |
| M8-04 Clientes y Match Codes | 1 | — | N |
| M8-05 Área de administración | 2 | Rutas `admin/*` ([`frontend/src/app/app.routes.ts:118-164`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/app.routes.ts#L118-L164)): créditos, exenciones, usuarios, importación de BL, Aduana, plazos, auditoría y reportes | P |
| M8-06 Admin con visibilidad total | 1 | Administrador, SuperAdmin y "Admin" reciben todos los permisos; el rol expuesto es ADMIN/USER (`LoginCommandHandler.cs:55`) | P |
| M8-07 Bloqueo de pagos por horario | 1 | — | N |
| M8-08 Impersonación | 2/rev | — | N |
| M10-01, M10-03 a M10-06 | 1/2 | — | N |
| M10-02 Procesos y procedimientos | 1 | FAQ por país sembrada en BD, con búsqueda por texto (`FAQsController.cs`, [`frontend/src/app/features/faq/faq.ts:234-263`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/features/faq/faq.ts#L234-L263)); no hay asistente conversacional | P |

#### 2.6 Plataforma, integraciones y no funcionales (27 NF: 1 C, 18 P, 8 N)

**Arquitectura.**
- Clean Architecture en 5 proyectos (`backend/src/HapagPortal.{Domain,Application,Infrastructure,WebApi,DatabaseMigrations}`) más 5 proyectos de pruebas.
- CQRS con MediatR y `ValidationBehavior` ([`backend/src/HapagPortal.Application/DependencyInjection.cs:13-18`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Application/DependencyInjection.cs#L13-L18)), patrón `Result<T>`.
- Los handlers usan `IApplicationDbContext` directamente; no hay clases de repositorio.
- EF Core 9 sobre **PostgreSQL** (Npgsql; [`backend/src/HapagPortal.Infrastructure/DependencyInjection/DependencyInjection.cs:25-38`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/DependencyInjection/DependencyInjection.cs#L25-L38)). No se usan Supabase ni SQL Server.
- `net9.0` en todos los `.csproj`.
- Las reglas de capas se verifican con pruebas ([`backend/tests/HapagPortal.ArchitectureTests/LayerDependencyTests.cs`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/tests/HapagPortal.ArchitectureTests/LayerDependencyTests.cs)).

**Integraciones:**

| Sistema | Estado en código |
|---|---|
| Nexus, FIS, Data Lake, Navesoft, DBNet, Mercurio, TATC/Flagare, Santander | No existe (0 coincidencias; no hay `HttpClient`/`AddHttpClient` en [`backend/src`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src)) |
| API dummy de Nexus (Pendientes #28) | No existe |
| Aduana | Stub en proceso |
| Pasarela de pago | Simulada en proceso |
| Khipu, Banco Chile | Solo webhook entrante, con secreto compartido |
| BCI | Solo constante ([`backend/src/HapagPortal.Domain/Constants/PaymentMethods.cs:15`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Constants/PaymentMethods.cs#L15)) |
| Correo | SMTP construido ([`backend/src/HapagPortal.Infrastructure/Services/EmailService.cs`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Services/EmailService.cs)) |
| Almacenamiento de archivos | No existe |

**No funcionales:**

| NF | Cob. | Evidencia |
|---|---|---|
| NF-01 Idempotencia | P | Webhooks y recibo idempotentes; la creación de pago no tiene clave de idempotencia |
| NF-02 Estado único | P | Constantes de estado; sin historial de transiciones |
| NF-03 Consistencia pago/servicio | N | Sin cola ni reintento |
| NF-04 Conciliación | P | `PaymentNumber` único y `ExternalReference`; sin `TransactionId` persistido |
| NF-05 Segregación | P | Filtro por `ClientId` en cada handler; sin filtro global |
| NF-06 Mandatos | N | — |
| NF-07 Protección | P | AES-GCM para secretos ([`backend/src/HapagPortal.Infrastructure/Secrets/AesSecretProtector.cs`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Secrets/AesSecretProtector.cs)), BCrypt; sin `UseHttpsRedirection`/HSTS |
| NF-08 Instrumentos de pago | C | `Payment` guarda solo referencia y resultado |
| NF-09 Credenciales | P | `SecretCredential` + `SecretResolver`; tipos solo para SII y Aduana |
| NF-10 Ventana de servicio | N | Solo `/health` ([`backend/src/HapagPortal.WebApi/Program.cs:165`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.WebApi/Program.cs#L165)) |
| NF-11 APIs de origen caídas | N | Errores genéricos por componente; el dashboard los silencia |
| NF-12 Pasarela caída | P | Si la pasarela falla, el pago queda `Failed`; no hay timeout ni reintento |
| NF-13 Respaldo | N | — |
| NF-14 Registro de operaciones | P | Columnas `CreatedBy`/`ModifiedBy` vía interceptor ([`backend/src/HapagPortal.Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs:27-55`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs#L27-L55)) |
| NF-15 Cambios en mantenedores | P | Sin valor anterior; `AuditLog.OldValues` sin escritor |
| NF-16 Conservación | P | Tabla de auditoría y borrado lógico; sin política |
| NF-17 Volumetría | N | — |
| NF-18 Tiempos de respuesta | N | — |
| NF-19 Operaciones masivas | P | Importación de BL síncrona con resultado por fila; sin procesamiento en segundo plano |
| NF-20 Navegadores | N | Sin `browserslist` |
| NF-21 Dispositivos | P | Grilla Bootstrap, `table-responsive` en todas las tablas y un único breakpoint ([`frontend/src/styles.scss:434`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/styles.scss#L434)) |
| NF-22 Huso horario | P | UTC en auditoría; sin huso de referencia |
| NF-23 Idioma y formatos | P | Solo español; sin `LOCALE_ID`; fechas con patrón fijo |
| NF-24 Ambiente de pruebas | P | Staging en Railway con datos demo (`RAILWAY-DEPLOY.md`) |
| NF-25 Puesta en producción | P | Dockerfile y migraciones automáticas; sin rollback |
| NF-26 Monitoreo | P | Solo `/health` |
| NF-27 Registros de diagnóstico | P | `ExceptionHandlingMiddleware` + `ILogger` |

**CI y pruebas.**
- `.github/workflows/pr-tests.yml` corre build y pruebas del backend y build de producción del frontend en PR hacia `develop`/`main`.
- Pruebas: 3 archivos de arquitectura, 53 de Application, 37 de Domain, 8 de Infrastructure y 14 de WebApi. No hay pruebas de integración, de carga ni de frontend.

### 3. Interfaz, accesibilidad e idiomas (requerimiento adicional, sin ficha)

**Stack.**
- Angular `^21.2.0`, Bootstrap `^5.3.8` y bootstrap-icons ([`frontend/package.json:13-24`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/package.json#L13-L24)).
- Los 31 componentes son standalone, con signals, `inject()` y control flow `@if`/`@for`. No hay Angular CDK ni Material.

**Tokens de color** ([`frontend/src/styles.scss:2-15`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/styles.scss#L2-L15), replicados como CSS vars en `:32-37`):

| Token | Hex | Mapeo Bootstrap |
|---|---|---|
| `$hl-dark` | `#33424f` | `$secondary`; fondo de navbar, sidebar y footer |
| `$hl-orange` | `#ff6600` | `$primary` |
| `$hl-green` | `#009840` | `$success` |
| `$hl-blue` | `#004d6c` | `$info` |
| `$hl-light-gray` | `#f5f7fa` | `$body-bg` |
| `$hl-border` | `#e2e8f0` | Bordes de inputs |

- En total hay 31 hex distintos en [`frontend/src`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src).
- Los hex de marca también se repiten como literales en plantillas, por ejemplo `#FF6600` en el logo SVG de `navbar.html:15`, `login.html:7` y `dashboard.html:19`.

**Tipografía.** Inter y Montserrat desde Google Fonts ([`frontend/src/index.html:8-13`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/index.html#L8-L13)).

**Tema.** Solo claro: 0 usos de `prefers-color-scheme`/`data-bs-theme`.

**i18n.**
- Ninguna: 0 usos de `$localize`, `i18n`, ngx-translate, transloco y `@angular/localize`.
- `lang="es"` está fijo ([`frontend/src/index.html:2`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/index.html#L2)) y no hay selector de idioma.
- Los textos están en español directamente en las plantillas (≈379 nodos de texto en `.html`, más plantillas inline) y en `STATUS_LABEL_MAP` ([`frontend/src/app/shared/components/status-badge/status-badge.ts:20-32`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/shared/components/status-badge/status-badge.ts#L20-L32)).

**Formatos locales.**
- 0 usos de `registerLocaleData`/`LOCALE_ID`, así que aplica el en-US por defecto de Angular.
- `number:'1.2-2'` aparece 30 veces. El pipe `currency` no se usa: el monto se muestra como código + número.
- Las fechas usan patrones fijos (`dd/MM/yyyy`, `dd-MM-yyyy HH:mm`).

**Accesibilidad.**

| Elemento | Situación |
|---|---|
| `aria-label` | 3 |
| `aria-expanded` | 1, estático |
| `aria-live`, `aria-describedby`, `aria-hidden` | 0 |
| `role="status"` | 12 |
| `role="alert"` | 1 |
| `<label>` / `for=` | 69 / 21 |
| `scope=` / `<caption>` | 0 en 17 tablas |
| Skip-link | No hay |
| `outline` personalizado / `prefers-reduced-motion` | 0 / 0 |
| Botón hamburguesa ([`frontend/src/app/shared/components/navbar/navbar.html:5-9`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/shared/components/navbar/navbar.html#L5-L9)) | Sin nombre accesible |
| Landmarks | `nav`, `aside`, `main`, `footer`; no hay `header` |

**Responsive.** Una sola media query (`max-width: 991.98px`, [`frontend/src/styles.scss:434`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/styles.scss#L434)). Viewport meta presente.

**Navegadores.** No hay `browserslist` (aplican los valores por defecto de Angular 21). El target es `ES2022`.

## Code References

- [`backend/src/HapagPortal.Infrastructure/Persistence/ApplicationDbContext.cs:108-165`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Persistence/ApplicationDbContext.cs#L108-L165) — semilla RBAC (8 roles, 14 permisos)
- [`backend/src/HapagPortal.Infrastructure/Authentication/PermissionResolver.cs:10-31`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Authentication/PermissionResolver.cs#L10-L31) — resolución de permisos por rol
- [`backend/src/HapagPortal.Application/BillsOfLading/Import/ImportBillsOfLadingCommandHandler.cs:25-119`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Application/BillsOfLading/Import/ImportBillsOfLadingCommandHandler.cs#L25-L119) — única entrada de BL
- [`backend/src/HapagPortal.Application/Payments/Create/CreatePaymentCommandHandler.cs:82-84`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Application/Payments/Create/CreatePaymentCommandHandler.cs#L82-L84) — moneda del pago = moneda del BL
- [`backend/src/HapagPortal.Infrastructure/Services/PaymentGatewayService.cs:6-43`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Services/PaymentGatewayService.cs#L6-L43) — pasarela simulada
- [`backend/src/HapagPortal.Application/Receipts/Read/GetPdf/GetReceiptPdfQueryHandler.cs:31`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Application/Receipts/Read/GetPdf/GetReceiptPdfQueryHandler.cs#L31) — PDF placeholder
- [`backend/src/HapagPortal.Infrastructure/Customs/StubCustomsTransmitter.cs`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/Customs/StubCustomsTransmitter.cs) — transmisor de Aduana simulado
- [`backend/src/HapagPortal.Domain/Constants/ChargeTypes.cs:3-12`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Domain/Constants/ChargeTypes.cs#L3-L12) — tipos de cargo sin uso
- [`backend/src/HapagPortal.Infrastructure/DependencyInjection/DependencyInjection.cs:25-57`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/backend/src/HapagPortal.Infrastructure/DependencyInjection/DependencyInjection.cs#L25-L57) — registro de PostgreSQL e infraestructura
- [`frontend/src/styles.scss:2-40`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/styles.scss#L2-L40) — tokens de marca y overrides de Bootstrap
- [`frontend/src/index.html:2`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/index.html#L2) — `lang="es"` fijo
- [`frontend/src/app/features/payments/payment-form/payment-form.ts:44-69`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/features/payments/payment-form/payment-form.ts#L44-L69) — medios de pago e IVA fijos en el frontend
- [`frontend/src/app/app.routes.ts:118-164`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/app.routes.ts#L118-L164) — rutas de administración

## Architecture Documentation

- **Backend.**
  - Clean Architecture con 4 capas más migraciones.
  - CQRS con MediatR: una carpeta por caso de uso con Command/Query + Handler + Validator (FluentValidation).
  - Handlers `sealed` con constructor primario, errores en `DomainErrors` y traducción a ProblemDetails en `ApiController.HandleFailure`.
  - Versionado `api/v1`, autorización fina con `[HasPermission("modulo.accion")]` y gruesa con `[Authorize(Roles="Admin")]`.
  - Entidades `BaseAuditableEntity` con interceptor de auditoría y borrado lógico.
  - Configuración EF por entidad y semilla determinista con `HasData`.
- **Frontend.**
  - Angular standalone con signals y rutas lazy (`loadComponent`).
  - Guards funcionales (`authGuard`, `adminGuard`, `internalGuard`).
  - `ApiService` y `API_ENDPOINTS` ([`frontend/src/app/core/constants/app.constants.ts`](https://github.com/brypenalozav-stack/hapag-portal/blob/f49ea8af9cace4140ba908d51f434e0daaf50861/frontend/src/app/core/constants/app.constants.ts)).
  - Clases CSS con prefijo `hl-` sobre Bootstrap.
- **Segregación.** Se aplica en cada handler: `bl.ClientId == currentUserService.ClientId`, y si no coincide devuelve NotFound.
- **Despliegue.** Docker en Railway con PostgreSQL gestionado. El entorno queda fijado como Staging en el Dockerfile y las migraciones se aplican al arrancar.

## Historical Context (from thoughts/)

> Nota (2026-10-05): los documentos de agosto citados abajo, salvo `habilitar-terceros.md`, se eliminaron de `thoughts/` y `docs/` al limpiar el repositorio. Su contenido relevante queda resumido en esta sección.

- `thoughts/shared/research/2026-08-22-compatibilidad-docs-funcionales-portal.md` analizó la especificación **v2.0 de `docs/`** (51 RF y 90 US), además del mockup y la propuesta de 1.960 h. Llegó a conclusiones que este research confirma sobre el código:
  - stubs de pasarela, PDF y Aduana;
  - mantenedores sin consumidores (CreditClient, DemurrageExemption);
  - dos vocabularios de permisos;
  - configuración de país fija en el frontend.

  Ese análisis es anterior a los documentos de `katu`. La numeración de fichas cambió: hoy son 97 RF en 10 módulos y el capítulo de integración pasó a ser el 14.
- `thoughts/shared/plans/2026-08-10-plataforma-operativa-aduana.md` — sus 9 fases se entregaron (commits a7e1742 … f49ea8a). Es el origen del RBAC interno, la importación de BL, Aduana, plazos, auditoría, reportes y notificaciones que hoy existen. Sus checkboxes siguen sin marcar.
- `thoughts/shared/plans/2026-08-10-habilitar-terceros.md` — plan de acceso de terceros, sin implementar. Su alcance coincide con M1-03 y M1-11 a M1-24 de la especificación validada.
- Ningún documento previo trata WCAG 2.2 AA ni ES/EN. `docs/especificacion-tecnico-funcional-portal-2.0.html` fija NF-23 en español.

## Related Research

- `thoughts/shared/plans/2026-10-05-actualizacion-requerimientos-portal-2-0-ui-integraciones.md` (plan derivado de este research)
- `thoughts/shared/plans/2026-08-10-habilitar-terceros.md`

## Open Questions

1. ¿Cuál es la fase definitiva de M3-02, M6-02 y M3-17? ¿Cómo se resuelve la dependencia de M5-07 y M6-07 (Fase 1) sobre M7-03 (Fase 2)?
2. M9-02 y M9-03 se citan en M9-01 y NF-27: ¿se eliminan las referencias o se redactan las fichas?
3. ¿Integración con FIS y Data Lake por API (capítulo 14) o por sincronización de BD (§1)? NexusV2 deja la función "Datos" sin decidir (push vs. consulta).
4. El módulo "Supabase / Base de Datos" de Pendientes, ¿se reformula sobre PostgreSQL/EF Core, que es lo que usa el código?
5. ¿Qué responsables nominales se asignan en Pendientes? En NexusV2 hay RACI con nombres; en Pendientes no hay ninguno.
6. ¿Cómo entran al portal las 10 pantallas FIS y las macros del Gantt? Hoy no existe en el código ningún punto de entrada aparte del importador de BL.
7. Counter, Errores de facturación y Plazos documentales/Feriados existen en Nexus sin ficha en la especificación. ¿Entran al alcance del portal?
8. Bilingüismo ES/EN: ¿alcanza la interfaz, los documentos PDF, los correos y el asistente? ¿Qué variante de inglés y qué glosario marítimo?
9. ¿Qué nivel de conformidad se exige (WCAG 2.2 AA completo) y con qué evidencia de aceptación?
