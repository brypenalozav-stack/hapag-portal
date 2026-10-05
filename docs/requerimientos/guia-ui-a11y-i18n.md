# Guía de UI, accesibilidad e idiomas — Portal 2.0 v4

Tokens, tipografía, componentes accesibles, checklist WCAG 2.2 AA e idiomas ES/EN

Versión 4.0 · 05-10-2026 · Preparado para: Hapag-Lloyd Chile y Bolivia · Elaborado por: Equipo de desarrollo Portal 2.0. Fuente: Fase 3 del plan de actualización de requerimientos Portal 2.0; decisiones Q8, Q9, DC2, DC3 y DC4 del registro de decisiones v4. Generado por `scripts/requerimientos/generar_guia.py`; no editar a mano.

## 1. Tokens de diseño

Los valores son los que fija la Fase 5a §2 del plan (decisión DC4). En el código viven solo en `frontend/src/styles/_tokens.scss`, como variables Sass sin CSS emitido; el resto del frontend usa `var(--hl-…)` o las variables de Bootstrap configuradas con `@use 'bootstrap/scss/bootstrap' with (…)`. Hoy la interfaz muestra el azul por defecto de Bootstrap (`--bs-primary: #0d6efd`) porque los overrides de `styles.scss` no se aplican: el cambio visual de la Fase 5a es mayor de lo que sugiere el código actual.

### 1.1 Lista cerrada de tokens

Es la misma lista que usa el prototipo de la Fase 4. Un color nuevo se agrega aquí, en `_tokens.scss` y en el prototipo en el mismo cambio; ningún otro archivo del frontend contiene valores hexadecimales (`npm run check:hex`).

| Token Sass | Variable CSS | Valor | Uso | Variante oscura |
|---|---|---|---|---|
| `$hl-dark` | `--hl-dark` | #33424f | Secundario (`$secondary`), barra de navegación y menú lateral | Sin cambio (#33424f); también es la superficie de tarjetas en oscuro |
| `$hl-orange` | `--hl-orange` | #ff6600 | Solo logotipo y acentos no textuales | Sin cambio (#ff6600) |
| `$hl-orange-700` | `--hl-orange-700` | #b84a00 | Primario (`$primary`): botones, enlaces y borde de foco de campos | #b84a00 solo como fondo de botón con texto blanco; los enlaces usan `--hl-dark-text` subrayado |
| `$hl-green` | `--hl-green` | #009840 | Acentos no textuales de éxito (íconos grandes, barras) | Sin cambio (#009840) |
| `$hl-green-700` | `--hl-green-700` | #007a33 | Éxito (`$success`): texto e insignias | #007a33 como fondo de insignia con texto blanco |
| `$hl-blue` | `--hl-blue` | #004d6c | Información (`$info`) y anillo de foco sobre fondos claros | #004d6c como fondo; el foco pasa a `--hl-white` |
| `$hl-light-gray` | `--hl-light-gray` | #f5f7fa | Fondo de página (`$body-bg`) | #1a202c (`$hl-dark-bg`) |
| `$hl-border` | `--hl-border` | #e2e8f0 | Bordes de tarjetas, tablas y separadores | #4a5568 (`$hl-text-muted`) |
| `$hl-body-color` | `--hl-body-color` | #2d3748 | Texto de cuerpo (`$body-color`) | #e2e8f0 (`$hl-dark-text`) |
| `$hl-white` | `--hl-white` | #ffffff | Superficies (tarjetas, tablas) y texto sobre fondos oscuros | Superficie: #33424f (`$hl-dark`); texto y foco: #ffffff |
| `$hl-dark-bg` | `--hl-dark-bg` | #1a202c | Fondo de página en tema oscuro | — |
| `$hl-dark-text` | `--hl-dark-text` | #e2e8f0 | Texto en tema oscuro | — |
| `$hl-surface-muted` | `--hl-surface-muted` | #f8fafc | Superficies secundarias (cabeceras de tabla, filtros) | #33424f (`$hl-dark`) |
| `$hl-text-muted` | `--hl-text-muted` | #4a5568 | Texto secundario; reemplaza a #718096 (4,02:1 sobre blanco) | #e2e8f0 (`$hl-dark-text`) |
| — | `--hl-focus` | #004d6c | Anillo de foco `:focus-visible` (3 px, desplazamiento 2 px). En `.hl-navbar` y `.hl-sidebar` vale #ffffff | #ffffff (`$hl-white`) |

