# CT-DISP – Dispute de productos digitales

| Campo | Valor |
|---|---|
| Contrato | CT-DISP |
| Estado | PROPUESTA – pendiente de validación con Customer Service – Cami/Mati |
| Responsable | Customer Service – Cami/Mati |
| Puerto | Sin puerto |
| Fichas | M2-05 |
| Tareas de Pendientes | Sin tarea |

## Propósito

El módulo de Dispute de productos digitales existe como sitio web de Hapag-Lloyd, pero el portal no lo enlaza. M2-05 pide un acceso visible que redirija a ese sitio. No hay intercambio de datos: la integración es un enlace.

## Comportamiento propuesto

- El enlace se muestra en el detalle del embarque y en el menú de ayuda.
- La URL se lee de la configuración del portal, por país e idioma, para cambiarla sin desplegar.
- El enlace se abre en una pestaña nueva con `rel="noopener noreferrer"` y lo anuncia en el texto accesible ("se abre en una pestaña nueva"), según M11-04 y M11-05.
- El portal no envía datos del usuario ni del embarque en la URL, salvo que Customer Service confirme que el sitio de Dispute acepta el número de BL como parámetro.

## Puntos a validar

1. URL del sitio de Dispute para Chile y Bolivia, y si existe versión en inglés.
2. Si el sitio acepta el número de BL como parámetro de consulta.
3. Pantallas del portal donde debe aparecer el acceso.
