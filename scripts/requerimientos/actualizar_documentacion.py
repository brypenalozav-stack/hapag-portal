"""Fase 7: actualiza docs/documentacion.html con Portal 2.0 v4. Sale con 1 si un marcador no cuadra.

- Trabaja solo con textos exactos, nunca con números de línea:
  - anclas únicas: deben aparecer exactamente una vez; si no, termina con código 1 sin escribir nada;
  - terminadores relativos (`</tbody>`, `</ul>`, `</script>`, `    });`): se buscan como la primera
    coincidencia después de su ancla, nunca en todo el archivo.
- Idempotente: si ya existe `id="t-integraciones"` y no queda ningún `data-bpmn`, imprime "ya aplicado".
- Alcance:
  - región de contenido (`<main>` … `</main>`): los 3 procesos pasan a flowcharts Mermaid, 4 secciones
    nuevas, 3 filas ADR, fuentes y nota de fuentes;
  - eliminación del XML BPMN embebido, de la librería del visor BPMN y de su código de inicialización;
  - eliminación de la regla CSS `.bpmn`.
  El `<script>` de Mermaid queda byte a byte igual.
- Lee y escribe UTF-8 sin cambiar los finales de línea.
"""
import os
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
HTML = os.path.join(REPO, "docs", "documentacion.html")


class MarcadorError(Exception):
    pass


# ---------------------------------------------------------------------------
# Contenido nuevo (con "\n"; se convierte al final de línea del archivo)
# ---------------------------------------------------------------------------

FLUJOS = [
    {
        "seccion_antes": '    <section id="f-bpmn-carga">',
        "seccion_despues": '    <section id="f-flujo-carga">',
        "h2_antes": "      <h2>Proceso BPMN — Carga de Bill of Lading</h2>",
        "h2_despues": "      <h2>Proceso — Carga de Bill of Lading</h2>",
        "diagrama_antes": '      <div class="diagram"><div class="bpmn" data-bpmn="bpmn-carga"></div></div>',
        "diagrama_despues": """      <div class="diagram"><pre class="mermaid">
flowchart LR
  s1(("BL recibido")) --> t1["Subir filas"] --> t2["Validar por fila"] --> g1{"¿Errores?"}
  g1 -- "No" --> t3["Confirmar carga"] --> e1(("Cargado"))
  g1 -- "Sí" --> t4["Corregir datos"] --> e2(("Rechazado"))
      </pre></div>""",
    },
    {
        "seccion_antes": '    <section id="f-bpmn-aduana">',
        "seccion_despues": '    <section id="f-flujo-aduana">',
        "h2_antes": "      <h2>Proceso BPMN — Transmisión a Aduana (dos etapas)</h2>",
        "h2_despues": "      <h2>Proceso — Transmisión a Aduana (dos etapas)</h2>",
        "diagrama_antes": '      <div class="diagram"><div class="bpmn" data-bpmn="bpmn-aduana"></div></div>',
        "diagrama_despues": """      <div class="diagram"><pre class="mermaid">
flowchart LR
  a_s(("Manifiesto listo")) --> a_h["Transmitir encabezado"] --> a_g1{"¿Aceptado?"}
  a_g1 -- "Sí" --> a_bl["Transmitir B/L"] --> a_e(("Manifiesto aceptado"))
  a_g1 -- "No" --> a_retry["Reintentar / Aclarar"] --> a_h
      </pre></div>""",
    },
    {
        "seccion_antes": '    <section id="f-bpmn-plazos">',
        "seccion_despues": '    <section id="f-flujo-plazos">',
        "h2_antes": "      <h2>Proceso BPMN — Control de plazos</h2>",
        "h2_despues": "      <h2>Proceso — Control de plazos</h2>",
        "diagrama_antes": '      <div class="diagram"><div class="bpmn" data-bpmn="bpmn-plazos"></div></div>',
        "diagrama_despues": """      <div class="diagram"><pre class="mermaid">
flowchart LR
  p_s(("Evento base")) --> p_calc["Calcular plazo"] --> p_g{"Estado"}
  p_g -- "OK" --> p_ok(("En plazo"))
  p_g -- "Riesgo" --> p_alert["Alertar (en riesgo)"] --> p_e2(("Notificado"))
  p_g -- "Vencido" --> p_esc["Escalar (vencido)"] --> p_e2
      </pre></div>""",
    },
]

