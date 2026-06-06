namespace HapagPortal.WebApi.Abstractions;

using HapagPortal.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiController : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleFailure(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot handle failure for a successful result.");
        }

        if (result is IValidationResult validationResult)
        {
            var problemDetails = new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            };

            foreach (var error in validationResult.Errors)
            {
                problemDetails.Errors.Add(error.Code, [error.Message]);
            }

            return BadRequest(problemDetails);
        }

        var statusCode = result.Error.Code switch
        {
            _ when result.Error.Code.EndsWith(".NotFound") => StatusCodes.Status404NotFound,
            "Error.Unauthorized" => StatusCodes.Status401Unauthorized,
            "Error.Forbidden" => StatusCodes.Status403Forbidden,
            _ when result.Error.Code.EndsWith(".HasData") => StatusCodes.Status409Conflict,
            _ when result.Error.Code.EndsWith(".Exists") => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: result.Error.Code,
            detail: result.Error.Message,
            statusCode: statusCode);
    }
}
