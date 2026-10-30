namespace HapagPortal.Application.Common.Helpers;

/// <summary>Idiomas de las planillas exportables (es por defecto).</summary>
public static class ExportLanguages
{
    public const string Es = "es", En = "en", Pt = "pt";
    public const string ValidationMessage = "Language must be es, en or pt.";

    public static bool IsValid(string? language) => language is null or Es or En or Pt;

    public static string Of(string? language) => language is En or Pt ? language : Es;
}
