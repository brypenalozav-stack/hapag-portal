# Patrones para páginas de detalle de registro largas y densas (B2B) — p. ej. detalle de BL con ~15 secciones

> Notas de investigación (2026-10-06). Prioridad a fuentes primarias 2018–2026. Se distingue **[Evidencia]** (investigación con usuarios / norma) de **[Guía de sistema de diseño]** (recomendación normativa sin estudio publicado) y **[Opinión/práctica]** (blogs/consultoras).
> Nota de acceso: las páginas actuales de SAP Fiori (sap.com/design-system/...) devolvieron HTTP 403 al fetch; la guía Fiori se tomó de un PDF archivado (2020-11-18) de "Object Page Floorplan" y de resultados de búsqueda que citan la documentación de SAP. El resumen del PDF fue generado por un modelo de extracción, por lo que las citas Fiori deben tratarse como paráfrasis, no texto literal.

## 1. Tabs vs acordeones vs página larga con navegación interna (TOC sticky/anclas) vs master-detail

### Takeaway
El consenso entre NN/g, GOV.UK y Carbon: **no usar tabs si el usuario necesita comparar o cruzar información entre secciones**; tabs sirven para pocas secciones independientes con una sección claramente prioritaria. Para registros con muchas secciones relacionadas, el patrón de referencia en apps empresariales (SAP Fiori Object Page) es **una sola página con barra de anclas (anchor bar) + carga diferida por sección**, cambiando a tabs solo cuando las secciones son independientes y muy pesadas.

