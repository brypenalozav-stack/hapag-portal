using System.Text;
using System.Text.RegularExpressions;
using HapagPortal.IntegrationSimulator;

// Simulador HTTP de los sistemas externos (Fase 6c). Implementa CT-NEXUS, CT-FIS, CT-KHIPU (API v3),
// CT-DBNET y CT-TRACK bajo /nexus, /fis, /khipu, /dbnet y /tracking, con los mismos
// escenarios de datos que los adaptadores Dummy y escenarios de falla por la cabecera X-Sim-Scenario
// (error500, timeout, lento, 429). /contracts/{nombre} sirve el YAML del contrato.
//
//   dotnet run --project backend/tools/HapagPortal.IntegrationSimulator --urls http://localhost:5199

var builder = WebApplication.CreateBuilder(args);

// Las pruebas de contrato pueden enviar cabeceras Latin-1; se aceptan en vez de cortar la conexión.
builder.WebHost.ConfigureKestrel(options => options.RequestHeaderEncodingSelector = _ => Encoding.Latin1);

var app = builder.Build();

var contractsPath = ResolveContractsPath(app.Configuration["Simulator:ContractsPath"]);
app.Logger.LogInformation("Contratos servidos desde {ContractsPath}", contractsPath ?? "(no encontrado)");

app.Use(SimulatorHttp.ApplyScenarioAsync);

app.MapGet("/contracts/{name}", (HttpContext context, string name) =>
{
    if (contractsPath is null || !Regex.IsMatch(name, "^[a-z0-9-]+\\z"))
        return SimulatorHttp.NotFound(context);

    var file = Path.Combine(contractsPath, $"{name}.openapi.yaml");
    return File.Exists(file)
        ? Results.File(file, "application/yaml")
        : SimulatorHttp.NotFound(context);
});

app.MapNexus();
app.MapFis();
app.MapKhipu();
app.MapDbNet();
app.MapTracking();

app.MapFallback("{**path}", (HttpContext context) => SimulatorHttp.NotFound(context));

app.Run();

// Simulator:ContractsPath (o Simulator__ContractsPath) explícito; si no, se sube desde
// AppContext.BaseDirectory hasta la raíz del repositorio y se baja a docs/integraciones/contratos.
static string? ResolveContractsPath(string? configured)
{
    if (!string.IsNullOrWhiteSpace(configured))
        return Path.GetFullPath(configured);

    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
        var candidate = Path.Combine(dir.FullName, "docs", "integraciones", "contratos");
        if (Directory.Exists(candidate))
            return candidate;
    }

    return null;
}

public partial class Program;
