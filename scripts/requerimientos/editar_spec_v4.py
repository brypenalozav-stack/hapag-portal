"""Fase 1: genera la especificación funcional v4.0 a partir del original validado.

Lee `katu\\Portal_2.0_Especificacion_Funcional_validada.docx` (nunca lo escribe) y guarda
`katu\\v4\\Portal_2.0_Especificacion_Funcional_v4.docx` con python-docx. Es re-ejecutable: cada
corrida parte del original. Aplica, en orden, los pasos 1–20 de la Fase 1 del plan
`thoughts/shared/plans/2026-10-05-actualizacion-requerimientos-portal-2-0-ui-integraciones.md`
y las decisiones de `docs/requerimientos/registro-decisiones-v4.md`.
"""
import copy
import os
import re
import sys

import docx
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.text.paragraph import Paragraph

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import config  # noqa: E402
import docx_utils as u  # noqa: E402

SALIDA = config.ruta_v4(config.MAPA_V4[os.path.basename(config.ESPECIFICACION)])

EQUIPO = "Hapag-Lloyd, a través del equipo de desarrollo del Portal 2.0, define y publica"
NUMID_VINETA = 1  # abstractNum 4: viñeta "–"
ABSTRACT_CRITERIOS = 0  # abstractNum 0: "1." como los criterios existentes


# ---------------------------------------------------------------- búsqueda

def parrafos_cuerpo(doc):
    return [Paragraph(el, doc) for el in doc.element.body.iterchildren(qn("w:p"))]


def buscar(doc, prefijo, estilo=None):
    """Único párrafo del cuerpo cuyo texto empieza con `prefijo`."""
    hallados = [p for p in parrafos_cuerpo(doc)
                if p.text.strip().startswith(prefijo) and (estilo is None or p.style.name == estilo)]
    if len(hallados) != 1:
        raise RuntimeError(f"se esperaba 1 párrafo con {prefijo!r} y hay {len(hallados)}")
    return hallados[0]


def reemplazar(p, viejo, nuevo):
    if not u.replace_in_runs(p, viejo, nuevo):
        raise RuntimeError(f"no se encontró {viejo!r} en {p.text[:60]!r}")


def reemplazar_global(doc, viejo, nuevo):
    """Reemplaza en todos los párrafos del cuerpo y de las tablas. Devuelve cuántos cambió."""
    n = 0
    for el in doc.element.body.iter(qn("w:p")):
        p = Paragraph(el, doc)
        while viejo in p.text and u.replace_in_runs(p, viejo, nuevo):
            n += 1
    return n


def tabla_con(doc, texto_celda):
    """Única tabla del cuerpo que contiene una celda con exactamente `texto_celda`."""
    hallados = [t for t in doc.element.body.iterchildren(qn("w:tbl"))
                if any(u.texto_el(tc).strip() == texto_celda for tc in t.iter(qn("w:tc")))]
    if len(hallados) != 1:
        raise RuntimeError(f"se esperaba 1 tabla con {texto_celda!r} y hay {len(hallados)}")
    return hallados[0]


def fila_con(tbl, texto_primera_celda):
    for tr in tbl.iterchildren(qn("w:tr")):
        if u.texto_el(tr.find(qn("w:tc"))).strip() == texto_primera_celda:
            return tr
    raise RuntimeError(f"sin fila {texto_primera_celda!r}")


def celdas(tr):
    return tr.findall(qn("w:tc"))


def eliminar(el):
    el.getparent().remove(el)


def fijar_espaciado(p_el, **attrs):
    ppr = p_el.find(qn("w:pPr"))
    sp = ppr.find(qn("w:spacing"))
    if sp is None:
        sp = OxmlElement("w:spacing")
        ppr.append(sp)
    for k in list(sp.attrib):
        del sp.attrib[k]
    for k, v in attrs.items():
        sp.set(qn(f"w:{k}"), v)


# ---------------------------------------------------------------- modelos de formato

def tomar_modelos(doc):
    """Copias de párrafos y tablas del original que sirven de molde para el contenido nuevo."""
    h3 = u.find_ficha(doc, "M1-01")._p
    rotulo = h3.getnext()
    cuerpo = rotulo.getnext()
    lista = next(p._p for p in parrafos_cuerpo(doc) if p.style.name == "List Paragraph")
    return {
        "h1": copy.deepcopy(buscar(doc, "14.   Modelo de integración", "Heading 1")._p),
        "h2": copy.deepcopy(buscar(doc, "2.3  Clasificación de los servicios", "Heading 2")._p),
        "intro": copy.deepcopy(buscar(doc, "El Portal 2.0 es la plataforma")._p),
        "h3": copy.deepcopy(h3),
        "rotulo": copy.deepcopy(rotulo),
        "cuerpo": copy.deepcopy(cuerpo),
        "lista": copy.deepcopy(lista),
        "tabla2": u.quitar_marcadores(copy.deepcopy(tabla_con(doc, "Qué contiene"))),
    }


def parrafo_normal(modelos, texto):
    return u.clonar_parrafo(modelos["intro"], [texto])


def parrafo_vineta(modelos, texto):
    return u.clonar_parrafo(modelos["lista"], [texto], num_id=NUMID_VINETA)


def insertar_despues(ancla, elementos):
    for el in elementos:
        ancla.addnext(el)
        ancla = el
    return ancla


# ---------------------------------------------------------------- pasos 1–10

def paso1_portada(doc):
    reemplazar(buscar(doc, "Flowcraft"), "Flowcraft", "Hapag-Lloyd Chile y Bolivia")
    reemplazar(buscar(doc, "Versión 3.0"), "Versión 3.0", "Versión 4.0")


def paso2_d1(doc):
    """Los Heading 2 de texto corrido de los capítulos 1–2 pasan a Normal / List Paragraph."""
    convertidos = []
    for p in parrafos_cuerpo(doc):
        if p.style.name == "Heading 1" and p.text.strip().startswith("3."):
            break
        if p.style.name != "Heading 2" or u._campos_tc(p._p):
            continue
        texto = p.text.strip()
        if texto.startswith("- "):
            u.set_style(p, "List Paragraph")
            ppr = p._p.get_or_add_pPr()
            numpr = OxmlElement("w:numPr")
            ilvl = OxmlElement("w:ilvl")
            ilvl.set(qn("w:val"), "0")
            nid = OxmlElement("w:numId")
            nid.set(qn("w:val"), str(NUMID_VINETA))
            numpr.append(ilvl)
            numpr.append(nid)
            ppr.find(qn("w:pStyle")).addnext(numpr)
            fijar_espaciado(p._p, after="60", line="272", lineRule="auto")
            u.fijar_texto(p, texto[2:])
        else:
            u.set_style(p, "Normal")
            fijar_espaciado(p._p, after="110", line="276", lineRule="auto")
        convertidos.append(texto[:50])
    if len(convertidos) != 12:
        raise RuntimeError(f"D1: se esperaban 12 párrafos y hay {len(convertidos)}")
    print(f"D1: {len(convertidos)} párrafos de texto corrido salen del estilo de título")


def paso3_d2(doc):
    p = buscar(doc, "4.2  Registro y administración de la organización")
    u.set_style(p, "Heading 2")
    ppr = p._p.get_or_add_pPr()
    sp = OxmlElement("w:spacing")
    sp.set(qn("w:before"), "300")
    sp.set(qn("w:after"), "150")
    ppr.find(qn("w:pStyle")).addnext(sp)
    if not u._campos_tc(p._p):
        u.insert_tc_field(p, "4.2  Registro y administración de la organización", 2)


