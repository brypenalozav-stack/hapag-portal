namespace HapagPortal.UnitTests.Infrastructure.Persistence;

using System.Text.Json;
using FluentAssertions;
using HapagPortal.Application.Common.Access;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>
/// Semilla de Fase 1 Ola E sobre un proveedor EF real (InMemory): documentos de demostración con su modelo,
/// carta FFWW vigente en BL13 y faltante en BL11 (M4-04), comprobante Collect visible solo para la agencia
/// (M6-04), concepto y tarifa del certificado de transbordo (M6-01) y demoras anticipadas pagadas en BL05.
/// </summary>
public sealed class DocumentsSeedTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public DocumentsSeedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private ShipmentAccessEvaluator EvaluatorFor(Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.HasPermission(Arg.Any<string>()).Returns(call => call.Arg<string>() == AccessPermissions.OperateShipments);
        return new ShipmentAccessEvaluator(_context, currentUser);
    }

    [Fact]
    public async Task SeededDocuments_ShouldHaveAValidTemplateAndNoFileYet()
    {
        var documents = await _context.ShipmentDocuments.AsNoTracking().ToListAsync();

        documents.Select(d => d.DocumentType).Should().BeEquivalentTo(
        [
            ShipmentDocumentTypes.TransshipmentCertificate, ShipmentDocumentTypes.BlCopyNonValued,
            ShipmentDocumentTypes.CollectReceipt, ShipmentDocumentTypes.ResponsibilityLetter
        ]);
        documents.Should().OnlyContain(d => d.StorageKey == null && d.Origin == ShipmentDocumentOrigins.Seed);
        foreach (var document in documents)
        {
            var model = JsonSerializer.Deserialize<PdfDocumentModel>(document.TemplateJson, ShipmentDocumentService.JsonOptions)!;
            model.DocumentNumber.Should().Be(document.DocumentNumber);
            model.VerificationCode.Should().Be(document.VerificationCode);
            document.DocumentNumber.Should().StartWith(DocumentPrefixes.ForDocument(document.DocumentType));
        }

        (await _context.ShipmentDocumentEvents.CountAsync(e => e.EventType == ShipmentDocumentEventTypes.Issued)).Should().Be(documents.Count);
    }

    [Fact]
    public async Task FreightForwarderLetter_ShouldLiftTheBlockOnBl13_AndKeepBl11Blocked()
    {
        var status = new ResponsibilityLetterStatus(_context);

        (await status.GetStatusAsync(SeedDataIds.BL13, SeedDataIds.FfwwDemoClient)).Should().Be(ProcessRequirementStatus.Fulfilled);
        (await status.GetStatusAsync(SeedDataIds.BL11, SeedDataIds.FfwwDemoClient)).Should().Be(ProcessRequirementStatus.Missing);
    }

    [Fact]
    public async Task CollectReceiptOfBl12_ShouldBeVisibleOnlyToTheCustomsAgency()
    {
        var bl12 = await _context.BillsOfLading.AsNoTracking().SingleAsync(b => b.Id == SeedDataIds.BL12);

        var agency = EvaluatorFor(SeedDataIds.AgentUserCL);
        var importer = EvaluatorFor(SeedDataIds.DemoUserCL);
        var agencyPermissions = await agency.EvaluateAsync(await agency.GetScopeAsync(), bl12);
        var importerPermissions = await importer.EvaluateAsync(await importer.GetScopeAsync(), bl12);

        bl12.FreightTerms.Should().Be("Collect");
        ShipmentDocumentService.CanView(agencyPermissions, ShipmentDocumentTypes.CollectReceipt).Should().BeTrue();
        ShipmentDocumentService.CanView(importerPermissions, ShipmentDocumentTypes.CollectReceipt).Should().BeFalse();
        ShipmentDocumentService.CanView(importerPermissions, ShipmentDocumentTypes.BlCopyValued).Should().BeTrue();
    }

    [Fact]
    public async Task TransshipmentConceptAndTariff_AndBl05AdvanceDemurrage_ShouldBeSeeded()
    {
        (await _context.ChargeConcepts.AnyAsync(c => c.Code == ChargeConceptCodes.TransshipmentCertificate && c.Countries == "CL")).Should().BeTrue();
        (await _context.Tariffs.SingleAsync(t => t.Id == SeedDataIds.TariffTransshipmentCL)).Amount.Should().Be(35000m);
        (await _context.LocalCharges.SingleAsync(c => c.Id == SeedDataIds.LocalCharge25)).Status.Should().Be(ChargeStatus.Paid);
    }

    public void Dispose() => _context.Dispose();
}
