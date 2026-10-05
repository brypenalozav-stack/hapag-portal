"""Fase 4: verifica el prototipo navegable. Sale con 1 si algo falla.

- Existen docs/prototipo/portal-2.0-prototipo.html y la copia katu\\v4\\Prototipo_Portal_2.0_v4.html, idénticas.
- Marcas obligatorias: skip-link, landmarks, :focus-visible, prefers-reduced-motion, tema según
  prefers-color-scheme con hl_theme, idioma con hl_lang, aria-live, estados NF-11, 4 pantallas, 4 monedas, Intl.
- Tokens: las 15 variables de la lista cerrada (Fase 5a §2) con sus valores; ninguna otra --hl-*; ningún hex
  fuera de los valores de los tokens.
- Solo hosts externos permitidos (cdn.jsdelivr.net/npm/, fonts.googleapis.com, fonts.gstatic.com).
- localStorage solo dentro de try.
- Diccionario es/en: mismas claves, sin valores vacíos, mismos parámetros; toda clave usada existe.
- 0 términos prohibidos (terminos.py).
"""
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import config  # noqa: E402
import terminos  # noqa: E402
from linea_base import REPO  # noqa: E402
from verificar_guia import FOCO_FASE_5A, tokens_esperados  # noqa: E402

HTML = os.path.join(REPO, "docs", "prototipo", "portal-2.0-prototipo.html")
COPIA = os.path.join(config.V4, "Prototipo_Portal_2.0_v4.html")

HOSTS_PERMITIDOS = {"cdn.jsdelivr.net", "fonts.googleapis.com", "fonts.gstatic.com"}

MARCAS = [
    'class="hl-skip-link" href="#contenido-principal"',
    "<header",
    "<nav",
    '<main id="contenido-principal"',
    "<footer",
    ":focus-visible",
    "prefers-reduced-motion",
    "prefers-color-scheme",
    "data-bs-theme",
    "'hl_lang'",
    "'hl_theme'",
    'id="hl-theme"',
    "data-lang=\"es\"",
    "data-lang=\"en\"",
    "aria-pressed",
    "root.lang = nuevo",
    'aria-live="polite"',
    'role="status"',
    'role="alert"',
    "'state.retry'",
    'data-screen="dashboard"',
    'data-screen="embarques"',
    'data-screen="detalle-bl"',
    'data-screen="carro"',
    "#/dashboard",
    "#/embarques",
    "#/bl/",
    "#/carro",
    'role="tablist"',
    "MONEDAS = ['CLP', 'USD', 'BOB', 'EUR']",
    "Intl.NumberFormat",
    "Intl.DateTimeFormat",
    "currencyDisplay: 'code'",
    "moneda === 'CLP' ? 0 : 2",
    "America/Santiago",
    "hourCycle: 'h23'",
    'scope="col"',
    "<caption",
    'role="search"',
    "cdn.jsdelivr.net/npm/bootstrap@5.3.",
    "cdn.jsdelivr.net/npm/bootstrap-icons@",
    "family=Inter",
    "family=Montserrat",
]

RE_URL = re.compile(r"(?:https?:)?//([A-Za-z0-9.-]+\.[A-Za-z]{2,})(/[^\s\"'<>)]*)?")
RE_HEX = re.compile(r"#[0-9a-fA-F]{3,8}\b")
RE_VAR_HL = re.compile(r"(--hl-[A-Za-z0-9-]+)\s*:\s*([^;]+);")
RE_USO_T = re.compile(r"\b(?:t|tHtml)\('([\w.]+)'\s*[,)]")
RE_USO_TP = re.compile(r"\btp\('([\w.]+)'\s*,")
RE_PARAM = re.compile(r"\{\{(\w+)\}\}")


def verificar_marcas(html, errores):
    for m in MARCAS:
        if m not in html:
            errores.append(f"falta la marca obligatoria: {m}")


def verificar_tokens(html, errores):
    esperados, fuente = tokens_esperados()
    esperados = {"--" + k.lstrip("$"): v.lower() for k, v in esperados.items()}
    esperados[FOCO_FASE_5A[0]] = FOCO_FASE_5A[1]
    bloque = re.search(r':root,\s*\[data-bs-theme="light"\]\s*\{(.*?)\}', html, re.S)
    if not bloque:
        errores.append("no se encontró el bloque de tokens ':root, [data-bs-theme=\"light\"]'")
        return fuente
    declarados = {k: v.strip().lower() for k, v in RE_VAR_HL.findall(bloque.group(1))}
    for nombre, valor in esperados.items():
        if declarados.get(nombre) != valor:
            errores.append(f"token {nombre}: se esperaba {valor}, hay {declarados.get(nombre)}")
    usados = set(re.findall(r"--hl-[A-Za-z0-9-]+", html))
    extra = sorted(usados - set(esperados))
    if extra:
        errores.append(f"variables --hl-* fuera de la lista cerrada: {extra}")
    valores = set(esperados.values())
    sueltos = sorted({h.lower() for h in RE_HEX.findall(html)} - valores)
    if sueltos:
        errores.append(f"hex fuera de los tokens: {sueltos}")
    return fuente


