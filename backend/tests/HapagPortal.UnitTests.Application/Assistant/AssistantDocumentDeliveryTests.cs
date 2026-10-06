namespace HapagPortal.UnitTests.Application.Assistant;

using FluentAssertions;
using HapagPortal.Application.Assistant;
using HapagPortal.Application.Documents.BlCopy;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.Shipments.Detail;
using HapagPortal.Application.Shipments.Search;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Entrega de documentos por el asistente (M10-04): identifica el documento y el embarque, entrega solo lo que el usuario
/// puede descargar en el repositorio (M6-09) con las mismas restricciones de M1-11 (shipper solo la copia no valorada,
/// Collect solo la agencia de aduanas), no revela documentos de otras organizaciones y registra cada entrega y descarga
/// con el canal Assistant (NF-14).
/// </summary>
public sealed class AssistantDocumentDeliveryTests
{
    private const string BlNumber = "HLCU-DLV-001";
    private readonly DocumentsFixture _f = new();
    private readonly AssistantSettings _settings = new();
    private readonly BillOfLading _bl;
    private readonly PaymentsFixture.Actor _shipper;
    private readonly PaymentsFixture.Actor _agency;
    private readonly PaymentsFixture.Actor _stranger;

    public AssistantDocumentDeliveryTests()
    {
        _bl = _f.Payments.Rules.OwnBl(BlNumber);
        _f.Payments.Rules.AddContainer(_bl, "HLXU1234567", "40HC");

        _shipper = _f.Payments.NewActor();
        AccessTestData.AddRole(_f.Db, _bl, _shipper.Organization, ShipmentRoleCodes.Shipper);
        _agency = _f.Payments.NewActor(OrganizationTypes.CustomsAgency);
        AccessTestData.AddRole(_f.Db, _bl, _agency.Organization, ShipmentRoleCodes.CustomsAgency);
        _stranger = _f.Payments.NewActor();
    }

    private sealed record Handlers(
        StartAssistantSessionCommandHandler Start,
        SendAssistantMessageCommandHandler Send,
        DownloadAssistantDeliveryCommandHandler Download);

    private Handlers For(PaymentsFixture.Actor actor)
    {
        var evaluator = actor.Evaluator(_f.Db);
        var sender = new TestSender()
            .Register(new GetShipmentDetailQueryHandler(_f.Db, evaluator, _f.Payments.Rules.ChargeRules(), TestShipmentSources.IssuanceReader()))
            .Register(new SearchShipmentsQueryHandler(_f.Db, evaluator))
            .Register(_f.List(actor))
            .Register(_f.Download(actor));

        var rules = new RulesAssistantEngine();
        var responder = new AssistantResponder(_f.Db, rules, rules, new AssistantDataRetriever(sender, _f.Db, evaluator));
        return new Handlers(
            new StartAssistantSessionCommandHandler(_f.Db, evaluator, actor.CurrentUser, rules, _settings),
            new SendAssistantMessageCommandHandler(_f.Db, evaluator, responder, _settings),
            new DownloadAssistantDeliveryCommandHandler(_f.Db, evaluator, sender));
    }

    private async Task<(Handlers Handlers, Guid SessionId, AssistantMessageDto Reply)> AskAsync(PaymentsFixture.Actor actor, string message)
    {
        var handlers = For(actor);
        var session = await handlers.Start.Handle(new StartAssistantSessionCommand(), CancellationToken.None);
        var reply = await handlers.Send.Handle(new SendAssistantMessageCommand(session.Value.Id, message), CancellationToken.None);
        reply.IsSuccess.Should().BeTrue(reply.IsFailure ? reply.Error.Message : null);
        return (handlers, session.Value.Id, reply.Value.Reply);
    }

    private async Task<ShipmentDocumentDto> CopyAsync(bool valued)
    {
        var copy = await _f.BlCopy(_f.Payments.Owner).Handle(new RequestBlCopyCommand(BlNumber, valued, SendEmail: false), CancellationToken.None);
        return copy.Value.Document;
    }

    private async Task<ShipmentDocument> CollectReceiptAsync()
    {
        var documents = _f.Documents();
        var now = DateTime.UtcNow;
        var data = await documents.LoadDataAsync(_bl, CancellationToken.None);
        var header = documents.NewHeader(ShipmentDocumentTypes.CollectReceipt, _bl, now);
        var issued = await documents.IssueAsync(new DocumentIssue(
            ShipmentDocumentTypes.CollectReceipt, _bl,
            ShipmentDocumentTemplates.CollectReceipt(data, header, "Agencia", "96555444-3", "PAY-1", "RCP-1", 1500m, "USD", now),
            ShipmentDocumentOrigins.Payment, DocumentActor.System, _agency.Organization.Id, ["HLXU1234567"]), CancellationToken.None);
        return issued.Value;
    }