### Cited Findings
- [Guía basada en investigación NN/g] Tabs funcionan cuando hay pocas agrupaciones claras, el contenido varía en importancia (la pestaña por defecto recibe más atención) y las etiquetas son de 1–2 palabras; **evitar tabs cuando los usuarios deben comparar información entre secciones**, porque obliga a alternar y carga la memoria a corto plazo; en ese caso es preferible una sola página. Siempre seleccionar una pestaña por defecto; evitar overflow/carrusel de pestañas; no usar MAYÚSCULAS. (Artículo 2024-08-02, revisado 2026) — [NN/g, Tabs, Used Right](https://www.nngroup.com/articles/tabs-used-right/)
- [Guía de sistema de diseño] GOV.UK: usar tabs solo si el contenido se separa claramente, la primera sección es la más relevante para la mayoría, los usuarios no necesitan ver todo a la vez, y son usuarios frecuentes. No usar si el contenido total hace lenta la página, como navegación de página, si hay que comparar entre tabs, o si hay que leer en orden. "Tabs hide content from users and not everyone will notice them". Recomienda **probar primero sin tabs**: simplificar contenido, dividir en páginas, una sola página con encabezados, o una tabla de contenidos. El componente **no tiene pruebas directas con usuarios**; se basa en Inclusive Components y NN/g. — [GOV.UK Design System – Tabs](https://design-system.service.gov.uk/components/tabs/)
- [Guía de sistema de diseño] GOV.UK compara tabs/acordeón/details: tabs cuando se alterna con frecuencia entre secciones; acordeón cuando hay muchas secciones; details para contenido menor. — [GOV.UK – Tabs](https://design-system.service.gov.uk/components/tabs/)
- [Guía de sistema de diseño] Carbon: tabs reducen carga cognitiva agrupando información; **no usarlas para comparar información** (clic constante), ni para filtrar formatos del mismo contenido (usar content switcher), ni para progreso (usar progress indicator); no usar tabs verticales como navegación; **8 o menos tabs** en breakpoints grandes. — [Carbon – Tabs usage](https://carbondesignsystem.com/components/tabs/usage/)
- [Guía basada en investigación NN/g] Acordeones en desktop: apropiados cuando el usuario necesita pocas piezas de información, secciones independientes, o ventanas pequeñas; **inapropiados cuando el público necesita la mayoría o todo el contenido**, jerarquías profundas, o contenido difícil de resumir en un encabezado. Alternativas: anclas, tabs, navegación local vertical, página única. "Easy access to essential information matters more than reduced page length". (2023-07-30) — [NN/g, Accordions on Desktop](https://www.nngroup.com/articles/accordions-on-desktop/)
- [Guía de sistema de diseño] GOV.UK Accordion: usar solo si la investigación lo respalda, para usuarios que necesitan una visión general de secciones relacionadas y elegir cuáles abrir; no para contenido que todos deben ver, ni acordeones anidados; recuerda secciones abiertas en sessionStorage; tiene "Show all sections". — [GOV.UK – Accordion](https://design-system.service.gov.uk/components/accordion/)
- [Guía basada en investigación NN/g] Enlaces en página (TOC "On this page"): más valiosos cuanto más larga es la página; colocarlos arriba, etiquetarlos ("On this page") para distinguirlos de enlaces externos; antes de añadirlos, considerar si se puede acortar/reorganizar el contenido; en móvil, acordeones pueden ser mejores porque reducen la longitud. (2023-10-01) — [NN/g, In-Page Links for Content Navigation](https://www.nngroup.com/articles/in-page-links-content-navigation/); ver también [NN/g, Table of Contents guide](https://www.nngroup.com/articles/table-of-contents/)
- [Guía de sistema de diseño, SAP Fiori] Object Page: por defecto se genera una **anchor bar** cuando hay más de una sección; puede reemplazarse por tab bar (`useIconTabBar=true`). — resumen de búsqueda sobre doc SAP Fiori Elements: [SAP Fiori – Object Page content area](https://www.sap.com/design-system/fiori-design-web/v1-145/discover/frameworks/sap-fiori-elements/object-page/object-page-content-area-sap-fiori-elements)
- [Guía de sistema de diseño, SAP Fiori, paráfrasis] Usar tab bar cuando las secciones son independientes y contienen mucho contenido; anchor bar para casos más ligeros; ordenar secciones por prioridad/flujo del usuario y limitar su número; "show more" dentro de subsecciones para densidad. — [PDF archivado Object Page Floorplan, 2020](https://2227428884-files.gitbook.io/~/files/v0/b/gitbook-legacy-files/o/assets%2F-M7nTCCM8rifZ18NJbqH%2F-MMOwyS3Jyav-BwaYztJ%2F-MMPA_OvyQwePPa5LNQJ%2FObject%20Page%20Floorplan%20_%20SAP%20Fiori%20Design%20Guidelines.pdf?alt=media&token=e9efe194-7155-4635-bff3-7eddd96e0671)
- [Guía de plataforma, Salesforce] Lightning record pages: dividir elementos en pestañas personalizables; **todo excepto la pestaña principal se beneficia de lazy loading** (menor EPT); mover Related Lists a una pestaña secundaria, mostrar 1–2 related lists clave en la principal, reducir a ≤3, o usar "Related List Quick Links". — [Salesforce Help – Optimize Lightning Experience Performance](https://help.salesforce.com/s/articleView?id=000390330&language=en_US&type=1); [Salesforce Help – Improve performance](https://help.salesforce.com/s/articleView?id=000382793&language=en_US&type=1); [Salesforce Admins blog 2018](https://admin.salesforce.com/blog/2018/how-to-configure-lightning-pages-that-work-for-your-users) (contenido de búsqueda, no fetch completo)
- [Guía de sistema de diseño, Shopify Polaris] Resource details layout: dos columnas, primaria (2/3) con la información que define el objeto, secundaria (1/3) con estado, metadatos y resúmenes; contenido en cards agrupando lo similar; ordenar por importancia. — [Polaris – Resource details layout](https://polaris.shopify.com/patterns/resource-details-layout)
- [Opinión experta] Heydon Pickering: un tabbed interface es en esencia una tabla de contenidos con enlaces a secciones de la misma página; sin JS, los usuarios pueden seguir/compartir enlaces a secciones por hash, lo cual es UX esperada de la web. — [Inclusive Components (vía resumen)](http://theadhocracy.co.uk/note/inclusive-tabbed-interfaces)

### Inferences
- Para un detalle de BL (~15 secciones donde el usuario cruza datos: p. ej. contenedores ↔ demurrage ↔ cargos locales ↔ requisitos de liberación), las fuentes favorecen **página única con encabezados + barra de anclas sticky** ("En esta página") estilo Fiori, más que 15 tabs (Carbon sugiere ≤8; NN/g advierte overflow).
- Un híbrido razonable: agrupar en 4–6 grupos de primer nivel (p. ej. Resumen/Liberación, Carga y contenedores, Finanzas, Servicios y solicitudes, Documentos y accesos) navegables por ancla; tabs solo si un grupo es independiente y pesado (criterio Fiori). Evitar acordeones anidados (GOV.UK) y >2 niveles de disclosure (NN/g, abajo).
- Master-detail (lista a la izquierda, detalle a la derecha) no se encontró como recomendado para registros de 15 secciones; es más útil para recorrer listas de BLs (ver Gaps).

### Gaps
- No se pudo leer la versión actual de las guías SAP Fiori (403); la recomendación precisa de Fiori sobre número máximo de secciones o umbral anchor-vs-tab no se verificó textualmente.
- No se consultaron ServiceNow (Next Experience record page), Atlassian Design System ni Material 3 tabs por límite de tiempo.
- No se encontró estudio cuantitativo que compare directamente tabs vs anchor-scroll en páginas de registro empresariales.

## 2. Encabezado resumen / "highlights", estado, acciones y divulgación progresiva

### Takeaway
Todos los sistemas convergen en un **encabezado con título del objeto + estado + 3–6 datos clave**, que permanece visible (sticky/colapsable) al hacer scroll, y acciones primarias en el encabezado (o pie para acciones de finalización). Lo poco usado va detrás de un segundo nivel de divulgación, nunca más de dos niveles.

### Cited Findings
- [Fiori, paráfrasis] El header muestra título del objeto, estado y metadatos clave; el contenido del header (header facets) da acceso rápido a datos críticos; el header "snapping" se colapsa al hacer scroll conservando lo esencial; acciones de finalización en toolbar de pie. — [PDF Object Page Floorplan 2020](https://2227428884-files.gitbook.io/~/files/v0/b/gitbook-legacy-files/o/assets%2F-M7nTCCM8rifZ18NJbqH%2F-MMOwyS3Jyav-BwaYztJ%2F-MMPA_OvyQwePPa5LNQJ%2FObject%20Page%20Floorplan%20_%20SAP%20Fiori%20Design%20Guidelines.pdf?alt=media&token=e9efe194-7155-4635-bff3-7eddd96e0671)
- [Fiori Elements] Tipos de header facets: formulario (dataset), texto plano, imagen, key value, micro chart, progress indicator, rating indicator; `requestGroupId` agrupa peticiones del header según tiempo de carga para separar consultas lentas de rápidas. — [SAP docs (GitHub) – header facets](https://github.com/SAP-docs/sapui5/blob/main/docs/06_SAP_Fiori_Elements/extension-points-for-object-page-header-facets-61cf0ee.md) (vía resumen de búsqueda)
- [Salesforce] Poner los campos más importantes en el Highlights Panel según necesidades de usuario; preferir el Dynamic Highlights Panel frente a compact layouts. — [Salesforce Admins – Highlight key fields (2020)](https://admin.salesforce.com/blog/2020/how-i-solved-this-highlight-key-fields-on-lightning-record-pages) (vía búsqueda)
- [Polaris] Estado, metadatos y resúmenes en columna secundaria; acciones únicas de la página arriba en la lista de acciones, acciones típicas del objeto abajo. — [Polaris – Resource details layout](https://polaris.shopify.com/patterns/resource-details-layout)
- [NN/g] Divulgación progresiva: mostrar inicialmente solo las opciones más importantes y ofrecer especializadas bajo demanda; claves: elegir bien la división (frecuente arriba, raro en secundario) y controles con buena "information scent"; **más de dos niveles de divulgación suele causar problemas de usabilidad**. Mejora aprendizaje, eficiencia y tasa de error. — [NN/g, Progressive Disclosure](https://www.nngroup.com/articles/progressive-disclosure/)
- [GOV.UK] Summary list (`<dl>`) para pares clave-valor (no tabulares); acciones por fila con texto oculto para lectores de pantalla ("Change name"); **summary card** con título único y acciones a nivel de tarjeta cuando hay varias listas (p. ej. varias partes). — [GOV.UK – Summary list](https://design-system.service.gov.uk/components/summary-list/)

### Inferences
- Para un BL: encabezado con nº BL, estado de emisión (chip), estado de liberación/TATC (chip con texto, no solo color), naviera/buque/ETA, puerto POL/POD y la **siguiente acción requerida** (p. ej. "Falta pago de cargos locales") como tarjeta de tarea; resto en secciones.
- Partes (shipper/consignee/notify) encajan con summary cards GOV.UK; contenedores/cargos/demurrage son tabulares (tabla, no summary list).
- "Servicios disponibles", "accesos de terceros" y "solicitudes" son candidatos a divulgación de segundo nivel (sección colapsada o carga bajo demanda) si los datos de uso lo confirman.

### Gaps
- No se encontró evidencia empírica específica sobre "next best action"/task cards en páginas de registro (solo práctica de producto, p. ej. Salesforce Einstein Next Best Action, no investigado). GOV.UK Task list pattern no se consultó en esta ronda.
- No hay cifra validada sobre cuántos datos clave mostrar en el header; "3–6" es inferencia, no fuente.

## 3. Secciones específicas por rol (cliente vs interno) en el mismo registro

### Takeaway
El patrón de plataforma (Salesforce Dynamic Forms) es **una sola página de registro con visibilidad condicional por perfil/permiso/dispositivo/datos del registro**, a nivel de campo y de sección, siempre respaldada por seguridad en servidor.

### Cited Findings
- [Salesforce] Visibility rules en campos y secciones de campos según valores del registro, perfil, permiso, dispositivo o campos de User; el componente queda oculto hasta cumplir la lógica de filtro. — [Salesforce Help – Visibility Rules on Lightning Pages](https://help.salesforce.com/s/articleView?id=platform.lightning_page_components_visibility.htm&language=en_US&type=5); [Trailhead](https://trailhead.salesforce.com/content/learn/modules/lightning_app_builder/add-visibility-rules-for-dynamic-pages-lab)
- [Salesforce] La seguridad a nivel de campo (FLS) sigue aplicando: ocultar en UI no sustituye la autorización. — [SalesforceBen – Dynamic Forms deep dive](https://www.salesforceben.com/salesforce-dynamic-forms-overview-deep-dive-tutorial/) (vía búsqueda)

### Inferences
- Ocultar las secciones no aplicables por rol (en lugar de mostrarlas vacías/deshabilitadas) reduce longitud y carga cognitiva; la barra de anclas debe generarse a partir de las secciones visibles para ese rol. Autorización en backend obligatoria.
- Para interno, considerar marcar visualmente "Solo interno" en secciones que el cliente no ve, para evitar errores al comunicar.

### Gaps
- No se encontró investigación de usuario publicada sobre cómo señalizar secciones "internas" vs "visibles al cliente" en portales B2B.

## 4. Accesibilidad: tabs (APG), enlaces profundos, foco, acordeón/disclosure, encabezados, skip links, WCAG 2.2

### Takeaway
Si se usan tabs, seguir el patrón APG al pie de la letra (roving tabindex, flechas, `aria-selected`, `tabpanel` con `tabindex=0` si no tiene foco interno) y **activación automática solo si el panel aparece sin latencia** (si el panel carga datos, usar activación manual). Acordeones: `button` dentro de `heading`, `aria-expanded`/`aria-controls`, sin `region` si hay >~6 paneles. En páginas con encabezado/anchor bar sticky, cumplir WCAG 2.2 SC 2.4.11 con `scroll-padding`.

### Cited Findings
- [Norma W3C APG] Tabs: `tablist`/`tab`/`tabpanel`; `aria-selected`; `aria-controls` y `aria-labelledby`; flechas mueven entre tabs con wrap; Home/End opcionales; Tab entra en la pestaña activa. "It is recommended that tabs activate automatically when they receive focus as long as their associated tab panels are displayed without noticeable latency"; si no, activación manual con Space/Enter. Panel sin elementos enfocables → `tabindex="0"`. — [W3C APG – Tabs Pattern](https://www.w3.org/WAI/ARIA/apg/patterns/tabs/)
- [Norma W3C APG] Acordeón: el `button` es el único elemento dentro del `heading` (con `aria-level` adecuado); `aria-expanded`, `aria-controls`; `region` opcional pero **evitarla con más de ~6 paneles expandibles a la vez** (proliferación de landmarks). Enter/Space expande; Tab recorre normalmente. — [W3C APG – Accordion Pattern](https://www.w3.org/WAI/ARIA/apg/patterns/accordion/)
- [Evidencia GOV.UK] Actualización dic-2021 del acordeón: usuarios de reconocimiento de voz y de navegación por elementos podían confundir encabezados con enlaces; se añadieron affordances de botón más claras. — [GOV.UK – Accordion](https://design-system.service.gov.uk/components/accordion/)
- [Norma W3C WCAG 2.2] SC 2.4.11 Focus Not Obscured (Minimum, AA): el componente con foco no debe quedar totalmente oculto por contenido del autor; headers/footers sticky son la causa típica; técnica: CSS `scroll-padding`. — [W3C Understanding SC 2.4.11](https://www.w3.org/WAI/WCAG22/Understanding/focus-not-obscured-minimum.html)
- [Opinión experta] Enlaces profundos: seguir/compartir secciones por hash es la UX esperada de la web; el hash debe actualizarse al abrir secciones. — [Inclusive tabbed interfaces (resumen)](http://theadhocracy.co.uk/note/inclusive-tabbed-interfaces)
- [GOV.UK] Acordeón persiste el estado abierto en sessionStorage (desactivable). — [GOV.UK – Accordion](https://design-system.service.gov.uk/components/accordion/)
- [Angular] Envolver bloques `@defer` en regiones `aria-live="polite"` para que lectores de pantalla anuncien el contenido cargado. — [Angular – Deferred loading with @defer](https://angular.dev/guide/templates/defer)

### Inferences
- Criterios WCAG 2.2 adicionales relevantes (no verificados en esta ronda, ver Gaps): 1.3.1 Info y relaciones (encabezados h2/h3 reales por sección), 2.4.1 Saltar bloques (skip link al contenido/encabezado del BL), 2.4.6 Encabezados y etiquetas, 2.4.7 Foco visible, 4.1.2 Nombre-rol-valor (tabs/acordeones), 1.4.1 Uso del color (chips de estado con texto), 2.4.13 Apariencia del foco (AAA).
- Anclas: al navegar a `#seccion`, mover el foco al encabezado de la sección (`tabindex="-1"`) y usar `scroll-margin-top`/`scroll-padding-top` igual a la altura del header sticky.
- Con Angular Router, reflejar la sección/tab activa en la URL (fragment o query param) para deep-linking y botón Atrás; si las tabs cargan datos con latencia, usar activación manual (APG).

### Gaps
- No se fetchearon las páginas "Understanding" de 2.4.1, 1.3.1, 2.4.6 ni el patrón APG Disclosure; los criterios listados en Inferences provienen de conocimiento general de WCAG y deben verificarse en https://www.w3.org/TR/WCAG22/.

## 5. Rendimiento: carga diferida de secciones/tabs, Angular @defer y rendimiento percibido

### Takeaway
SAP Fiori y Salesforce cargan diferidamente todo lo que no está en el viewport/pestaña inicial; en Angular, `@defer (on viewport)` con `@placeholder` del mismo tamaño (skeleton) es el equivalente directo. Skeletons para esperas de ~2–10 s; no diferir contenido above-the-fold (layout shift).

### Cited Findings
- [Fiori Elements] Al abrir el object page se envían en paralelo la petición del header y la de las secciones 1–2 (en viewport); el resto se carga lazy cuando se detecta visible. Para rendimiento se recomienda modo IconTabBar, lazy loading en secciones custom, y que componentes reutilizados disparen llamadas OData solo cuando la sección es visible. — [SAP Community blog: LROP Performance optimisation (2020)](https://blogs.sap.com/2020/09/22/sap-fiori-elements-list-report-object-page-lrop-performance-optimisation/) (vía resumen de búsqueda)
- [Salesforce] Todo salvo la pestaña principal se beneficia de lazy loading y reduce EPT; componentes pesados detrás de una pestaña. — [Salesforce Help – Optimize Lightning Experience Performance](https://help.salesforce.com/s/articleView?id=000390330&language=en_US&type=1)
- [GOV.UK] No usar tabs/acordeón si el volumen total de contenido hace lenta la página. — [GOV.UK – Tabs](https://design-system.service.gov.uk/components/tabs/)
- [Angular docs] Triggers `@defer`: `idle` (por defecto), `viewport`, `interaction`, `hover`, `immediate`, `timer`, y `when <cond>` (una sola vez); `prefetch on idle` separado del trigger; `@placeholder (minimum …)` y `@loading (after 100ms; minimum 1s)` evitan parpadeos; **no diferir contenido visible en el viewport inicial** (layout shift/CWV); usar triggers distintos en `@defer` anidados para evitar cargas en cascada; `DeferBlockBehavior.Manual` para tests; evitar barrel files para que el chunk sea lazy; SSR muestra solo el placeholder salvo incremental hydration. — [Angular – @defer](https://angular.dev/guide/templates/defer)
- [NN/g] Skeleton screens: <1 s sin indicador; 2–10 s skeleton o spinner; >10 s barra de progreso; evitar skeletons de solo marco (header/footer); "skeleton screens do not replace performance-optimization efforts". — [NN/g, Skeleton Screens 101](https://www.nngroup.com/articles/skeleton-screens/)

### Inferences
- Patrón para el detalle BL en Angular: header + resumen + liberación cargan inmediatamente (y en una única llamada); cada sección inferior en `@defer (on viewport; prefetch on idle)` con `@placeholder` skeleton de altura similar; la petición HTTP de la sección se dispara dentro del componente diferido (análogo a Fiori).
- Ojo: `@defer` difiere el **código** del componente; la carga de **datos** debe ligarse también al momento de render del componente (p. ej. en su init o vía `resource`), no en el padre.
- Interacción anchor-bar + lazy: saltar a una sección lejana puede provocar desplazamiento al cargar las anteriores; reservar altura en placeholders o usar `on viewport` con `minimum` para estabilidad.

### Gaps
- No se encontraron métricas publicadas de mejora (p. ej. % de EPT) de Salesforce/Fiori por lazy loading; solo recomendaciones.
- No se consultó web.dev sobre CLS/skeletons por límite de llamadas.
