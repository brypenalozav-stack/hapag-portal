# Hoja de ruta de integraciones — Portal 2.0

Hitos por sistema, alineados con las tareas de Pendientes de Nexus (25–41), Integración FIS (42–46), Pagos/DBNet (47–55) y Pagos/BCI (116–118), y con el calendario del plan (`CALENDARIO` en `scripts/requerimientos/config.py`, decisión DC7).

Reglas:
- Ejecuta siempre el Equipo de desarrollo Portal 2.0. Las fechas son días hábiles de lunes a viernes; el 12-10-2026 es feriado.
- Si una fase se atrasa, las siguientes se corren la misma cantidad de días hábiles.
- La **validación de cada contrato** la ejecuta su responsable en Hapag-Lloyd. No tiene fecha en las fuentes porque su plazo no depende del equipo de desarrollo; se registra como tarea "Validar contrato" en Pendientes_v4 (Fase 2).
- El **paso a `Mode=Real`** contra un endpoint productivo ocurre después del plan y exige el contrato Validado y el checklist Dummy → Real de la Fase 6c.

## Fases del plan que tocan integraciones

| Fase | Inicio | Fin | Resultado | Valida (propuesta) |
|---|---|---|---|---|
| 6a Contratos de integración | 08-10-2026 | 14-10-2026 | Inventario, contratos PROPUESTA y esta hoja de ruta | Lucho (Nexus), Diego (FIS), Fer y Ricardo (pagos) |
| 6b Puertos y adaptadores Dummy | 15-10-2026 | 20-10-2026 | 10 puertos con adaptador Dummy, seleccionados por `Integrations:<Sistema>:Mode` | Jorge |
| 6c Simulador y resiliencia | 21-10-2026 | 27-10-2026 | Simulador HTTP, clientes Real con resiliencia, pruebas de contrato en CI y webhooks verificados | Jorge, Diego |
| 2 Documentos operativos v4 | 03-11-2026 | 05-11-2026 | Pendientes_v4 con las tareas "Validar contrato" y los vínculos a 6a–6c | Lucho, Fer |

## Nexus (tareas 25–41)

| Tarea | Descripción | Hito del plan | Fecha | Pendiente fuera del plan |
|---|---|---|---|---|
| 25 | Definir alcance y responsabilidades Nexus / Portal / Hapag-Lloyd | Inventario publicado (`README.md`) como insumo | 14-10-2026 | Definición de Hapag-Lloyd |
| 26 | Definir API e integración Portal–Nexus | CT-NEXUS en PROPUESTA | 14-10-2026 | Validación con Nexus/IT – Lucho |
| 27 | Planificación, dependencias e hitos Nexus–Portal | Esta hoja de ruta | 14-10-2026 | Calendario de desarrollo en Nexus |
| 28 | API Dummy para simular Nexus | Adaptadores Dummy (6b) y simulador HTTP (6c) | 20-10-2026 / 27-10-2026 | — |
| 29 | Créditos | `GET /customers/{taxId}/conditions` en CT-NEXUS; Dummy con crédito a 30 días (6b) | 14-10-2026 / 20-10-2026 | Tabla de créditos en Nexus y reglas de Finanzas |
| 30 | Exenciones | `GET /exemptions` en CT-NEXUS; Dummy con exención de Gate In y EDS (6b) | 14-10-2026 / 20-10-2026 | Conceptos y condiciones por escenario (Finanzas) |
| 31 | Confirmación de pago | Flujo descrito en CT-DEP | 14-10-2026 | Elección de opción A o B (Finanzas con Nexus/IT) |
| 32 | Cambio de almacén | CT-MERC y CT-TATC en solo lectura | 14-10-2026 | Actualización de Mercurio y TATC desde Nexus |
| 33 | Generar TATC | Estado del TATC en CT-TATC | 14-10-2026 | Estados del TATC definidos con los equipos técnicos (M2-09) |
| 34 | Generar CLD | Estado del CLD en CT-TATC | 14-10-2026 | Automatización del CLD en Nexus |
| 35 | Conexión Portal | Cliente Real de Nexus con resiliencia, probado contra el simulador (6c) | 27-10-2026 | Endpoint productivo y retiro del CSV/FTP (CT-NAVE) |
| 36 | API Estado de Cuenta | Sin contrato en el plan (M7-03 es de Fase 2) | — | Definición de la API en Nexus |
| 37 | Datos | Resuelta por Q3: consulta bajo demanda con caché corta | 07-10-2026 (Fase 0) | — |
| 38 | Cuenta Admin | Sin contrato: no es una integración del portal | — | Definición en Nexus |
| 39 | TC | `GET /exchange-rates` en CT-NEXUS; Dummy CLP 950 y BOB 6,91 (6b) | 14-10-2026 / 20-10-2026 | Fuente y aprobación de EUR y otras monedas |
| 40 | Tarifa | `GET /tariffs` en CT-NEXUS | 14-10-2026 | Tabla de tarifas con vigencia y mantenedor en Nexus |
| 41 | Counter | Sin contrato en el plan (M8-09 es de Fase 2) | — | Definición funcional del counter |