def paso4_d3(doc):
    inicio, fin = u.bloque_ficha(doc, "M8-08")
    m9 = buscar(doc, "12.   M9.", "Heading 1")._p
    u.move_block(inicio, fin, m9)


def paso5_d4(doc):
    """Q2: se eliminan las referencias a M9-02/M9-03 y las marcas de validación pendiente."""
    u.reemplazar_seccion(doc, "M9-01", "DEPENDENCIAS", [
        "Requiere el perfil administrador de M8-06. Se alimenta de las transacciones y de las condiciones "
        "de Nexus aplicadas en M4, y se relaciona con la auditoría de accesos de M1-23 y con los registros "
        "de diagnóstico de NF-27.",
    ])
    u.fijar_texto(buscar(doc, "Este módulo reúne la información que Hapag-Lloyd necesita"),
                  "Este módulo reúne la información que Hapag-Lloyd necesita para controlar la operación del "
                  "portal: la reportería general de transacciones por servicio y el detalle de las excepciones "
                  "aplicadas. Los errores de integración que afectan la disponibilidad de los embarques se "
                  "registran según NF-27.")
    u.reemplazar_seccion(doc, "NF-27", "REQUERIMIENTO", [
        "Los errores deben quedar registrados con información suficiente para su diagnóstico, incluidos los "
        "errores de integración que impiden la publicación de embarques.",
        "Cada error de integración registra el sistema, la operación, el código de respuesta, la duración y "
        "el identificador de correlación de la solicitud, e incrementa un indicador de errores por sistema.",
    ])
    u.reemplazar_seccion(doc, "NF-27", "CRITERIOS DE ACEPTACIÓN", [
        "Los registros permiten identificar la causa de un error sin requerir su reproducción.",
        "Cada error de integración queda registrado con el sistema, la operación, el código de respuesta, "
        "la duración y el identificador de correlación.",
        "El indicador de errores por sistema permite configurar las alertas descritas en NF-26.",
    ])
    # M2-09 citaba un criterio de publicación sin ficha vigente (el contenido de la antigua M9-03).
    u.reemplazar_seccion(doc, "M2-09", "DEPENDENCIAS", [
        "Se apoya en las reglas de publicación de M2-01 para los embarques no publicados. Corresponde a "
        "CL-IMP-13 y BO-IMP-13.",
    ])


def paso6_d5(doc):
    tbl = tabla_con(doc, "Historial de pagos y boletas")
    tr = next(tr for tr in tbl.iterchildren(qn("w:tr")) if u.texto_el(celdas(tr)[1]).strip() == "Historial de pagos y boletas")
    u.fijar_texto_celda(celdas(tr)[0], "M7-02")


def paso7_d6(doc):
    eliminar(buscar(doc, "Agregar Funcionaliad para bloquear pagos")._p)
    eliminar(buscar(doc, "Además, dado que todavía no está definido quién lo hará")._p)


def paso8_q1(doc):
    # M3-02 = Fase 1
    reemplazar(buscar(doc, "Incorporar el servicio MHD, en su segunda fase,"),
               "Incorporar el servicio MHD, en su segunda fase, dentro", "Incorporar el servicio MHD dentro")
    # M6-02 = Fase 2, primera entrega sin pago ni carro
    u.fijar_texto(buscar(doc, "Consideración para la primera etapa:"),
                  "Consideración para la primera entrega dentro de Fase 2, sin pago ni carro: esta funcionalidad "
                  "deberá estar habilitada únicamente para la gestión e ingreso de información, sin activar las "
                  "opciones de pago ni la incorporación al carro de compra. En una entrega posterior se podrá "
                  "evaluar la habilitación del pago en línea y la incorporación del servicio al carro de compra, "
                  "una vez definido y validado el flujo de cobro correspondiente en moneda boliviana (BOB).")
    reemplazar(buscar(doc, "En la primera etapa la funcionalidad no presenta"),
               "En la primera etapa", "En la primera entrega dentro de Fase 2,")
    # M3-17 = Fase 2; el Web Service lo desarrolla y administra Hapag-Lloyd
    reemplazar(u.find_ficha(doc, "M3-17"), " / EN REVISIÓN", "")
    u.reemplazar_seccion(doc, "M3-17", "REQUERIMIENTO", [
        "Habilitar un canal de integración mediante Web Service que permita a determinados clientes enviar y "
        "recibir requerimientos operacionales asociados a sus BL sin necesidad de gestionarlos uno a uno desde "
        "la interfaz del portal.",
        "En una primera definición, este canal podría considerar procesos como carta de responsabilidad y "
        "cambio de almacén, manteniendo la trazabilidad, estados y controles de acceso definidos para el portal.",
        "El Web Service lo desarrolla y administra Hapag-Lloyd. Antes de su construcción se definen su "
        "arquitectura, el mecanismo de autenticación y las responsabilidades de soporte y mantenimiento.",
        "El alcance definitivo deberá ser validado tanto técnica como funcionalmente antes de su construcción.",
    ])
    u.reemplazar_seccion(doc, "M3-17", "CRITERIOS DE ACEPTACIÓN", [
        "El Web Service es desarrollado y administrado por Hapag-Lloyd.",
        "Se establece la arquitectura, mecanismo de autenticación y responsabilidades de soporte y mantenimiento.",
        "Se identifican los procesos que podrán gestionarse mediante este canal.",
        "Las solicitudes recibidas mantienen la misma trazabilidad y estados que las ingresadas por la interfaz del portal.",
        "Se aplican los mismos controles de acceso y seguridad definidos para el portal.",
        "El alcance definitivo es validado antes de iniciar el desarrollo.",
    ])
    u.reemplazar_seccion(doc, "M3-17", "DEPENDENCIAS", [
        "Definición de arquitectura e integración por parte de Hapag-Lloyd, seguridad y credenciales, y "
        "validación de los procesos que serán habilitados mediante Web Service.",
    ])
    # D8: M5-07 y M6-07 dependen de la lectura de la condición de crédito, no de M7-03
    u.reemplazar_seccion(doc, "M5-07", "REQUERIMIENTO", [
        "Habilitar la opción de pago vía portal para clientes con condición de crédito carta, tanto en "
        "exportación como en importación, permitiendo una gestión más integrada de sus servicios y cargos.",
        "El portal reconoce la condición de crédito del cliente mediante la lectura de Nexus descrita en M8-02 "
        "y aplica la exclusión del recargo IPO de M4-03. Los cargos de estos clientes se pagan desde una vista "
        "de pago propia de su condición, separada del carro de compra unificado de M5-01, que queda reservado "
        "para los clientes sin esa condición. Cuando el estado de cuenta en línea de M7-03 esté disponible "
        "(Fase 2), el pago de estos clientes se gestiona desde allí.",
        "El pago realizado a través del portal gatilla la liberación de la carga con la misma lógica aplicable "
        "a cualquier cliente, sin tratamiento diferenciado por la condición de crédito.",
    ])
    u.reemplazar_seccion(doc, "M5-07", "CRITERIOS DE ACEPTACIÓN", [
        "Un cliente con condición de crédito carta paga la totalidad de sus cargos de exportación e importación "
        "desde la vista de pago propia de su condición.",
        "Los cargos de un cliente con condición de crédito no se incorporan al carro de compra unificado.",
        "El portal reconoce la condición de crédito carta del cliente, leída desde Nexus, para dirigir su pago "
        "a esa vista.",
        "El pago efectuado en el portal gatilla la liberación de la carga sin intervención adicional.",
    ])
    u.reemplazar_seccion(doc, "M5-07", "DEPENDENCIAS", [
        "Requiere M8-02 para reconocer la condición de crédito y M4-03 para la exclusión del recargo IPO. Se "
        "relaciona con M7-01 y, en Fase 2, con el estado de cuenta de M7-03. Excluye a estos clientes de M5-01.",
    ])
    u.reemplazar_seccion(doc, "M6-07", "DEPENDENCIAS", [
        "Se apoya en M8-02 y M4-03 para la condición de crédito del cliente, en las facturas de M7-01 para "
        "verificar la deuda del embarque y en M3-16 para validar el pago obligatorio de demoras anticipadas. "
        "Corresponde a BO-IMP-15 y se publica en M6-09.",
    ])


