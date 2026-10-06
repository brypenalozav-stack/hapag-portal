namespace HapagPortal.UnitTests.Application.Documents;

using FluentAssertions;
using HapagPortal.Application.ChargeRules.Charges;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Carta de responsabilidad (M6-06): el consignee FFWW la emite con sus datos y los términos aceptados; una
/// carta vigente cumple M4-04 y levanta el bloqueo en cargos y carro solo sobre ese BL. La carta nueva
/// reemplaza la anterior.
/// </summary>
public sealed class ResponsibilityLetterTests
{
    private const string BlNumber = "BL-FFWW";
    private const string OtherBlNumber = "BL-FFWW-2";
    private readonly DocumentsFixture _f = new();
    private readonly PaymentsFixture.Actor _ffww;
    private readonly BillOfLading _bl;
    private readonly BillOfLading _otherBl;
    private readonly LocalCharge _thc;
    private readonly LocalCharge _otherThc;

    public ResponsibilityLetterTests()
    {
        _ffww = _f.Payments.NewActor(OrganizationTypes.FreightForwarder);
        _f.FreightForwarder(_ffww);

        _bl = _f.Payments.Rules.OwnBl(BlNumber, consignee: false);
        _otherBl = _f.Payments.Rules.OwnBl(OtherBlNumber, consignee: false);
        AccessTestData.AddRole(_f.Db, _bl, _ffww.Organization, ShipmentRoleCodes.Consignee);
        AccessTestData.AddRole(_f.Db, _otherBl, _ffww.Organization, ShipmentRoleCodes.Consignee);
        _thc = _f.Payments.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 185000m);
        _otherThc = _f.Payments.Rules.AddCharge(_otherBl, ChargeConceptCodes.Thc, 185000m);
    }

    private string FfwwTaxId => TaxIdNormalizer.Normalize(_ffww.Organization.TaxId);

    private Task<Result<CartDto>> AddToCartAsync(LocalCharge charge) =>
        _f.Payments.Add(_ffww).Handle(
            new AddCartItemCommand(PayableItemTypes.LocalCharge, charge.Id, null, FfwwTaxId, null), CancellationToken.None);

    private Task<Result<ShipmentChargesDto>> ChargesAsync(string blNumber) =>
        new GetShipmentChargesQueryHandler(_f.Db, _ffww.Evaluator(_f.Db), _f.Payments.Rules.ChargeRules())
            .Handle(new GetShipmentChargesQuery(blNumber), CancellationToken.None);

    [Fact]
    public async Task WithoutLetter_TheFreightForwarderIsBlocked()
    {
        var added = await AddToCartAsync(_thc);
        var charges = await ChargesAsync(BlNumber);
        var list = await _f.List(_ffww).Handle(new GetShipmentDocumentsQuery(BlNumber), CancellationToken.None);

        added.Error.Code.Should().Be("Cart.ResponsibilityLetterRequired");
        charges.Value.Requirements.Should().ContainSingle(r =>
            r.Code == ProcessRequirements.ResponsibilityLetter && r.Status == ProcessRequirementStatus.Missing && r.BlocksProcess);
        charges.Value.CanProceed.Should().BeFalse();
        list.Value.ResponsibilityLetter.Should().Be(new ResponsibilityLetterStateDto(true, ProcessRequirementStatus.Missing, true));
        list.Value.Actions.CanIssueResponsibilityLetter.Should().BeTrue();
    }

    [Fact]
    public async Task IssuedLetter_ShouldLiftTheBlockOnlyOnItsBl()
    {
        var issued = await _f.Letter(_ffww).Handle(DocumentsFixture.LetterCommand(BlNumber), CancellationToken.None);

        issued.IsSuccess.Should().BeTrue();
        issued.Value.DocumentType.Should().Be(ShipmentDocumentTypes.ResponsibilityLetter);
        issued.Value.DocumentNumber.Should().StartWith(DocumentPrefixes.ResponsibilityLetter);
        issued.Value.TermsVersion.Should().Be(ResponsibilityLetterTerms.Version);
        issued.Value.IssuedForOrganizationId.Should().Be(_ffww.Organization.Id);
        _f.Renderer.Rendered.Single().Sections.Should().Contain(s => s.Heading == "Firmante"
            && s.Fields!.Any(f => f.Label == "Nombre" && f.Value == "Felipe Forwarder"));

        (await new ResponsibilityLetterStatus(_f.Db).GetStatusAsync(_bl.Id, _ffww.Organization.Id))
            .Should().Be(ProcessRequirementStatus.Fulfilled);

        var charges = await ChargesAsync(BlNumber);
        charges.Value.Requirements.Single().Status.Should().Be(ProcessRequirementStatus.Fulfilled);
        charges.Value.CanProceed.Should().BeTrue();
        (await AddToCartAsync(_thc)).IsSuccess.Should().BeTrue();

        // La carta es por BL: el otro BL del mismo FFWW sigue bloqueado.
        (await AddToCartAsync(_otherThc)).Error.Code.Should().Be("Cart.ResponsibilityLetterRequired");
        (await ChargesAsync(OtherBlNumber)).Value.CanProceed.Should().BeFalse();
    }

    [Fact]
    public async Task NewLetter_ShouldSupersedeThePreviousOne()
    {
        var first = await _f.Letter(_ffww).Handle(DocumentsFixture.LetterCommand(BlNumber), CancellationToken.None);
        var second = await _f.Letter(_ffww).Handle(DocumentsFixture.LetterCommand(BlNumber), CancellationToken.None);

        _f.Db.ShipmentDocumentList.Single(d => d.Id == first.Value.Id).Status.Should().Be(ShipmentDocumentStatus.Superseded);
        _f.Db.ShipmentDocumentList.Single(d => d.Id == second.Value.Id).Status.Should().Be(ShipmentDocumentStatus.Issued);
        (await new ResponsibilityLetterStatus(_f.Db).GetStatusAsync(_bl.Id, _ffww.Organization.Id))
            .Should().Be(ProcessRequirementStatus.Fulfilled);
    }

    [Fact]
    public async Task ExpiredLetter_ShouldNotFulfilTheRequirement()
    {
        var issued = await _f.Letter(_ffww).Handle(DocumentsFixture.LetterCommand(BlNumber), CancellationToken.None);
        _f.Db.ShipmentDocumentList.Single(d => d.Id == issued.Value.Id).ValidUntil = DateTime.UtcNow.AddMinutes(-1);

        (await new ResponsibilityLetterStatus(_f.Db).GetStatusAsync(_bl.Id, _ffww.Organization.Id))
            .Should().Be(ProcessRequirementStatus.Missing);
    }

    [Theory]
    [InlineData(false, ResponsibilityLetterTerms.Version, "ResponsibilityLetter.TermsNotAccepted")]
    [InlineData(true, "CARTA-RESP-2020-01", "ResponsibilityLetter.TermsVersionMismatch")]
    public async Task Letter_RequiresTheCurrentTermsToBeAccepted(bool accept, string version, string error)
    {
        var result = await _f.Letter(_ffww).Handle(DocumentsFixture.LetterCommand(BlNumber, accept, version), CancellationToken.None);

        result.Error.Code.Should().Be(error);
        _f.Db.ShipmentDocumentList.Should().BeEmpty();
    }

    [Fact]
    public async Task CustomerWithoutTheFreightForwarderException_CannotIssueTheLetter()
    {
        var result = await _f.Letter(_f.Payments.Owner).Handle(DocumentsFixture.LetterCommand(BlNumber), CancellationToken.None);

        result.Error.Should().Be(Error.Forbidden);
    }
}
