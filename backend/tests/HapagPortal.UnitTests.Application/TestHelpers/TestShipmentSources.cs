namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Shipments.Issuance;
using HapagPortal.Domain.Results;
using NSubstitute;

/// <summary>Origen de embarques (CT-FIS) para pruebas: sin datos por defecto o con registros fijos.</summary>
public static class TestShipmentSources
{
    public static IShipmentSource With(params ShipmentRecord[] records)
    {
        var source = Substitute.For<IShipmentSource>();
        source.GetByBlNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<ShipmentRecord?>.Success(
                records.FirstOrDefault(r => string.Equals(r.BlNumber, call.Arg<string>(), StringComparison.OrdinalIgnoreCase))));
        return source;
    }

    public static ShipmentIssuanceReader IssuanceReader(IShipmentSource? source = null) => new(source ?? With());
}
