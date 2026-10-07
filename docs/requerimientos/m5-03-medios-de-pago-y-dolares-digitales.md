# M5-03: medios de pago y futura incorporación de dólares digitales

Ficha M5-03 (Fase 1, en revisión) de `Portal_2.0_Especificacion_Funcional_validada(1).docx`. El tercer criterio de aceptación pide que "la definición funcional deje registrada la eventual incorporación futura de dólares digitales como medio de pago". Este documento es ese registro, y además resume cómo se cumplen los otros dos criterios.

## 1. Medios de pago vigentes

El portal mantiene operativos los tres medios de pago actuales:

| Medio | Tipo | Proveedor en el código |
|---|---|---|
| Khipu | En línea | `HttpKhipuPaymentProvider` (API v3) |
| Botón de bancos | En línea | Santander: `GetnetPaymentProvider` (Getnet Web Checkout). BCI: `BciPagosPaymentProvider` (Bci Pagos). Banco de Chile: `BancoChileFormPaymentProvider` (formulario firmado configurable, sin configurar hasta recibir el manual del banco). Detalle en `docs/integraciones/pasarelas-pago.md` |
| Depósito bancario mediante boleta | Depósito | Sin proveedor. Se emite la boleta y Finanzas confirma el abono |

Los contratos de cada integración siguen como **propuesta pendiente de validación** con Finanzas y Nexus/IT (ver `docs/requerimientos/matriz-trazabilidad.md`, fila M5-03).

## 2. Agregar un medio o reemplazar Khipu

Los medios de pago son datos configurables, no código fijo:

- Cada medio es un registro de configuración (`PaymentMethodConfig`) con estos campos: país, código, nombre visible, tipo (`Kind`: en línea o depósito), monedas, orden y la clave del proveedor que lo procesa (`ProviderKey`). Se administra desde el mantenedor de medios de pago del Backoffice, con historial de cambios.
- El cobro en línea pasa por la interfaz `IPaymentProvider`. Agregar un medio nuevo requiere implementar un proveedor, registrarlo con su clave y crear el registro de configuración. Al guardar, el mantenedor valida que la clave corresponda a un proveedor registrado (`PaymentMethodFields.CheckProvider`).
- Para reemplazar Khipu se registra el nuevo proveedor, se crea el medio y se deshabilita el anterior. El carro y el historial no cambian.

## 3. Dólares digitales: registro de la eventual incorporación

**Decisión registrada.** Se deja constancia de que, a futuro, Hapag-Lloyd podría incorporar **dólares digitales** (por ejemplo, monedas estables respaldadas en dólares estadounidenses) como medio de pago del portal. **No forma parte del alcance de Fase 1 ni de Fase 2** y no hay ningún desarrollo comprometido.

Cuando Hapag-Lloyd decida avanzar, la incorporación seguiría el mecanismo de la sección 2 (un proveedor nuevo y un medio configurado). Quedan definidas desde ya las condiciones previas que tendría que resolver:

| Tema | Lo que se debe definir |
|---|---|
| Regulación | Tratamiento legal y tributario en Chile y en Bolivia; aprobación de Cumplimiento y Finanzas. |
| Proveedor | Procesador o pasarela que reciba el pago y liquide a Hapag-Lloyd, con confirmación por notificación (webhook) igual que los medios en línea. |
| Moneda y tipo de cambio | Moneda en que se registra el cargo (USD) y regla de conversión cuando el cargo esté en moneda local (relación con M5-04 y M5-05). |
| Conciliación y facturación | Cómo Finanzas concilia el abono y cómo se emite la factura del pago. |
| Devoluciones | Procedimiento de reversa o devolución si el pago se anula. |

Hasta que esas definiciones existan, el portal no muestra ni anuncia este medio a los clientes.

## Dependencias

Se relaciona con M5-01 (carro de compra), M5-04 (monedas por recargo) y M5-06 (comprobante de depósito).
