# Registro de decisiones — Portal 2.0 v4

Fase 0 del plan `thoughts/shared/plans/2026-10-05-actualizacion-requerimientos-portal-2-0-ui-integraciones.md`.
Cada decisión parte en estado **Propuesta** y pasa a **Aprobada** o **Modificada** cuando la valida el usuario.
Ninguna fase posterior a la 0 empieza sin la aprobación de este registro.

Fuente de requerimientos: los 5 documentos depurados de `C:\Users\klaze\Desktop\katu` (línea base en `linea-base-katu.md`).

Aprobación: el usuario aprobó todas las decisiones el 05-10-2026.

## Decisiones principales

| ID | Pregunta | Opciones consideradas | Decisión propuesta | Impacto | Estado |
|---|---|---|---|---|---|
| Q1 | ¿Qué fase rige en M3-02, M6-02 y M3-17? ¿Cómo se resuelve que M5-07 y M6-07 (Fase 1) dependan de M7-03 (Fase 2)? | Manda el encabezado · Manda el texto · Mover M7-03 a Fase 1 | Manda el encabezado. **M3-02 = Fase 1** (se quita "en su segunda fase"). **M6-02 = Fase 2** ("primera etapa" pasa a "primera entrega dentro de Fase 2, sin pago ni carro"). **M3-17 = Fase 2** (se quitan la nota de opinión y "en revisión"; el Web Service lo desarrolla y administra Hapag-Lloyd). M5-07 y M6-07 dependen de M4-03/M8-02 (lectura de condición de crédito), no de M7-03, que sigue en Fase 2. | Fase 1 de la especificación | Aprobada (05-10-2026) |
| Q2 | M9-02 y M9-03 se citan sin ficha | Eliminar referencias · Redactar las fichas | Se **eliminan las referencias**. M9-01 se relaciona con M1-23 y NF-27; NF-27 queda autocontenida. | Fase 1 | Aprobada (05-10-2026) |
| Q3 | FIS y Data Lake: ¿API o sincronización de BD? Función "Datos" de NexusV2 | API · Sincronización de BD · Mixto | **API**, sin acceso directo a BD; se elimina la frase contraria de §1. En "Datos", **el portal consulta a Nexus vía API bajo demanda, con caché corta**; no hay sincronización de tablas. | Fases 1, 2 y 6a–6c | Aprobada (05-10-2026) |
| Q4 | Módulo de base de datos de Pendientes (tareas 19–24), que hoy nombra una plataforma distinta de la que usa el código | Mantener · Reformular | Se reformula como **"Base de datos (PostgreSQL)"**, con EF Core y migraciones en `HapagPortal.DatabaseMigrations`. Las tareas 19–24 conservan su intención. | Fase 2 | Aprobada (05-10-2026) |
| Q5 | ¿Cómo se llenan los responsables de Pendientes? | Solo área · Área + nombre propuesto | Regla: función NexusV2 → R del RACI (marca "RACI NexusV2"); tarea de desarrollo de este plan → `Desarrollo – Equipo de desarrollo Portal 2.0 (R); apoyo: <validadores de la fase>` (marca "Plan Portal 2.0"); resto → área + nombre propuesto (marca "Propuesta"). Ver tabla persona→área abajo. Formato de celda: `Área – Nombre (R); apoyo: Área – Nombre`. | Fase 2 | Aprobada (05-10-2026) |
| Q6 | ¿Cómo entran al portal los datos de FIS? | API vía puerto · Importador actual · Macros | Destino: puerto `IShipmentSource`, contrato CT-FIS (`fis.openapi.yaml`). Entrada transitoria: el importador existente `POST bills-of-lading/import` hasta validar CT-FIS y conmutar a Real. El Gantt y las macros (tareas 102–106) quedan **fuera del alcance** del desarrollo Portal 2.0 (área Macros/RPX). | Fases 2 y 6a–6c | Aprobada (05-10-2026) |
| Q7 | Counter, Errores de facturación y Plazos documentales existen en Nexus sin ficha | Fichas nuevas · Fuera de alcance | **M8-09 "Counter Bolivia/Ultramar" (Fase 2)** y **M2-10 "Consulta de plazos documentales por nave" (Fase 2)**, fichas nuevas. **Errores de facturación** queda fuera del alcance (sigue en Nexus). Total: 134 fichas. | Fases 1 y 2 | Aprobada (05-10-2026) |
| Q8 | Alcance del bilingüismo, variante de inglés y glosario | Solo interfaz · Interfaz + PDF + correos + asistente | Fase 1: interfaz ES/EN con cambio en caliente. Fase 2: correos, PDF y asistente M10. Inglés internacional con ortografía estadounidense. Fechas: es-CL `dd-MM-yyyy`, es-BO `dd/MM/yyyy`, EN `dd MMM yyyy`, 24 h con huso del país (America/Santiago / America/La_Paz). Montos siempre con código ISO; CLP sin decimales, BOB/USD/EUR con 2. Glosario en `glosario-es-en.md`. | Fases 1, 3 y 5b | Aprobada (05-10-2026) |
| Q9 | Nivel de conformidad de accesibilidad y evidencia | AA parcial · WCAG 2.2 AA completo | **WCAG 2.2 AA completo**: los 55 criterios A+AA (sin 4.1.1, obsoleto). Evidencia: `ng lint` sin errores en CI; axe con 0 violaciones en ES y EN; Lighthouse Accesibilidad ≥ 95; prueba manual con teclado y NVDA según el checklist de la Guía. | Fases 1, 3, 4 y 5a–5c | Aprobada (05-10-2026) |

