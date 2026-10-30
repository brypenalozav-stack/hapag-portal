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
/// Modelo), que también usa el Código IMDG. Solo los nombres de clase, sin texto del código. En portugués, los
/// nombres de la Resolução ANTT nº 5.998/2022.
/// </summary>
public static class DangerousGoodClasses
{
    private static readonly Dictionary<string, (string Es, string En, string Pt)> Names = new(StringComparer.Ordinal)
    {
        ["1"] = ("Explosivos", "Explosives", "Explosivos"),
        ["2.1"] = ("Gases inflamables", "Flammable gases", "Gases inflamáveis"),
        ["2.2"] = ("Gases no inflamables, no tóxicos", "Non-flammable, non-toxic gases", "Gases não inflamáveis, não tóxicos"),
        ["2.3"] = ("Gases tóxicos", "Toxic gases", "Gases tóxicos"),
        ["3"] = ("Líquidos inflamables", "Flammable liquids", "Líquidos inflamáveis"),
        ["4.1"] = ("Sólidos inflamables", "Flammable solids", "Sólidos inflamáveis"),
        ["4.2"] = ("Sustancias que pueden experimentar combustión espontánea", "Substances liable to spontaneous combustion", "Substâncias sujeitas à combustão espontânea"),
        ["4.3"] = ("Sustancias que en contacto con el agua desprenden gases inflamables", "Substances which, in contact with water, emit flammable gases", "Substâncias que, em contato com água, emitem gases inflamáveis"),
        ["5.1"] = ("Sustancias comburentes", "Oxidizing substances", "Substâncias oxidantes"),
        ["5.2"] = ("Peróxidos orgánicos", "Organic peroxides", "Peróxidos orgânicos"),
        ["6.1"] = ("Sustancias tóxicas", "Toxic substances", "Substâncias tóxicas"),
        ["6.2"] = ("Sustancias infecciosas", "Infectious substances", "Substâncias infectantes"),
        ["7"] = ("Material radiactivo", "Radioactive material", "Material radioativo"),
        ["8"] = ("Sustancias corrosivas", "Corrosive substances", "Substâncias corrosivas"),
        ["9"] = ("Sustancias y objetos peligrosos varios", "Miscellaneous dangerous substances and articles", "Substâncias e artigos perigosos diversos"),
    };

    /// <summary>Nombre de la clase o división; para "1.4" usa la clase 1. Nulo si no se reconoce.</summary>
    public static (string Es, string En, string Pt)? NameOf(string? hazardClass)
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