SECCION_F_REQ_PORTAL2 = """    <section id="f-req-portal2">
      <h2>Requerimientos Portal 2.0 (v4)</h2>
      <p class="lead">La especificación funcional v4 del Portal 2.0 (Hapag-Lloyd Chile y Bolivia) reúne <strong>134 fichas</strong>: <strong>107 requerimientos funcionales (RF)</strong> repartidos en 11 módulos y <strong>27 no funcionales (NF)</strong>. Esta plataforma es la base de código sobre la que se construye el Portal 2.0.</p>
      <table>
        <thead><tr><th>Fase</th><th>RF</th><th>Qué agrupa</th></tr></thead>
        <tbody>
          <tr><td>Fase 0</td><td>4</td><td>Fichas habilitantes previas: M1-19, M2-08, M3-03 y M7-04.</td></tr>
          <tr><td>Fase 1</td><td>72</td><td>Alcance de la primera salida a producción, incluido el módulo M11. Los 27 NF también son de Fase 1.</td></tr>
          <tr><td>Fase 2</td><td>31</td><td>Ampliaciones posteriores, entre ellas M2-10 (plazos documentales por nave) y M8-09 (Counter Bolivia/Ultramar).</td></tr>
        </tbody>
      </table>
      <table>
        <thead><tr><th>Módulo</th><th>Nombre</th><th>RF (Fase 0 / 1 / 2)</th></tr></thead>
        <tbody>
          <tr><td>M1</td><td>Acceso, usuarios y experiencia del cliente</td><td>27 (1 / 20 / 6)</td></tr>
          <tr><td>M2</td><td>Disponibilidad de embarques y habilitación de servicios</td><td>10 (1 / 6 / 3)</td></tr>
          <tr><td>M3</td><td>Flujos de solicitud y gestión de servicios</td><td>19 (1 / 6 / 12)</td></tr>
          <tr><td>M4</td><td>Lectura y aplicación de reglas de negocio y exenciones de Nexus</td><td>4 (0 / 4 / 0)</td></tr>
          <tr><td>M5</td><td>Carro de compra y medios de pago</td><td>10 (0 / 8 / 2)</td></tr>
          <tr><td>M6</td><td>Generación documental</td><td>9 (0 / 7 / 2)</td></tr>
          <tr><td>M7</td><td>Facturación y estado de cuenta</td><td>4 (1 / 2 / 1)</td></tr>
          <tr><td>M8</td><td>Administración interna y mantenedores</td><td>9 (0 / 6 / 3)</td></tr>
          <tr><td>M9</td><td>Reportería y trazabilidad</td><td>1 (0 / 0 / 1)</td></tr>
          <tr><td>M10</td><td>Asistente virtual y autoatención</td><td>6 (0 / 5 / 1)</td></tr>
          <tr><td>M11</td><td>Interfaz, accesibilidad e idiomas</td><td>8 (0 / 8 / 0)</td></tr>
        </tbody>
      </table>
      <p>M11 es el capítulo nuevo de la v4: tokens de diseño, selector ES/EN en caliente, formatos por país, WCAG 2.2 AA, teclado y foco visible, anuncios <code>aria-live</code>, tema claro/oscuro con reducción de movimiento y navegadores soportados. Las integraciones de la especificación (capítulo 15) se apoyan en la capa descrita en la pestaña Técnica, sección <em>Integraciones: puertos, Dummy y Real</em>.</p>
      <h3>Documentos v4</h3>
      <ul>
        <li><code>Portal_2.0_Especificacion_Funcional_v4.docx</code> — especificación funcional (134 fichas).</li>
        <li><code>Portal_2_0_Pendientes_v4.xlsx</code> — tareas con responsable, origen y contrato de integración.</li>
        <li><code>Funcionalidades NexusV2_v4.xlsx</code> — funciones NexusV2 con RACI y estado en el portal.</li>
        <li><code>Comparacion_Requerimientos_Por_Fase_v4.xlsx</code> — cobertura de cada ficha en esta base de código (C/P/N).</li>
        <li><code>Guia_UI_Accesibilidad_i18n_v4.docx</code> — tokens, patrones, checklist WCAG 2.2 AA y estrategia i18n.</li>
        <li><code>Matriz_Trazabilidad_v4.docx</code> — ficha → tareas, NexusV2, contratos y código.</li>
        <li><code>Registro_Decisiones_v4.xlsx</code> — decisiones Q1–Q9 y DC1–DC7.</li>
      </ul>
      <div class="note">Copias en el repositorio: <a href="requerimientos/guia-ui-a11y-i18n.md">Guía de UI, accesibilidad e idiomas</a> y <a href="requerimientos/matriz-trazabilidad.md">Matriz de trazabilidad</a>.</div>
    </section>

"""

