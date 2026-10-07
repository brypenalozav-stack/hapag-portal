# CT-NAVE – Navesoft (intercambio actual por CSV/FTP)

| Campo | Valor |
|---|---|
| Contrato | CT-NAVE |
| Estado | PROPUESTA – pendiente de validación con Nexus/IT – Lucho |
| Responsable | Nexus/IT – Lucho |
| Puerto | Sin puerto |
| Fichas | M3-04, M3-06; capítulo de integración de la especificación |
| Tareas de Pendientes | 35 (Conexión Portal – implementar conexión con Nexus mediante API) |
| Función NexusV2 | Conexión Portal, Cambio de Almacén |

## Propósito

Describe el intercambio que existe hoy entre Nexus/Facturación y el portal de pagos en producción, que corre sobre Navesoft. No hay API: el intercambio es por archivos CSV vía FTP. El Portal 2.0 no consume estos archivos; el documento sirve para que la definición de la API de la tarea 35 cubra los mismos datos y para planificar el retiro del CSV/FTP.

## Intercambio actual

| Archivo / flujo | Dirección | Contenido | Observación |
|---|---|---|---|
| Import | Nexus/Facturación → Navesoft | Cargos de importación por BL | Envío por nave |
| MHD | Nexus/Facturación → Navesoft | Cargos MHD | |
| Pagos Naves | Nexus/Facturación → Navesoft | Cargos y montos por nave, incluidas las tarifas de cambio de almacén | Las tarifas viajan hoy fijas en el CSV (NexusV2 «Tarifa») |
| Cambio de almacén | Navesoft → Facturación | Solicitudes de cambio de almacén y su cobro | Las solicitudes no llegan a Nexus; llegan por Navesoft |
| Pagos y cupones | Navesoft → Facturación | Pagos, cupones y cupones anulados del portal en producción | Vuelven a las tablas de cruce de Facturación, que Nexus consulta en solo lectura |

## Datos a confirmar en la validación

| Dato | Estado |
|---|---|
| Nombre y estructura (columnas, separador, codificación) de cada archivo | Por confirmar |
| Frecuencia y horario de envío | Por confirmar |
| Servidor FTP y mecanismo de autenticación | Por confirmar (no se registra en el repositorio) |
| Manejo de reenvíos y duplicados | Por confirmar |

## Relación con el Portal 2.0

- Los cargos y tarifas que hoy viajan en el CSV se leen desde Nexus por API (CT-NEXUS, `GET /tariffs`).
- Las solicitudes de cambio de almacén se registran en el Portal 2.0 (M3-04, M3-05) y el estado de almacén se consulta en Mercurio (CT-MERC).
- El retiro del CSV/FTP depende de la definición de la API de la tarea 35 (quién expone, autenticación, idempotencia).
