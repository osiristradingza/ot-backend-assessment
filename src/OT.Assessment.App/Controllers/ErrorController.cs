using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OT.Assessment.App.Controllers;

/// <summary>
/// Global error handling controller
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : ControllerBase
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Global error handler
    /// </summary>
    [Route("/api/error")]
    public IActionResult Error()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var exception = context?.Error;

        if (exception != null)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }

        return Problem(
            title: "An error occurred while processing your request",
            statusCode: 500
        );
    }
}