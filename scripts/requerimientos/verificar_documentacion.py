"""Fase 7: verifica docs/documentacion.html contra su versión anterior. Sale con 1 si algo falla.

Uso: python verificar_documentacion.py [--antes <ref>]   (por defecto origin/develop)

La versión anterior se lee con `git show <ref>:docs/documentacion.html`. Comprueba:
- 0 apariciones (sin distinguir mayúsculas) de los marcadores del visor BPMN retirado en todo el archivo;
- f-req-portal2, u-idioma-teclado, t-ui-i18n-a11y y t-integraciones existen una vez cada una, dentro del
  panel funcional, usuario, tecnica y tecnica (html.parser);
- f-flujo-carga, f-flujo-aduana y f-flujo-plazos contienen un pre.mermaid cada una, y no quedan f-bpmn-*;
- el número de <pre class="mermaid"> es el de la versión anterior + 4;
- el SHA-256 del <script> de Mermaid es igual al de la versión anterior;
- el tamaño en bytes es menor que el de la versión anterior;
  (tamaño y SHA-256 con finales de línea normalizados a LF, como los guarda git);
- 0 términos prohibidos y 0 usos de "proveedor" fuera de las frases permitidas en <main> (terminos.py).
"""
import argparse
import hashlib
import os
import subprocess
import sys
from html.parser import HTMLParser

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terminos  # noqa: E402

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RUTA_GIT = "docs/documentacion.html"
HTML = os.path.join(REPO, *RUTA_GIT.split("/"))

PROHIBIDOS_BPMN = ["bpmn-js", "bpmn.io", "bpmnjs", "data-bpmn", "application/bpmn+xml", 'class="bpmn"']
SECCIONES_NUEVAS = {
    "f-req-portal2": "funcional",
    "u-idioma-teclado": "usuario",
    "t-ui-i18n-a11y": "tecnica",
    "t-integraciones": "tecnica",
}
FLUJOS = ["f-flujo-carga", "f-flujo-aduana", "f-flujo-plazos"]
PRE_MERMAID = '<pre class="mermaid">'
NUEVOS_PRE_MERMAID = 4  # 3 procesos BPMN reemplazados + el diagrama de t-integraciones
INICIO_MERMAID = '<script>"use strict";var __esbuild_esm_mermaid_nm'


class Estructura(HTMLParser):
    """Registra, por cada <section id>, el panel que la contiene y cuántos pre.mermaid tiene."""

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.divs = []  # panel (data-tab) o None por cada <div> abierto
        self.seccion = None
        self.secciones = []  # (id, panel)
        self.mermaid_por_seccion = {}

    def panel(self):
        for p in reversed(self.divs):
            if p:
                return p
        return None

    def handle_starttag(self, tag, attrs):
        a = dict(attrs)
        clases = (a.get("class") or "").split()
        if tag == "div":
            self.divs.append(a.get("data-tab") if "panel" in clases else None)
        elif tag == "section":
            self.seccion = a.get("id")
            self.secciones.append((self.seccion, self.panel()))
        elif tag == "pre" and "mermaid" in clases and self.seccion:
            self.mermaid_por_seccion[self.seccion] = self.mermaid_por_seccion.get(self.seccion, 0) + 1

    def handle_endtag(self, tag):
        if tag == "div" and self.divs:
            self.divs.pop()
        elif tag == "section":
            self.seccion = None


def mermaid_sha(texto):
    ini = texto.find(INICIO_MERMAID)
    if ini == -1 or texto.count(INICIO_MERMAID) != 1:
        return None
    fin = texto.index("</script>", ini) + len("</script>")
    return hashlib.sha256(texto[ini:fin].encode("utf-8")).hexdigest()


