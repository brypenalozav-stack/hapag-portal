namespace HapagPortal.UnitTests.Domain.Shipments;

using FluentAssertions;
using HapagPortal.Domain.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Shipments;

/// <summary>
/// Reglas puras de la Ola F: publicación por DIFU de destino final (M2-01), traducción del estado de emisión
/// del origen (M2-02), estados del TATC (M2-09) y texto normalizado para las búsquedas libres.
/// </summary>
public sealed class ShipmentInformationRulesTests
{
    private static readonly ShipmentPublicationRule AntofagastaFromSanAntonio = new()
    {
        Country = CountryCodes.Chile,
        FinalDestinationCode = "CLANF",
        DischargePortCode = "CLSAI"
    };

    private static BillOfLading Bl(string? destination, string? discharge = "CLSAI", string? difu = null, string? difuLocation = null, string country = CountryCodes.Chile) => new()
    {
        BLNumber = "HLCU-TEST",
        ShipmentType = "Import",
        FreightCurrency = "USD",
        Status = "Arrived",
        Country = country,
        FinalDestinationCode = destination,
        PortOfDischargeCode = discharge,
        DifuCode = difu,
        DifuLocationCode = difuLocation
    };

    [Fact]
    public void Publication_FinalDestinationWithoutDifu_ShouldHideTheBl()
    {
        var decision = ShipmentPublication.Evaluate(Bl("CLANF"), [AntofagastaFromSanAntonio]);

        decision.Should().Be(new PublicationDecision(false, ShipmentPublicationReasons.DifuMissing, AntofagastaFromSanAntonio.Id));
    }

    [Fact]
    public void Publication_DifuAssociatedToTheFinalDestination_ShouldPublish()
    {
        var decision = ShipmentPublication.Evaluate(Bl("CLANF", difu: "ANF-01", difuLocation: "clanf"), [AntofagastaFromSanAntonio]);

        decision.Published.Should().BeTrue();
        decision.ReasonCode.Should().Be(ShipmentPublicationReasons.DifuAssociated);
    }

    [Fact]
    public void Publication_DifuOfAnotherLocation_ShouldHideTheBl()
    {
        var decision = ShipmentPublication.Evaluate(Bl("CLANF", difu: "PUQ-01", difuLocation: "CLPUQ"), [AntofagastaFromSanAntonio]);

        decision.Published.Should().BeFalse();
        decision.ReasonCode.Should().Be(ShipmentPublicationReasons.DifuOtherLocation);
    }

    [Theory]
    [InlineData(null, "CLSAI", ShipmentPublicationReasons.NoRule)]
    [InlineData("CLANF", "CLANF", ShipmentPublicationReasons.SameAsDischarge)]
    [InlineData("CLSCL", "CLSAI", ShipmentPublicationReasons.NoRule)]
    [InlineData("CLANF", "CLVAP", ShipmentPublicationReasons.NoRule)]
    public void Publication_WithoutAnApplicableRule_ShouldPublish(string? destination, string discharge, string reason)
    {
        var decision = ShipmentPublication.Evaluate(Bl(destination, discharge), [AntofagastaFromSanAntonio]);

        decision.Published.Should().BeTrue();
        decision.ReasonCode.Should().Be(reason);
    }

    [Fact]
    public void Publication_InactiveRuleOrOtherCountry_ShouldNotApply()
    {
        var inactive = new ShipmentPublicationRule { Country = CountryCodes.Chile, FinalDestinationCode = "CLANF", IsActive = false };
        var bolivia = new ShipmentPublicationRule { Country = CountryCodes.Bolivia, FinalDestinationCode = "CLANF" };

        ShipmentPublication.Evaluate(Bl("CLANF"), [inactive, bolivia]).Published.Should().BeTrue();
    }

    [Fact]
    public void Publication_RuleForAnyDischargePort_ShouldApplyToEveryPort()
    {
        var anyPort = new ShipmentPublicationRule { Country = CountryCodes.Chile, FinalDestinationCode = "CLANF" };

        ShipmentPublication.Evaluate(Bl("CLANF", "CLVAP"), [anyPort]).ReasonCode.Should().Be(ShipmentPublicationReasons.DifuMissing);
    }

