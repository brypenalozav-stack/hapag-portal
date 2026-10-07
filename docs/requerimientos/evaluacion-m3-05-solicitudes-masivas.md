# M3-05: evaluación de solicitudes masivas de cambio de almacén

Ficha M3-05 (Fase 1) de `Portal_2.0_Especificacion_Funcional_validada(1).docx`. Este documento cumple sus dos criterios de aceptación:

1. Entrega el resultado de la evaluación: la alternativa propuesta para que los clientes de alto volumen ingresen solicitudes masivas y su factibilidad.
2. Considera los casos de alto volumen mencionados en la ficha: Delfin, Falabella y Bagno.

## Problema

Hoy un cliente de alto volumen ingresa una solicitud de cambio de almacén por embarque. Con decenas de BL por semana, el portal deja de servir para su operación diaria y la gestión vuelve al correo o a Customer Service.

## Alternativas evaluadas

| Alternativa | Descripción | Esfuerzo | Riesgo | Resultado |
|---|---|---|---|---|
| **A. Lista en el portal** | El cliente pega desde su planilla, o escribe, una línea por solicitud con el formato `BL;almacén de destino;contenedor;almacén actual`. Puede indicar un destino común para todas las líneas. | Bajo | Bajo | **Recomendada e implementada** |
| B. Carga de archivo Excel | El cliente sube un archivo `.xlsx` con una plantilla fija. | Medio | Medio: versiones de plantilla y formatos de celda. | Opcional, sobre la base de A |
| C. Integración B2B (API) | El sistema del cliente envía las solicitudes directamente, con credenciales de API. | Alto | Medio: contrato y soporte por cliente. | Fase posterior, si un cliente lo pide |
| D. Gestión asistida | Customer Service ingresa el lote en nombre del cliente. | Bajo | Alto: mantiene la carga manual que la ficha busca eliminar. | Descartada como solución principal |

La alternativa A cubre el caso común, que es copiar columnas desde una planilla, sin exigir una plantilla ni un formato de archivo. La alternativa B puede agregarse después reutilizando la misma validación y el mismo proceso en segundo plano. La alternativa C solo se justifica si un cliente necesita automatizar el envío desde su propio sistema.

## Cómo funciona la alternativa implementada

- **Ingreso.** En "Cambio de almacén", la sección de solicitud masiva acepta hasta **500 líneas** por envío. Se separan con punto y coma, coma o tabulación, y si la primera línea es un encabezado ("BL…") se omite. El BL es lo único obligatorio en cada línea; el destino puede venir del valor común del formulario.
- **Validación al recibir.** Se valida el acceso del usuario a cada BL (NF-05). Las líneas sin acceso o con datos inválidos quedan rechazadas de inmediato, con su motivo, y no bloquean las demás.
- **Proceso en segundo plano.** El resto de las líneas se procesa por tramos: `WarehouseChanges:BatchChunkSize` (50 por defecto) cada `WarehouseChanges:BatchIntervalSeconds`. Así la operación individual del resto de los usuarios no se degrada (NF-19).
- **Avance y resultado.** El cliente ve el porcentaje de avance y el resultado de cada línea (procesada o fallida, con su motivo) en la vista de progreso del lote (NF-19).
- **Cobro.** Las solicitudes que generan cargo siguen el mismo flujo de pago que las individuales (relación con M5-01).

Referencias en el código:

- `backend/src/HapagPortal.Application/WarehouseChanges/Bulk/WarehouseChangeBatchCommands.cs`
- `backend/src/HapagPortal.WebApi/BackgroundServices/WarehouseChangeBatchWorker.cs`
- `frontend/src/app/features/warehouse/bulk-lines.ts`
- `frontend/src/app/features/warehouse/warehouse-bulk-progress/`
- Endpoints `POST/GET api/v1/warehouse-changes/bulk` y `GET api/v1/warehouse-changes/bulk/{id}`

## Casos de alto volumen

La ficha nombra a tres clientes. Hapag-Lloyd no ha entregado sus volúmenes, así que esta evaluación no supone cifras. La factibilidad se evalúa por perfil de uso.

| Cliente | Perfil a confirmar con Hapag-Lloyd | Ajuste de la alternativa A |
|---|---|---|
| **Delfin** | Importador frecuente con muchos BL por semana y destinos de almacén recurrentes. | El destino común del formulario evita repetirlo en cada línea; 500 líneas por envío cubren una semana típica de alto volumen. |
| **Falabella** | Retail con varios contenedores por BL y operación concentrada en temporadas. | La línea admite el contenedor, de modo que cada contenedor se solicita por separado. En temporada alta, varios envíos de 500 líneas se encolan sin afectar a otros usuarios. |
| **Bagno** | Volumen alto y estable, con lista proveniente de su sistema interno. | Pegar columnas desde la planilla exportada funciona sin plantilla. Si quiere automatizar, la alternativa C queda como evolución. |

**Factibilidad: alta.** La alternativa A está construida y probada (unitarias y e2e) y no depende de integraciones nuevas. Lo único pendiente es validar con los tres clientes el formato de su planilla y el volumen real de un envío. Si alguno supera 500 líneas habitualmente, el límite se ajusta en el validador (`SubmitWarehouseChangeBatchCommandValidator.MaxItems`) o se divide en varios envíos.

## Próximos pasos propuestos

1. Hapag-Lloyd confirma con Delfin, Falabella y Bagno el volumen típico de un envío y una muestra de su planilla.
2. Se prueba un envío real de cada cliente en el ambiente de pruebas (NF-24).
3. Según el resultado, se decide si hace falta la alternativa B (archivo Excel) o la C (API).