    [Fact]
    public async Task Owner_ShouldReceiveTheRequestedCopy_AndEveryDeliveryAndDownloadIsLogged()
    {
        var nonValued = await CopyAsync(valued: false);
        await CopyAsync(valued: true);

        var (handlers, sessionId, reply) = await AskAsync(_f.Payments.Owner, $"envíame la copia no valorada del BL {BlNumber}");

        reply.Intent.Should().Be(AssistantIntents.DocumentDelivery);
        reply.AnswerType.Should().Be(AssistantAnswerTypes.Data);
        var action = reply.Actions.Should().ContainSingle().Subject;
        action.Type.Should().Be(AssistantReferences.DownloadDocument);
        action.Id.Should().Be(nonValued.Id);

        var delivery = _f.Db.AssistantDocumentDeliveryList.Single();
        delivery.SessionId.Should().Be(sessionId);
        delivery.MessageId.Should().Be(reply.Id);
        delivery.DocumentId.Should().Be(nonValued.Id);
        delivery.OrganizationId.Should().Be(_f.Payments.Owner.Organization.Id);
        action.Path.Should().Be($"/api/v1/assistant/sessions/{sessionId}/deliveries/{delivery.Id}/download");
        _f.Db.ShipmentDocumentEventList.Should().ContainSingle(e => e.ShipmentDocumentId == nonValued.Id
            && e.EventType == ShipmentDocumentEventTypes.Delivered && e.Channel == DocumentChannels.Assistant);

        var file = await handlers.Download.Handle(new DownloadAssistantDeliveryCommand(sessionId, delivery.Id), CancellationToken.None);

        file.IsSuccess.Should().BeTrue(file.IsFailure ? file.Error.Message : null);
        file.Value.FileName.Should().Be(nonValued.FileName);
        delivery.DownloadCount.Should().Be(1);
        delivery.DownloadedAt.Should().NotBeNull();
        _f.Db.ShipmentDocumentEventList.Should().ContainSingle(e => e.ShipmentDocumentId == nonValued.Id
            && e.EventType == ShipmentDocumentEventTypes.Downloaded && e.Channel == DocumentChannels.Assistant);
    }

    [Fact]
    public async Task Shipper_ShouldNotReceiveTheValuedCopy_AndTheAnswerDoesNotRevealThatItExists()
    {
        await CopyAsync(valued: true);
        var nonValued = await CopyAsync(valued: false);

        var (_, _, valued) = await AskAsync(_shipper, $"envíame la copia valorada del BL {BlNumber}");
        var (_, _, copies) = await AskAsync(_shipper, $"necesito la copia del BL {BlNumber}");

        valued.AnswerType.Should().Be(AssistantAnswerTypes.NotAvailable);
        valued.Actions.Should().NotContain(a => a.Type == AssistantReferences.DownloadDocument);
        copies.Actions.Where(a => a.Type == AssistantReferences.DownloadDocument).Select(a => a.Id).Should().Equal(nonValued.Id);
        _f.Db.AssistantDocumentDeliveryList.Should().ContainSingle(d => d.DocumentId == nonValued.Id);

        // Mismo texto que pedir un tipo nunca emitido: no se distingue un documento sin permiso de uno inexistente.
        var (_, _, neverIssued) = await AskAsync(_f.Payments.Owner, $"envíame la copia valorada del BL {BlNumber}-X");
        valued.Content.Should().NotContain(DocumentPrefixes.BlCopy);
        neverIssued.AnswerType.Should().Be(AssistantAnswerTypes.NotAvailable);
    }

    [Fact]
    public async Task CollectReceipt_ShouldOnlyBeDeliveredToTheAuthorizedCustomsAgency()
    {
        var receipt = await CollectReceiptAsync();

        var (_, _, owner) = await AskAsync(_f.Payments.Owner, $"envíame el comprobante collect del BL {BlNumber}");
        var (_, _, agency) = await AskAsync(_agency, $"envíame el comprobante collect del BL {BlNumber}");

        owner.AnswerType.Should().Be(AssistantAnswerTypes.NotAvailable);
        agency.AnswerType.Should().Be(AssistantAnswerTypes.Data);
        agency.Actions.Should().Contain(a => a.Type == AssistantReferences.DownloadDocument && a.Id == receipt.Id);
        _f.Db.AssistantDocumentDeliveryList.Should().ContainSingle()
            .Which.OrganizationId.Should().Be(_agency.Organization.Id);
    }