SECCION_U_IDIOMA_TECLADO = """    <section id="u-idioma-teclado">
      <h2>Idioma y uso con teclado</h2>
      <p class="lead">El portal funciona en <strong>español</strong> e <strong>inglés</strong>, y se puede usar completo sin mouse.</p>

      <h3>1. Cambiar el idioma</h3>
      <ol>
        <li>En la <strong>barra superior</strong> busca los botones <strong>ES</strong> y <strong>EN</strong>.</li>
        <li>Pulsa el idioma que quieras. Los textos cambian <strong>al instante, sin recargar la página</strong> ni perder lo que estabas haciendo.</li>
        <li>El portal <strong>recuerda tu elección</strong> en este navegador: la próxima vez entrarás en el mismo idioma.</li>
      </ol>

      <h3>2. Formatos según tu país</h3>
      <table>
        <thead><tr><th>Idioma / país</th><th>Fechas</th><th>Montos</th><th>Hora</th></tr></thead>
        <tbody>
          <tr><td>Español, Chile</td><td>dd-MM-aaaa (05-10-2026)</td><td>CLP sin decimales; USD y EUR con 2</td><td>24 h, hora de Santiago</td></tr>
          <tr><td>Español, Bolivia</td><td>dd/MM/aaaa (05/10/2026)</td><td>BOB, USD y EUR con 2 decimales</td><td>24 h, hora de La Paz</td></tr>
          <tr><td>Inglés</td><td>dd MMM aaaa (05 Oct 2026)</td><td>Igual que en tu país, con separadores en inglés</td><td>24 h, hora de tu país</td></tr>
        </tbody>
      </table>
      <div class="note">Los montos siempre muestran el <strong>código de la moneda</strong> (CLP, BOB, USD, EUR) para que no haya dudas sobre en qué moneda pagas.</div>

      <h3>3. Moverte con el teclado</h3>
      <ul>
        <li><strong>Tab</strong> avanza al siguiente botón, enlace o campo; <strong>Shift + Tab</strong> retrocede.</li>
        <li>El <strong>primer Tab</strong> muestra el enlace <strong>“Saltar al contenido principal”</strong>: con <strong>Enter</strong> vas directo al contenido, sin recorrer el menú.</li>
        <li><strong>Enter</strong> activa enlaces y botones; <strong>Espacio</strong> activa botones y marca casillas.</li>
        <li>En pantallas angostas el menú lateral se abre con el <strong>botón de menú (hamburguesa)</strong> de la barra superior: llega a él con Tab y ábrelo o ciérralo con Enter o Espacio.</li>
        <li>El elemento activo siempre se ve con un <strong>borde de foco visible</strong> alrededor, para que sepas dónde estás.</li>
      </ul>
      <div class="note">Los avisos importantes (pago confirmado, resultado de una carga, errores al enviar) también se anuncian a los lectores de pantalla. Si el servicio no responde verás <strong>“Servicio temporalmente no disponible”</strong> con un botón <strong>Reintentar</strong>, distinto del mensaje <strong>“No hay datos”</strong>.</div>
    </section>

"""

