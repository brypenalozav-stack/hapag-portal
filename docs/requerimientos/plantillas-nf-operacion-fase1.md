# Requisitos no funcionales de operación (Fase 1): plantillas para publicar

Según la decisión **DC6** (`registro-decisiones-v4.md`, aprobada el 05-10-2026), los valores de estas fichas los "define y publica Hapag-Lloyd, a través del equipo de desarrollo del Portal 2.0". Este documento es la plantilla para hacerlo:

- Cada ficha indica qué valor falta, dónde se mide y qué apoyo existe hoy en el portal.
- **Ningún valor se completa por suposición.** Todo lo marcado como *Por definir* lo completa Hapag-Lloyd.

Cuando se publique un valor, reemplace *Por definir* por el valor y anote la fecha y el responsable en la tabla de control al final.

---

## NF-10: Ventana de servicio y disponibilidad

**Criterios de aceptación:** la disponibilidad comprometida está indicada y es medible; los mantenimientos se avisan con anticipación y se hacen dentro de la ventana.

| Dato | Valor |
|---|---|
| Disponibilidad comprometida (% mensual) | *Por definir* |
| Ventana de mantenimiento (días y horario, Chile y Bolivia) | *Por definir* |
| Anticipación mínima del aviso | *Por definir* |
| Canal de aviso a los clientes | *Por definir* (sugerido: anuncio en el portal y correo) |
| Cómo se mide | Chequeo externo periódico de `GET /health` del API y del frontend; la disponibilidad se calcula como tiempo arriba sobre tiempo total del mes, descontando la ventana avisada. |

---

## NF-13: Respaldo y recuperación

**Criterios de aceptación:** el respaldo corre con la periodicidad publicada y su resultado es verificable; el procedimiento de recuperación está documentado y se probó al menos una vez.

| Dato | Valor |
|---|---|
| Periodicidad del respaldo de la base de datos (PostgreSQL) | *Por definir* |
| Retención de los respaldos | *Por definir* |
| Pérdida máxima tolerada (RPO) | *Por definir* |
| Tiempo máximo de recuperación (RTO) | *Por definir* |
| Respaldo de archivos adjuntos (almacenamiento) | *Por definir* |
| Fecha de la última prueba de recuperación y resultado | *Por definir* |

**Procedimiento de recuperación (borrador para validar):**

1. Detener la escritura: poner el API en mantenimiento.
2. Restaurar el respaldo elegido en una instancia nueva de PostgreSQL.
3. Aplicar las migraciones pendientes (`HapagPortal.DatabaseMigrations`) si el respaldo es anterior a la versión desplegada.
4. Apuntar la cadena de conexión del API a la instancia restaurada y reiniciar.
5. Verificar con `GET /health` y con una consulta de embarque y un historial de pagos.
6. Registrar el tiempo total (para comparar con el RTO) y la hora del respaldo usado (para comparar con el RPO).

---

## NF-17: Volumetría de referencia

**Criterios de aceptación:** la solución soporta la volumetría publicada sin degradar los tiempos de respuesta; el dimensionamiento permite crecer sin rediseño.

| Dato (por país) | Chile | Bolivia |
|---|---|---|
| Usuarios activos por mes (línea base del portal actual) | *Por definir* | *Por definir* |
| Usuarios concurrentes en hora punta | *Por definir* | *Por definir* |
| BL consultados por día | *Por definir* | *Por definir* |
| Pagos por día | *Por definir* | *Por definir* |
| Solicitudes de servicio por día (incluye las que hoy son manuales) | *Por definir* | *Por definir* |
| Crecimiento proyectado anual | *Por definir* | *Por definir* |

Apoyo existente para crecer sin rediseño:

- API sin estado (JWT), que puede escalar horizontalmente.
- Procesos masivos en segundo plano por tramos (NF-19).
- Paginación en el servidor de los listados.

---

## NF-18: Tiempos de respuesta