    [Fact]
    public async Task AnotherOrganization_ShouldNotReceiveDocumentsOfTheBl_NorDownloadSomeoneElsesDelivery()
    {
        await CopyAsync(valued: false);
        var (ownerHandlers, ownerSession, _) = await AskAsync(_f.Payments.Owner, $"envíame la copia no valorada del BL {BlNumber}");
        var delivery = _f.Db.AssistantDocumentDeliveryList.Single();

        var (strangerHandlers, strangerSession, stranger) = await AskAsync(_stranger, $"envíame la copia no valorada del BL {BlNumber}");
        var (_, _, missing) = await AskAsync(_stranger, "envíame la copia no valorada del BL HLCU-NOPE-999");

        stranger.AnswerType.Should().Be(AssistantAnswerTypes.NotAvailable);
        stranger.Content.Replace(BlNumber, "X").Should().Be(missing.Content.Replace("HLCU-NOPE-999", "X"));
        _f.Db.AssistantDocumentDeliveryList.Should().ContainSingle();

        // La entrega es de la conversación de su dueño: otra conversación u otro usuario no la descargan.
        (await strangerHandlers.Download.Handle(new DownloadAssistantDeliveryCommand(strangerSession, delivery.Id), CancellationToken.None))
            .Error.Code.Should().Be("AssistantDelivery.NotFound");
        (await strangerHandlers.Download.Handle(new DownloadAssistantDeliveryCommand(ownerSession, delivery.Id), CancellationToken.None))
            .Error.Code.Should().Be("AssistantSession.NotFound");
        (await ownerHandlers.Download.Handle(new DownloadAssistantDeliveryCommand(ownerSession, delivery.Id), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delivery_ShouldRevalidatePermissionsWhenDownloading()
    {
        await CopyAsync(valued: false);
        var (handlers, sessionId, _) = await AskAsync(_shipper, $"envíame la copia no valorada del BL {BlNumber}");
        var delivery = _f.Db.AssistantDocumentDeliveryList.Single();

        // El shipper pierde su rol en el BL antes de descargar: el enlace ya no entrega el documento.
        _f.Db.ShipmentRoleList.RemoveAll(r => r.ClientId == _shipper.Organization.Id);
        var file = await handlers.Download.Handle(new DownloadAssistantDeliveryCommand(sessionId, delivery.Id), CancellationToken.None);

        file.Error.Code.Should().Be("BillOfLading.NotFound");
        delivery.DownloadCount.Should().Be(0);
    }

    [Theory]
    [InlineData("envíame la copia no valorada del BL X", new[] { ShipmentDocumentTypes.BlCopyNonValued })]
    [InlineData("mándame el certificado de transbordo del BL X", new[] { ShipmentDocumentTypes.TransshipmentCertificate })]
    [InlineData("necesito la carta de responsabilidad", new[] { ShipmentDocumentTypes.ResponsibilityLetter })]
    [InlineData("comprobante collect del BL X", new[] { ShipmentDocumentTypes.CollectReceipt })]
    [InlineData("dame el cupón de retiro", new[] { ShipmentDocumentTypes.GateOutCoupon })]
    [InlineData("envíame el certificado de flete", new[] { ShipmentDocumentTypes.FreightCertificate })]
    [InlineData("carta de liberación y desconsolidado", new[] { ShipmentDocumentTypes.ReleaseLetter })]
    [InlineData("envíame las boletas y facturas del BL X", new[] { AssistantDeliveryKinds.Receipt, AssistantDeliveryKinds.Invoice })]
    public void RequestedDocuments_ShouldIdentifyTheDocument(string message, string[] kinds) =>
        AssistantIntentRules.RequestedDocuments(message).Should().BeEquivalentTo(kinds);

    [Theory]
    [InlineData("detalle de la factura HL-CL-2026-003987", AssistantIntents.InvoiceDetail)]
    [InlineData("envíame las facturas del BL HLCU123456", AssistantIntents.DocumentDelivery)]
    [InlineData("envíame la copia no valorada del BL HLCU123456", AssistantIntents.DocumentDelivery)]
    [InlineData("documentos del BL HLCU123456", AssistantIntents.ShipmentDocuments)]
    public void Rules_ShouldTellDeliveryFromOtherDocumentQueries(string message, string intent) =>
        AssistantIntentRules.Classify(message).Intent.Should().Be(intent);
}