SECCION_T_INTEGRACIONES = """    <section id="t-integraciones">
      <h2>Integraciones: puertos, Dummy y Real</h2>
      <p class="lead">Cada sistema externo se consume a través de un <strong>puerto</strong> (interfaz en <code>HapagPortal.Application/Common/Interfaces/</code>) con dos implementaciones en <code>HapagPortal.Infrastructure/Integrations/</code>: un <strong>adaptador Dummy</strong> determinista y, cuando existe, un <strong>cliente Real</strong> HTTP. Así el desarrollo avanza aunque el sistema externo aún no esté disponible. Todos los puertos devuelven <code>Result&lt;T&gt;</code>: sin datos es <code>Success(null)</code> o lista vacía, y las fallas son <code>DomainErrors.Integration.*</code> (<code>Unavailable</code>, <code>Timeout</code>, <code>InvalidResponse</code>, <code>NotConfigured</code>).</p>
      <div class="diagram"><pre class="mermaid">
flowchart LR
  H["Handlers (Application)"] --> P["Puertos<br/>Common/Interfaces"]
  P -->|"Mode=Dummy (por defecto)"| D["Adaptadores Dummy<br/>deterministas"]
  P -->|"Mode=Real"| R["Clientes Real Http*<br/>logging NF-27 + resiliencia"]
  R --> S["Simulador HTTP<br/>backend/tools"]
  R --> X["Sistema externo<br/>con contrato validado"]
      </pre></div>
      <table>
        <thead><tr><th>Puerto</th><th>Sistema</th><th>Contrato</th><th>Dummy</th><th>Cliente Real</th></tr></thead>
        <tbody>
          <tr><td><code>IExemptionReader</code></td><td>Nexus</td><td>CT-NEXUS</td><td><code>DummyExemptionReader</code></td><td><code>HttpNexusClient</code></td></tr>
          <tr><td><code>ICreditConditionReader</code></td><td>Nexus</td><td>CT-NEXUS</td><td><code>DummyCreditConditionReader</code></td><td><code>HttpNexusClient</code></td></tr>
          <tr><td><code>IExchangeRateProvider</code></td><td>Nexus</td><td>CT-NEXUS</td><td><code>DummyExchangeRateProvider</code></td><td><code>HttpNexusClient</code></td></tr>
          <tr><td><code>ITariffProvider</code></td><td>Nexus</td><td>CT-NEXUS</td><td><code>DummyTariffProvider</code></td><td><code>HttpNexusClient</code></td></tr>
          <tr><td><code>IShipmentSource</code></td><td>FIS / Data Lake</td><td>CT-FIS</td><td><code>DummyShipmentSource</code></td><td><code>HttpShipmentSource</code></td></tr>
          <tr><td><code>IPaymentProvider</code> (con clave)</td><td>Khipu · Banco de Chile · Santander · BCI</td><td>CT-KHIPU · CT-BCH · CT-SANT · CT-BCI</td><td><code>DummyPaymentProvider</code></td><td><code>HttpKhipuPaymentProvider</code>, <code>HttpBancoChilePaymentProvider</code> (Santander y BCI: solo Dummy)</td></tr>
          <tr><td><code>IInvoiceProvider</code></td><td>DBNet / SII</td><td>CT-DBNET</td><td><code>DummyInvoiceProvider</code></td><td><code>HttpInvoiceProvider</code></td></tr>
          <tr><td><code>IDocumentSigner</code></td><td>Firma electrónica</td><td>CT-SIGN</td><td><code>DummyDocumentSigner</code></td><td>Solo Dummy</td></tr>
          <tr><td><code>IFileStorage</code></td><td>Almacenamiento</td><td>CT-STORAGE</td><td><code>DummyFileStorage</code> (en memoria)</td><td>Solo Dummy</td></tr>
          <tr><td><code>ITrackingProvider</code></td><td>Tracking</td><td>CT-TRACK</td><td><code>DummyTrackingProvider</code></td><td><code>HttpTrackingProvider</code></td></tr>
        </tbody>
      </table>
      <div class="note">Todos los contratos (OpenAPI 3.1 en <code>docs/integraciones/contratos/</code>) están en estado <strong>PROPUESTA</strong>: ninguno fue entregado aún por Hapag-Lloyd y cada uno espera la validación de su responsable.</div>

      <h3>Selección por configuración y fail-fast</h3>
      <ul>
        <li><code>AddIntegrations</code> (<code>DI.Integrations.Partial.cs</code>) lee <code>Integrations:{Sistema}:Mode</code> para Nexus, Fis, Khipu, BancoChile, Santander, Bci, DbNet, Signature, Storage y Tracking. Si falta, usa <code>Dummy</code>.</li>
        <li>Con <code>Mode=Real</code> el sistema necesita una <code>BaseUrl</code> absoluta (se normaliza para terminar en <code>/</code>); la API key sale de <code>ISecretResolver</code>.</li>
        <li><strong>Fail-fast:</strong> se valida todo antes de registrar. <code>Mode=Real</code> en Santander, Bci, Signature o Storage (sin cliente Real), un modo desconocido o una <code>BaseUrl</code> inválida lanzan <code>InvalidOperationException</code> y la API no arranca.</li>
        <li>En Railway se configura con doble guion bajo, por ejemplo <code>Integrations__Nexus__Mode=Real</code>. La vuelta atrás es <code>Mode=Dummy</code>.</li>
      </ul>

      <h3>Simulador HTTP</h3>
      <p><code>backend/tools/HapagPortal.IntegrationSimulator</code> es una Minimal API (.NET 9, sin paquetes externos) con las rutas de CT-NEXUS, CT-FIS, CT-KHIPU, CT-BCH, CT-DBNET y CT-TRACK bajo <code>/nexus</code>, <code>/fis</code>, <code>/khipu</code>, <code>/banco-chile</code>, <code>/dbnet</code> y <code>/tracking</code>, con los mismos datos de escenario que los Dummy. La cabecera <code>X-Sim-Scenario</code> fuerza fallas: <code>error500</code> (500 con <code>Problem</code>), <code>timeout</code> (30 s), <code>lento</code> (3 s) y <code>429</code> (con <code>Retry-After</code>). <code>/contracts/{nombre}</code> sirve el YAML del contrato. Las pruebas de <code>HapagPortal.IntegrationTests</code> y el job <code>contracts</code> de CI (schemathesis) corren contra él.</p>

      <h3>Resiliencia (clientes Real)</h3>
      <table>
        <thead><tr><th>Parámetro</th><th>Valor</th><th>Configuración</th></tr></thead>
        <tbody>
          <tr><td>Timeout por intento</td><td>10 s</td><td><code>Integrations:{Sistema}:TimeoutSeconds</code></td></tr>
          <tr><td>Timeout total de la llamada</td><td>30 s</td><td><code>Integrations:{Sistema}:TotalTimeoutSeconds</code></td></tr>
          <tr><td>Reintentos</td><td>3, backoff exponencial desde 2000 ms</td><td><code>Integrations:{Sistema}:RetryBaseDelayMs</code></td></tr>
          <tr><td>Circuit breaker</td><td>mínimo 5 llamadas, 50 % de fallas, ventana max(30 s, 2 × timeout por intento), apertura de 15 s</td><td>Fijo en <code>AddStandardResilienceHandler</code></td></tr>
        </tbody>
      </table>
      <p>Las respuestas 5xx, los timeouts y el circuito abierto se traducen a <code>DomainErrors.Integration.*</code>; un 404 es <code>Success(null)</code>.</p>

      <h3>Observabilidad (NF-27)</h3>
      <p><code>IntegrationLoggingHandler</code> va por fuera de la tubería de resiliencia: propaga o genera <code>X-Correlation-Id</code> y, cuando la respuesta es ≥ 400 o hay excepción, registra en Warning <code>System</code>, <code>Operation</code>, <code>StatusCode</code>, <code>DurationMs</code> y <code>CorrelationId</code>, e incrementa el contador <code>hapagportal.integrations.errors</code> (medidor <code>HapagPortal.Integrations</code>, etiqueta <code>system</code>).</p>

      <h3>Webhooks de pago</h3>
      <ul>
        <li><strong>Khipu:</strong> con <code>Mode=Dummy</code> se mantiene el comportamiento anterior (secreto compartido). Con <code>Mode=Real</code>, además, el handler verifica la notificación contra Khipu y exige que la referencia y el monto coincidan; si no, responde <code>Unauthorized</code> sin cambios.</li>
        <li><strong>Banco de Chile:</strong> exige el secreto y la firma <code>X-Signature</code> (HMAC-SHA256 del cuerpo) con la clave <code>Payments:Webhooks:BancoChile:SigningKey</code>.</li>
      </ul>
      <div class="note">Detalle por sistema en <a href="integraciones/README.md">el inventario de integraciones</a>. Para pasar un sistema a Real en un ambiente se sigue el <a href="integraciones/checklist-dummy-a-real.md">checklist Dummy → Real</a>.</div>
    </section>

"""