### 1.2 Contraste de los pares documentados

Contraste calculado por `generar_guia.py` con la fórmula de luminancia relativa de WCAG 2.2. Umbrales: 4,5:1 para texto normal (1.4.3) y 3:1 para texto grande, componentes de interfaz y foco (1.4.3 y 1.4.11).

| Tema | Primer plano | Fondo | Contraste | Texto normal (4,5:1) | Texto grande y no textual (3:1) | Uso |
|---|---|---|---|---|---|---|
| Claro | `$hl-body-color` #2d3748 | `$hl-white` #ffffff | 11,99:1 | Cumple | Cumple | Texto de cuerpo sobre tarjetas |
| Claro | `$hl-body-color` #2d3748 | `$hl-light-gray` #f5f7fa | 11,17:1 | Cumple | Cumple | Texto de cuerpo sobre el fondo de página |
| Claro | `$hl-text-muted` #4a5568 | `$hl-white` #ffffff | 7,53:1 | Cumple | Cumple | Texto secundario |
| Claro | `$hl-text-muted` #4a5568 | `$hl-light-gray` #f5f7fa | 7,01:1 | Cumple | Cumple | Texto secundario sobre el fondo de página |
| Claro | `$hl-text-muted` #4a5568 | `$hl-surface-muted` #f8fafc | 7,19:1 | Cumple | Cumple | Texto secundario en cabeceras de tabla y filtros |
| Claro | `$hl-orange-700` #b84a00 | `$hl-white` #ffffff | 5,23:1 | Cumple | Cumple | Enlaces y botones de contorno |
| Claro | `$hl-orange-700` #b84a00 | `$hl-light-gray` #f5f7fa | 4,87:1 | Cumple | Cumple | Enlaces sobre el fondo de página |
| Claro | `$hl-white` #ffffff | `$hl-orange-700` #b84a00 | 5,23:1 | Cumple | Cumple | Texto de botón primario |
| Claro | `$hl-green-700` #007a33 | `$hl-white` #ffffff | 5,48:1 | Cumple | Cumple | Texto de éxito |
| Claro | `$hl-green-700` #007a33 | `$hl-light-gray` #f5f7fa | 5,11:1 | Cumple | Cumple | Texto de éxito sobre el fondo de página |
| Claro | `$hl-white` #ffffff | `$hl-green-700` #007a33 | 5,48:1 | Cumple | Cumple | Insignia de éxito |
| Claro | `$hl-blue` #004d6c | `$hl-white` #ffffff | 9,22:1 | Cumple | Cumple | Texto informativo y anillo de foco |
| Claro | `$hl-blue` #004d6c | `$hl-light-gray` #f5f7fa | 8,59:1 | Cumple | Cumple | Anillo de foco sobre el fondo de página |
| Claro | `$hl-dark` #33424f | `$hl-white` #ffffff | 10,33:1 | Cumple | Cumple | Títulos y elementos secundarios |
| Claro | `$hl-white` #ffffff | `$hl-dark` #33424f | 10,33:1 | Cumple | Cumple | Texto y foco en la barra de navegación y el menú lateral |
| Claro | `$hl-orange` #ff6600 | `$hl-white` #ffffff | 2,94:1 | No cumple | No cumple | Logotipo y acentos: no apto para texto ni para bordes de controles |
| Claro | `$hl-orange` #ff6600 | `$hl-dark` #33424f | 3,52:1 | No cumple | Cumple | Logotipo sobre la barra de navegación |
| Claro | `$hl-green` #009840 | `$hl-white` #ffffff | 3,77:1 | No cumple | Cumple | Acento no textual de éxito; no apto para texto |
| Claro | `$hl-border` #e2e8f0 | `$hl-white` #ffffff | 1,23:1 | No cumple | No cumple | Separadores decorativos (ver nota sobre campos de formulario) |
| Oscuro | `$hl-dark-text` #e2e8f0 | `$hl-dark-bg` #1a202c | 13,24:1 | Cumple | Cumple | Texto de cuerpo |
| Oscuro | `$hl-dark-text` #e2e8f0 | `$hl-dark` #33424f | 8,38:1 | Cumple | Cumple | Texto sobre superficies |
| Oscuro | `$hl-white` #ffffff | `$hl-dark-bg` #1a202c | 16,32:1 | Cumple | Cumple | Anillo de foco |
| Oscuro | `$hl-white` #ffffff | `$hl-orange-700` #b84a00 | 5,23:1 | Cumple | Cumple | Texto de botón primario |
| Oscuro | `$hl-orange-700` #b84a00 | `$hl-dark-bg` #1a202c | 3,12:1 | No cumple | Cumple | Enlace naranja sobre fondo oscuro: no permitido como texto |
| Oscuro | `$hl-text-muted` #4a5568 | `$hl-dark-bg` #1a202c | 2,17:1 | No cumple | No cumple | Separadores decorativos |