def es_leyenda(el):
    if el.tag != qn("w:p") or not u.texto_el(el).strip():
        return False
    r = next(iter(el.iterchildren(qn("w:r"))), None)
    rpr = r.find(qn("w:rPr")) if r is not None else None
    if rpr is None:
        return False
    color = rpr.find(qn("w:color"))
    return rpr.find(qn("w:i")) is not None and color is not None and color.get(qn("w:val")) == "7A8899"


def paso9_d9(doc):
    """Elimina leyendas huérfanas: texto Normal de cierre de ficha que no sigue a una imagen."""
    huerfanas = []
    previo = None
    for el in list(doc.element.body.iterchildren()):
        vacio = el.tag == qn("w:p") and not u.texto_el(el).strip() and not u.tiene_dibujo(el)
        if vacio:
            continue
        if el.tag == qn("w:p") and Paragraph(el, doc).style.name == "Normal" and u.texto_el(el).strip():
            if (es_leyenda(el) and not u.tiene_dibujo(previo)) or (previo is not None and es_leyenda(previo) and not es_leyenda(el)):
                huerfanas.append(el)
        previo = el
    for el in huerfanas:
        print(f"D9: leyenda sin imagen eliminada: {u.texto_el(el).strip()!r}")
        eliminar(el)


def paso10_d10(doc):
    _, sa = u.parrafos_seccion(doc, "M8-05", "SITUACIÓN ACTUAL")
    movido = u.texto_el(sa[0]).strip()
    _, req = u.parrafos_seccion(doc, "M8-05", "REQUERIMIENTO")
    textos_req = [u.texto_el(p).strip() for p in req]
    u.reemplazar_seccion(doc, "M8-05", "REQUERIMIENTO", textos_req[:2] + [movido] + textos_req[2:])
    u.reemplazar_seccion(doc, "M8-05", "SITUACIÓN ACTUAL", [
        "Las funciones internas de administración están repartidas en pantallas separadas, cada una con su "
        "propio control de acceso por rol interno: clientes con condición de crédito, exenciones de demurrage, "
        "usuarios, importación de BL, aduana, plazos, auditoría y reportes. No existe un área única que reúna "
        "la gestión de organizaciones, perfiles, permisos, aprobaciones y configuraciones operacionales.",
    ])


# ---------------------------------------------------------------- paso 11

def paso11_terceros(doc):
    u.fijar_texto(buscar(doc, "La integración del portal dependerá de distintas fuentes"),
                  "La integración del portal depende de distintas fuentes de información de Hapag-Lloyd. Toda la "
                  "información se obtiene mediante APIs, sin acceso directo a bases de datos: Nexus, FIS y Data "
                  "Lake se consultan a través de las APIs descritas en el capítulo 14.")
    u.fijar_texto(buscar(doc, "Esta arquitectura aún está sujeta a validación técnica"),
                  "Esta arquitectura está sujeta a la validación técnica del equipo de desarrollo del Portal 2.0 "
                  "de Hapag-Lloyd. Por este motivo, la disponibilidad de las integraciones y la validación de sus "
                  "contratos constituyen una dependencia relevante para el avance del desarrollo.")
    u.fijar_texto(buscar(doc, "Requiere definición funcional con Finanzas y validación de la propuesta"),
                  "Requiere definición funcional con Finanzas y validación técnica del equipo de desarrollo del "
                  "Portal 2.0.")
    # M5-04
    for prefijo in ("El portal deberá permitir administrar internamente las monedas",
                    "La configuración puede modificarse internamente sin requerir"):
        reemplazar(buscar(doc, prefijo), "intervención del proveedor", "intervención del equipo de desarrollo")
    # Firma electrónica de M6-01, M6-02 y M6-07 (DC6)
    firma = ("El certificado se emite con firma electrónica conforme a la normativa aplicable. " + EQUIPO +
             " el mecanismo de firma y la forma en que se acredita su validez.")
    criterio = ("El certificado se emite con firma electrónica y la validez del mecanismo utilizado se acredita "
                "según lo publicado por Hapag-Lloyd.")
    for p in parrafos_cuerpo(doc):
        t = p.text.strip()
        if t.startswith("El certificado se emite con firma electrónica conforme a la normativa aplicable. El proveedor"):
            u.fijar_texto(p, firma)
        elif t == "El certificado se emite con firma electrónica y el proveedor acredita la validez del mecanismo utilizado.":
            u.fijar_texto(p, criterio)
    # NF (DC6)
    u.fijar_texto(buscar(doc, "Varios de estos requerimientos toman como referencia"),
                  "Varios de estos requerimientos toman como referencia el comportamiento del portal actualmente "
                  "en producción. Los valores que la solución compromete los define y publica Hapag-Lloyd, a "
                  "través del equipo de desarrollo del Portal 2.0.")
    u.fijar_texto(buscar(doc, "El proveedor debe indicar el nivel de disponibilidad"),
                  EQUIPO + " el nivel de disponibilidad comprometido y la ventana de mantenimiento, considerando "
                  "el horario hábil de ambos países.")
    u.fijar_texto(buscar(doc, "El proveedor debe indicar la política de respaldo"),
                  EQUIPO + " la política de respaldo, la pérdida máxima de información tolerada y el tiempo "
                  "máximo de recuperación.")
    reemplazar(buscar(doc, "El respaldo se ejecuta con la periodicidad indicada por el proveedor"),
               "indicada por el proveedor", "publicada")
    u.fijar_texto(buscar(doc, "El proveedor debe indicar la volumetría"),
                  EQUIPO + " la volumetría considerada en el dimensionamiento.")
    reemplazar(buscar(doc, "La solución soporta la volumetría indicada por el proveedor"),
               "indicada por el proveedor", "publicada")
    u.fijar_texto(buscar(doc, "El proveedor debe indicar los tiempos de respuesta"),
                  EQUIPO + " los tiempos de respuesta comprometidos para las operaciones de consulta y para el "
                  "cálculo de la calculadora de sobreestadía.")


# ---------------------------------------------------------------- pasos 12–13: contenido nuevo

M11_INTRO = [
    "Este módulo define cómo se presenta el portal a sus usuarios: el sistema visual común, el idioma de la "
    "interfaz, los formatos locales de cada país y las condiciones de accesibilidad que permiten operarlo con "
    "teclado, con lector de pantalla y desde dispositivos móviles. Es transversal: cada pantalla del portal debe "
    "cumplir estos requerimientos.",
    "El nivel de conformidad de accesibilidad es WCAG 2.2 AA y la interfaz se ofrece en español y en inglés. La "
    "traducción de correos, documentos PDF y respuestas del asistente de M10 corresponde a Fase 2.",
]

