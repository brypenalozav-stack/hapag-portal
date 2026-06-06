namespace HapagPortal.WebApi.Controllers.V1;

using Asp.Versioning;
using HapagPortal.Application.FAQ.Commands.Create;
using HapagPortal.Application.FAQ.Read.GetAllFAQs;
using HapagPortal.Application.FAQ.Read.GetCategories;
using HapagPortal.Application.FAQ.Read.Search;
using HapagPortal.WebApi.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiVersion("1.0")]
public sealed class FAQsController : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? country,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var query = new GetAllFAQsQuery(country, category);
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetFAQCategoriesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string searchText,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SearchFAQsQuery(searchText), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,BA")]
    public async Task<IActionResult> Create(
        [FromBody] CreateFAQCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
