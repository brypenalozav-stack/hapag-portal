namespace HapagPortal.Domain.Constants;

/// <summary>Resultado del buscador de mercancías peligrosas (M10-06).</summary>
public static class DangerousGoodResultCodes
{
    /// <summary>La carga tiene clase DG y número ONU.</summary>
    public const string Classified = "CLASSIFIED";

    /// <summary>La carga consultada figura en la base como no clasificada como mercancía peligrosa.</summary>
    public const string NotClassified = "NOT_CLASSIFIED";

    /// <summary>La base de referencia no tiene clasificación para la carga consultada.</summary>
    public const string NoMatch = "NO_MATCH";
}

/// <summary>Origen de una entrada de la base de referencia DG.</summary>
public static class DangerousGoodSources
{
    /// <summary>Muestra representativa sembrada con el portal.</summary>
    public const string Sample = "SAMPLE";

    /// <summary>Cargada por la importación interna (CSV).</summary>
    public const string Import = "IMPORT";
}

/// <summary>
/// Nombre de las clases y divisiones de mercancías peligrosas de las Recomendaciones de la ONU (Reglamentación
/// Modelo), que también usa el Código IMDG. Solo los nombres de clase, sin texto del código.
/// </summary>
public static class DangerousGoodClasses
{
    private static readonly Dictionary<string, (string Es, string En)> Names = new(StringComparer.Ordinal)
    {
        ["1"] = ("Explosivos", "Explosives"),
        ["2.1"] = ("Gases inflamables", "Flammable gases"),
        ["2.2"] = ("Gases no inflamables, no tóxicos", "Non-flammable, non-toxic gases"),
        ["2.3"] = ("Gases tóxicos", "Toxic gases"),
        ["3"] = ("Líquidos inflamables", "Flammable liquids"),
        ["4.1"] = ("Sólidos inflamables", "Flammable solids"),
        ["4.2"] = ("Sustancias que pueden experimentar combustión espontánea", "Substances liable to spontaneous combustion"),
        ["4.3"] = ("Sustancias que en contacto con el agua desprenden gases inflamables", "Substances which, in contact with water, emit flammable gases"),
        ["5.1"] = ("Sustancias comburentes", "Oxidizing substances"),
        ["5.2"] = ("Peróxidos orgánicos", "Organic peroxides"),
        ["6.1"] = ("Sustancias tóxicas", "Toxic substances"),
        ["6.2"] = ("Sustancias infecciosas", "Infectious substances"),
        ["7"] = ("Material radiactivo", "Radioactive material"),
        ["8"] = ("Sustancias corrosivas", "Corrosive substances"),
        ["9"] = ("Sustancias y objetos peligrosos varios", "Miscellaneous dangerous substances and articles"),
    };

    /// <summary>Nombre de la clase o división; para "1.4" usa la clase 1. Nulo si no se reconoce.</summary>
    public static (string Es, string En)? NameOf(string? hazardClass)
    {
        if (string.IsNullOrWhiteSpace(hazardClass))
            return null;

        var code = hazardClass.Trim();
        if (Names.TryGetValue(code, out var name))
            return name;

        var main = code.Split('.')[0];
        return main == "1" && Names.TryGetValue(main, out var explosives) ? explosives : null;
    }

    public static bool IsValid(string? hazardClass) => NameOf(hazardClass) is not null;
}
