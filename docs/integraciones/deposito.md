# CT-DEP – Depósito y transferencia bancaria

| Campo | Valor |
|---|---|
| Contrato | CT-DEP |
| Estado | PROPUESTA – pendiente de validación con Nexus/IT – Lucho (apoyo: Finanzas – Fer, Ricardo) |
| Responsable | Nexus/IT – Lucho (apoyo: Finanzas – Fer, Ricardo) |
| Puerto | Sin puerto. El adjunto del comprobante usa `IFileStorage` (CT-STORAGE) |
| Fichas | M5-03, M5-06, M7-02, NF-04 |
| Tareas de Pendientes | 31 (Confirmación de pago), 54 (Implementar y testear depósito/transferencia) |
| Función NexusV2 | Confirmación Pago |

## Propósito

Describe cómo se confirma hoy un pago por depósito o transferencia bancaria y qué datos debe recibir el portal para reflejar esa confirmación. Es un medio de pago vigente (M5-03) que no pasa por una pasarela.

## Flujo actual

1. El cliente genera la boleta en el portal en producción y paga por depósito o transferencia.
2. Envía el comprobante por canales externos (correo).
3. Un equipo interno de Hapag-Lloyd verifica el abono en el banco y actualiza el estado de la gestión.
4. Nexus solo consulta boletas y pagos por BL (módulo «Consultas»); no tiene ingreso manual de transferencias o depósitos ni escribe la confirmación hacia el portal.

En el Portal 2.0 existe hoy la confirmación manual de pagos por un administrador; el campo `Payment.DepositProofUrl` existe, pero no hay carga del comprobante.

## Datos de la confirmación (NexusV2 «Confirmación Pago»)

| Dato | Descripción |
|---|---|
| BL | Número de BL asociado |
| Boleta / cupón | Número de boleta o cupón del portal |
| Monto y moneda | Monto abonado y código ISO 4217 |
| Fecha de pago | Fecha del abono en el banco |
| Banco | Banco de destino del abono |
| N° de transacción | Número de operación bancaria (NF-04) |
| Comprobante | Archivo adjunto por el cliente (M5-06, Fase 2) |

## Opciones a validar

| Opción | Descripción | Estado |
|---|---|---|
| A. Confirmación en el portal | Finanzas confirma el pago desde el área de administración del portal, con el comprobante adjunto | Existe la confirmación manual; falta el adjunto (M5-06, Fase 2) |
| B. Confirmación desde Nexus | Nexus registra el abono (tabla `payments_received`) y el portal lo consulta por API | Requiere el módulo nuevo en Nexus (tarea 31) |

La elección entre A y B la toma Finanzas con Nexus/IT. Mientras no se decida, no hay puerto ni contrato HTTP.