    [Theory]
    [InlineData("NOT_ISSUED", BlIssuanceStatuses.Pending)]
    [InlineData("DRAFT", BlIssuanceStatuses.Pending)]
    [InlineData("ISSUED", BlIssuanceStatuses.Issued)]
    [InlineData("issued", BlIssuanceStatuses.Issued)]
    [InlineData("DESTINATION_ISSUANCE_AUTHORIZED", BlIssuanceStatuses.AuthorizedAtDestination)]
    [InlineData("ISSUED_AT_DESTINATION", BlIssuanceStatuses.IssuedAtDestination)]
    [InlineData("TRANSFERRED", BlIssuanceStatuses.Transferred)]
    [InlineData("SURRENDERED", BlIssuanceStatuses.Surrendered)]
    [InlineData("TELEX_RELEASED", BlIssuanceStatuses.TelexReleased)]
    [InlineData("VOIDED", BlIssuanceStatuses.Cancelled)]
    [InlineData("SOMETHING_NEW", BlIssuanceStatuses.Unknown)]
    public void IssuanceStatus_ShouldMapTheSourceCode(string source, string expected)
    {
        BlIssuanceMapper.MapStatus(source).Should().Be(expected);
    }

    [Fact]
    public void IssuanceStatus_NotInformed_ShouldStayNull()
    {
        BlIssuanceMapper.MapStatus(null).Should().BeNull();
        BlIssuanceMapper.MapStatus(" ").Should().BeNull();
    }

    [Theory]
    [InlineData("BL", TransportDocumentTypes.Bl)]
    [InlineData("OBL", TransportDocumentTypes.Bl)]
    [InlineData("SEA_WAYBILL", TransportDocumentTypes.Swb)]
    [InlineData("swb", TransportDocumentTypes.Swb)]
    [InlineData("EBL", TransportDocumentTypes.Ebl)]
    [InlineData("PAPERLESS", null)]
    [InlineData(null, null)]
    public void DocumentType_ShouldMapOnlyKnownTypes(string? source, string? expected)
    {
        BlIssuanceMapper.MapDocumentType(source).Should().Be(expected);
    }

    [Theory]
    [InlineData(new string[0], TatcStatuses.NotRegistered)]
    [InlineData(new[] { TatcStatuses.Issued, TatcStatuses.Issued }, TatcStatuses.Issued)]
    [InlineData(new[] { TatcStatuses.Issued, TatcStatuses.NotIssued }, TatcStatuses.PartiallyIssued)]
    [InlineData(new[] { TatcStatuses.PreTatc, TatcStatuses.NotIssued }, TatcStatuses.PreTatc)]
    [InlineData(new[] { TatcStatuses.Cancelled }, TatcStatuses.Cancelled)]
    [InlineData(new[] { TatcStatuses.NotIssued, TatcStatuses.Cancelled }, TatcStatuses.NotIssued)]
    [InlineData(new[] { TatcStatuses.Issued, TatcStatuses.Unknown }, TatcStatuses.Unknown)]
    public void Tatc_ShouldAggregateTheContainers(string[] containers, string expected)
    {
        TatcStatusMapper.Aggregate(containers).Should().Be(expected);
    }

    [Theory]
    [InlineData("PRE_TATC", TatcStatuses.PreTatc)]
    [InlineData("issued", TatcStatuses.Issued)]
    [InlineData("LOST", TatcStatuses.Unknown)]
    [InlineData(null, TatcStatuses.Unknown)]
    public void Tatc_ShouldMapTheContainerCode(string? source, string expected)
    {
        TatcStatusMapper.MapContainer(source).Should().Be(expected);
    }

    [Fact]
    public void SearchText_ShouldIgnoreCaseAccentsAndSpacing()
    {
        SearchText.Normalize("  Ácido   SULFÚRICO ").Should().Be("acido sulfurico");
        SearchText.Words("¿Dónde está mi BL HLCU-123?", minLength: 3).Should().Equal("donde", "esta", "hlcu", "123");
    }
}