### 1.3 Reglas de uso del color

- `#ff6600` (`$hl-orange`) se reserva para el logotipo, que está exento de 1.4.3, y para acentos no textuales que no transmiten información por sí solos. Con 2,94:1 sobre blanco no alcanza ni el 3:1 de 1.4.11, por eso no se usa en texto, bordes de controles, íconos de estado ni foco.
- El primario es `#b84a00` (`$hl-orange-700`): 5,23:1 sobre blanco y 4,87:1 sobre `#f5f7fa`.
- El éxito en texto es `#007a33` (`$hl-green-700`, 5,48:1). `#009840` (3,77:1) solo se usa en acentos no textuales.
- El texto secundario es `#4a5568` (`$hl-text-muted`, 7,53:1); `#718096` (4,02:1) no se usa.
- El color nunca es el único medio para transmitir un estado (1.4.1): las insignias de estado llevan texto y los errores llevan ícono y mensaje.
- Campos de formulario: `$hl-border` da 1,23:1 sobre blanco. Si el borde es lo único que identifica el campo, 1.4.11 exige 3:1. Observación para la Fase 5a: antes de cerrar M11-04, el borde de los campos debe usar un tono con al menos 3:1 (por ejemplo `$hl-text-muted`, 7,53:1) o el campo debe tener otro indicador visual con ese contraste. En tarjetas, tablas y separadores el borde es decorativo y no requiere 3:1.
- Tema oscuro: los tokens oscuros se declaran en `[data-bs-theme="dark"]`; el selector de tema queda pendiente (tarea M11-07). Sobre `#1a202c` el naranja `#b84a00` da 3,12:1, por eso en oscuro los enlaces usan `--hl-dark-text` subrayado y el naranja queda como fondo de botón con texto blanco.

## 2. Tipografía

Ambas familias tienen licencia SIL Open Font License 1.1 (OFL-1.1), que permite incrustarlas y servirlas desde el portal sin costo. Se configuran en la Fase 5a mediante `$font-family-sans-serif` y `$headings-font-family` de Bootstrap.

| Uso | Familia y respaldo | Peso | Tamaño | Licencia |
|---|---|---|---|---|
| Cuerpo, formularios y tablas | Inter, system-ui, -apple-system, sans-serif | 400; 600 para énfasis | 1rem (16 px) con interlineado 1,5 | OFL-1.1 |
| Títulos (h1–h6) | Montserrat, Inter, system-ui, sans-serif | 600 (`$headings-font-weight`) | Escala de Bootstrap en rem | OFL-1.1 |
| Montos, números de BL y fechas en tablas | Inter con `font-variant-numeric: tabular-nums` | 400 | 0,875rem como mínimo | OFL-1.1 |

- Todos los tamaños se expresan en `rem`, para que el texto crezca al 200 % sin pérdida de contenido (1.4.4).
- Ningún texto informativo baja de 0,75rem (12 px); el texto de cuerpo no baja de 0,875rem (14 px).
- Sin alturas fijas en contenedores de texto: el contenido admite el espaciado de 1.4.12 (interlineado 1,5; párrafos 2; letras 0,12; palabras 0,16 veces el tamaño de la fuente).
- No se usan imágenes de texto (1.4.5); el logotipo es la única excepción.
- Los títulos siguen una jerarquía sin saltos (h1 único por pantalla, luego h2, h3).

## 3. Componentes y patrones

Patrones obligatorios para las pantallas de Angular y para el prototipo. Todos los textos, incluidos `aria-label`, `alt`, `title` y `placeholder`, salen de claves Transloco (sección 5).

### 3.1 Tablas de datos

