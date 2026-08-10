using HapagPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace HapagPortal.Infrastructure.Services;

public sealed class AppEnvironment(IHostEnvironment environment) : IAppEnvironment
{
    public bool IsProduction => environment.IsProduction();
}
