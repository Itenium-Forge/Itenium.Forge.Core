using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.Forge.ExampleApp.Controllers;

/// <summary>
/// Test controller for verifying ProblemDetails error handling.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProblemController : ControllerBase
{
    /// <summary>
    /// Returns a 400 Bad Request response with ProblemDetails.
    /// </summary>
    [HttpGet("bad-request")]
    public IActionResult BadRequest400() => Problem(
        detail: "Validation failed",
        statusCode: StatusCodes.Status400BadRequest);

    /// <summary>
    /// Returns a 401 Unauthorized response.
    /// </summary>
    [HttpGet("unauthorized")]
    public IActionResult Unauthorized401() => Unauthorized();

    /// <summary>
    /// Returns a 403 Forbidden response with ProblemDetails.
    /// </summary>
    [HttpGet("forbidden")]
    public IActionResult Forbidden403() => Problem(
        detail: "Access denied",
        statusCode: StatusCodes.Status403Forbidden);

    /// <summary>
    /// Returns a 404 Not Found response with ProblemDetails.
    /// </summary>
    [HttpGet("not-found")]
    public IActionResult NotFound404() => Problem(
        detail: "Resource not found",
        statusCode: StatusCodes.Status404NotFound);

    /// <summary>
    /// Throws an uncaught exception to trigger 500 Internal Server Error.
    /// </summary>
    [HttpGet("exception")]
    public IActionResult ThrowException() => throw new InvalidOperationException("Test exception");

    /// <summary>
    /// Endpoint with required query parameter for model validation errors.
    /// </summary>
    [HttpGet("validation")]
    public IActionResult ValidationError([FromQuery, Required] string param) => Ok(param);
}
