namespace HapagPortal.UnitTests.Application.Documents;

using System.Text.Json;
using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.BlCopy;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Copia del BL (M6-05) y repositorio documental (M6-09): permisos por tipo de documento según M1-11 (el
/// shipper solo la copia no valorada, el comprobante Collect solo para la agencia de aduanas autorizada,
/// M6-04), envío al correo registrado y registro de descargas y envíos (NF-14).
/// </summary>
public sealed class BlCopyAndRepositoryTests
{
    private const string BlNumber = "BL-DOCS";
    private readonly DocumentsFixture _f = new();
    private readonly BillOfLading _bl;
    private readonly PaymentsFixture.Actor _shipper;

    public BlCopyAndRepositoryTests()
    {
        _bl = _f.Payments.Rules.OwnBl(BlNumber);
        _bl.FreightAmount = 4200m;
        _bl.FreightCurrency = "USD";
        _f.Payments.Rules.AddContainer(_bl, "HLXU0000001", "40HC");
        _f.Payments.Rules.AddCharge(_bl, ChargeConceptCodes.Thc, 185000m);

        _shipper = _f.Payments.NewActor();
        AccessTestData.AddRole(_f.Db, _bl, _shipper.Organization, ShipmentRoleCodes.Shipper);
    }

    [Fact]
    public async Task Shipper_CannotRequestTheValuedCopy_ButGetsTheNonValuedOneByEmail()
    {
        var valued = await _f.BlCopy(_shipper).Handle(new RequestBlCopyCommand(BlNumber, Valued: true), CancellationToken.None);
        var nonValued = await _f.BlCopy(_shipper).Handle(new RequestBlCopyCommand(BlNumber, Valued: false), CancellationToken.None);

        valued.Error.Should().Be(Error.Forbidden);
        nonValued.IsSuccess.Should().BeTrue();
        nonValued.Value.Document.DocumentType.Should().Be(ShipmentDocumentTypes.BlCopyNonValued);
        nonValued.Value.Document.DocumentNumber.Should().StartWith(DocumentPrefixes.BlCopy);
        nonValued.Value.Document.ContainerNumbers.Should().Equal("HLXU0000001");
        nonValued.Value.SentTo.Should().Equal(_shipper.Organization.Email);

        await _f.Email.Received(1).SendEmailAsync(
            _shipper.Organization.Email,
            Arg.Is<string>(s => s.Contains(BlNumber)),
            Arg.Any<string>(),
            Arg.Is<IReadOnlyList<EmailAttachment>>(a => a.Count == 1 && a[0].ContentType == "application/pdf"),
            Arg.Any<CancellationToken>());

        var document = _f.Db.ShipmentDocumentList.Single();
        document.IssuedForOrganizationId.Should().Be(_shipper.Organization.Id);
        document.IssuedByUserId.Should().Be(_shipper.User.Id);
        document.StorageKey.Should().NotBeNull();
        _f.Storage.Files.Should().ContainKey(document.StorageKey!);
        document.ContentHash.Should().HaveLength(64);
        document.RetainUntil.Should().Be(document.IssuedAt.AddYears(_f.Settings.RetentionYears));
        _f.Db.ShipmentDocumentEventList.Select(e => (e.EventType, e.Channel, e.UserId, e.OrganizationId, e.Recipient)).Should().Equal(
            (ShipmentDocumentEventTypes.Issued, DocumentChannels.Portal, (Guid?)_shipper.User.Id, (Guid?)_shipper.Organization.Id, (string?)null),
            (ShipmentDocumentEventTypes.Sent, DocumentChannels.Email, _shipper.User.Id, _shipper.Organization.Id, _shipper.Organization.Email));
    }

    [Fact]
    public async Task ValuedCopy_ShouldIncludeTheCommercialValues_AndTheNonValuedCopyShouldExcludeThem()
    {
        var valued = await _f.BlCopy(_f.Payments.Owner).Handle(new RequestBlCopyCommand(BlNumber, true, SendEmail: false), CancellationToken.None);
        var nonValued = await _f.BlCopy(_f.Payments.Owner).Handle(new RequestBlCopyCommand(BlNumber, false, SendEmail: false), CancellationToken.None);

        valued.IsSuccess.Should().BeTrue();
        nonValued.IsSuccess.Should().BeTrue();
        valued.Value.SentTo.Should().BeEmpty();
        await _f.Email.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default!, default!, default(IReadOnlyList<EmailAttachment>)!, default);

        var valuedModel = _f.Renderer.Rendered[0];
        var nonValuedModel = _f.Renderer.Rendered[1];
        var values = valuedModel.Sections.Single(s => s.Heading == "Valores comerciales");
        values.Fields.Should().Contain(f => f.Label == "Flete" && f.Value == "4.200,00 USD");
        values.Table!.Rows.Should().ContainSingle(r => r[0] == ChargeConceptCodes.Thc && r[4] == "220.150,00");

        var hidden = nonValuedModel.Sections.Single(s => s.Heading == "Valores comerciales");
        hidden.Fields.Should().BeNull();
        hidden.Table.Should().BeNull();
        JsonSerializer.Serialize(nonValuedModel).Should().NotContain("4.200,00").And.NotContain("220.150,00");
    }

    [Fact]
    public async Task Repository_ShouldListOnlyTheDocumentTypesTheUserMayView()
    {
        await _f.BlCopy(_f.Payments.Owner).Handle(new RequestBlCopyCommand(BlNumber, true, false), CancellationToken.None);
        await _f.BlCopy(_f.Payments.Owner).Handle(new RequestBlCopyCommand(BlNumber, false, false), CancellationToken.None);

        var owner = await _f.List(_f.Payments.Owner).Handle(new GetShipmentDocumentsQuery(BlNumber), CancellationToken.None);
        var shipper = await _f.List(_shipper).Handle(new GetShipmentDocumentsQuery(BlNumber), CancellationToken.None);
        var stranger = await _f.List(_f.Payments.NewActor()).Handle(new GetShipmentDocumentsQuery(BlNumber), CancellationToken.None);

        owner.Value.Documents.Select(d => d.DocumentType).Should().BeEquivalentTo(
            [ShipmentDocumentTypes.BlCopyValued, ShipmentDocumentTypes.BlCopyNonValued]);
        owner.Value.Actions.CanRequestValuedCopy.Should().BeTrue();
        owner.Value.TimeZone.Should().Be("America/Santiago");

        shipper.Value.Documents.Select(d => d.DocumentType).Should().Equal(ShipmentDocumentTypes.BlCopyNonValued);
        shipper.Value.Actions.CanRequestValuedCopy.Should().BeFalse();
        shipper.Value.Actions.CanRequestNonValuedCopy.Should().BeTrue();

        stranger.Error.Code.Should().Be("BillOfLading.NotFound");
    }

    [Fact]
    public async Task CollectReceipt_ShouldBeVisibleAndDownloadableOnlyByTheAuthorizedCustomsAgency()
    {
        var agency = _f.Payments.NewActor(OrganizationTypes.CustomsAgency);
        AccessTestData.AddRole(_f.Db, _bl, agency.Organization, ShipmentRoleCodes.CustomsAgency);
        var collect = await IssueCollectAsync(agency.Organization.Id);

        var agencyList = await _f.List(agency).Handle(new GetShipmentDocumentsQuery(BlNumber), CancellationToken.None);
        var ownerList = await _f.List(_f.Payments.Owner).Handle(new GetShipmentDocumentsQuery(BlNumber), CancellationToken.None);
        var agencyDownload = await _f.Download(agency).Handle(new DownloadShipmentDocumentCommand(BlNumber, collect.Id), CancellationToken.None);
        var ownerDownload = await _f.Download(_f.Payments.Owner).Handle(new DownloadShipmentDocumentCommand(BlNumber, collect.Id), CancellationToken.None);
        var shipperDownload = await _f.Download(_shipper).Handle(new DownloadShipmentDocumentCommand(BlNumber, collect.Id), CancellationToken.None);

        agencyList.Value.Documents.Should().ContainSingle(d => d.Id == collect.Id);
        ownerList.Value.Documents.Should().NotContain(d => d.Id == collect.Id);
        agencyDownload.IsSuccess.Should().BeTrue();
        agencyDownload.Value.ContentType.Should().Be("application/pdf");
        ownerDownload.Error.Code.Should().Be("ShipmentDocument.NotFound");
        shipperDownload.Error.Code.Should().Be("ShipmentDocument.NotFound");

        _f.Db.ShipmentDocumentEventList.Where(e => e.EventType == ShipmentDocumentEventTypes.Downloaded)
            .Should().ContainSingle(e => e.UserId == agency.User.Id && e.OrganizationId == agency.Organization.Id && e.Channel == DocumentChannels.Portal);
    }

    [Fact]
    public async Task Download_ShouldLogUserOrganizationChannelAndTime_IncludingTheAssistantChannel()
    {
        var copy = await _f.BlCopy(_shipper).Handle(new RequestBlCopyCommand(BlNumber, false, false), CancellationToken.None);
        var before = DateTime.UtcNow;

        var portal = await _f.Download(_shipper).Handle(new DownloadShipmentDocumentCommand(BlNumber, copy.Value.Document.Id), CancellationToken.None);
        var assistant = await _f.Download(_shipper).Handle(
            new DownloadShipmentDocumentCommand(BlNumber, copy.Value.Document.Id, DocumentChannels.Assistant), CancellationToken.None);

        portal.IsSuccess.Should().BeTrue();
        assistant.Value.Content.Should().Equal(portal.Value.Content);
        portal.Value.FileName.Should().EndWith(".pdf");

        var downloads = _f.Db.ShipmentDocumentEventList.Where(e => e.EventType == ShipmentDocumentEventTypes.Downloaded).ToList();
        downloads.Select(d => d.Channel).Should().Equal(DocumentChannels.Portal, DocumentChannels.Assistant);
        downloads.Should().OnlyContain(d =>
            d.ShipmentDocumentId == copy.Value.Document.Id
            && d.UserId == _shipper.User.Id
            && d.UserEmail == _shipper.User.Email
            && d.OrganizationId == _shipper.Organization.Id
            && d.OccurredAt >= before);
    }

    [Fact]
    public async Task Send_ShouldResendToTheRegisteredEmailOfTheOrganization()
    {
        var copy = await _f.BlCopy(_shipper).Handle(new RequestBlCopyCommand(BlNumber, false, false), CancellationToken.None);

        var sent = await _f.Send(_shipper).Handle(new SendShipmentDocumentCommand(BlNumber, copy.Value.Document.Id), CancellationToken.None);

        sent.Value.SentTo.Should().Equal(_shipper.Organization.Email);
        _f.Db.ShipmentDocumentEventList.Should().ContainSingle(e =>
            e.EventType == ShipmentDocumentEventTypes.Sent && e.Recipient == _shipper.Organization.Email && e.Channel == DocumentChannels.Email);
    }

    [Fact]
    public async Task SeededDocumentWithoutFile_ShouldBeGeneratedFromItsTemplateOnTheFirstDownload()
    {
        var model = new PdfDocumentModel(
            "Copia de BL - no valorada", null, "Hapag-Lloyd Chile SpA", "Chile", "CBL-SEED-1", new DateTime(2026, 10, 3, 16, 30, 0, DateTimeKind.Utc),
            "America/Santiago", [], [], "AAAA-BBBB-CCCC-DDDD", null, null);
        var seeded = new ShipmentDocument
        {
            DocumentType = ShipmentDocumentTypes.BlCopyNonValued,
            DocumentNumber = "CBL-SEED-1",
            Status = ShipmentDocumentStatus.Issued,
            BillOfLadingId = _bl.Id,
            BlNumber = BlNumber,
            Country = CountryCodes.Chile,
            IssuedAt = model.IssuedAt,
            Origin = ShipmentDocumentOrigins.Seed,
            FileName = "copia.pdf",
            ContentType = "application/pdf",
            VerificationCode = "AAAA-BBBB-CCCC-DDDD",
            TemplateJson = JsonSerializer.Serialize(model, ShipmentDocumentService.JsonOptions)
        };
        _f.Db.ShipmentDocumentList.Add(seeded);

        var first = await _f.Download(_shipper).Handle(new DownloadShipmentDocumentCommand(BlNumber, seeded.Id), CancellationToken.None);
        var second = await _f.Download(_shipper).Handle(new DownloadShipmentDocumentCommand(BlNumber, seeded.Id), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        seeded.StorageKey.Should().NotBeNull();
        seeded.ContentHash.Should().NotBeNull();
        second.Value.Content.Should().Equal(first.Value.Content);
        _f.Renderer.Rendered.Should().ContainSingle(m => m.DocumentNumber == "CBL-SEED-1");
    }

    private async Task<ShipmentDocument> IssueCollectAsync(Guid organizationId)
    {
        var documents = _f.Documents();
        var now = DateTime.UtcNow;
        var data = await documents.LoadDataAsync(_bl, CancellationToken.None);
        var header = documents.NewHeader(ShipmentDocumentTypes.CollectReceipt, _bl, now);
        var issued = await documents.IssueAsync(
            new DocumentIssue(
                ShipmentDocumentTypes.CollectReceipt,
                _bl,
                ShipmentDocumentTemplates.CollectReceipt(data, header, "AGA", null, "PAY-1", "RCP-1", 4200m, "USD", now),
                ShipmentDocumentOrigins.Payment,
                DocumentActor.System,
                organizationId,
                ["HLXU0000001"]),
            CancellationToken.None);
        return issued.Value;
    }
}