M11_FICHAS = [
    ("M11-01", "Sistema visual y tokens de diseño", [
        ("SITUACIÓN ACTUAL", [
            "Los colores de marca se repiten como valores sueltos en las distintas pantallas y no existe una "
            "definición única que los gobierne. El naranja de marca (#ff6600) y el verde (#009840) no alcanzan el "
            "contraste mínimo de 4,5:1 sobre fondo blanco cuando se usan en texto o en botones con texto blanco.",
            "En la base actual del Portal 2.0 la interfaz muestra además el azul por defecto del framework de "
            "estilos (#0d6efd), porque la personalización de colores no se aplica. La adopción de los colores "
            "corregidos produce, por lo tanto, un cambio visual mayor que el que sugiere el código actual.",
        ]),
        ("REQUERIMIENTO", [
            "Definir un sistema visual único basado en tokens de diseño: un conjunto cerrado y con nombre de "
            "colores, tipografías, radios y espaciados, declarado en un solo lugar y utilizado por todas las "
            "pantallas del portal.",
            "Los colores de texto y de componentes interactivos deben cumplir el contraste AA: primario #b84a00 "
            "(5,23:1 sobre blanco), éxito #007a33 (5,48:1) y texto secundario #4a5568 (7,53:1). El naranja de "
            "marca #ff6600 se reserva para el logo y para acentos que no transmiten texto.",
            "Las tipografías son Inter para el texto y Montserrat para los títulos.",
        ]),
        ("CRITERIOS DE ACEPTACIÓN", [
            "Los colores de la interfaz provienen del conjunto de tokens; no existen valores de color sueltos "
            "fuera de su definición.",
            "El color primario de botones, enlaces y elementos activos es #b84a00 y el color de éxito es #007a33.",
            "El texto secundario usa #4a5568.",
            "Todo texto alcanza un contraste mínimo de 4,5:1, o de 3:1 en texto grande, y los componentes no "
            "textuales alcanzan 3:1 (WCAG 1.4.3 y 1.4.11).",
            "El naranja #ff6600 solo se usa en el logo y en acentos que no transmiten texto.",
        ]),
        ("DEPENDENCIAS", ["Transversal a todos los módulos. Desarrolla M1-01 y es la base de M11-04 y M11-07."]),
    ]),
    ("M11-02", "Selector de idioma ES/EN en caliente", [
        ("SITUACIÓN ACTUAL", [
            "El portal se presenta solo en español y los textos están escritos directamente en cada pantalla, "
            "por lo que los clientes y los equipos que operan en inglés no disponen de una interfaz en su idioma.",
        ]),
        ("REQUERIMIENTO", [
            "Incorporar un selector de idioma ES/EN visible en todas las pantallas, incluidas las de ingreso y "
            "registro, que cambie el idioma de la interfaz sin recargar la página y sin perder el trabajo en curso.",
            "El inglés es internacional, con ortografía estadounidense, y los términos del negocio se traducen "
            "según el glosario ES/EN publicado por Hapag-Lloyd.",
            "La preferencia de idioma queda guardada para el usuario y se aplica en su próximo ingreso. Sin "
            "preferencia guardada, el portal usa inglés cuando el navegador está configurado en inglés y español "
            "en los demás casos.",
            "El idioma declarado del documento se actualiza con el idioma activo, de modo que los lectores de "
            "pantalla pronuncien correctamente el contenido.",
            "En Fase 1 el alcance es la interfaz: textos, etiquetas, mensajes de error y de estado, nombres "
            "accesibles y textos alternativos. Los correos, los documentos PDF y las respuestas del asistente de "
            "M10 se traducen en Fase 2.",
        ]),
        ("CRITERIOS DE ACEPTACIÓN", [
            "El selector de idioma ES/EN está disponible en todas las pantallas, incluidas las de ingreso y registro.",
            "Al cambiar el idioma, la interfaz se presenta en el idioma elegido sin recargar la página y sin "
            "perder los datos ingresados.",
            "Tras el cambio no quedan textos de la interfaz en el idioma anterior, incluidos mensajes de error, "
            "estados, nombres accesibles y textos alternativos.",
            "La preferencia de idioma se conserva entre sesiones.",
            "El idioma declarado del documento corresponde al idioma activo.",
            "La terminología en inglés coincide con el glosario ES/EN.",
        ]),
        ("DEPENDENCIAS", [
            "Requiere M11-03 para los formatos locales. Define el alcance de idioma de NF-23. Se relaciona con "
            "M1-04 y M10-01.",
        ]),
    ]),
    ("M11-03", "Formatos locales por país", [
        ("SITUACIÓN ACTUAL", [
            "Las fechas y los montos se presentan con patrones fijos que no dependen del país ni del idioma, de "
            "modo que un mismo valor puede leerse de forma distinta en Chile y en Bolivia.",
        ]),
        ("REQUERIMIENTO", [
            "Presentar fechas, horas, números y montos según el país de operación seleccionado en M1-04 y el "
            "idioma activo de M11-02.",
            "Fechas: dd-MM-yyyy en Chile, dd/MM/yyyy en Bolivia y dd MMM yyyy en inglés. Las horas se presentan "
            "en formato de 24 horas, en el huso horario del país (America/Santiago o America/La_Paz), "
            "indicándolo conforme a NF-22.",
            "Montos: siempre acompañados del código ISO de su moneda (CLP, BOB, USD o EUR). Los montos en CLP se "
            "presentan sin decimales y los montos en BOB, USD y EUR con dos decimales. Los separadores de miles "
            "y de decimales siguen el idioma activo.",
        ]),
        ("CRITERIOS DE ACEPTACIÓN", [
            "En Chile las fechas se presentan como dd-MM-yyyy, en Bolivia como dd/MM/yyyy y en inglés como dd MMM yyyy.",
            "Las horas se presentan en formato de 24 horas e indican el huso horario del país.",
            "Todo monto se presenta con el código ISO de su moneda.",
            "Los montos en CLP no muestran decimales y los montos en BOB, USD y EUR muestran dos.",
            "Los separadores de miles y de decimales corresponden al idioma activo.",
            "Cambiar de país o de idioma actualiza los formatos sin recargar la página.",
        ]),
        ("DEPENDENCIAS", ["Requiere M1-04 y M11-02. Se relaciona con NF-22 y NF-23."]),
    ]),
    ("M11-04", "Conformidad WCAG 2.2 AA", [
        ("SITUACIÓN ACTUAL", [
            "La interfaz no tiene un nivel de accesibilidad definido ni verificado: hay botones sin nombre "
            "accesible, tablas sin encabezados asociados y mensajes que no se anuncian a los lectores de pantalla.",
        ]),
        ("REQUERIMIENTO", [
            "El portal debe cumplir el nivel AA de las Pautas de Accesibilidad para el Contenido Web (WCAG) 2.2 "
            "del W3C en sus 55 criterios de nivel A y AA. El criterio 4.1.1, declarado obsoleto en WCAG 2.2, "
            "queda fuera.",
            "La conformidad se verifica en español y en inglés con cuatro evidencias: revisión automática de "
            "accesibilidad de las plantillas sin errores en la integración continua, prueba automática con axe "
            "sin violaciones en las pantallas principales, puntaje de accesibilidad de Lighthouse igual o "
            "superior a 95 y prueba manual con teclado y con el lector de pantalla NVDA según el checklist de la "
            "Guía de interfaz, accesibilidad e idiomas.",
            "Cada pantalla nueva debe cumplir este nivel antes de su puesta en producción.",
        ]),
        ("CRITERIOS DE ACEPTACIÓN", [
            "La revisión automática de accesibilidad de las plantillas no reporta errores en la integración continua.",
            "La prueba automática con axe reporta 0 violaciones en las pantallas principales, en español y en inglés.",
            "Lighthouse otorga un puntaje de accesibilidad igual o superior a 95 en las pantallas principales.",
            "La prueba manual con teclado y NVDA según el checklist de la Guía se completa sin hallazgos bloqueantes.",
            "Las tablas de datos tienen encabezados asociados y todos los controles de formulario tienen etiqueta.",
        ]),
        ("DEPENDENCIAS", [
            "Transversal a todos los módulos. Requiere M11-01, M11-05 y M11-06. Se relaciona con NF-21.",
        ]),
    ]),
    ("M11-05", "Operación por teclado y foco visible", [
        ("SITUACIÓN ACTUAL", [
            "Algunos controles solo responden al ratón y el foco del teclado no siempre es visible, lo que impide "
            "operar el portal sin un dispositivo apuntador.",
        ]),
        ("REQUERIMIENTO", [
            "Todas las funcionalidades del portal deben poder operarse solo con el teclado, en un orden de "
            "navegación coherente con la lectura de la pantalla y sin trampas de foco.",
            "El foco debe ser siempre visible, con un indicador de contraste mínimo de 3:1, y no quedar oculto "
            "detrás de cabeceras, barras fijas o ventanas emergentes (WCAG 2.4.7 y 2.4.11).",
            "Cada página ofrece un enlace para saltar al contenido principal, y los diálogos devuelven el foco al "
            "elemento que los abrió al cerrarse.",
        ]),
        ("CRITERIOS DE ACEPTACIÓN", [
            "Todas las funcionalidades, incluidos el carro de compra y el pago, se completan usando solo el teclado.",
            "El orden de tabulación sigue el orden visual de la pantalla.",
            "El foco es visible en todo elemento interactivo, con un contraste mínimo de 3:1, y no queda oculto "
            "por otros elementos.",
            "El primer elemento enfocable de cada página es un enlace para saltar al contenido principal.",
            "Los diálogos se cierran con la tecla Escape y, al cerrarse, el foco vuelve al elemento que los abrió.",
            "No existen trampas de foco.",
        ]),
        ("DEPENDENCIAS", ["Requiere M11-01. Forma parte de la conformidad de M11-04."]),
    ]),
    ("M11-06", "Anuncios dinámicos (aria-live) en pagos, cargas y errores", [
        ("SITUACIÓN ACTUAL", [
            "Los cambios de estado que ocurren sin recargar la página, como la confirmación de un pago, el avance "
            "de una carga o un error de validación, solo se muestran visualmente y no se anuncian a los usuarios "
            "de lectores de pantalla.",
        ]),
        ("REQUERIMIENTO", [
            "Los cambios de estado relevantes deben anunciarse mediante regiones dinámicas accesibles (aria-live), "
            "sin mover el foco del usuario: confirmaciones y resultados de pago, avance y resultado de cargas y "
            "procesos masivos, cambios del carro de compra y mensajes de error.",
            "Los errores que impiden continuar se anuncian de inmediato y los mensajes informativos de forma no "
            "intrusiva. En los formularios, cada error se asocia a su campo y, al enviar, el foco se dirige al "
            "primer campo con error.",
        ]),
        ("CRITERIOS DE ACEPTACIÓN", [
            "La confirmación o el rechazo de un pago se anuncia al lector de pantalla sin recargar la página.",
            "El avance y el resultado de una carga o de un proceso masivo se anuncian al lector de pantalla.",
            "Agregar o quitar un ítem del carro se anuncia junto con el nuevo subtotal.",
            "Los errores de validación se asocian a su campo y se anuncian; al enviar, el foco va al primer campo "
            "con error.",
            "Los anuncios se emiten en el idioma activo.",
        ]),
        ("DEPENDENCIAS", [
            "Requiere M11-02. Se relaciona con M5-01, M5-08, NF-11, NF-12 y NF-19. Forma parte de la "
            "conformidad de M11-04.",
        ]),
    ]),
    ("M11-07", "Tema claro/oscuro y reducción de movimiento", [
        ("SITUACIÓN ACTUAL", [
            "El portal tiene un único tema claro y sus animaciones no consideran la preferencia de movimiento "
            "reducido que el usuario configura en su sistema operativo.",
        ]),
        ("REQUERIMIENTO", [
            "Ofrecer un tema claro y un tema oscuro, construidos sobre los tokens de M11-01, con un selector "
            "visible que conserve la preferencia del usuario. Sin preferencia guardada, el portal aplica el tema "
            "configurado en el sistema operativo.",
            "Ambos temas cumplen los contrastes exigidos en M11-01 y M11-04.",
            "Cuando el usuario tiene activada la preferencia de reducir el movimiento, el portal suprime las "
            "animaciones y transiciones no esenciales.",
        ]),
        ("CRITERIOS DE ACEPTACIÓN", [
            "El usuario puede alternar entre el tema claro y el oscuro, y la preferencia se conserva entre sesiones.",
            "Sin preferencia guardada, se aplica el tema del sistema operativo.",
            "Ambos temas cumplen los contrastes de M11-01 y M11-04.",
            "Con la preferencia de reducir el movimiento activa, no se ejecutan animaciones ni transiciones no esenciales.",
        ]),
        ("DEPENDENCIAS", ["Requiere M11-01. Se relaciona con M11-04."]),
    ]),
    ("M11-08", "Navegadores y dispositivos soportados", [
        ("SITUACIÓN ACTUAL", [
            "No existe una definición explícita de los navegadores y tamaños de pantalla soportados, por lo que "
            "la compatibilidad se verifica caso a caso.",
        ]),
        ("REQUERIMIENTO", [
            "El portal soporta las dos últimas versiones mayores de Chrome, Edge, Firefox, Safari (macOS), Safari "
            "iOS y Chrome Android, según NF-20, y los anchos de pantalla de 360, 768, 1024 y 1280 px o más, según "
            "NF-21.",
            "El listado de navegadores es la referencia única de compatibilidad: la misma lista define el código "
            "que se genera para el navegador y el aviso de navegador no soportado.",
            "La consulta y el pago funcionan en dispositivos móviles, y a 320 px de ancho el contenido se "
            "reorganiza sin desplazamiento horizontal (WCAG 1.4.10).",
        ]),
        ("CRITERIOS DE ACEPTACIÓN", [
            "Las funcionalidades operan en las dos últimas versiones mayores de los navegadores listados en NF-20.",
            "Al ingresar desde un navegador no soportado, el portal muestra un aviso en el idioma activo.",
            "Las pantallas se presentan sin pérdida de información en anchos de 360, 768, 1024 y 1280 px o más.",
            "A 320 px de ancho el contenido no requiere desplazamiento horizontal, salvo tablas de datos y diagramas.",
            "La consulta de embarques y el pago se completan en un dispositivo móvil.",
        ]),
        ("DEPENDENCIAS", ["Desarrolla NF-20 y NF-21. Se relaciona con M11-04."]),
    ]),
]

