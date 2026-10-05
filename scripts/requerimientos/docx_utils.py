"""Utilidades de edición de la especificación (.docx) con python-docx.

El índice de la especificación es un campo `TOC \\f P \\h \\l "1-2"` que se arma con
campos TC (`TC "<texto>" \\f P \\l <nivel>`), no con los estilos de título. Por eso
toda inserción o renumeración de títulos debe mantener sus campos TC.
El .docx solo se guarda con python-docx; LibreOffice nunca lo reescribe.
"""
import copy
import os
import re
import shutil
import tempfile
import zipfile

from docx.oxml.ns import qn
from docx.oxml import OxmlElement
from docx.table import Table
from docx.text.paragraph import Paragraph

RE_TC = re.compile(r'^\s*TC\s+"(.*)"\s+\\f\s+P\s+\\l\s+(\d+)\s*$', re.S)
RE_FICHA = re.compile(r"^((?:M\d+|NF)-\d+)\s+")


# ---------------------------------------------------------------- recorrido

def iter_bloques(doc):
    """Párrafos y tablas del cuerpo, en orden de documento."""
    for el in doc.element.body.iterchildren():
        if el.tag == qn("w:p"):
            yield Paragraph(el, doc)
        elif el.tag == qn("w:tbl"):
            yield Table(el, doc)


def iter_parrafos(doc):
    for b in iter_bloques(doc):
        if isinstance(b, Paragraph):
            yield b


# ---------------------------------------------------------------- campos TC

def _campos_tc(p_el):
    """Campos TC de un párrafo: lista de (instrText_elements, texto, nivel)."""
    campos = []
    dentro = False
    instr = []
    for el in p_el.iter():
        if el.tag == qn("w:fldChar"):
            tipo = el.get(qn("w:fldCharType"))
            if tipo == "begin":
                dentro, instr = True, []
            elif tipo == "end" and dentro:
                dentro = False
                m = RE_TC.match("".join(i.text or "" for i in instr))
                if m:
                    campos.append((instr, m.group(1), int(m.group(2))))
        elif el.tag == qn("w:instrText") and dentro:
            instr.append(el)
    return campos


def list_tc_fields(doc):
    """[(texto, nivel)] de todos los campos TC del documento, en orden."""
    out = []
    for p in iter_parrafos(doc):
        for _, texto, nivel in _campos_tc(p._p):
            out.append((texto, nivel))
    return out


def update_tc_fields(doc, old, new):
    """Reemplaza el texto del campo TC cuyo texto es exactamente `old`. Devuelve cuántos cambió."""
    n = 0
    for p in iter_parrafos(doc):
        for instr, texto, nivel in _campos_tc(p._p):
            if texto == old:
                instr[0].text = f' TC "{new}" \\f P \\l {nivel} '
                instr[0].set(qn("xml:space"), "preserve")
                for extra in instr[1:]:
                    extra.text = ""
                n += 1
    return n


def _run(hijo):
    r = OxmlElement("w:r")
    r.append(hijo)
    return r


def insert_tc_field(paragraph, text, level):
    """Inserta al inicio del párrafo un campo TC con la misma estructura que los existentes."""
    begin = OxmlElement("w:fldChar")
    begin.set(qn("w:fldCharType"), "begin")
    instr = OxmlElement("w:instrText")
    instr.set(qn("xml:space"), "preserve")
    instr.text = f' TC "{text}" \\f P \\l {level} '
    end = OxmlElement("w:fldChar")
    end.set(qn("w:fldCharType"), "end")
    p = paragraph._p
    pos = 1 if (len(p) and p[0].tag == qn("w:pPr")) else 0
    for i, el in enumerate([_run(begin), _run(instr), _run(end)]):
        p.insert(pos + i, el)


def remove_tc_fields(paragraph):
    """Elimina los campos TC de un párrafo (runs entre begin y end inclusive). Devuelve cuántos."""
    p = paragraph._p
    n = 0
    for instr, _, _ in _campos_tc(p):
        primer_run = instr[0].getparent()
        # retroceder hasta el run con fldChar begin y avanzar hasta el end
        runs = list(p.iterchildren(qn("w:r")))
        i = runs.index(primer_run)
        while i > 0 and runs[i].find(qn("w:fldChar")) is None:
            i -= 1
        j = i
        while j < len(runs):
            fc = runs[j].find(qn("w:fldChar"))
            if fc is not None and fc.get(qn("w:fldCharType")) == "end":
                break
            j += 1
        for r in runs[i:j + 1]:
            p.remove(r)
        n += 1
    return n


