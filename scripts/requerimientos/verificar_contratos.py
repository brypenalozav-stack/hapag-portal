"""Fase 6a: verifica el inventario y los contratos de docs/integraciones. Sale con 1 si algo falla.

- Cada contrato OpenAPI declara `openapi: 3.1.0`, `info.x-estado` es "PROPUESTA – …" o
  "Validado (<dd-mm-aaaa>, <responsable>)", la misma frase abre `info.description`, e `info.x-responsable`
  no está vacío. Los contratos en Markdown declaran las filas "Estado" y "Responsable" con la misma regla.
- El README cubre todos los sistemas del inventario y cita cada contrato.
- 0 términos prohibidos y 0 usos de "proveedor" fuera de las frases permitidas (terminos.py).
"""
import glob
import os
import re
import sys

import yaml

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terminos  # noqa: E402

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
INTEGRACIONES = os.path.join(REPO, "docs", "integraciones")
CONTRATOS = os.path.join(INTEGRACIONES, "contratos")
README = os.path.join(INTEGRACIONES, "README.md")

SISTEMAS = [
    "Nexus", "FIS/Data Lake", "Navesoft", "Khipu", "Banco de Chile", "Santander", "BCI", "Depósito",
    "DBNet/SII", "Mercurio", "TATC/Flagare", "Tracking", "Dispute", "Firma", "Storage", "Correo", "IA",
]
CONTRATOS_MD = ["storage.md", "navesoft.md", "deposito.md", "dispute.md"]

RE_ESTADO = re.compile(r"^(PROPUESTA – pendiente de validación con \S.*|Validado \(\d{2}-\d{2}-\d{4}, [^)]+\))$")


def verificar_openapi(ruta, errores):
    nombre = os.path.basename(ruta)
    with open(ruta, encoding="utf-8") as f:
        doc = yaml.safe_load(f)
    if str(doc.get("openapi")) != "3.1.0":
        errores.append(f"{nombre}: openapi debe ser 3.1.0 (es {doc.get('openapi')!r})")
    info = doc.get("info") or {}
    estado = str(info.get("x-estado") or "").strip()
    if not RE_ESTADO.match(estado):
        errores.append(f"{nombre}: info.x-estado inválido: {estado!r}")
    elif not str(info.get("description") or "").startswith(estado):
        errores.append(f"{nombre}: info.description no empieza con la frase de x-estado")
    if not str(info.get("x-responsable") or "").strip():
        errores.append(f"{nombre}: info.x-responsable vacío")
    return estado


def fila_tabla(texto, campo):
    m = re.search(rf"^\|\s*{re.escape(campo)}\s*\|\s*(.+?)\s*\|\s*$", texto, re.MULTILINE)
    return m.group(1).strip() if m else ""


def verificar_md(ruta, errores):
    nombre = os.path.basename(ruta)
    if not os.path.exists(ruta):
        errores.append(f"falta el contrato {nombre}")
        return ""
    with open(ruta, encoding="utf-8") as f:
        texto = f.read()
    estado = fila_tabla(texto, "Estado")
    if not RE_ESTADO.match(estado):
        errores.append(f"{nombre}: fila Estado inválida: {estado!r}")
    if not fila_tabla(texto, "Responsable"):
        errores.append(f"{nombre}: fila Responsable vacía")
    return estado


def verificar_readme(archivos_contrato, errores):
    if not os.path.exists(README):
        errores.append("falta docs/integraciones/README.md")
        return
    with open(README, encoding="utf-8") as f:
        texto = f.read()
    primeras = set()
    for linea in texto.splitlines():
        if linea.startswith("|") and not linea.startswith("|---"):
            celdas = [c.strip() for c in linea.strip().strip("|").split("|")]
            if celdas:
                primeras.add(celdas[0].replace("*", "").lower())
    for s in SISTEMAS:
        if s.lower() not in primeras:
            errores.append(f"README: falta la fila del sistema {s!r} en el inventario")
    for a in archivos_contrato:
        if a not in texto:
            errores.append(f"README: no cita el contrato {a}")


def verificar_terminos(errores):
    archivos = [p for p in glob.glob(os.path.join(INTEGRACIONES, "**", "*"), recursive=True) if os.path.isfile(p)]
    for p in archivos:
        with open(p, encoding="utf-8", errors="ignore") as f:
            texto = f.read()
        rel = os.path.relpath(p, REPO)
        hits = terminos.encontrar_prohibidos(texto)
        if hits:
            errores.append(f"{rel}: {len(hits)} término(s) prohibido(s)")
        for frag in terminos.proveedor_fuera_de_permitidas(texto):
            errores.append(f"{rel}: 'proveedor' fuera de las frases permitidas: …{frag}…")
    return len(archivos)


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    errores = []
    yamls = sorted(glob.glob(os.path.join(CONTRATOS, "*.openapi.yaml")))
    if not yamls:
        errores.append("no hay contratos *.openapi.yaml en docs/integraciones/contratos")
    estados = {}
    for y in yamls:
        estados[os.path.basename(y)] = verificar_openapi(y, errores)
    for m in CONTRATOS_MD:
        estados[m] = verificar_md(os.path.join(INTEGRACIONES, m), errores)
    verificar_readme(list(estados), errores)
    revisados = verificar_terminos(errores)

    for nombre, estado in estados.items():
        print(f"{nombre}: {estado.split(' – ')[0] if estado else '(sin estado)'}")
    for e in errores:
        print("ERROR", e)
    if errores:
        print(f"{len(errores)} problema(s)")
        return 1
    print(f"contratos correctos: {len(estados)} contratos, {len(SISTEMAS)} sistemas, {revisados} archivos sin términos prohibidos")
    return 0


if __name__ == "__main__":
    sys.exit(main())