SECCION_T_UI_I18N_A11Y = """    <section id="t-ui-i18n-a11y">
      <h2>UI, idiomas y accesibilidad</h2>
      <p class="lead">Base de interfaz del frontend Angular 21: tokens de marca aplicados a Bootstrap, español e inglés en caliente con Transloco y conformidad WCAG 2.2 AA verificada en CI.</p>

      <h3>Tokens de diseño (DC4)</h3>
      <ul>
        <li><code>frontend/src/styles/_tokens.scss</code> declara solo variables Sass y es el único archivo con valores hexadecimales (<code>npm run check:hex</code>).</li>
        <li><code>styles.scss</code> los aplica con <code>@use 'bootstrap/scss/bootstrap' with (…)</code>: primario <code>$hl-orange-700</code> (#b84a00), éxito <code>$hl-green-700</code> (#007a33), secundario <code>$hl-dark</code> (#33424f) e información <code>$hl-blue</code> (#004d6c), todos con contraste AA. El naranja de marca #ff6600 queda para el logo y acentos no textuales.</li>
        <li>Expone las variables CSS <code>--hl-*</code>, incluido <code>--hl-focus</code>, con su variante en <code>[data-bs-theme="dark"]</code>. <code>npm run check:theme</code> comprueba en el CSS compilado que <code>--bs-primary</code> es #b84a00.</li>
      </ul>

      <h3>Idiomas con Transloco</h3>
      <ul>
        <li><code>@jsverse/transloco</code> con <code>availableLangs: ['es','en']</code>, <code>defaultLang</code> y <code>fallbackLang</code> <code>'es'</code> y <code>reRenderOnLangChange: true</code>. Los textos están en <code>frontend/public/i18n/es.json</code> y <code>en.json</code>, con claves <code>modulo.componente.elemento</code>; los plurales usan ICU (<code>@jsverse/transloco-messageformat</code>).</li>
        <li><code>LocaleService</code>: signal <code>lang</code> guardada en <code>hl_lang</code>, <code>locale</code> calculado (<code>es-CL</code>, <code>es-BO</code> o <code>en</code>) y huso del país (America/Santiago o America/La_Paz). <code>setLang()</code> cambia el idioma activo y <code>&lt;html lang&gt;</code> sin recargar.</li>
        <li>Pipes <code>hlDate</code>, <code>hlNumber</code> y <code>hlCurrency</code> (<code>pure: false</code>): usan <code>Intl.*</code> con formateadores en caché por locale y opciones. Fechas según Q8 con huso visible; montos con código ISO (CLP sin decimales; BOB, USD y EUR con 2).</li>
        <li><code>npm run check:i18n</code> (paridad de claves, sin vacíos ni claves inexistentes) y <code>npm run check:i18n-text</code> (sin textos literales en plantillas).</li>
      </ul>

      <h3>Accesibilidad WCAG 2.2 AA</h3>
      <ul>
        <li><strong>Shell:</strong> enlace “Saltar al contenido principal” como primer foco, <code>&lt;header&gt;</code>, <code>&lt;main id="contenido-principal"&gt;</code>, menú hamburguesa con nombre accesible y <code>aria-expanded</code>, foco visible <code>:focus-visible</code> de 3 px y <code>prefers-reduced-motion</code>.</li>
        <li><strong>Anuncios:</strong> <code>LiveAnnouncerService</code> escribe en dos regiones <code>aria-live</code> (polite y assertive) del shell; lo usan el pago, la importación de BL y los errores de envío.</li>
        <li><strong>Estados NF-11:</strong> <code>StateMessageComponent</code> (<code>app-state-message</code>) con <code>empty</code> (<code>role="status"</code>) y <code>error</code> (<code>role="alert"</code> y Reintentar) ante HTTP 5xx o sin conexión.</li>
        <li><strong>Tablas y formularios:</strong> <code>th scope="col"</code>, <code>caption</code>, <code>label for</code>, <code>aria-describedby</code> y <code>aria-invalid</code>.</li>
      </ul>
      <table>
        <thead><tr><th>Evidencia (Q9)</th><th>Dónde</th></tr></thead>
        <tbody>
          <tr><td><code>ng lint</code> con las reglas de accesibilidad de plantillas en error</td><td><code>frontend/eslint.config.js</code>, job <code>frontend</code> de CI</td></tr>
          <tr><td>axe con 0 violaciones en ES y EN (WCAG 2.0/2.1/2.2 A y AA)</td><td><code>frontend/e2e/a11y/pantallas.a11y.spec.ts</code> (Playwright)</td></tr>
          <tr><td>Teclado: skip-link, hamburguesa y <code>scope</code> de tablas</td><td><code>frontend/e2e/a11y/teclado.spec.ts</code></td></tr>
          <tr><td>Chequeos <code>check:hex</code>, <code>check:theme</code>, <code>check:i18n</code>, <code>check:i18n-text</code> y <code>check:table-scope</code></td><td><code>frontend/scripts/</code>, job <code>frontend</code> de CI</td></tr>
          <tr><td>Lighthouse Accesibilidad ≥ 95 y recorrido con teclado y NVDA</td><td>Verificación manual según el checklist de la Guía</td></tr>
        </tbody>
      </table>
      <div class="note">Detalle de tokens, patrones y el checklist de los 55 criterios A y AA en la <a href="requerimientos/guia-ui-a11y-i18n.md">Guía de UI, accesibilidad e idiomas</a>.</div>
    </section>

"""