M2_10 = ("M2-10", "Consulta de plazos documentales por nave", [
    ("SITUACIÓN ACTUAL", [
        "Nexus calcula los plazos documentales de cada nave a partir de su ETA y del calendario de feriados, y "
        "los publica mediante una planilla enviada por correo y una página web separada del portal. El cliente "
        "debe recurrir a esos canales para conocer los plazos que condicionan sus gestiones documentales.",
    ]),
    ("REQUERIMIENTO", [
        "Habilitar en el portal la consulta de los plazos documentales por nave, viaje y puerto, con la ETA y la "
        "fecha límite de cada plazo.",
        "Nexus sigue siendo responsable de calcular los plazos y de mantener el calendario de feriados. El "
        "portal consulta la información vigente mediante la API de Nexus descrita en el capítulo 15, sin "
        "replicar el cálculo.",
        "Desde el detalle de un embarque, descrito en M2-06, el cliente visualiza los plazos documentales de la "
        "nave y el viaje asociados. Las fechas se presentan en el huso del país, según NF-22.",
    ]),
    ("CRITERIOS DE ACEPTACIÓN", [
        "El cliente consulta los plazos documentales filtrando por nave, viaje o puerto.",
        "Cada plazo muestra la nave, el viaje, el puerto, la ETA y la fecha límite.",
        "Los plazos presentados coinciden con los calculados por Nexus, incluidos los ajustes por feriados.",
        "El detalle del embarque muestra los plazos de su nave y viaje.",
        "Cuando la API de Nexus no responde, el portal lo informa según NF-11.",
    ]),
    ("DEPENDENCIAS", [
        "Requiere M2-06 y que el contrato de integración con Nexus del capítulo 15 incorpore la consulta de "
        "plazos documentales. Se relaciona con M3-04, M3-13, M3-14 y NF-22.",
    ]),
])