def set_update_fields_on_open(docx_path):
    """Agrega <w:updateFields w:val="true"/> a word/settings.xml, copiando el resto del paquete tal cual."""
    tmp_fd, tmp = tempfile.mkstemp(suffix=".docx")
    os.close(tmp_fd)
    with zipfile.ZipFile(docx_path) as zin, zipfile.ZipFile(tmp, "w", zipfile.ZIP_DEFLATED) as zout:
        for item in zin.infolist():
            data = zin.read(item.filename)
            if item.filename == "word/settings.xml":
                s = data.decode("utf-8")
                if "w:updateFields" not in s:
                    s = re.sub(r"(<w:settings[^>]*>)", r'\1<w:updateFields w:val="true"/>', s, count=1)
                data = s.encode("utf-8")
            zout.writestr(item, data)
    shutil.move(tmp, docx_path)


# ---------------------------------------------------------------- fichas y edición

def find_ficha(doc, ficha_id):
    """Párrafo Heading 3 de la ficha (por ejemplo 'M1-11')."""
    for p in iter_parrafos(doc):
        if p.style is not None and p.style.name == "Heading 3":
            m = RE_FICHA.match(p.text.strip())
            if m and m.group(1) == ficha_id:
                return p
    return None


def fichas(doc):
    """[(id, titulo, fase_encabezado)] de todas las fichas, en orden."""
    out = []
    for p in iter_parrafos(doc):
        if p.style is not None and p.style.name == "Heading 3":
            t = p.text.strip()
            m = RE_FICHA.match(t)
            if m:
                resto = t[m.end():].strip()
                titulo, _, fase = resto.rpartition("–")
                out.append((m.group(1), titulo.strip(), fase.strip()))
    return out


def set_style(paragraph, style_name):
    paragraph.style = paragraph.part.document.styles[style_name]


def replace_in_runs(paragraph, old, new):
    """Reemplaza texto conservando el formato del primer run involucrado. Devuelve True si cambió."""
    runs = paragraph.runs
    full = "".join(r.text for r in runs)
    i = full.find(old)
    if i < 0:
        return False
    pos = 0
    first = None
    for r in runs:
        start, end = pos, pos + len(r.text)
        if end > i and start < i + len(old):
            a = max(i - start, 0)
            b = min(i + len(old) - start, len(r.text))
            if first is None:
                r.text = r.text[:a] + new + r.text[b:]
                first = r
            else:
                r.text = r.text[:a] + r.text[b:]
        pos = end
    return True


def move_block(start_el, end_el, before_el):
    """Mueve los elementos del cuerpo desde start_el hasta end_el (inclusive) antes de before_el."""
    bloque = []
    el = start_el
    while el is not None:
        bloque.append(el)
        if el is end_el:
            break
        el = el.getnext()
    for el in bloque:
        before_el.addprevious(el)


def insert_ficha_after(anchor_el, ficha_id, titulo, fase, secciones, plantilla_doc):
    """Inserta una ficha nueva después de anchor_el copiando el formato de una ficha existente.

    secciones: lista de (rotulo, [parrafos]) con rotulo en SITUACIÓN ACTUAL / REQUERIMIENTO /
    CRITERIOS DE ACEPTACIÓN / DEPENDENCIAS. Las viñetas de criterios usan 'List Paragraph'.
    Devuelve el último elemento insertado.
    """
    modelo = next(p for p in iter_parrafos(plantilla_doc) if p.style.name == "Heading 3")
    h = copy.deepcopy(modelo._p)
    for t in h.iter(qn("w:t")):
        t.text = ""
    primer_t = next(h.iter(qn("w:t")))
    primer_t.text = f"{ficha_id}     {titulo} – {fase}"
    anchor_el.addnext(h)
    ultimo = h
    styles = plantilla_doc.styles
    for rotulo, parrafos in secciones:
        for texto, estilo in [(rotulo, "Normal")] + [(x, "List Paragraph" if rotulo.startswith("CRITERIOS") else "Normal") for x in parrafos]:
            p = OxmlElement("w:p")
            ultimo.addnext(p)
            par = Paragraph(p, plantilla_doc)
            par.style = styles[estilo]
            par.add_run(texto)
            ultimo = p
    return ultimo