- Cada tabla lleva `<caption>`; si el título ya es visible, el caption puede ir con `class="visually-hidden"`.
- Los `th` de cabecera llevan `scope="col"`; los encabezados de fila, `scope="row"`.
- Las tablas no se usan para maquetar.
- El contenedor con desplazamiento horizontal (`.table-responsive`) es enfocable (`tabindex="0"`) y tiene nombre accesible, para que se pueda recorrer con teclado.
- El orden de columnas se anuncia con `aria-sort` en el `th` activo.
- Los montos se alinean a la derecha y siempre muestran el código ISO de la moneda.

```
<table class="table">
  <caption class="visually-hidden">{{ 'bl.list.caption' | transloco }}</caption>
  <thead>
    <tr>
      <th scope="col">{{ 'bl.list.number' | transloco }}</th>
      <th scope="col">{{ 'bl.list.vessel' | transloco }}</th>
    </tr>
  </thead>
</table>
```

### 3.2 Formularios

- Cada control tiene `label for` asociado a su `id` (regla `label-has-associated-control` de angular-eslint).
- Las ayudas y el mensaje de error se vinculan con `aria-describedby`; el campo con error lleva `aria-invalid="true"`.
- Los campos obligatorios usan `required` y lo indican en texto, no solo con un asterisco de color.
- Los campos de datos personales llevan `autocomplete` (`email`, `username`, `current-password`, `new-password`, `organization`, `tel`) (1.3.5).
- Al enviar con errores se muestra un **resumen de errores** al inicio del formulario: contenedor con `tabindex="-1"` que recibe el foco, un título y un enlace por error que lleva al campo.
- El mensaje de error dice qué pasó y cómo corregirlo (3.3.3), por ejemplo el formato esperado del RUT o NIT.
- Los datos ya ingresados en el mismo proceso no se vuelven a pedir (3.3.7).
- El pago muestra un resumen para confirmar antes de enviar (3.3.4).

```
<label for="rut" class="form-label">{{ 'register.form.taxId' | transloco }}</label>
<input id="rut" class="form-control" required autocomplete="off"
       aria-describedby="rut-ayuda rut-error" [attr.aria-invalid]="rutInvalido">
<div id="rut-ayuda" class="form-text">{{ 'register.form.taxIdHelp' | transloco }}</div>
<div id="rut-error" class="invalid-feedback">{{ 'register.form.taxIdError' | transloco }}</div>
```

### 3.3 Mensajes dinámicos con aria-live

- El shell tiene dos regiones `visually-hidden`: `aria-live="polite"` y `aria-live="assertive"`. Las escribe `LiveAnnouncerService` (Fase 5c), sin CDK.
- **Pagos:** el resultado (confirmado, rechazado, pendiente) se anuncia en la región polite sin mover el foco.
- **Cargas:** la importación de BL anuncia el inicio y el resultado con el número de registros.
- **Errores de envío:** se anuncian en la región assertive y se muestra el resumen de errores.
- Los avisos temporales (toasts) no desaparecen antes de 5 segundos y se pueden cerrar con teclado.

### 3.4 Estados vacío y error (NF-11)

Componente `state-message` (Fase 5c) con `kind: 'empty' | 'error'`. Se usa en dashboard, listado y detalle de BL, listado de pagos y comprobantes.

| Estado | Condición | Rol | Texto (ES) | Acción |
|---|---|---|---|---|
| Vacío (`empty`) | HTTP 200 con lista vacía | `role="status"` | No hay datos | Ninguna |
| Error (`error`) | HTTP 5xx o estado 0 (sin conexión) | `role="alert"` | Servicio temporalmente no disponible | Botón Reintentar, que repite la consulta |

### 3.5 Navegación, foco y movimiento

- Enlace para saltar al contenido (`.hl-skip-link`) como primer elemento enfocable; lleva a `<main id="contenido-principal" tabindex="-1">`.
- Regiones `<header>`, `<nav>` con `aria-label` y `<main>`; el enlace activo del menú lleva `aria-current="page"`.
- Foco visible en todo elemento interactivo: `:focus-visible { outline: 3px solid var(--hl-focus); outline-offset: 2px; }`. Ningún elemento fijo tapa el foco (2.4.11).
- Botón de menú con `type="button"`, nombre accesible, `aria-controls` y `aria-expanded`.
- Selector de idioma ES/EN como grupo de botones con `aria-pressed`.
- Los botones de solo ícono miden al menos 24×24 px (2.5.8) y llevan `aria-label`; los íconos decorativos, `aria-hidden="true"`.
- Con `prefers-reduced-motion: reduce`, animaciones y transiciones bajan a 0,01 ms y `scroll-behavior` pasa a `auto`.