M8_09 = ("M8-09", "Counter Bolivia/Ultramar", [
    ("SITUACIÓN ACTUAL", [
        "El canje del BL, la recepción del HBL y la marca de desconsolidado de los embarques de Bolivia y de "
        "ultramar se registran hoy fuera del portal, en registros separados y planillas, y el cierre de la "
        "desconsolidación solo queda en el sistema del depósito. No existe una pantalla para registrar la "
        "recepción del HBL ni la marca de desconsolidado.",
    ]),
    ("REQUERIMIENTO", [
        "Incorporar en el portal interno una funcionalidad de Counter que permita a los usuarios internos "
        "autorizados registrar, por BL, la fecha de canje, la recepción del HBL y la marca de desconsolidado, "
        "indicando el país de la operación, Chile o Bolivia.",
        "Cada registro queda asociado al BL y al usuario que lo realizó, y su estado es consultable por los "
        "perfiles internos desde el detalle del embarque.",
        "El estado registrado en Counter es insumo de la carta de liberación y desconsolidado de M6-08 y del "
        "certificado de libre deuda de M6-07.",
    ]),
    ("CRITERIOS DE ACEPTACIÓN", [
        "El usuario interno autorizado registra por BL la fecha de canje, la recepción del HBL y la marca de "
        "desconsolidado.",
        "Cada registro indica el país de la operación.",
        "Cada registro y cada modificación quedan registrados con usuario, fecha y hora, según NF-15.",
        "Los perfiles internos consultan el estado de canje, recepción del HBL y desconsolidado desde el "
        "detalle del embarque.",
        "La funcionalidad solo está disponible para los perfiles internos autorizados según M8-06.",
    ]),
    ("DEPENDENCIAS", ["Requiere M8-05 y M8-06. Se relaciona con M2-06, M6-07 y M6-08."]),
])


def insertar_ficha(doc, modelos, ancla, ficha):
    ficha_id, titulo, secciones = ficha
    fase = "FASE 1" if ficha_id.startswith("M11") else "FASE 2"
    return u.insert_ficha_after(ancla, ficha_id, titulo, fase, secciones, doc, modelos=modelos,
                                num_id=u.nuevo_num_id(doc, ABSTRACT_CRITERIOS))


def paso12_m11(doc, modelos):
    destino = buscar(doc, "15.   Modelo de integración", "Heading 1")._p
    titulo = "14.   M11. Interfaz, accesibilidad e idiomas – FASE 1"
    h1 = u.clonar_parrafo(modelos["h1"], ["14.   ", "M11. Interfaz, accesibilidad e idiomas ", "– FASE ", "1"], tc=titulo)
    destino.addprevious(h1)
    ultimo = insertar_despues(h1, [parrafo_normal(modelos, t) for t in M11_INTRO])
    for ficha in M11_FICHAS:
        ultimo = insertar_ficha(doc, modelos, ultimo, ficha)


def paso13_nexus(doc, modelos):
    _, fin = u.bloque_ficha(doc, "M2-09")
    insertar_ficha(doc, modelos, fin, M2_10)
    _, fin = u.bloque_ficha(doc, "M8-08")
    insertar_ficha(doc, modelos, fin, M8_09)


# ---------------------------------------------------------------- paso 14: renumeración (DC1)

def paso14_renumerar(doc):
    titulos = [p for p in parrafos_cuerpo(doc) if u._campos_tc(p._p)]
    cambios = [(16, 17), (15, 16), (14, 15)]
    for viejo, nuevo in cambios:
        for p in titulos:
            t = p.text
            m = re.match(rf"^{viejo}(\.\d+)?(\.?\s+)", t)
            if not m:
                continue
            prefijo = f"{nuevo}{m.group(1) or ''}{m.group(2)}"
            reemplazar(p, t[:m.end()], prefijo)
            if "integración– FASE" in p.text:
                reemplazar(p, "integración–", "integración –")
            u.fijar_tc(p._p, p.text)
    n14 = reemplazar_global(doc, "capítulo 14", "capítulo 15")
    n413 = reemplazar_global(doc, "capítulos 4 a 13", "capítulos 4 a 14")
    print(f"DC1: referencias 'capítulo 14' → 15: {n14}; 'capítulos 4 a 13' → 4 a 14: {n413}")


# ---------------------------------------------------------------- pasos 15–16: reescritura