def verificar_hosts(html, errores):
    hosts = set()
    for host, ruta in RE_URL.findall(html):
        hosts.add(host)
        if host not in HOSTS_PERMITIDOS:
            errores.append(f"host externo no permitido: {host}{ruta}")
        elif host == "cdn.jsdelivr.net" and not ruta.startswith("/npm/"):
            errores.append(f"ruta de cdn.jsdelivr.net fuera de /npm/: {ruta}")
    return hosts


def verificar_almacenamiento(html, errores):
    for m in re.finditer(r"localStorage\.", html):
        # El try más cercano hacia atrás debe estar abierto (sin un catch posterior antes del uso).
        previo = html[max(0, m.start() - 400):m.start()]
        if previo.rfind("try") == -1 or previo.rfind("try") < previo.rfind("catch"):
            errores.append(f"localStorage fuera de try en la posición {m.start()}")


def verificar_diccionario(html, errores):
    m = re.search(r'<script type="application/json" id="hl-i18n">(.*?)</script>', html, re.S)
    if not m:
        errores.append("no se encontró el diccionario <script id=\"hl-i18n\">")
        return 0
    dic = json.loads(m.group(1))
    if set(dic) != {"es", "en"}:
        errores.append(f"el diccionario debe tener exactamente es y en: {sorted(dic)}")
        return 0
    es, en = dic["es"], dic["en"]
    for k in sorted(set(es) - set(en)):
        errores.append(f"clave solo en es: {k}")
    for k in sorted(set(en) - set(es)):
        errores.append(f"clave solo en en: {k}")
    for idioma, d in dic.items():
        for k, v in d.items():
            if not isinstance(v, str) or not v.strip():
                errores.append(f"valor vacío en {idioma}: {k}")
    for k in set(es) & set(en):
        if set(RE_PARAM.findall(es[k])) != set(RE_PARAM.findall(en[k])):
            errores.append(f"parámetros distintos entre es y en: {k}")
    usadas = set(re.findall(r'data-i18n="([\w.]+)"', html))
    for attr in re.findall(r'data-i18n-attr="([^"]+)"', html):
        usadas |= {par.split("=", 1)[1].strip() for par in attr.split(";") if "=" in par}
    usadas |= set(RE_USO_T.findall(html))
    for base in RE_USO_TP.findall(html):
        usadas |= {base + ".one", base + ".other"}
    for k in sorted(usadas - set(es)):
        errores.append(f"clave usada que no existe en el diccionario: {k}")
    return len(es)


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    errores = []
    for p in (HTML, COPIA):
        if not os.path.exists(p):
            errores.append(f"no existe {p}")
    if errores:
        for e in errores:
            print("ERROR", e)
        return 1
    with open(HTML, "rb") as f:
        crudo = f.read()
    with open(COPIA, "rb") as f:
        if f.read() != crudo:
            errores.append(f"la copia {COPIA} no es idéntica a {HTML} (ejecutar copiar_prototipo.py)")
    html = crudo.decode("utf-8")

    verificar_marcas(html, errores)
    fuente = verificar_tokens(html, errores)
    hosts = verificar_hosts(html, errores)
    verificar_almacenamiento(html, errores)
    n_claves = verificar_diccionario(html, errores)
    hits = terminos.encontrar_prohibidos(html)
    if hits:
        errores.append(f"{len(hits)} término(s) prohibido(s)")
    fuera = terminos.proveedor_fuera_de_permitidas(html)
    if fuera:
        errores.append(f"'proveedor' fuera de las frases permitidas: {fuera}")

    print(f"tokens comparados con: {fuente}")
    print(f"marcas: {len(MARCAS)}; claves es/en: {n_claves}; hosts externos: {sorted(hosts)}")
    for e in errores:
        print("ERROR", e)
    if errores:
        print(f"{len(errores)} problema(s)")
        return 1
    print("prototipo correcto")
    return 0


if __name__ == "__main__":
    sys.exit(main())
