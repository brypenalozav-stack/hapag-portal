namespace HapagPortal.UnitTests.WebApi.Controllers;

using FluentAssertions;
using HapagPortal.Application.Invoices;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Application.ShoppingCart;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using HapagPortal.UnitTests.WebApi.TestHelpers;
using HapagPortal.WebApi.Controllers.V1;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

/// <summary>Contratos HTTP de la Ola D: idempotencia del cierre, códigos de error y archivos.</summary>
public sealed class CartAndInvoicesControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    [Fact]
    public async Task Checkout_ShouldPassTheIdempotencyKeyHeaderToTheCommand()
    {
        var controller = new CartController();
        ControllerTestHelper.SetupController(controller, _sender);
        _sender.Send(Arg.Any<CheckoutCartCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CheckoutResultDto>.Failure(DomainErrors.PaymentFlow.ProviderUnavailable));

        var result = await controller.Checkout(new CheckoutCartRequest("CL", "CLP", "KHIPU"), "key-123", CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CheckoutCartCommand>(c => c.IdempotencyKey == "key-123" && c.Country == "CL" && c.PaymentMethodCode == "KHIPU"),
            Arg.Any<CancellationToken>());
        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task DuplicateItem_ShouldMapToConflict()
    {
        var controller = new CartController();
        ControllerTestHelper.SetupController(controller, _sender);
        _sender.Send(Arg.Any<AddCartItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CartDto>.Failure(DomainErrors.Cart.Duplicate));

        var result = await controller.AddItem(new AddCartItemRequest("LocalCharge", Guid.NewGuid(), null, "76123456-7", null), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task InvoicePdf_ShouldReturnAFile()
    {
        var controller = new InvoicesController();
        ControllerTestHelper.SetupController(controller, _sender);
        _sender.Send(Arg.Any<GetInvoicePdfQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceFileDto>.Success(new InvoiceFileDto([1, 2, 3], "application/pdf", "factura-1.pdf")));

        var result = await controller.GetPdf(Guid.NewGuid(), CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("factura-1.pdf");
        file.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task ForeignOrganization_ShouldMapToForbidden()
    {
        var controller = new InvoicesController();
        ControllerTestHelper.SetupController(controller, _sender);
        _sender.Send(Arg.Any<GetInvoicesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceListDto>.Failure(Error.Forbidden));

        var result = await controller.GetAll(new GetInvoicesQuery(OrganizationId: Guid.NewGuid()), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }
}
