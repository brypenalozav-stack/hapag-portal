namespace HapagPortal.UnitTests.Infrastructure.Documents;

using FluentAssertions;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Certificado de flete (M6-02): la primera entrega de Fase 2 es sin pago ni carro. El modo pagado en BOB queda reservado
/// y, mientras no exista su flujo de cobro validado, configurarlo detiene el arranque en vez de cobrar a medias.
/// </summary>
public sealed class FreightCertificateModeRegistrationTests
{
    private static Action Register(string? mode) => () =>
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly1234567890!",
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=none"
        };
        if (mode is not null)
            settings["Documents:FreightCertificateMode"] = mode;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureServices(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<DocumentSettings>().FreightCertificateMode.Should().Be(FreightCertificateModes.Free);
    };

    [Theory]
    [InlineData(null)]
    [InlineData(FreightCertificateModes.Free)]
    public void FreeMode_ShouldBeTheOnlyAvailableMode(string? mode) =>
        Register(mode).Should().NotThrow();

    [Theory]
    [InlineData(FreightCertificateModes.Paid)]
    [InlineData("Unknown")]
    public void PaidOrUnknownMode_ShouldStopTheStartup(string mode) =>
        Register(mode).Should().Throw<InvalidOperationException>().WithMessage("*FreightCertificateMode*");
}