## FIS / Data Lake (tareas 42–46)

Por Q3 y Q6, la "sincronización" de las tareas 42–44 se cumple por API: se valida CT-FIS en lugar de sincronizar bases de datos.

| Tarea | Descripción | Hito del plan | Fecha | Pendiente fuera del plan |
|---|---|---|---|---|
| 42 | Validar sincronización de organizaciones | Partes con Match Code y RUT en CT-FIS (`parties`) | 14-10-2026 | Validación con Macros/RPX – Diego |
| 43 | Validar sincronización de datos | `GET /shipments` y `GET /shipments/{blNumber}` en CT-FIS | 14-10-2026 | Validación con Macros/RPX – Diego |
| 44 | Validar sincronización de documentos | Referencias de documento en CT-FIS (`documentNumber`, `documentReference`) | 14-10-2026 | Validación con Macros/RPX – Diego |
| 45 | Testear integración FIS | Cliente Real probado contra el simulador (6c) | 27-10-2026 | Prueba contra el endpoint de FIS |
| 46 | Ajustes posteriores a pruebas FIS | — | — | Depende del resultado de la tarea 45 |

Hasta que CT-FIS esté Validado y en `Mode=Real`, los embarques entran por `POST bills-of-lading/import` (Q6).

## Pagos y DBNet (tareas 47–55)

| Tarea | Descripción | Hito del plan | Fecha | Pendiente fuera del plan |
|---|---|---|---|---|
| 47 | Alcance funcional de Pagos | Inventario de medios de pago en `README.md` | 14-10-2026 | Definición de Finanzas |
| 48 | Integraciones y APIs de Pagos | CT-KHIPU, CT-BCH, CT-SANT y CT-BCI en PROPUESTA | 14-10-2026 | Validación con Finanzas – Fer y Ricardo |
| 49 | Dependencias y momento de integración | Esta hoja de ruta | 14-10-2026 | — |
| 50 | Integración con DBNet | CT-DBNET en PROPUESTA | 14-10-2026 | Validación con Finanzas – Fer |
| 51 | API BAP y homologación con DBNet | Insumo para validar CT-DBNET | — | Análisis en curso (In Analysis) |
| 52 | Banco de Chile / Santander | Banco de Chile: Dummy (6b), cliente Real y webhook con `X-Signature` (6c). Santander: solo Dummy (6b) | 20-10-2026 / 27-10-2026 | Sandbox de cada banco; adaptador Real de Santander |
| 53 | Khipu | Dummy (6b), cliente Real y verificación de la notificación (6c) | 20-10-2026 / 27-10-2026 | Prueba en el sandbox de Khipu |
| 54 | Depósito / transferencia | Flujo descrito en CT-DEP | 14-10-2026 | Decisión de la opción de confirmación |
| 55 | Implementar y testear bancos | Cubierta por las tareas 52 y 116–118 | — | — |

## Pagos BCI (tareas 116–118)

| Tarea | Descripción | Hito del plan | Fecha | Pendiente fuera del plan |
|---|---|---|---|---|
| 116 | Integración y reglas de negocio BCI | CT-BCI en PROPUESTA | 14-10-2026 | Modalidad de integración y reglas de conciliación (Finanzas – Fer) |
| 117 | Implementar integración BCI | Adaptador Dummy (6b) | 20-10-2026 | Adaptador Real |
| 118 | Testear y validar flujo de pago BCI | — | — | Prueba de punta a punta tras el adaptador Real |

## Sistemas sin tareas en estos grupos

| Sistema | Hito del plan | Fecha |
|---|---|---|
| Tracking | CT-TRACK en PROPUESTA (6a); Dummy (6b); cliente Real contra simulador (6c) | 14-10-2026 / 20-10-2026 / 27-10-2026 |
| DBNet/SII | Dummy (6b); cliente Real contra simulador (6c) | 20-10-2026 / 27-10-2026 |
| Firma | CT-SIGN en PROPUESTA (6a); Dummy (6b) | 14-10-2026 / 20-10-2026 |
| Storage | CT-STORAGE en PROPUESTA (6a); Dummy en memoria (6b) | 14-10-2026 / 20-10-2026 |
| Dispute | CT-DISP en PROPUESTA (6a) | 14-10-2026 |
| Navesoft | CT-NAVE en PROPUESTA (6a) | 14-10-2026 |
| Correo | Sin cambios: SMTP existente | — |
| IA | Fase 2, sin contrato | — |