def paso15_nf(doc):
    u.reemplazar_seccion(doc, "NF-20", "REQUERIMIENTO", [
        "El portal debe operar correctamente en las dos últimas versiones mayores de Chrome, Edge, Firefox, "
        "Safari (macOS), Safari iOS y Chrome Android.",
        "Este listado es la referencia única de compatibilidad del portal y se revisa con cada nueva versión "
        "mayor de los navegadores.",
        "Al ingresar desde un navegador no soportado, el portal muestra un aviso que indica los navegadores "
        "soportados.",
    ])
    u.reemplazar_seccion(doc, "NF-20", "CRITERIOS DE ACEPTACIÓN", [
        "Las funcionalidades del portal operan correctamente en las dos últimas versiones mayores de Chrome, "
        "Edge, Firefox, Safari (macOS), Safari iOS y Chrome Android.",
        "El portal informa al usuario cuando accede desde un navegador no soportado.",
    ])
    u.reemplazar_seccion(doc, "NF-21", "REQUERIMIENTO", [
        "El portal debe ser utilizable en equipos de escritorio y en dispositivos móviles, en los anchos de "
        "referencia de 360, 768, 1024 y 1280 px o más.",
        "Las funcionalidades de consulta y de pago deben estar disponibles en dispositivos móviles, y a 320 px "
        "de ancho el contenido debe reorganizarse sin desplazamiento horizontal (WCAG 1.4.10).",
    ])
    u.reemplazar_seccion(doc, "NF-21", "CRITERIOS DE ACEPTACIÓN", [
        "Las funcionalidades de consulta y de pago están disponibles y son utilizables en dispositivos móviles.",
        "La interfaz se adapta a los anchos de 360, 768, 1024 y 1280 px o más sin pérdida de información.",
        "A 320 px de ancho el contenido no requiere desplazamiento horizontal, salvo tablas de datos y diagramas.",
    ])
    u.reemplazar_seccion(doc, "NF-22", "REQUERIMIENTO", [
        "Todo cálculo que dependa del tiempo transcurrido, incluidas las tarifas definidas por tramos descritas "
        "en M2-03, M2-04 y M8-01, se realiza en UTC y aplica el calendario de negocio del país, con sus días "
        "hábiles y feriados.",
        "El resultado se presenta en el huso horario del país de la operación (America/Santiago o "
        "America/La_Paz), indicando el huso junto a la fecha y la hora.",
        "La solución considera que Chile y Bolivia no comparten huso horario y que en Chile existe cambio de "
        "horario estacional.",
        EQUIPO + " el calendario de negocio de cada país.",
    ])
    u.reemplazar_seccion(doc, "NF-22", "CRITERIOS DE ACEPTACIÓN", [
        "Los cálculos que dependen del tiempo transcurrido se realizan en UTC con el calendario de negocio del país.",
        "El resultado de un cálculo no varía por efecto del cambio de horario estacional.",
        "Las fechas y horas presentadas al cliente están en el huso del país e indican el huso al que corresponden.",
    ])
    u.reemplazar_seccion(doc, "NF-23", "REQUERIMIENTO", [
        "El portal debe presentarse en español o en inglés, a elección del usuario, mediante el selector de "
        "idioma ES/EN sin recargar la página descrito en M11-02.",
        "Los formatos de fecha, hora, número y moneda corresponden al país seleccionado según M1-04 y al idioma "
        "activo, conforme a M11-03.",
    ])
    u.reemplazar_seccion(doc, "NF-23", "CRITERIOS DE ACEPTACIÓN", [
        "La interfaz está disponible en español y en inglés, y el idioma se cambia sin recargar la página.",
        "Los formatos de fecha, número y moneda corresponden al país seleccionado y al idioma activo.",
        "Los montos se presentan siempre con el código ISO de su moneda.",
    ])


CAPITULO_15 = [
    ("p", "La integración del portal con los sistemas de Hapag-Lloyd se realiza exclusivamente mediante APIs. No "
          "se contempla el acceso directo a las bases de datos ni a los sistemas de origen: Nexus, FIS y Data Lake "
          "se consultan por API, sin sincronización de tablas entre bases de datos."),
    ("p", "Los datos de Nexus se consultan bajo demanda, con una caché de corta duración. Nexus sigue siendo la "
          "fuente de las condiciones de crédito, las exenciones, los FFWW autorizados, el tipo de cambio, las "
          "tarifas y los plazos documentales. Errores de facturación permanece en Nexus y queda fuera del alcance "
          "del portal."),
    ("p", "Mientras el contrato de integración con FIS no esté validado, los embarques entran al portal mediante "
          "la importación de BL existente (POST bills-of-lading/import), que se mantiene como entrada transitoria "
          "hasta conmutar FIS a la integración por API."),
    ("p", "El inventario de sistemas, los contratos de integración y la hoja de ruta están publicados en el "
          "repositorio del Portal 2.0, en docs/integraciones/. Todos los contratos están hoy en estado PROPUESTA: "
          + EQUIPO + " cada contrato, y el responsable de validación de cada sistema lo revisa y aprueba antes "
          "de su uso productivo."),
    ("p", "Cada sistema externo se accede a través de un puerto de integración que funciona en uno de dos modos, "
          "seleccionado por configuración (Integrations:<Sistema>:Mode):"),
    ("v", "Dummy: adaptador simulado y determinista que permite desarrollar y probar sin depender de la "
          "disponibilidad del sistema externo. Es el modo por defecto."),
    ("v", "Real: cliente HTTP contra la API del sistema. Se habilita en un ambiente solo con el contrato validado "
          "y el checklist de paso de Dummy a Real completado, y puede volver a Dummy cambiando la configuración."),
    ("p", "Un simulador HTTP reproduce los contratos y los escenarios de falla (error del servidor, tiempo de "
          "espera agotado, respuesta lenta y límite de solicitudes) para probar los clientes Real y verificar los "
          "contratos en la integración continua."),
    ("p", "Resiliencia: cada llamada a un sistema externo tiene un tiempo máximo por intento y un tiempo máximo "
          "total, reintentos con espera exponencial ante fallas transitorias y un interruptor de circuito que "
          "suspende temporalmente las llamadas cuando la tasa de fallas supera el umbral. Una falla de integración "
          "se traduce en un error controlado y se informa al cliente según NF-11."),
    ("p", "Diagnóstico: los errores de integración se registran según NF-27, con el sistema, la operación, el "
          "código de respuesta, la duración y el identificador de correlación, y alimentan un indicador de "
          "errores por sistema que permite configurar las alertas de NF-26. Las credenciales de cada integración "
          "se administran según NF-09."),
]


def paso16_integracion(doc, modelos):
    h1 = buscar(doc, "15.   Modelo de integración", "Heading 1")._p
    el = h1.getnext()
    while el is not None and not u.es_titulo(el):
        siguiente = el.getnext()
        eliminar(el)
        el = siguiente
    nuevos = [parrafo_normal(modelos, t) if tipo == "p" else parrafo_vineta(modelos, t) for tipo, t in CAPITULO_15]
    insertar_despues(h1, nuevos + [u.clonar_parrafo(modelos["intro"], [""])])


# ---------------------------------------------------------------- pasos 17–19: cierre

def paso17_cifras(doc):
    tbl = tabla_con(doc, "Requerimientos funcionales")
    tr = fila_con(tbl, "Requerimientos funcionales")
    u.fijar_texto_celda(celdas(tr)[1], "107")
    u.fijar_texto_celda(celdas(tr)[2], "Fichas detalladas, agrupadas en once módulos: 72 de Fase 1, 31 de Fase 2 y 4 de Fase 0.")
    u.agregar_fila(tbl, tr, ["Requerimientos no funcionales", "27",
                             "Integridad, seguridad, disponibilidad, trazabilidad, rendimiento, compatibilidad, "
                             "localización y operación."])
    u.agregar_fila(tbl, tr, ["Total de fichas", "134", "Requerimientos funcionales y no funcionales."])

    mod = tabla_con(doc, "M10")
    u.fijar_texto_celda(celdas(fila_con(mod, "M2"))[2], "10")
    u.fijar_texto_celda(celdas(fila_con(mod, "M8"))[2], "9")
    u.agregar_fila(mod, fila_con(mod, "M10"), ["M11", "Interfaz, accesibilidad e idiomas", "8"])

    h2 = buscar(doc, "Los diez módulos funcionales", "Heading 2")
    reemplazar(h2, "diez", "once")
    u.fijar_tc(h2._p, "Los once módulos funcionales")
    n = reemplazar_global(doc, "diez módulos", "once módulos")
    reemplazar(buscar(doc, "Capacidades transversales de plataforma"),
               "integraciones y reportería.", "integraciones, reportería, interfaz bilingüe y accesibilidad.")
    print(f"Cifras: 'diez módulos' → 'once módulos' en {n} párrafo(s)")


NUEVAS_ANEXO = [("M2-10", M2_10[1]), ("M8-09", M8_09[1])] + [(f[0], f[1]) for f in M11_FICHAS]


