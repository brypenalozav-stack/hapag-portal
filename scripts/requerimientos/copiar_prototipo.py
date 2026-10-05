"""Fase 4: copia el prototipo navegable a katu\\v4 como Prototipo_Portal_2.0_v4.html."""
import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import config  # noqa: E402
from linea_base import REPO  # noqa: E402

ORIGEN = os.path.join(REPO, "docs", "prototipo", "portal-2.0-prototipo.html")
NOMBRE_V4 = "Prototipo_Portal_2.0_v4.html"


def main():
    if not os.path.exists(ORIGEN):
        print("ERROR no existe", ORIGEN)
        return 1
    destino = config.ruta_v4(NOMBRE_V4)
    os.makedirs(config.V4, exist_ok=True)
    shutil.copyfile(ORIGEN, destino)
    print(f"copiado: {destino}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
