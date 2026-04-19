using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KazoOCR.Api.Controllers;

/// <summary>
/// Controller for health check operations.
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint.
    /// </summary>
    /// <returns>Health status of the API.</returns>
    /// <response code="200">The API is healthy.</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Health check",
        Description = "Returns the health status of the API. Returns 200 OK if the service is healthy.")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new HealthResponse("Healthy", DateTimeOffset.UtcNow));
    }
}

/// <summary>
/// Response model for health check.
/// </summary>
/// <param name="Status">The health status.</param>
/// <param name="Timestamp">The server timestamp.</param>
public record HealthResponse(string Status, DateTimeOffset Timestamp);