FILAS_ADR = """          <tr><td>Transloco para i18n en caliente</td><td>Cambia ES/EN sin recargar ni compilar un bundle por idioma, con un solo build; claves JSON con paridad verificada en CI y licencia MIT.</td></tr>
          <tr><td>Adaptadores Dummy y simulador HTTP antes de los clientes Real</td><td>Los contratos siguen en PROPUESTA: los Dummy deterministas y el simulador permiten desarrollar y probar resiliencia y contratos sin depender de los sistemas externos; el paso a Real es por configuración y con checklist.</td></tr>
          <tr><td>Diagramas solo con Mermaid (MIT)</td><td>Todos los diagramas usan una única librería de código abierto con licencia OSI. Se retira el visor BPMN anterior porque su licencia no es OSI y exigía mostrar una marca de agua; los procesos pasan a flowcharts.</td></tr>
"""

FUENTES_LI = """        <li>WCAG 2.2 — <a href="https://www.w3.org/TR/WCAG22/">w3.org/TR/WCAG22</a></li>
        <li>Transloco — <a href="https://jsverse.gitbook.io/transloco">jsverse.gitbook.io/transloco</a></li>
        <li>Resiliencia HTTP en .NET — <a href="https://learn.microsoft.com/dotnet/core/resilience/http-resilience">learn.microsoft.com</a></li>
        <li>OpenAPI 3.1 — <a href="https://spec.openapis.org/oas/v3.1.0">spec.openapis.org/oas/v3.1.0</a></li>
        <li>RFC 9457 (Problem Details for HTTP APIs) — <a href="https://www.rfc-editor.org/rfc/rfc9457">rfc-editor.org</a></li>
"""

NOTA_FUENTES_ANTES = '      <div class="note">Diagramas Mermaid y BPMN incrustados <strong>inline</strong> (sin CDN) para que este archivo funcione con doble clic y sin conexión. Los BPMN se pueden abrir/editar en demo.bpmn.io.</div>'
NOTA_FUENTES_DESPUES = '      <div class="note">Diagramas Mermaid 11.16.1 (MIT) incrustados <strong>inline</strong> (sin CDN) para que este archivo funcione con doble clic y sin conexión.</div>'