## 4. Checklist WCAG 2.2 AA

Nivel de conformidad decidido en Q9: los 55 criterios A y AA de la Recomendación W3C *Web Content Accessibility Guidelines (WCAG) 2.2* (https://www.w3.org/TR/WCAG22/); 4.1.1 queda fuera por obsoleto. La lista canónica está en `scripts/requerimientos/wcag22_a_aa.json`.

Evidencia exigida por Q9: `ng lint` sin errores en CI; axe con 0 violaciones en ES y EN; Lighthouse Accesibilidad ≥ 95; prueba manual con teclado y NVDA siguiendo esta tabla.

- **eslint:** reglas `templateAccessibility` de angular-eslint en `ng lint`.
- **axe:** `@axe-core/playwright` con los tags `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa` y `wcag22aa`.
- **teclado:** recorrido solo con Tab, Mayús+Tab, Enter, Espacio, flechas y Escape.
- **NVDA:** lector de pantalla NVDA con Chrome o Firefox.
- **Lighthouse:** categoría Accesibilidad.
- **revisión visual:** inspección manual con zoom, ancho de 320 px, espaciado de texto o emulación.

| ID | Criterio | Nivel | Aplica | Método | Pantalla |
|---|---|---|---|---|---|
| 1.1.1 | Contenido no textual | A | Sí | eslint, axe, NVDA | Todas: logotipo, íconos y gráficos del dashboard |
| 1.2.1 | Solo audio y solo vídeo (grabado) | A | No aplica: el portal no publica audio ni vídeo | revisión visual | Todas (se revisa al incorporar contenido multimedia) |
| 1.2.2 | Subtítulos (grabado) | A | No aplica: el portal no publica audio ni vídeo | revisión visual | Todas (se revisa al incorporar contenido multimedia) |
| 1.2.3 | Audiodescripción o medio alternativo (grabado) | A | No aplica: el portal no publica audio ni vídeo | revisión visual | Todas (se revisa al incorporar contenido multimedia) |
| 1.2.4 | Subtítulos (en directo) | AA | No aplica: el portal no publica audio ni vídeo | revisión visual | Todas (se revisa al incorporar contenido multimedia) |
| 1.2.5 | Audiodescripción (grabado) | AA | No aplica: el portal no publica audio ni vídeo | revisión visual | Todas (se revisa al incorporar contenido multimedia) |
| 1.3.1 | Información y relaciones | A | Sí | eslint, axe, NVDA | Listado de BL, detalle de BL, pagos, comprobantes y formularios |
| 1.3.2 | Secuencia significativa | A | Sí | NVDA, revisión visual | Todas |
| 1.3.3 | Características sensoriales | A | Sí | revisión visual | Todas (instrucciones sin depender de forma, color o posición) |
| 1.3.4 | Orientación | AA | Sí | revisión visual | Todas, en móvil vertical y horizontal (NF-21) |
| 1.3.5 | Identificación del propósito de las entradas | AA | Sí | axe, revisión visual | Inicio de sesión, registro y recuperación de contraseña |
| 1.4.1 | Uso del color | A | Sí | axe, revisión visual | Insignias de estado, errores de formulario y enlaces en texto |
| 1.4.2 | Control del audio | A | No aplica: el portal no reproduce audio | revisión visual | Todas |
| 1.4.3 | Contraste (mínimo) | AA | Sí | axe, Lighthouse | Todas (tokens de la sección 1) |
| 1.4.4 | Cambio de tamaño del texto | AA | Sí | revisión visual | Todas, con zoom del navegador al 200 % |
| 1.4.5 | Imágenes de texto | AA | Sí | revisión visual | Todas (logotipo exento) |
| 1.4.10 | Reajuste del contenido (reflow) | AA | Sí | revisión visual | Todas, a 320 px de ancho (NF-21) |
| 1.4.11 | Contraste no textual | AA | Sí | revisión visual | Campos de formulario, foco, íconos de estado y botones de contorno |
| 1.4.12 | Espaciado del texto | AA | Sí | revisión visual | Todas, con un marcador de espaciado de texto |
| 1.4.13 | Contenido al pasar el puntero o recibir el foco | AA | Sí | teclado, revisión visual | Tooltips y menús desplegables |
| 2.1.1 | Teclado | A | Sí | eslint, teclado | Todas |
| 2.1.2 | Sin trampas para el foco del teclado | A | Sí | teclado | Modales, menú lateral móvil y selectores de fecha |
| 2.1.4 | Atajos de teclado de un solo carácter | A | Sí: no se definen atajos de un solo carácter | teclado | Todas |
| 2.2.1 | Tiempo ajustable | A | Sí | teclado, revisión visual | Expiración de sesión y formulario de pago |
| 2.2.2 | Pausar, detener, ocultar | A | Sí | revisión visual | Dashboard (comunicados) e indicadores de carga |
| 2.3.1 | Umbral de tres destellos o menos | A | Sí | revisión visual | Todas |
| 2.4.1 | Evitar bloques | A | Sí | axe, teclado | Todas (enlace para saltar al contenido y regiones) |
| 2.4.2 | Página titulada | A | Sí | axe, Lighthouse | Todas (título traducido por ruta) |
| 2.4.3 | Orden del foco | A | Sí | teclado, NVDA | Todas |
| 2.4.4 | Propósito de los enlaces (en contexto) | A | Sí | axe, NVDA | Todas |
| 2.4.5 | Múltiples vías | AA | Sí | revisión visual | Menú lateral y búsqueda de BL |
| 2.4.6 | Encabezados y etiquetas | AA | Sí | NVDA, revisión visual | Todas |
| 2.4.7 | Foco visible | AA | Sí | teclado | Todas (`:focus-visible` con `--hl-focus`) |
| 2.4.11 | Foco no oculto (mínimo) | AA | Sí | teclado | Todas (barra superior fija y paneles laterales) |
| 2.5.1 | Gestos del puntero | A | Sí | revisión visual | Todas (sin gestos multipunto ni de trayectoria) |
| 2.5.2 | Cancelación del puntero | A | Sí | revisión visual | Botones y enlaces (la acción ocurre al soltar) |
| 2.5.3 | Etiqueta en el nombre | A | Sí | axe, NVDA | Botones e íconos con texto visible |
| 2.5.4 | Activación mediante movimiento | A | No aplica: no hay funciones por movimiento del dispositivo | revisión visual | Todas |
| 2.5.7 | Movimientos de arrastre | AA | Sí | teclado, revisión visual | Importación de BL y adjuntos (alternativa con botón) |
| 2.5.8 | Tamaño del objetivo (mínimo) | AA | Sí | axe, revisión visual | Botones de ícono, paginación y selector de idioma |
| 3.1.1 | Idioma de la página | A | Sí | axe, Lighthouse | Todas (`html[lang]` según el idioma activo) |
| 3.1.2 | Idioma de las partes | AA | Sí | NVDA, revisión visual | Selector de idioma (opción en el otro idioma con su `lang`) |
| 3.2.1 | Al recibir el foco | A | Sí | teclado | Todas |
| 3.2.2 | Al recibir entradas | A | Sí | teclado, NVDA | Selectores de país e idioma y filtros de listados |
| 3.2.3 | Navegación coherente | AA | Sí | revisión visual | Todas (shell común) |
| 3.2.4 | Identificación coherente | AA | Sí | revisión visual | Todas (términos del glosario de la sección 6) |
| 3.2.6 | Ayuda coherente | A | Sí | revisión visual | Preguntas frecuentes y asistente en la misma posición |
| 3.3.1 | Identificación de errores | A | Sí | axe, NVDA | Formularios (`aria-invalid` y resumen de errores) |
| 3.3.2 | Etiquetas o instrucciones | A | Sí | eslint, axe | Formularios |
| 3.3.3 | Sugerencias ante errores | AA | Sí | NVDA, revisión visual | Formularios (formato de RUT o NIT, fechas y montos) |
| 3.3.4 | Prevención de errores (legales, financieros, de datos) | AA | Sí | teclado, revisión visual | Formulario de pago y carro por moneda (confirmación) |
| 3.3.7 | Entrada redundante | A | Sí | revisión visual | Registro y pago |
| 3.3.8 | Autenticación accesible (mínima) | AA | Sí | teclado, revisión visual | Inicio de sesión (pegar contraseña, gestor de contraseñas) |
| 4.1.2 | Nombre, función, valor | A | Sí | eslint, axe, NVDA | Todas (menú con `aria-expanded`, selector ES/EN con `aria-pressed`) |
| 4.1.3 | Mensajes de estado | AA | Sí | NVDA | Pago, importación de BL y estados de NF-11 |

## 5. Estrategia i18n con Transloco

Alcance decidido en Q8: en Fase 1, la interfaz en español e inglés con cambio de idioma en caliente; en Fase 2, correos, PDF y asistente M10. El inglés es internacional con ortografía estadounidense. La implementación es la Fase 5b del plan.

### 5.1 Configuración

- Librería `@jsverse/transloco` (MIT) con `provideTransloco`: `availableLangs: ['es','en']`, `defaultLang: 'es'`, `fallbackLang: 'es'` y `reRenderOnLangChange: true`.
- Archivos de traducción en `frontend/public/i18n/es.json` y `frontend/public/i18n/en.json`, servidos como `/i18n/{lang}.json` por `TranslocoHttpLoader`.
- `LocaleService` guarda el idioma en `hl_lang`, calcula el locale (`es-CL`, `es-BO` o `en`) y el huso del país, y actualiza `document.documentElement.lang` sin recargar la página.
- El selector ES/EN está en la barra de navegación.

### 5.2 Claves

- Formato `modulo.componente.elemento`, en minúsculas y camelCase dentro de cada segmento: `bl.list.caption`, `payments.form.submit`, `auth.login.password`.
- Textos compartidos bajo `common.*` (por ejemplo `common.actions.retry`); los estados de negocio, bajo `status.<code>`.
- Una clave por contexto: no se reutiliza una clave en pantallas distintas aunque el texto coincida.
- También son claves los atributos `aria-label`, `alt`, `title` y `placeholder`, y los mensajes en TypeScript (`translate()`).
- Los términos salen del glosario de la sección 6.

### 5.3 Formatos por idioma y país (Q8)

| Idioma / país | Locale | Fecha | Hora | Huso horario | Número | Montos |
|---|---|---|---|---|---|---|
| Español, Chile | `es-CL` | dd-MM-yyyy (05-10-2026) | 24 h (14:30) | America/Santiago | 1.234.567,89 | CLP 1.234.567 (0 decimales); USD 1.234,56 |
| Español, Bolivia | `es-BO` | dd/MM/yyyy (05/10/2026) | 24 h (14:30) | America/La_Paz | 1.234.567,89 | BOB 1.234,56; USD 1.234,56 |
| Inglés | `en` | dd MMM yyyy (05 Oct 2026) | 24 h (14:30) | El del país del usuario | 1,234,567.89 | CLP 1,234,567; BOB 1,234.56; EUR 1,234.56 |

- Los pipes `hlDate`, `hlNumber` y `hlCurrency` (Fase 5b) usan `Intl.*`, que admite husos IANA, y guardan en caché los formateadores por locale y opciones.
- Las fechas muestran el huso (`timeZoneName: 'short'`). Los plazos se calculan en UTC con calendario de negocio y se presentan en el huso del país (DC3).
- Los montos siempre llevan el código ISO (`currencyDisplay: 'code'`): CLP sin decimales; BOB, USD y EUR con 2.
- Los ejemplos de la tabla son ilustrativos; el formato final lo produce `Intl` con el locale indicado.

### 5.4 Redacción sin concatenar

Una frase es una sola clave con parámetros. No se arma texto uniendo claves o literales, porque el orden de las palabras cambia entre idiomas.

```
// Incorrecto
"payments.summary.prefix": "Total a pagar:",
"payments.summary.suffix": "en"

// Correcto
"payments.summary.total": "Total a pagar: {{ amount }} en {{ count }} BL"
```

### 5.5 Plurales con ICU

Los plurales usan la sintaxis ICU MessageFormat. Transloco la interpreta con el complemento `@jsverse/transloco-messageformat` (MIT); la Fase 5b debe agregarlo junto con Transloco y registrarlo en `licencias-dependencias.md`.

```
"bl.list.count": "{count, plural, =0 {Sin BL} one {# BL} other {# BL}}"
"notifications.inbox.unread": "{count, plural, =0 {No unread notifications} one {# unread notification} other {# unread notifications}}"
```

### 5.6 Paridad de claves en CI

- `npm run check:i18n` (`frontend/scripts/check-i18n.mjs`): mismas claves en `es.json` y `en.json`, sin valores vacíos y sin claves usadas que no existan.
- `npm run check:i18n-text` (`frontend/scripts/check-hardcoded-text.mjs`): ningún texto ni atributo accesible literal en las plantillas; las excepciones se declaran en el mismo script.
- `npx transloco-keys-manager extract` agrega las claves nuevas al extraer textos.
- Ambos chequeos corren en el job `frontend` de `.github/workflows/pr-tests.yml`.
- Fuera de alcance por ahora: la traducción de ProblemDetails, correos y PDF del backend (tarea de Fase 2).

## 6. Glosario ES/EN

Fuente única: `docs/requerimientos/glosario-es-en.md` (Q8). Las claves Transloco y los textos en inglés usan estos términos. 56 términos.

- Las siglas de la industria no se traducen (BL, SWB, EBL, TATC, CLD, EDS, MHD, IPO, XOM, DG, UN).
- Inglés internacional con ortografía estadounidense.
- La primera aparición de una sigla en pantalla lleva su expansión en el idioma activo.

| Español | English | Nota de uso |
|---|---|---|
| BL (conocimiento de embarque) | BL (bill of lading) | Documento de transporte; no traducir la sigla. |
| SWB / EBL | SWB (sea waybill) / EBL (electronic bill of lading) | EBL incluye, por ejemplo, Wave BL. |
| Booking / reserva | Booking | En pantalla se usa "booking" en ambos idiomas. |
| Embarque | Shipment | Conjunto BL o booking con sus contenedores. |
| Nave / viaje | Vessel / voyage |  |
| Puerto de descarga (POD) | Port of discharge (POD) |  |
| Demurrage / sobreestadía | Demurrage | Cobro por uso del contenedor sobre los días libres en terminal. |
| Detention | Detention | Cobro por uso del contenedor fuera del terminal. |
| Días libres | Free time |  |
| Calculadora de demurrage | Demurrage calculator |  |
| Gate In / Gate Out | Gate In / Gate Out | No traducir. |
| EDS | EDS (equipment depot service) | Asociado al Gate In. |
| MHD | MHD | Concepto de cobro asociado a demurrage. |
| Cargos locales | Local charges |  |
| Recargo | Surcharge |  |
| Recargo on demand | On-demand charge |  |
| Flete prepaid / collect | Prepaid / collect freight |  |
| Cambio de almacén | Warehouse change |  |
| TATC | TATC | Documento de traspaso; no traducir. |
| CLD (certificado de libre deuda) | CLD (debt clearance certificate) | Bolivia. |
| Certificado de transbordo | Transshipment certificate |  |
| Certificado de flete | Freight certificate |  |
| Carta de responsabilidad | Letter of indemnity |  |
| Carta de liberación y desconsolidado | Release and devanning letter | Bolivia. |
| Canje | BL exchange | Canje del BL por la liberación de la carga. |
| Desconsolidado | Devanning |  |
| Cupón de retiro | Pick-up voucher | Gate Out. |
| Comprobante Collect | Collect receipt | Para agencias de aduanas. |
| DIFU | DIFU | Estructura de distribución por destino final; no traducir. |
| Depósito | Depot |  |
| Consignatario (CN) | Consignee (CN) |  |
| Embarcador (SH) | Shipper (SH) |  |
| Cliente titular (CU) | Customer (CU) |  |
| Tercero | Third party |  |
| Agencia de aduanas (AGA) | Customs broker |  |
| Freight forwarder (FFWW) | Freight forwarder (FFWW) |  |
| Transportista | Carrier (haulier) | En pantalla: "Haulier". |
| Match Code (MC) | Match Code (MC) | Identificador del cliente en Hapag-Lloyd. |
| RUT / NIT | Tax ID (RUT / NIT) | RUT en Chile, NIT en Bolivia. |
| Razón social | Legal name |  |
| Mandato | Mandate |  |
| Acceso otorgado | Granted access |  |
| Acceso abierto por número de BL | Open access by BL number |  |
| Carro de compra | Cart |  |
| Medio de pago | Payment method |  |
| Boleta | Payment slip | Comprobante de pago del portal, no la boleta tributaria. |
| Comprobante de pago | Receipt |  |
| Factura | Invoice |  |
| Estado de cuenta | Statement of account |  |
| Tipo de cambio (TC) | Exchange rate |  |
| Condición de crédito | Credit terms |  |
| Exención | Exemption |  |
| Mercancía peligrosa (DG) | Dangerous goods (DG) |  |
| Orden de servicio (ODS) | Service order |  |
| Comunicado | Announcement |  |
| Bandeja de notificaciones | Notification inbox |  |
