"""Fase 1: verifica la especificación funcional v4.0. Sale con 1 si algo falla.

Uso: python verificar_spec.py <ruta del .docx v4>

- Fichas: 134 Heading 3; por prefijo M1 27, M2 10, M3 19, M4 4, M5 10, M6 9, M7 4, M8 9, M9 1,
  M10 6, M11 8, NF 27; en RF, Fase 1 72, Fase 2 31 y Fase 0 4.
- Estructura: ningún Heading 2 supera 120 caracteres; "4.2 Registro…" es Heading 2; M8-08 está
  dentro del capítulo de M8.
- Texto: 0 apariciones de "M9-02", "M9-03" y "[Validación pendiente"; 0 términos prohibidos y
  "proveedor" solo en frases permitidas (terminos.py).
- Campos TC: nivel 1 en orden "1." … "17." más "Anexo A"; el texto de cada campo coincide con el
  texto literal de su título; existen los campos de "14.   M11…", "4.2" y "2.4".
- word/settings.xml contiene w:updateFields w:val="true".
"""
import os
import re
import sys
import zipfile
from collections import Counter

import docx

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import docx_utils as u  # noqa: E402
import terminos  # noqa: E402

POR_PREFIJO = {"M1": 27, "M2": 10, "M3": 19, "M4": 4, "M5": 10, "M6": 9, "M7": 4, "M8": 9, "M9": 1,
               "M10": 6, "M11": 8, "NF": 27}
POR_FASE_RF = {"1": 72, "2": 31, "0": 4}
TOTAL_FICHAS = 134
TEXTOS_PROHIBIDOS = ["M9-02", "M9-03", "[Validación pendiente"]
RE_FASE = re.compile(r"FASE\s+(\d)")


def verificar_fichas(doc, errores):
    h3 = [p for p in u.iter_parrafos(doc) if p.style is not None and p.style.name == "Heading 3"]
    if len(h3) != TOTAL_FICHAS:
        errores.append(f"Heading 3: {len(h3)} (se esperaban {TOTAL_FICHAS})")
    fichas = u.fichas(doc)
    prefijos = Counter(f[0].split("-")[0] for f in fichas)
    for pref, n in POR_PREFIJO.items():
        if prefijos.get(pref, 0) != n:
            errores.append(f"fichas {pref}: {prefijos.get(pref, 0)} (se esperaban {n})")
    extra = set(prefijos) - set(POR_PREFIJO)
    if extra:
        errores.append(f"prefijos inesperados: {sorted(extra)}")
    fases = Counter()
    for ficha_id, _, fase in fichas:
        if ficha_id.startswith("NF"):
            continue
        m = RE_FASE.search(fase)
        fases[m.group(1) if m else "?"] += 1
    for fase, n in POR_FASE_RF.items():
        if fases.get(fase, 0) != n:
            errores.append(f"RF de Fase {fase}: {fases.get(fase, 0)} (se esperaban {n})")
    if fases.get("?"):
        errores.append(f"{fases['?']} ficha(s) RF sin fase legible en el encabezado")
    return len(fichas), prefijos, fases


def verificar_estructura(doc, errores):
    capitulo = None
    m8_08 = None
    tiene_42 = False
    for p in u.iter_parrafos(doc):
        estilo = p.style.name if p.style is not None else ""
        t = p.text.strip()
        if estilo == "Heading 2" and len(t) > 120:
            errores.append(f"Heading 2 de {len(t)} caracteres: {t[:60]!r}…")
        if t.startswith("4.2  Registro y administración de la organización"):
            tiene_42 = estilo == "Heading 2"
            if not tiene_42:
                errores.append(f"'4.2 Registro…' tiene estilo {estilo!r}, no Heading 2")
        if estilo == "Heading 1":
            capitulo = t
        if estilo == "Heading 3" and t.startswith("M8-08"):
            m8_08 = capitulo
    if m8_08 is None:
        errores.append("no se encontró la ficha M8-08")
    elif "M8." not in m8_08:
        errores.append(f"M8-08 está dentro del capítulo {m8_08!r}, no de M8")
    return tiene_42


def verificar_texto(doc, errores):
    texto = u.texto_documento(doc)
    for t in TEXTOS_PROHIBIDOS:
        n = texto.count(t)
        if n:
            errores.append(f"{n} aparición(es) de {t!r}")
    hits = terminos.encontrar_prohibidos(texto)
    if hits:
        errores.append(f"{len(hits)} término(s) prohibido(s)")
    for frag in terminos.proveedor_fuera_de_permitidas(texto):
        errores.append(f"'proveedor' fuera de las frases permitidas: …{frag}…")


def verificar_tc(doc, errores):
    campos = u.list_tc_fields(doc)
    nivel1 = [t for t, n in campos if n == 1]
    esperado = [f"{i}." for i in range(1, 18)] + ["Anexo A"]
    obtenido = [re.match(r"^(\d+\.|Anexo A)", t).group(1) if re.match(r"^(\d+\.|Anexo A)", t) else t for t in nivel1]
    if obtenido != esperado:
        errores.append(f"TC nivel 1 fuera de orden: {obtenido}")
    for p in u.iter_parrafos(doc):
        for _, texto, nivel in u._campos_tc(p._p):
            if texto.strip() != p.text.strip():
                errores.append(f"TC {texto!r} no coincide con el título {p.text.strip()!r}")
    textos = [t for t, _ in campos]
    for requerido, nivel in (("14.   M11.", 1), ("4.2  ", 2), ("2.4  ", 2)):
        if not any(t.startswith(requerido) and n == nivel for t, n in campos):
            errores.append(f"falta el campo TC de nivel {nivel} que empieza con {requerido!r}")
    return len(textos), nivel1


def verificar_settings(ruta, errores):
    with zipfile.ZipFile(ruta) as z:
        s = z.read("word/settings.xml").decode("utf-8")
    if '<w:updateFields w:val="true"/>' not in s:
        errores.append('word/settings.xml no contiene <w:updateFields w:val="true"/>')


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if len(sys.argv) != 2:
        print(__doc__)
        return 1
    ruta = sys.argv[1]
    if not os.path.exists(ruta):
        print(f"ERROR no existe {ruta}")
        return 1
    doc = docx.Document(ruta)
    errores = []
    total, prefijos, fases = verificar_fichas(doc, errores)
    verificar_estructura(doc, errores)
    verificar_texto(doc, errores)
    n_tc, nivel1 = verificar_tc(doc, errores)
    verificar_settings(ruta, errores)

    print(f"fichas: {total} ({', '.join(f'{k} {prefijos.get(k, 0)}' for k in POR_PREFIJO)})")
    print(f"RF por fase: Fase 1 {fases.get('1', 0)}, Fase 2 {fases.get('2', 0)}, Fase 0 {fases.get('0', 0)}")
    print(f"campos TC: {n_tc}; capítulos: {len(nivel1)}")
    for e in errores:
        print("ERROR", e)
    if errores:
        print(f"{len(errores)} problema(s)")
        return 1
    print("especificación v4 correcta")
    return 0


if __name__ == "__main__":
    sys.exit(main())
