namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Documents.BlCopy;
using HapagPortal.Application.Documents.Common;
using HapagPortal.Application.Documents.NoDebt;
using HapagPortal.Application.Documents.Repository;
using HapagPortal.Application.Documents.ResponsibilityLetter;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

/// <summary>Contratos HTTP de la Ola E: descarga de archivos, códigos de error y armado de comandos.</summary>
public sealed class DocumentsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly DocumentsController _controller = new();

    public DocumentsControllerTests() => ControllerTestHelper.SetupController(_controller, _sender);

    [Fact]
    public async Task Download_ShouldReturnThePdfFromThePortalChannel()
    {
        _sender.Send(Arg.Any<DownloadShipmentDocumentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DocumentFileDto>.Success(new DocumentFileDto([1, 2, 3], "application/pdf", "copia.pdf")));

        var result = await _controller.Download("BL1", Guid.NewGuid(), CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("copia.pdf");
        file.ContentType.Should().Be("application/pdf");
        await _sender.Received(1).Send(
            Arg.Is<DownloadShipmentDocumentCommand>(c => c.BlNumber == "BL1" && c.Channel == DocumentChannels.Portal),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DocumentNotVisible_ShouldMapToNotFound()
    {
        _sender.Send(Arg.Any<DownloadShipmentDocumentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DocumentFileDto>.Failure(DomainErrors.ShipmentDocument.NotFound(Guid.NewGuid())));

        var result = await _controller.Download("BL1", Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task BlCopy_ShouldSendByEmailByDefault_AndMapForbidden()
    {
        _sender.Send(Arg.Any<RequestBlCopyCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DocumentDeliveryDto>.Failure(Error.Forbidden));

        var result = await _controller.RequestBlCopy("BL1", new RequestBlCopyRequest(true, null), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        await _sender.Received(1).Send(
            Arg.Is<RequestBlCopyCommand>(c => c.BlNumber == "BL1" && c.Valued && c.SendEmail),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoDebtWithDebt_ShouldMapToBadRequestWithTheBlockers()
    {
        _sender.Send(Arg.Any<RequestNoDebtCertificateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ShipmentDocumentDto>.Failure(DomainErrors.NoDebtCertificate.DebtPending("PENDING_CHARGES (THC)")));

        var result = await _controller.RequestNoDebtCertificate("BL1", CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(400);
        problem.Value.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Contain("PENDING_CHARGES (THC)");
    }

    [Fact]
    public async Task ResponsibilityLetter_ShouldMapTheRequestToTheCommand()
    {
        _sender.Send(Arg.Any<IssueResponsibilityLetterCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ShipmentDocumentDto>.Failure(DomainErrors.ResponsibilityLetter.TermsNotAccepted));

        var result = await _controller.IssueResponsibilityLetter(
            "BL1",
            new IssueResponsibilityLetterRequest("Ana", "1-9", "Gerente", "ana@test.cl", null, null, null, false, "CARTA-RESP-2026-10"),
            CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
        await _sender.Received(1).Send(
            Arg.Is<IssueResponsibilityLetterCommand>(c => c.BlNumber == "BL1" && c.SignatoryName == "Ana" && !c.AcceptTerms),
            Arg.Any<CancellationToken>());
    }
}