# Eliminaciones fuera de <main>
COMENTARIO_XML = "<!-- ===== BPMN XML embebido (con DI para render offline) ===== -->"
COMENTARIO_LIBRERIAS = "<!-- ===== Librerías inline (offline) ===== -->"
INICIO_LIBRERIA_BPMN = "<script>/*! bpmn-js - bpmn-navigated-viewer v17.11.1"
INICIO_MERMAID = '<script>"use strict";var __esbuild_esm_mermaid_nm'
LINEA_VIEWERS = "  var bpmnViewers = {};"
INICIO_BLOQUE_RENDER = '    panel.querySelectorAll(".bpmn").forEach(function (el) {'
FIN_BLOQUE_RENDER = "    });"
COMENTARIO_RENDER_ANTES = "// Render perezoso por pestaña: flowchart/ER/BPMN necesitan"
COMENTARIO_RENDER_DESPUES = "// Render perezoso por pestaña: flowchart/ER necesitan"
CSS_BPMN = "  .bpmn { height: 360px; width: 100%; }"


# ---------------------------------------------------------------------------
# Utilidades de marcadores
# ---------------------------------------------------------------------------

def unica(texto, ancla):
    """Posición de un ancla que debe aparecer exactamente una vez."""
    n = texto.count(ancla)
    if n != 1:
        raise MarcadorError(f"el ancla debe aparecer 1 vez y aparece {n}: {ancla[:90]!r}")
    return texto.index(ancla)


def despues(texto, terminador, desde, limite=None):
    """Primera aparición de un terminador después de su ancla (y antes de `limite`, si se indica)."""
    i = texto.find(terminador, desde)
    if i == -1 or (limite is not None and i >= limite):
        raise MarcadorError(f"no se encontró {terminador!r} después de la posición {desde}")
    return i


def inicio_linea(texto, pos):
    return texto.rfind("\n", 0, pos) + 1


def fin_linea(texto, pos, nl):
    """Posición justo después del final de línea que sigue a `pos`."""
    i = texto.find(nl, pos)
    if i == -1:
        raise MarcadorError(f"falta el final de línea después de la posición {pos}")
    return i + len(nl)


def reemplazar_unica(texto, antes, despues_txt):
    unica(texto, antes)
    return texto.replace(antes, despues_txt, 1)


def quitar_linea_unica(texto, linea, nl):
    """Quita una línea completa (con su final de línea) cuyo contenido es exactamente `linea`."""
    marca = nl + linea + nl
    n = texto.count(marca)
    if n != 1:
        raise MarcadorError(f"la línea debe aparecer 1 vez y aparece {n}: {linea!r}")
    return texto.replace(marca, nl, 1)


def region_main(texto):
    ini = unica(texto, "<main>")
    fin = unica(texto, "</main>")
    if fin < ini:
        raise MarcadorError("</main> aparece antes que <main>")
    return ini, fin


def en_main(texto, ancla):
    ini, fin = region_main(texto)
    pos = unica(texto, ancla)
    if not ini < pos < fin:
        raise MarcadorError(f"el ancla no está dentro de <main>: {ancla[:90]!r}")
    return pos


# ---------------------------------------------------------------------------
# Pasos
# ---------------------------------------------------------------------------

def flujos_mermaid(t, nl):
    for f in FLUJOS:
        for clave in ("seccion", "h2", "diagrama"):
            en_main(t, f[clave + "_antes"])
        sec = unica(t, f["seccion_antes"])
        fin_sec = despues(t, "</section>", sec)
        for clave in ("h2", "diagrama"):
            pos = unica(t, f[clave + "_antes"])
            if not sec < pos < fin_sec:
                raise MarcadorError(f"{f[clave + '_antes']!r} no está dentro de {f['seccion_antes'].strip()}")
        t = reemplazar_unica(t, f["seccion_antes"], f["seccion_despues"])
        t = reemplazar_unica(t, f["h2_antes"], f["h2_despues"])
        t = reemplazar_unica(t, f["diagrama_antes"], f["diagrama_despues"].replace("\n", nl))
    return t


def insertar_antes_de_seccion(t, id_seccion, bloque, nl):
    ancla = f'    <section id="{id_seccion}">'
    pos = en_main(t, ancla)
    return t[:pos] + bloque.replace("\n", nl) + t[pos:]


def adr_y_fuentes(t, nl):
    adr = en_main(t, '<section id="t-adr">')
    fin_adr = despues(t, "</section>", adr)
    tbody = despues(t, "        </tbody>", adr, fin_adr)
    t = t[:tbody] + FILAS_ADR.replace("\n", nl) + t[tbody:]

    fuentes = en_main(t, '<section id="t-fuentes">')
    fin_fuentes = despues(t, "</section>", fuentes)
    ul = despues(t, "      </ul>", fuentes, fin_fuentes)
    t = t[:ul] + FUENTES_LI.replace("\n", nl) + t[ul:]

    nota = en_main(t, NOTA_FUENTES_ANTES)
    if not fuentes < nota < despues(t, "</section>", fuentes):
        raise MarcadorError("la nota de diagramas no está dentro de t-fuentes")
    return reemplazar_unica(t, NOTA_FUENTES_ANTES, NOTA_FUENTES_DESPUES)


