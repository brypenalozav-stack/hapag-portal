# Dashboards de inicio orientados a la acción para portales B2B operativos (clientes de naviera)

Notas de investigación (2026-10-06). Leyenda: **[Evidencia]** = dato de investigación/medición publicada; **[Guía]** = norma de un design system o de NN/g basada en práctica; **[Opinión]** = criterio de autor sin datos. Cuando la fuente es anterior a 2018 se indica.

## 1. Dashboard operativo vs analítico: número de widgets, layout, prioridades sobre el pliegue, colas de "pendientes" vs KPIs, listas largas, agregación por embarque vs por cargo

### Takeaway
La home de un cliente de naviera es un dashboard **operativo** (decisiones sensibles al tiempo: pagar, liberar, retirar documentos), así que debe abrir con lo que requiere acción, en una sola pantalla y con jerarquía clara arriba-izquierda; los KPIs/gráficos analíticos son secundarios. No hay un número "científico" de widgets: la evidencia apunta a "solo lo que cabe y se lee de un vistazo", y la evidencia de dominio logístico (Flexport) muestra que filtrar a excepciones reduce el tiempo de búsqueda.

### Cited Findings
- [Guía, 2017] NN/g distingue dashboards **operativos** (decisiones sensibles al tiempo, datos que se actualizan continuamente) de **analíticos** (revisión diaria/análisis sin urgencia). — [NN/g, Laubheimer, "Dashboards: Making Charts and Graphs Easier to Understand"](https://www.nngroup.com/articles/dashboards-preattentive/)
- [Guía, 2017] NN/g recomienda presentar la información en **una sola pantalla** para comprensión de un vistazo; usar longitud y posición 2D (barras, líneas) para cantidades; evitar tartas, donuts, gauges, treemaps y 3D en dashboards accionables; el color debe reforzar, no liderar (hasta 8% de hombres con daltonismo). — [NN/g, Laubheimer](https://www.nngroup.com/articles/dashboards-preattentive/)
- [Evidencia, eyetracking, 2017, revisado 2026] Los usuarios escanean en patrón F (horizontal arriba, luego bajando por la izquierda); implicación: poner lo más importante al inicio, encabezados más visibles que el texto, agrupación visual con bordes/fondos, enlaces con texto informativo y eliminar contenido innecesario. — [NN/g, Pernice, "F-Shaped Pattern"](https://www.nngroup.com/articles/f-shaped-pattern-reading-web-content/)
- [Evidencia de dominio, 2019] Flexport rediseñó su dashboard con feedback de >100 profesionales de logística, con la filosofía de mostrar "the most important information for them to know at any given moment – along with the solutions needed to make smart decisions and take action"; incluye notificaciones de excepción "where things require your attention" y un checklist centralizado de tareas priorizado por urgencia. Clientes que lo probaron dedicaron "30-40% less time on finding the shipment insights they're looking for". — [Flexport blog, Platform Experience 2.0 (27-sep-2019)](https://www.flexport.com/blog/introducing-the-flexport-platform-experience-2-0-your-launchpad-for-greater/)
- [Evidencia cualitativa citada en prensa, 2020] Cita de un usuario de logística de un gran fabricante textil: "I only want to see a shipment if it's bleeding", que orientó el diseño hacia problemas accionables; Flexport preconfiguró vistas para 3 personas (logistics manager de importador grande, de importador pequeño, y proveedor). — [PR Newswire, Flexport dashboard personalization](https://www.prnewswire.com/news-releases/flexport-releases-new-dashboard-personalization-features-301074104.html) (no pude leer el post original en Medium: [Flexport UX, Dashboard 2.0](https://medium.com/flexport-ux/the-flexport-dashboard-2-0-2524f8e92245) devolvió 403)
- [Ejemplo de industria] Maersk Logistics Hub se describe como "one simple dashboard" con vista consolidada de embarques, **tareas**, actualizaciones logísticas; "My Shipments" agrupa embarques actuales/futuros/pasados, cada uno con un "digital binder" (booking, documentos, contenedores, plan de transporte, precio). — [Maersk Logistics Hub](https://www.maersk.com/digital-services/logistics-hub)
- [Ejemplo de industria] El dashboard de MyFinance de Maersk resume la cuenta: facturas pendientes, **vencidas** y gráficos por estado; las facturas se acceden desde el Hub vía menú lateral (es decir, finanzas es un área separada con su propio resumen, no todo en la home). — [Maersk, Billing simplified with MyFinance (2023)](https://www.maersk.com/news/articles/2023/11/15/billing-with-myfinance); [Maersk FAQ, view invoices](https://www.maersk.com/support/faqs/view-invoices)
- [Ejemplo fintech] Stripe Dashboard home: por defecto muestra volumen bruto, balance, débitos próximos y vista de la última semana; los charts son "estimated" y remiten a Financial Reports para precisión (la home resume, el detalle vive en otra parte). — [Stripe Support, Dashboard home charts](https://support.stripe.com/questions/dashboard-home-page-charts-for-business-insights); en Stripe Connect existe una lista "Actions required" que aparece **arriba** de la página de cuenta cuando hay que actuar para evitar restricciones. — [Stripe Docs, managing individual accounts](https://docs.stripe.com/docs/connect/dashboard/managing-individual-accounts)

### Inferences
- Para el portal: priorizar arriba-izquierda una cola "Requiere tu acción" (cargos por pagar/vencidos, requisitos de liberación pendientes, documentos listos para descargar, solicitudes con respuesta), luego estado de embarques activos, y dejar KPIs/gráficos (si existen) abajo o fuera de la home. Esto sigue la lógica operativa de NN/g, el patrón F y el caso Flexport ("solo si sangra").
- Número de widgets: no encontré un número validado; una heurística defendible es que la home quepa en ~1 pantalla de escritorio (≈3–6 bloques), coherente con la regla "una sola pantalla" de NN/g. Tratarlo como opinión de diseño, no evidencia.
- Listas largas: mostrar top N ordenado por urgencia (vencimiento, ETA, bloqueo) con contador total y enlace "Ver todos (N)" al listado filtrado; el patrón Stripe/Maersk (resumen en home → detalle en sección propia) lo respalda como práctica de industria.
- Agregación: el caso Maersk agrupa por **embarque** ("digital binder") en operaciones, pero finanzas (MyFinance) agrupa por **factura/estado**. Para la home de acción, agregar por embarque/BL ("BL X: 3 cargos pendientes, 1 requisito") reduce el ruido cuando un BL tiene muchos cargos; mostrar el desglose por cargo dentro del detalle del BL. Un total por moneda arriba ayuda a la decisión de pago.

### Gaps
- No encontré estudios cuantitativos con un número óptimo de widgets/cards en home B2B.
- No pude verificar el texto de Stephen Few (el PDF de Perceptual Edge no fue legible). Definición ampliamente citada (de memoria, sin verificar en esta sesión, 2004): "a visual display of the most important information needed to achieve one or more objectives; consolidated and arranged on a single screen so the information can be monitored at a glance". Confirmar antes de citar.
- No encontré la justificación pública de "inverted pyramid" aplicada específicamente a dashboards de NN/g en 2018–2026 (es una práctica común de escritura web).

## 2. Patrones: bandeja "acción requerida", next-best-action, task list (GOV.UK), accesos rápidos vs duplicar navegación, recientes, búsquedas guardadas, personalización

### Takeaway
Los patrones con mejor respaldo son: (a) una lista de tareas/acciones con estado explícito y verbos de acción (GOV.UK, Flexport, Stripe "Actions required"), y (b) una guía de configuración descartable para onboarding (Shopify). La personalización (drag & drop de tiles) existe en Flexport, Maersk y Stripe, pero NN/g advierte que pocos usuarios la usan; debe ser complemento de buenos valores por defecto por rol, no sustituto.

### Cited Findings
- [Guía basada en research GDS] Task list de GOV.UK: usar solo en transacciones largas con varias tareas, potencialmente en varias sesiones; nombres de tarea que empiecen con verbo ("check", "declare", "report"); estados: Completed (texto negro, sin fondo), Incomplete (tag azul), In progress (tag teal), Not yet started (azul), Cannot start yet (texto gris, dependencias), There is a problem (tag rojo, solo errores); empezar con pocos estados y ampliar solo si la investigación lo justifica. — [GOV.UK Design System, Complete multiple tasks](https://design-system.service.gov.uk/patterns/complete-multiple-tasks/)
- [Ejemplo de industria] Flexport: "centralized task checklist" priorizado por urgencia + filtros por estado, fecha de llegada, excepciones, usuario asignado; etiquetas de prioridad y campos de referencia propios (PO, SKU). — [Flexport blog 2019](https://www.flexport.com/blog/introducing-the-flexport-platform-experience-2-0-your-launchpad-for-greater/)
- [Guía Shopify] Setup guide en la home de apps: pasos con checkbox de completado, indicador "X out of Y steps completed", botones que expanden instrucciones o navegan a la página relevante, descartable (X) y colapsable; "Mark tasks as complete when merchants finish them to reinforce progress". — [Shopify, Setup guide composition](https://shopify.dev/docs/api/app-home/patterns/compositions/setup-guide)
- [Ejemplo de industria] Maersk Hub: opción "Customize" para mover tiles con drag & drop. — [Maersk, Enhancing your Maersk experience (2023)](https://www.maersk.com/news/articles/2023/11/15/enhancing-your-maersk-experience); Stripe: añadir/quitar/reordenar widgets de la home (35+ widgets según Stripe). — [Stripe Support](https://support.stripe.com/questions/dashboard-home-page-charts-for-business-insights); Flexport: widgets drag & drop + vistas preconfiguradas por persona. — [PR Newswire 2020](https://www.prnewswire.com/news-releases/flexport-releases-new-dashboard-personalization-features-301074104.html)
- [Guía NN/g, 2016 — anterior a 2018] "many users don't know what they actually need and that most users are not interested in doing the work required to tweak the user interface"; "Personalization and customization should not be used as a fix for a broken site". — [NN/g, Schade, Customization vs Personalization](https://www.nngroup.com/articles/customization-personalization/)
- [Ejemplo/contraejemplo, 2026] El rediseño 2026 del admin de Shopify buscó respetar "the muscle memory merchants have built", colapsar navegación lateral y reducir contenedores apilados; generó quejas en la comunidad ("I hate the new admin layout"), recordatorio del coste de cambiar layouts conocidos. — [Shopify blog, admin new look](https://www.shopify.com/blog/admin-new-look); [Shopify Community thread](https://community.shopify.com/t/i-hate-the-new-admin-layout/639500)

### Inferences
- Bandeja "Requiere tu acción": cada ítem como fila con verbo + objeto + motivo + plazo ("Pagar 3 cargos del BL HLCU… – vence 08-oct"), con estado tipo tag GOV.UK y un solo CTA. Encaja con los requisitos de liberación paso a paso que ya existen en la rama actual (pueden reutilizar los estados "Cannot start yet" para requisitos bloqueados por dependencias, p. ej. liberación antes de pago).
- Accesos rápidos: limitar a 2–4 acciones de alta frecuencia que no son navegación evidente (p. ej. "Consultar BL", "Pagar cargos"); duplicar todo el menú en la home añade ruido (opinión, sin estudio específico encontrado).
- Personalización: ofrecer primero buenos valores por defecto por rol (importador, agente/forwarder, finanzas), y si se añade configuración, que sea opcional (ocultar/reordenar). Con base en NN/g, no invertir en drag & drop en una primera versión.
- Recientes/búsquedas guardadas: útiles para usuarios que consultan los mismos BL repetidamente (agentes), pero no encontré evidencia publicada específica.

### Gaps
- No hallé estudios publicados sobre "next-best-action cards" en portales B2B (el término proviene sobre todo de CRM/marketing; Salesforce Lightning no fue accesible: la página de layout devolvió contenido vacío).
- Sin evidencia cuantitativa sobre adopción de recientes/búsquedas guardadas en portales logísticos.
- No pude leer la investigación detallada del componente task list de GOV.UK (enlazada desde el patrón).

## 3. Estados vacíos y onboarding (NN/g, Polaris, Carbon)

### Takeaway
Un estado vacío debe (1) comunicar el estado del sistema, (2) enseñar brevemente qué aparecerá ahí y (3) ofrecer una acción primaria directa. Distinguir "sin datos aún", "sin resultados por acción del usuario", "todo al día" (éxito) y "error/permisos".

### Cited Findings
- [Guía NN/g, 2021] Tres principios para estados vacíos en apps complejas: comunicar estado del sistema ("There are no records to display for the selected date range" aumenta la confianza), ofrecer aprendizaje contextual tipo "pull revelation", y permitir acción directa enlazando a los pasos que poblarían el estado vacío. — [NN/g, Kaplan, Designing Empty States in Complex Applications](https://www.nngroup.com/articles/empty-state-interface-design/)
- [Guía Carbon] Tipos: sin datos (primer uso), por acción del usuario (búsqueda sin resultados, proceso completado), gestión de errores (permisos, sistema, configuración). Elementos: imagen opcional, título conciso en positivo, cuerpo con siguiente paso, acción primaria, secundaria opcional. Enfocarse en **una** acción primaria; el estado vacío reemplaza al elemento que normalmente se mostraría; en errores, "precisely indicate the problem, and constructively suggest a solution"; imágenes decorativas con alt vacío o role="presentation". — [Carbon, Empty states pattern](https://carbondesignsystem.com/patterns/empty-states-pattern/)
- [Guía Shopify] Para onboarding en home: setup guide descartable con progreso "X de Y". — [Shopify, Setup guide](https://shopify.dev/docs/api/app-home/patterns/compositions/setup-guide)

### Inferences
- Bloque "Requiere tu acción" vacío = estado de **éxito** ("Todo al día: no tienes cargos ni requisitos pendientes"), no un hueco; mantener visible el bloque con este mensaje para reforzar confianza.
- Cliente nuevo sin embarques: mensaje + acción primaria "Consultar un BL" y, opcionalmente, guía de configuración (vincular empresa/RUT, invitar usuarios, preferencias de notificación), descartable.
- Error de servicio por widget (p. ej. API de tarifas caída): mensaje específico por bloque con "Reintentar", sin tumbar toda la home.

### Gaps
- La URL de Polaris para empty states redirige (301) a la nueva documentación de Polaris en shopify.dev; no extraje su guía actual específica.

## 4. Accesibilidad del dashboard y rendimiento (lazy loading, skeletons)

### Takeaway
Estructurar la home con encabezados reales por bloque y landmarks, usar tablas/listas semánticas para colas de acción (en lugar de cards con información solo visual), dar alternativas textuales/tablas a gráficos, y no depender del color. Para carga: skeleton para la página completa, spinner por módulo, nada si <1 s, barra de progreso si >10 s.

### Cited Findings
- [Guía NN/g, 2023, revisado 2026] Skeleton screens para cargas de página completa <10 s; spinners para un solo módulo (p. ej. una card de dashboard) o cargas de 2–10 s; <1 s ningún indicador; >10 s barra de progreso; evitar skeletons "frame-display" que parecen página en blanco. — [NN/g, Tankala, Skeleton Screens 101](https://www.nngroup.com/articles/skeleton-screens/)
- [Guía, fuentes secundarias de accesibilidad] Cada visualización necesita nombre y descripción accesibles, encabezados claros y navegación eficiente entre visualizaciones; ofrecer tabla de datos real (th, scope) como alternativa al gráfico y alt text que comunique la conclusión, no cada punto. — [DASY Center, Dashboards accessibility tips](https://dasycenter.org/datavis-toolkit/dashboards/accessibility/); [A11Y Collective, checklist](https://www.a11y-collective.com/blog/accessible-charts/) (fuentes de calidad media; WCAG es la referencia normativa)
- [Guía NN/g, 2017] El color debe reforzar, no ser la señal principal (daltonismo). — [NN/g, Laubheimer](https://www.nngroup.com/articles/dashboards-preattentive/)
- [Guía GOV.UK] Estados de tarea comunicados con texto en tags (no solo color). — [GOV.UK, Complete multiple tasks](https://design-system.service.gov.uk/patterns/complete-multiple-tasks/)

### Inferences
- Implementación: `<main>` con `<h1>` de la página y `<section aria-labelledby>` + `<h2>` por bloque; la cola de acciones como `<ul>` o `<table>` según tenga columnas comparables (BL, cargo, monto, vencimiento → tabla); estados con texto ("Vencido") además de color; contadores con texto accesible ("3 cargos pendientes").
- Rendimiento: cargar primero la cola de acción (crítica) y diferir bloques secundarios; cada bloque con su propio estado loading/vacío/error para que una API lenta (p. ej. TATC o tarifas) no bloquee la home. Esto es inferencia de arquitectura alineada con la guía de NN/g sobre spinners por módulo.

### Gaps
- No pude acceder a la guía de Mass.gov sobre visualización accesible (403). No encontré una guía W3C/WAI dedicada a dashboards (existe guidance de imágenes complejas en WAI, no verificada aquí).
- Sin datos cuantitativos de impacto de lazy loading en dashboards B2B.

## 5. Ejemplos publicados de portales logísticos/fintech B2B y su justificación

### Takeaway
Los referentes del sector convergen en: home = resumen consolidado + excepciones/tareas arriba + enlace a secciones de detalle (embarques, finanzas) + personalización opcional de tiles. Flexport es el único con métricas publicadas de mejora.

### Cited Findings
- Flexport (2019): rediseño con >100 profesionales; excepciones + checklist de tareas por urgencia; −30–40% tiempo para encontrar insights; −~60% tiempo en documentación vs email; datos 4–4,5x más precisos vs email. — [Flexport blog](https://www.flexport.com/blog/introducing-the-flexport-platform-experience-2-0-your-launchpad-for-greater/)
- Flexport (2020): personalización con widgets drag & drop y vistas preconfiguradas para 3 personas; cita "I only want to see a shipment if it's bleeding". — [PR Newswire](https://www.prnewswire.com/news-releases/flexport-releases-new-dashboard-personalization-features-301074104.html); proceso de diseño de "action-items" y mensajería descrito por un diseñador de Flexport. — [Andrew Coyle, Designing Flexport](https://www.andrewcoyle.com/flexport) (no leído completo)
- Maersk: Hub con embarques, tareas y actualizaciones; tiles reordenables; MyFinance con resumen de pendientes/vencidas. — [Maersk Logistics Hub](https://www.maersk.com/digital-services/logistics-hub); [MyFinance](https://www.maersk.com/news/articles/2023/11/15/billing-with-myfinance)
- Stripe: home con métricas por defecto + widgets configurables; "Actions required" arriba cuando hay riesgo de restricción. — [Stripe Support](https://support.stripe.com/questions/dashboard-home-page-charts-for-business-insights); [Stripe Docs](https://docs.stripe.com/docs/connect/dashboard/managing-individual-accounts)
- Shopify: setup guide en home para onboarding; rediseño 2026 priorizando foco y memoria muscular. — [Shopify setup guide](https://shopify.dev/docs/api/app-home/patterns/compositions/setup-guide); [Shopify blog 2026](https://www.shopify.com/blog/admin-new-look)

### Inferences
- Para el portal Hapag: una home con (1) "Requiere tu acción" agrupado por BL, (2) embarques activos con hitos/ETA y excepciones, (3) documentos recientes/listos, (4) solicitudes en curso (devoluciones, etc.), (5) accesos rápidos mínimos; finanzas/tarifas con su propio resumen en sección aparte, como Maersk MyFinance.

### Gaps
- No hallé casos públicos de Hapag-Lloyd, CMA CGM o MSC con justificación de diseño de su home.
- No encontré estudios de banca B2B (portales corporativos) con evidencia publicada sobre layout de home.
