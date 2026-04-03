using Itenium.Forge.ExampleCoachingService.Client;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.Forge.ExampleApp.Controllers;

/// <summary>
/// Proxies coach data from the downstream CoachingService.
/// Demonstrates typed Refit client usage with traceparent propagation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CoachingController : ControllerBase
{
    private readonly ICoachingServiceClient _coachingService;
    private readonly ILogger<CoachingController> _logger;

    public CoachingController(ICoachingServiceClient coachingService, ILogger<CoachingController> logger)
    {
        _coachingService = coachingService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IReadOnlyList<Coach>> GetAll(CancellationToken ct)
    {
        _logger.LogInformation("Fetching coaches from CoachingService");
        return await _coachingService.GetCoachesAsync(ct);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Coach>> Get(int id, CancellationToken ct)
    {
        return await _coachingService.GetCoachAsync(id, ct);
    }
}
