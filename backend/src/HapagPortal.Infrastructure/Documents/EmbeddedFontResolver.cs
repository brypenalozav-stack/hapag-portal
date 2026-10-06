using System.Collections.Concurrent;
using PdfSharp.Fonts;

namespace HapagPortal.Infrastructure.Documents;

/// <summary>
/// Resuelve todas las familias de los documentos a Liberation Sans (OFL-1.1), incrustada en el ensamblado,
/// para que la generación sea idéntica en Windows y en el contenedor Linux sin fuentes del sistema.
/// </summary>
public sealed class EmbeddedFontResolver : IFontResolver
{
    public const string FamilyName = "Liberation Sans";

    private const string ResourcePrefix = "HapagPortal.Infrastructure.Documents.Fonts.";
    private const string Regular = "LiberationSans-Regular";
    private const string Bold = "LiberationSans-Bold";
    private const string Italic = "LiberationSans-Italic";
    private const string BoldItalic = "LiberationSans-BoldItalic";

    private readonly ConcurrentDictionary<string, byte[]> _faces = new(StringComparer.Ordinal);

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
        new((bold, italic) switch
        {
            (true, true) => BoldItalic,
            (true, false) => Bold,
            (false, true) => Italic,
            _ => Regular
        });

    public byte[]? GetFont(string faceName) =>
        _faces.GetOrAdd(faceName, static name =>
        {
            using var stream = typeof(EmbeddedFontResolver).Assembly.GetManifestResourceStream($"{ResourcePrefix}{name}.ttf")
                ?? throw new InvalidOperationException($"The embedded font '{name}' was not found.");
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        });
}
