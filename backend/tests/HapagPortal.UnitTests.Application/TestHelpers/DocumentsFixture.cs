namespace HapagPortal.UnitTests.Application.TestHelpers;

using HapagPortal.Application.Common.Helpers;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.BlCopy;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.NoDebt;
using HapagPortal.Application.Documents.PostPayment;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.Documents.ResponsibilityLetter;
using HapagPortal.Application.Payments.PostProcessing;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using NSubstitute;

/// <summary>
/// Contexto de pruebas de la Ola E sobre el de la Ola D: generación documental con un PDF de pruebas,
/// almacenamiento en memoria, firmante y correo simulados, y los manejadores armados para un actor.
/// </summary>
public sealed class DocumentsFixture
{
    public PaymentsFixture Payments { get; } = new();
    public MockApplicationDbContext Db => Payments.Db;
    public FakePdfDocumentRenderer Renderer { get; } = new();
    public InMemoryFileStorage Storage { get; } = new();
    public FakeDocumentSigner Signer { get; } = new();
    public IEmailService Email { get; } = Substitute.For<IEmailService>();
    public DocumentSettings Settings { get; } = new() { UmarEmail = "umar@test.cl" };

    public ShipmentDocumentService Documents() => new(Db, Renderer, Signer, Storage, Email, Settings);

    public NoDebtEvaluator NoDebt() => new(Db, Payments.Rules.ChargeRules(), Payments.Rules.DemurrageStatus());

    public RequestBlCopyCommandHandler BlCopy(PaymentsFixture.Actor actor) =>
        new(Db, actor.Evaluator(Db), actor.CurrentUser, Documents());

    public GetShipmentDocumentsQueryHandler List(PaymentsFixture.Actor actor) =>
        new(Db, actor.Evaluator(Db), Payments.Rules.ChargeRules(), new ResponsibilityLetterStatus(Db));

    public DownloadShipmentDocumentCommandHandler Download(PaymentsFixture.Actor actor) =>
        new(Db, actor.Evaluator(Db), actor.CurrentUser, Documents());

    public SendShipmentDocumentCommandHandler Send(PaymentsFixture.Actor actor) =>
        new(Db, actor.Evaluator(Db), actor.CurrentUser, Documents());

    public IssueResponsibilityLetterCommandHandler Letter(PaymentsFixture.Actor actor) =>
        new(Db, actor.Evaluator(Db), actor.CurrentUser, Documents());

    public RequestNoDebtCertificateCommandHandler NoDebtCertificate(PaymentsFixture.Actor actor) =>
        new(Db, actor.Evaluator(Db), actor.CurrentUser, NoDebt(), Documents());

    public GetNoDebtEligibilityQueryHandler NoDebtEligibility(PaymentsFixture.Actor actor) =>
        new(Db, actor.Evaluator(Db), NoDebt());

    public PaymentPostProcessor Processor() =>
        Payments.Processor(new GeneratePaymentDocumentsStep(Db, Documents(), Payments.Notifications));

    /// <summary>La organización figura en Nexus como FFWW autorizado (M8-03): exige carta (M4-04).</summary>
    public void FreightForwarder(PaymentsFixture.Actor actor) =>
        Payments.Rules.Conditions(TaxIdNormalizer.Normalize(actor.Organization.TaxId), true, null);

    public static IssueResponsibilityLetterCommand LetterCommand(string blNumber, bool accept = true, string version = ResponsibilityLetterTerms.Version) =>
        new(blNumber, "Felipe Forwarder", "12.345.678-5", "Gerente", "felipe@ffww.test", null, "Muebles", null, accept, version);
}

/// <summary>Almacenamiento en memoria con la regla de claves del contrato (CT-STORAGE).</summary>
public sealed class InMemoryFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);

    public async Task<Result<string>> SaveAsync(Stream content, string fileName, string contentType, string container, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var key = $"{container}/2026/10/{Guid.NewGuid():N}";
        Files[key] = buffer.ToArray();
        return Result<string>.Success(key);
    }

    public Task<Result<Stream?>> OpenReadAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<Stream?>.Success(Files.TryGetValue(key, out var content) ? new MemoryStream(content) : null));

    public Task<Result> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        Files.Remove(key);
        return Task.FromResult(Result.Success());
    }
}

/// <summary>Firmante de pruebas: agrega una marca al contenido y registra lo firmado.</summary>
public sealed class FakeDocumentSigner : IDocumentSigner
{
    public List<SignDocumentRequest> Requests { get; } = [];

    public string Provider => "TestSigner";

    public Task<Result<SignedDocument>> SignAsync(SignDocumentRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        var signed = request.Content.Concat("%SIGNED"u8.ToArray()).ToArray();
        return Task.FromResult(Result<SignedDocument>.Success(
            new SignedDocument($"TEST-SIG-{Requests.Count}", DateTime.UtcNow, "ADVANCED", "PAdES", signed)));
    }
}
