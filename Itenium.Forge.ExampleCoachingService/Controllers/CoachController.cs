using Itenium.Forge.ExampleCoachingService.Client;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.Forge.ExampleCoachingService.Controllers;

/// <summary>Provides the coach catalogue.</summary>
[ApiController]
[Route("coaches")]
public class CoachController : ControllerBase
{
    private static readonly IReadOnlyList<Coach> Coaches =
    [
        new(1, "Sarah De Backer",   "Agile & Scrum",          Available: true),
        new(2, "Tom Verhaegen",     ".NET Architecture",       Available: true),
        new(3, "Lien Janssen",      "Angular & TypeScript",    Available: false),
        new(4, "Pieter Hermans",    "DevOps & Kubernetes",     Available: true),
        new(5, "Nathalie Claeys",   "Leadership & Management", Available: false),
    ];

    [HttpGet]
    public IReadOnlyList<Coach> GetAll() => Coaches;

    [HttpGet("{id:int}")]
    public ActionResult<Coach> Get(int id)
    {
        var coach = Coaches.FirstOrDefault(c => c.Id == id);
        return coach is null ? NotFound() : Ok(coach);
    }
}