### Tabla persona → área (Q5, propuesta)

| Área | Personas |
|---|---|
| Finanzas | Fer, Ricardo |
| Comercial | Kari |
| Customer Service | Cami, Mati |
| Nexus / IT | Lucho, Jorge |
| Arquitectura / QA | Andrés |
| Macros / RPX | Diego, RPX |
| Producto / Negocio | Katu |
| Legal | Área Legal (titular del área) — sin nombre en fuentes |
| Seguridad TI | Área Seguridad TI (titular del área) — sin nombre en fuentes |

## Decisiones complementarias

| ID | Decisión propuesta | Estado |
|---|---|---|
| DC1 | M11 "Interfaz, accesibilidad e idiomas" se inserta como capítulo 14. Integración pasa a 15, No funcionales a 16 y Glosario a 17. | Aprobada (05-10-2026) |
| DC2 | NF-20: 2 últimas versiones mayores de Chrome, Edge, Firefox, Safari (macOS), Safari iOS y Chrome Android, con aviso en navegadores no soportados. NF-21: anchos 360, 768, 1024 y ≥1280 px; consulta y pago en móvil; reflow a 320 px (WCAG 1.4.10). | Aprobada (05-10-2026) |
| DC3 | NF-22: los cálculos por tiempo usan UTC con calendario de negocio y se presentan en el huso del país, indicándolo. | Aprobada (05-10-2026) |
| DC4 | Colores corregidos para contraste AA: primario `#b84a00` (5,23:1), éxito `#007a33` (5,48:1), texto secundario `#4a5568` (7,53:1); `#ff6600` solo para logo y acentos no textuales. Hoy la interfaz muestra el azul por defecto de Bootstrap (`--bs-primary: #0d6efd`) porque los overrides de `styles.scss` no se aplican; el cambio visual de la Fase 5a es mayor de lo que sugiere el código. | Aprobada (05-10-2026) |
| DC5 | Portada de la especificación: "Preparado para: Hapag-Lloyd Chile y Bolivia". | Aprobada (05-10-2026) |
| DC6 | Las frases que hoy encargan a un proveedor externo indicar valores (NF-10, NF-13, NF-17, NF-18, NF-20, NF-22 y la firma de M6-01, M6-02 y M6-07) pasan a "Hapag-Lloyd, a través del equipo de desarrollo del Portal 2.0, define y publica…". | Aprobada (05-10-2026) |
| DC7 | Calendario y validadores por fase (abajo). Ejecuta siempre el Equipo de desarrollo Portal 2.0; inicio 06-10-2026, fin 02-12-2026, feriado 12-10-2026 excluido. | Aprobada (05-10-2026) |

### Calendario y validadores (DC7)

| Orden | Fase | Inicio | Fin | Valida (propuesta) |
|---|---|---|---|---|
| 1 | 0 Línea base y decisiones | 06-10-2026 | 07-10-2026 | Katu, Kari |
| 2 | 6a Contratos de integración | 08-10-2026 | 14-10-2026 | Lucho (Nexus), Diego (FIS), Fer y Ricardo (pagos) |
| 3 | 6b Puertos y adaptadores Dummy | 15-10-2026 | 20-10-2026 | Jorge |
| 4 | 6c Simulador y resiliencia | 21-10-2026 | 27-10-2026 | Jorge, Diego |
| 5 | 1 Especificación v4 | 28-10-2026 | 02-11-2026 | Katu, Kari |
| 6 | 2 Documentos operativos v4 | 03-11-2026 | 05-11-2026 | Lucho, Fer |
| 7 | 3 Documentos nuevos | 06-11-2026 | 10-11-2026 | Katu |
| 8 | 4 Prototipo | 11-11-2026 | 12-11-2026 | Katu, Cami/Mati |
| 9 | 5a Lint, accesibilidad y tokens | 13-11-2026 | 17-11-2026 | Andrés |
| 10 | 5b i18n (Transloco) | 18-11-2026 | 24-11-2026 | Andrés, Kari (glosario EN) |
| 11 | 5c Base de accesibilidad | 25-11-2026 | 30-11-2026 | Andrés |
| 12 | 7 `documentacion.html` | 01-12-2026 | 02-12-2026 | Jorge |

## Entregables publicados

| Fase | Entregable | URL |
|---|---|---|
| — | (se completa en la Fase 4 con la URL del prototipo) | — |
