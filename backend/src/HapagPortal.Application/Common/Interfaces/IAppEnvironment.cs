namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Abstracción del entorno de ejecución para la capa Application, sin acoplarla a hosting.
/// </summary>
public interface IAppEnvironment
{
    bool IsProduction { get; }
}