**Criterios de aceptación:** los tiempos se miden y cumplen los valores comprometidos; si una operación excede lo esperado, el portal informa que está en proceso.

| Operación | Tiempo comprometido (percentil 95) |
|---|---|
| Consulta de embarque o BL | *Por definir* |
| Listados (embarques, cargos, historial de pagos) | *Por definir* |
| Cálculo de la calculadora de sobreestadía | *Por definir* |
| Inicio de un pago en línea (hasta la redirección) | *Por definir* |
| Referencia: portal actualmente en producción | *Por definir* (medición de línea base) |

Apoyo existente:

- El portal muestra indicadores de carga (spinner y esqueletos de tabla) mientras una operación está en curso.
- Informa la indisponibilidad de una API de origen sin presentar datos parciales (NF-11).
- La medición en producción requiere definir la herramienta de monitoreo (relación con NF-26).

---

## NF-24: Ambiente de pruebas

**Criterios de aceptación:** el ambiente de pruebas replica el comportamiento funcional de producción; los equipos de Hapag-Lloyd validan en él antes de cada paso a producción.

| Dato | Valor |
|---|---|
| URL del ambiente de pruebas (frontend y API) | *Por definir* |
| Origen de los datos de prueba | *Por definir* (sugerido: datos sintéticos o anonimizados, nunca datos reales de clientes sin anonimizar) |
| Integraciones en modo de prueba (Navesoft, pagos, correo) | *Por definir* por integración (modo Dummy o sandbox del proveedor) |
| Usuarios de prueba entregados a Hapag-Lloyd (por perfil) | *Por definir* |
| Responsable de la validación previa a cada paso a producción | *Por definir* |

Apoyo existente: `ASPNETCORE_ENVIRONMENT=Staging` (`RAILWAY-DEPLOY.md`), integraciones con modo Dummy configurable y flags de funcionalidades (`Features`) iguales a los de producción.

---

## NF-25: Puesta en producción y retorno

**Criterios de aceptación:** una puesta en producción no interrumpe ni pierde operaciones iniciadas por los clientes; existe un procedimiento para volver a la versión anterior.

| Dato | Valor |
|---|---|
| Horario permitido para pasos a producción | *Por definir* (dentro de la ventana de NF-10) |
| Aprobador del paso a producción | *Por definir* |
| Tiempo máximo para decidir un retorno | *Por definir* |

**Procedimiento de puesta en producción (borrador para validar):**

1. La versión se valida en el ambiente de pruebas (NF-24) y queda etiquetada en git.
2. Se toma un respaldo de la base de datos justo antes (NF-13).
3. Las migraciones de base de datos deben ser compatibles hacia atrás: la versión anterior del API debe seguir funcionando con el esquema nuevo. Las eliminaciones de columnas van en una versión posterior.
4. Se despliega la versión nueva del API y del frontend. Los pagos en curso se confirman por notificación del proveedor, que se reintenta y es idempotente, así que no se pierden durante el reinicio.
5. Se verifica con `GET /health`, una consulta de BL y un pago de prueba.

**Procedimiento de retorno:**

1. Volver a desplegar la imagen o la etiqueta anterior del API y del frontend.
2. No revertir migraciones salvo que la versión anterior no funcione con el esquema nuevo. Si hace falta, restaurar el respaldo del paso 2 (implica perder lo registrado desde entonces y debe ser aprobado).
3. Si el problema es una funcionalidad nueva, se puede apagar su flag en `Features` sin volver de versión.
4. Avisar a los clientes si hubo indisponibilidad.

---

## Control de publicación

| Ficha | Valor publicado | Fecha | Responsable |
|---|---|---|---|
| NF-10 | Pendiente | | |
| NF-13 | Pendiente | | |
| NF-17 | Pendiente | | |
| NF-18 | Pendiente | | |
| NF-24 | Pendiente | | |
| NF-25 | Pendiente | | |