def leer_antes(ref):
    r = subprocess.run(["git", "show", f"{ref}:{RUTA_GIT}"], cwd=REPO, capture_output=True)
    if r.returncode != 0:
        raise SystemExit(f"ERROR git show {ref}:{RUTA_GIT}: {r.stderr.decode('utf-8', 'replace').strip()}")
    return r.stdout


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--antes", default="origin/develop", help="ref git con la versión anterior")
    args = ap.parse_args()

    antes_crudo = leer_antes(args.antes)
    with open(HTML, "rb") as f:
        crudo = f.read()
    # El repositorio guarda el archivo con LF y la copia de trabajo puede tener CRLF (core.autocrlf):
    # tamaño y SHA-256 se comparan con los finales de línea normalizados a LF.
    antes_crudo = antes_crudo.replace(b"\r\n", b"\n")
    crudo = crudo.replace(b"\r\n", b"\n")
    antes = antes_crudo.decode("utf-8-sig")
    texto = crudo.decode("utf-8-sig")
    errores = []

    bajo = texto.lower()
    for m in PROHIBIDOS_BPMN:
        n = bajo.count(m.lower())
        if n:
            errores.append(f"{n} aparición(es) de {m!r}")

    est = Estructura()
    est.feed(texto)
    est.close()
    for id_, panel in SECCIONES_NUEVAS.items():
        encontradas = [p for s, p in est.secciones if s == id_]
        if len(encontradas) != 1:
            errores.append(f"la sección {id_} aparece {len(encontradas)} veces (se espera 1)")
        elif encontradas[0] != panel:
            errores.append(f"la sección {id_} está en el panel {encontradas[0]!r} (se espera {panel!r})")
    for id_ in FLUJOS:
        if [s for s, _ in est.secciones].count(id_) != 1:
            errores.append(f"la sección {id_} no aparece exactamente una vez")
        elif est.mermaid_por_seccion.get(id_, 0) < 1:
            errores.append(f"la sección {id_} no contiene un pre.mermaid")
    viejas = sorted({s for s, _ in est.secciones if s and s.startswith("f-bpmn-")})
    if viejas:
        errores.append(f"quedan secciones con id f-bpmn-*: {viejas}")

    n_antes, n_ahora = antes.count(PRE_MERMAID), texto.count(PRE_MERMAID)
    if n_ahora != n_antes + NUEVOS_PRE_MERMAID:
        errores.append(f"{PRE_MERMAID}: {n_ahora}, se esperaba {n_antes} + {NUEVOS_PRE_MERMAID}")

    sha_antes, sha_ahora = mermaid_sha(antes), mermaid_sha(texto)
    if not sha_antes or sha_antes != sha_ahora:
        errores.append(f"el <script> de Mermaid cambió o no se encontró ({sha_antes} → {sha_ahora})")

    if len(crudo) >= len(antes_crudo):
        errores.append(f"el archivo no es más chico: {len(antes_crudo)} → {len(crudo)} bytes")

    ini, fin = texto.find("<main>"), texto.find("</main>")
    if ini == -1 or fin < ini:
        errores.append("no se encontró la región <main>")
    else:
        main_txt = texto[ini:fin]
        hits = terminos.encontrar_prohibidos(main_txt)
        if hits:
            errores.append(f"{len(hits)} término(s) prohibido(s) en <main>")
        fuera = terminos.proveedor_fuera_de_permitidas(main_txt)
        if fuera:
            errores.append(f"'proveedor' fuera de las frases permitidas en <main>: {fuera}")

    print(f"antes: {args.antes} · {len(antes_crudo)} bytes (LF) · {antes.count(chr(10))} líneas · {n_antes} pre.mermaid")
    print(f"ahora: {len(crudo)} bytes (LF) · {texto.count(chr(10))} líneas · {n_ahora} pre.mermaid")
    print(f"Mermaid SHA-256: {sha_ahora}")
    for e in errores:
        print("ERROR", e)
    if errores:
        print(f"{len(errores)} problema(s)")
        return 1
    print("documentacion.html correcto")
    return 0


if __name__ == "__main__":
    sys.exit(main())
