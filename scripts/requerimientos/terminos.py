"""Detección de términos prohibidos sin guardarlos en claro en el repositorio.

`terminos_prohibidos.sha256` contiene un hash SHA-256 por línea, calculado sobre el
término normalizado (minúsculas, sin tildes, tokens [a-z0-9] unidos por un espacio).
El texto a revisar se tokeniza igual y se comparan los hashes de sus n-gramas de 1 a 3.

Generar el archivo de hashes desde una lista local en claro (fuera del repo):
    python terminos.py --generar <lista_en_claro.txt>
"""
import hashlib
import os
import re
import sys
import unicodedata

AQUI = os.path.dirname(os.path.abspath(__file__))
ARCHIVO_HASHES = os.path.join(AQUI, "terminos_prohibidos.sha256")
ARCHIVO_PERMITIDAS = os.path.join(AQUI, "proveedor_permitido.txt")
MAX_N = 3


def normalizar(texto):
    sin_tildes = "".join(c for c in unicodedata.normalize("NFD", texto) if unicodedata.category(c) != "Mn")
    return re.findall(r"[a-z0-9]+", sin_tildes.lower())


def hash_termino(termino):
    return hashlib.sha256(" ".join(normalizar(termino)).encode("utf-8")).hexdigest()


def _hashes():
    with open(ARCHIVO_HASHES, encoding="utf-8") as f:
        return {l.strip() for l in f if l.strip() and not l.startswith("#")}


def encontrar_prohibidos(texto):
    """Devuelve la lista de hashes prohibidos presentes en el texto."""
    prohibidos = _hashes()
    tokens = normalizar(texto)
    encontrados = set()
    for n in range(1, MAX_N + 1):
        for i in range(len(tokens) - n + 1):
            h = hashlib.sha256(" ".join(tokens[i:i + n]).encode("utf-8")).hexdigest()
            if h in prohibidos:
                encontrados.add(h)
    return sorted(encontrados)


def _permitidas():
    with open(ARCHIVO_PERMITIDAS, encoding="utf-8") as f:
        return [" ".join(normalizar(l)) for l in f if l.strip() and not l.startswith("#")]


def proveedor_fuera_de_permitidas(texto):
    """Devuelve los fragmentos donde aparece 'proveedor' fuera de las frases permitidas."""
    tokens = normalizar(texto)
    unido = " ".join(tokens)
    permitidas = _permitidas()
    for frase in permitidas:
        unido = unido.replace(frase, " ")
    hallazgos = []
    toks = unido.split()
    for i, t in enumerate(toks):
        if t.startswith("proveedor"):
            hallazgos.append(" ".join(toks[max(0, i - 4):i + 5]))
    return hallazgos


if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == "--generar":
        with open(sys.argv[2], encoding="utf-8") as f:
            terminos = [l.strip() for l in f if l.strip()]
        with open(ARCHIVO_HASHES, "w", encoding="utf-8", newline="\n") as out:
            out.write("# SHA-256 de términos prohibidos normalizados (ver terminos.py). No contiene texto en claro.\n")
            for t in terminos:
                if len(normalizar(t)) > MAX_N:
                    raise SystemExit(f"Término con más de {MAX_N} tokens no detectable: {len(normalizar(t))} tokens")
                out.write(hash_termino(t) + "\n")
        print(f"{len(terminos)} hashes escritos en {ARCHIVO_HASHES}")
    elif len(sys.argv) == 2:
        with open(sys.argv[1], encoding="utf-8", errors="ignore") as f:
            txt = f.read()
        hits = encontrar_prohibidos(txt)
        print(f"prohibidos: {len(hits)}")
        sys.exit(1 if hits else 0)
    else:
        print(__doc__)
