"""Configuración compartida de los scripts de requerimientos Portal 2.0 v4."""
import os

KATU = r"C:\Users\klaze\Desktop\katu"
V4 = os.path.join(KATU, "v4")

# Originales de katu (los 6 archivos de la línea base). Nunca se escriben.
ORIGINALES = [
    "Portal_2.0_Especificacion_Funcional_validada.docx",
    "Portal_2.0_Especificacion_Funcional_validada(1).docx",
    "Portal_2_0_Pendientes.xlsx",
    "Funcionalidades NexusV2.xlsx",
    "Gantt_Macros_Extraccion_QA.xlsx",
    "Comparacion_POC_Requerimientos_Por_Fase.xlsx",
]

ESPECIFICACION = os.path.join(KATU, "Portal_2.0_Especificacion_Funcional_validada.docx")

# Original -> versión v4. El Gantt queda fuera del alcance y no tiene _v4.
MAPA_V4 = {
    "Portal_2.0_Especificacion_Funcional_validada.docx": "Portal_2.0_Especificacion_Funcional_v4.docx",
    "Portal_2_0_Pendientes.xlsx": "Portal_2_0_Pendientes_v4.xlsx",
    "Funcionalidades NexusV2.xlsx": "Funcionalidades NexusV2_v4.xlsx",
    "Comparacion_POC_Requerimientos_Por_Fase.xlsx": "Comparacion_Requerimientos_Por_Fase_v4.xlsx",
}

# fase -> (inicio ISO, fin ISO, validadores). Sección "Calendario y responsables" del plan.
CALENDARIO = {
    "0": ("2026-10-06", "2026-10-07", "Katu, Kari"),
    "6a": ("2026-10-08", "2026-10-14", "Lucho (Nexus), Diego (FIS), Fer y Ricardo (pagos)"),
    "6b": ("2026-10-15", "2026-10-20", "Jorge"),
    "6c": ("2026-10-21", "2026-10-27", "Jorge, Diego"),
    "1": ("2026-10-28", "2026-11-02", "Katu, Kari"),
    "2": ("2026-11-03", "2026-11-05", "Lucho, Fer"),
    "3": ("2026-11-06", "2026-11-10", "Katu"),
    "4": ("2026-11-11", "2026-11-12", "Katu, Cami/Mati"),
    "5a": ("2026-11-13", "2026-11-17", "Andrés"),
    "5b": ("2026-11-18", "2026-11-24", "Andrés, Kari (glosario EN)"),
    "5c": ("2026-11-25", "2026-11-30", "Andrés"),
    "7": ("2026-12-01", "2026-12-02", "Jorge"),
}

EJECUTA = "Equipo de desarrollo Portal 2.0"


def ruta_v4(nombre):
    """Ruta de salida dentro de katu\\v4, validada."""
    p = os.path.join(V4, nombre)
    assert_not_original(p)
    return p


def assert_not_original(path):
    """Aborta cualquier escritura dentro de KATU que no sea un archivo _v4 dentro de V4."""
    ap = os.path.normcase(os.path.abspath(path))
    katu = os.path.normcase(os.path.abspath(KATU))
    v4 = os.path.normcase(os.path.abspath(V4))
    if ap.startswith(katu + os.sep) or ap == katu:
        nombre = os.path.basename(ap)
        dentro_v4 = ap.startswith(v4 + os.sep)
        if not dentro_v4 or "_v4" not in nombre:
            raise RuntimeError(f"Escritura no permitida sobre katu fuera de v4/_v4: {path}")