def paso18_anexo(doc):
    tbl = tabla_con(doc, "US-90")
    modelo = fila_con(tbl, "US-90")
    for ficha_id, titulo in NUEVAS_ANEXO:
        u.agregar_fila(tbl, modelo, ["Sin US: requerimiento adicional (v4)", titulo, ficha_id])
    p = buscar(doc, "Las historias que corresponden a servicios ya disponibles")
    p.add_run(" Los requerimientos incorporados en la versión 4.0 sin historia de usuario asociada se identifican "
              "como «Sin US: requerimiento adicional (v4)».")
    # el run agregado hereda el formato del párrafo; se copia el rPr del primer run
    rpr = p.runs[0]._r.find(qn("w:rPr"))
    if rpr is not None:
        p.runs[-1]._r.insert(0, copy.deepcopy(rpr))


HISTORIAL = [
    ("Portada", "Versión 4.0, preparada para Hapag-Lloyd Chile y Bolivia (DC5)."),
    ("D1 Estilos de título", "Doce párrafos de texto corrido de los capítulos 1 y 2 que tenían estilo de título "
                             "pasan a texto normal y viñetas, y dejan de aparecer en el índice."),
    ("D2 Título 4.2", "«4.2 Registro y administración de la organización» recupera su nivel de título y su "
                      "entrada en el índice."),
    ("D3 Ubicación de M8-08", "La ficha M8-08 se traslada al final del módulo M8; antes quedaba dentro del "
                              "capítulo de M9."),
    ("D4 Referencias sin ficha", "Se eliminan las referencias a fichas de M9 sin desarrollo y las marcas de "
                                 "validación pendiente. M9-01 se relaciona con M1-23 y NF-27, y NF-27 queda "
                                 "autocontenida (Q2)."),
    ("D5 Tabla de cierre de M7", "El historial de pagos y boletas se identifica como M7-02."),
    ("D6 Notas sueltas", "Se eliminan la nota de trabajo del inicio de M8 y la nota de opinión sobre la fase de M3-17."),
    ("D7 Fases de M3-02, M6-02 y M3-17", "Manda el encabezado de cada ficha: M3-02 queda en Fase 1; M6-02 en "
                                         "Fase 2, con una primera entrega sin pago ni carro; y M3-17 en Fase 2, con "
                                         "el Web Service desarrollado y administrado por Hapag-Lloyd (Q1)."),
    ("D8 Dependencias de M5-07 y M6-07", "Dependen de la lectura de la condición de crédito (M4-03 y M8-02) y no "
                                         "del estado de cuenta de M7-03, que sigue en Fase 2 (Q1)."),
    ("D9 Leyendas sin imagen", "Se elimina la leyenda sin imagen del cierre de M7-03."),
    ("D10 Situación actual de M8-05", "El texto que describía el requerimiento pasa a la sección Requerimiento y "
                                      "la situación actual se reescribe."),
    ("Capítulo 14, M11", "Nuevo módulo M11 «Interfaz, accesibilidad e idiomas», con las fichas M11-01 a M11-08 "
                         "en Fase 1. Integración pasa al capítulo 15, los requerimientos no funcionales al 16 y el "
                         "glosario al 17 (DC1)."),
    ("M2-10", "Nueva ficha «Consulta de plazos documentales por nave», Fase 2, a partir de la función Plazos "
              "documentales / Feriados de Nexus (Q7)."),
    ("M8-09", "Nueva ficha «Counter Bolivia/Ultramar», Fase 2, a partir de la función Counter de Nexus (Q7). "
              "Errores de facturación permanece en Nexus."),
    ("Q3, capítulo 15", "Integración con Nexus, FIS y Data Lake por API, sin acceso directo a bases de datos ni "
                        "sincronización de tablas. El capítulo 15 describe el inventario, los modos Dummy y Real, "
                        "la resiliencia y el registro de errores de integración."),
    ("Q4", "Sin cambios en este documento: la reformulación del módulo de base de datos aplica al listado de "
           "pendientes."),
    ("Q5", "Sin cambios en este documento: la asignación de responsables aplica al listado de pendientes."),
    ("Q6", "Los datos de FIS entran por la importación de BL existente hasta validar el contrato de integración "
           "con FIS (capítulo 15)."),
    ("Q8, NF-23", "Interfaz en español e inglés con cambio de idioma sin recargar la página y formatos locales "
                  "por país (M11-02 y M11-03). Correos, PDF y asistente en inglés quedan en Fase 2."),
    ("Q9, M11-04", "Conformidad WCAG 2.2 AA con evidencia automática y manual."),
    ("DC2, NF-20 y NF-21", "Navegadores soportados y anchos de pantalla de referencia definidos en el documento."),
    ("DC3, NF-22", "Cálculo en UTC con calendario de negocio y presentación en el huso del país."),
    ("DC4, M11-01", "Colores corregidos para contraste AA: primario #b84a00, éxito #007a33 y texto secundario #4a5568."),
    ("DC6", "Los valores de disponibilidad, respaldo, volumetría, tiempos de respuesta, navegadores, calendario "
            "de negocio y firma electrónica los define y publica Hapag-Lloyd, a través del equipo de desarrollo "
            "del Portal 2.0."),
    ("DC7", "Sin cambios en este documento: el calendario y los validadores aplican a la ejecución del plan."),
    ("Cifras", "107 requerimientos funcionales en once módulos (72 de Fase 1, 31 de Fase 2 y 4 de Fase 0) y 27 "
               "no funcionales: 134 fichas en total."),
]


def paso19_historial(doc, modelos):
    destino = buscar(doc, "3.   Catálogo de servicios por operación", "Heading 1")._p
    titulo = "2.4  Historial de cambios"
    h2 = u.clonar_parrafo(modelos["h2"], [titulo], tc=titulo)
    intro = parrafo_normal(modelos, "Esta sección resume los cambios de la versión 4.0 respecto de la versión 3.0. "
                                    "Los códigos D identifican correcciones de forma del documento; los códigos Q "
                                    "y DC, las decisiones del registro de decisiones v4 aprobado el 05-10-2026.")
    tabla = modelos["tabla2"]
    filas = tabla.findall(qn("w:tr"))
    cabecera, modelo = filas[0], filas[1]
    for tr in filas[1:]:
        tabla.remove(tr)
    for tc, texto in zip(celdas(cabecera), ["Cambio", "Descripción"]):
        u.fijar_texto_celda(tc, texto)
    for cambio, descripcion in HISTORIAL:
        u.agregar_fila(tabla, modelo, [cambio, descripcion])
    for el in (h2, intro, tabla, u.clonar_parrafo(modelos["intro"], [""])):
        destino.addprevious(el)


# ---------------------------------------------------------------- principal

def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    doc = docx.Document(config.ESPECIFICACION)
    modelos = tomar_modelos(doc)

    paso1_portada(doc)
    paso2_d1(doc)
    paso3_d2(doc)
    paso4_d3(doc)
    paso5_d4(doc)
    paso6_d5(doc)
    paso7_d6(doc)
    paso8_q1(doc)
    paso9_d9(doc)
    paso10_d10(doc)
    paso11_terceros(doc)
    paso14_renumerar(doc)  # antes de insertar el capítulo 14 nuevo
    paso12_m11(doc, modelos)
    paso13_nexus(doc, modelos)
    paso15_nf(doc)
    paso16_integracion(doc, modelos)
    paso17_cifras(doc)
    paso18_anexo(doc)
    paso19_historial(doc, modelos)

    os.makedirs(config.V4, exist_ok=True)
    config.assert_not_original(SALIDA)
    doc.save(SALIDA)
    u.set_update_fields_on_open(SALIDA)
    print(f"fichas: {len(u.fichas(doc))}; campos TC: {len(u.list_tc_fields(doc))}")
    print(f"guardado: {SALIDA}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
