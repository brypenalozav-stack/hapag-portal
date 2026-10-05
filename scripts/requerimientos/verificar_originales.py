"""Comprueba que los originales de katu conservan el SHA-256 de la línea base. Sale con 1 si alguno cambió."""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import config  # noqa: E402
from linea_base import DOCS, sha256  # noqa: E402


def main():
    with open(os.path.join(DOCS, "linea-base-katu.json"), encoding="utf-8") as f:
        base = json.load(f)
    errores = []
    for a in base["archivos"]:
        p = os.path.join(config.KATU, a["nombre"])
        if not os.path.exists(p):
            errores.append(f"falta: {a['nombre']}")
        elif sha256(p) != a["sha256"]:
            errores.append(f"modificado: {a['nombre']}")
    locks = [n for n in os.listdir(config.KATU) if n.startswith(".~lock")]
    errores += [f"archivo de bloqueo en katu: {n}" for n in locks]
    for e in errores:
        print("ERROR", e)
    print("originales intactos" if not errores else f"{len(errores)} problema(s)")
    return 1 if errores else 0


if __name__ == "__main__":
    sys.exit(main())
