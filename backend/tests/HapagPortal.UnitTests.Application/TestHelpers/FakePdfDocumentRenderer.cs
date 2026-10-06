namespace HapagPortal.UnitTests.Application.TestHelpers;

using System.Text;
using HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Generador de PDF de pruebas: devuelve un contenido mínimo con el número del documento y guarda los
/// modelos recibidos para verificar la plantilla. El PDF real (MigraDoc) se prueba en Infrastructure.
/// </summary>
public sealed class FakePdfDocumentRenderer : IPdfDocumentRenderer
{
    public List<PdfDocumentModel> Rendered { get; } = [];

    public byte[] Render(PdfDocumentModel document)
    {
        Rendered.Add(document);
        return Encoding.ASCII.GetBytes($"%PDF-1.7\n% {document.DocumentNumber}\n%%EOF\n");
    }
}