def quitar_xml_bpmn(t):
    ini = unica(t, COMENTARIO_XML)
    if ini != inicio_linea(t, ini):
        raise MarcadorError("el comentario del XML BPMN no empieza la línea")
    pos = ini
    for _ in range(3):
        apertura = despues(t, '<script type="application/bpmn+xml"', pos)
        pos = despues(t, "</script>", apertura) + len("</script>")
    siguiente = despues(t, COMENTARIO_LIBRERIAS, pos)
    if t[pos:siguiente].strip():
        raise MarcadorError("hay contenido inesperado entre el tercer </script> BPMN y las librerías inline")
    if t.find('<script type="application/bpmn+xml"', pos) != -1:
        raise MarcadorError("quedan bloques application/bpmn+xml después del tercero")
    # Se elimina desde el comentario hasta justo antes del comentario de librerías: queda una línea en blanco.
    return t[:ini] + t[siguiente:]


def quitar_libreria_bpmn(t, nl):
    ini = unica(t, INICIO_LIBRERIA_BPMN)
    if ini != inicio_linea(t, ini):
        raise MarcadorError("el banner de la librería BPMN no empieza la línea")
    mermaid = unica(t, INICIO_MERMAID)
    if ini < mermaid:
        raise MarcadorError("la librería BPMN aparece antes que la de Mermaid")
    cierre = despues(t, "</script>", ini)
    fin = fin_linea(t, cierre, nl)
    return t[:ini] + t[fin:]


def quitar_inicializacion_bpmn(t, nl):
    t = quitar_linea_unica(t, LINEA_VIEWERS, nl)
    render = unica(t, "function renderPanel")
    ini = unica(t, INICIO_BLOQUE_RENDER)
    if ini < render:
        raise MarcadorError("el bloque .bpmn no está dentro de renderPanel")
    ini = inicio_linea(t, ini)
    fin = despues(t, nl + FIN_BLOQUE_RENDER + nl, ini) + len(nl + FIN_BLOQUE_RENDER + nl)
    t = t[:ini] + t[fin:]
    return reemplazar_unica(t, COMENTARIO_RENDER_ANTES, COMENTARIO_RENDER_DESPUES)


def quitar_css_bpmn(t, nl):
    estilo_fin = unica(t, "</style>")
    if unica(t, CSS_BPMN) > estilo_fin:
        raise MarcadorError("la regla .bpmn no está dentro de <style>")
    return quitar_linea_unica(t, CSS_BPMN, nl)


def aplicar(t, nl):
    t = flujos_mermaid(t, nl)
    t = insertar_antes_de_seccion(t, "f-modulos", SECCION_F_REQ_PORTAL2, nl)
    t = insertar_antes_de_seccion(t, "u-intro", SECCION_U_IDIOMA_TECLADO, nl)
    t = insertar_antes_de_seccion(t, "t-api", SECCION_T_INTEGRACIONES, nl)
    t = insertar_antes_de_seccion(t, "t-seguridad", SECCION_T_UI_I18N_A11Y, nl)
    t = adr_y_fuentes(t, nl)
    t = quitar_xml_bpmn(t)
    t = quitar_libreria_bpmn(t, nl)
    t = quitar_inicializacion_bpmn(t, nl)
    t = quitar_css_bpmn(t, nl)
    return t


def mermaid_script(t):
    ini = unica(t, INICIO_MERMAID)
    return t[ini:despues(t, "</script>", ini) + len("</script>")]


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    with open(HTML, "rb") as f:
        crudo = f.read()
    bom = crudo.startswith(b"\xef\xbb\xbf")
    texto = crudo[3:].decode("utf-8") if bom else crudo.decode("utf-8")

    if 'id="t-integraciones"' in texto and "data-bpmn" not in texto:
        print("ya aplicado")
        return 0

    nl = "\r\n" if "\r\n" in texto else "\n"
    try:
        nuevo = aplicar(texto, nl)
        if mermaid_script(nuevo) != mermaid_script(texto):
            raise MarcadorError("el <script> de Mermaid cambió")
    except MarcadorError as e:
        print(f"ERROR {e}")
        print("no se escribió nada")
        return 1

    salida = (b"\xef\xbb\xbf" if bom else b"") + nuevo.encode("utf-8")
    with open(HTML, "wb") as f:
        f.write(salida)
    print(f"documentacion.html actualizado: {len(crudo)} → {len(salida)} bytes "
          f"({texto.count(nl) + 1} → {nuevo.count(nl) + 1} líneas)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
