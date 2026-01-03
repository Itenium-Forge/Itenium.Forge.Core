using Itenium.Forge.ExampleApp.Security;
using Itenium.Forge.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.Forge.ExampleApp.Controllers;

/// <summary>
/// Demonstrates security features with different authorization levels
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SecureController> _logger;

    public SecureController(ICurrentUser currentUser, ILogger<SecureController> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Public endpoint - no authentication required
    /// </summary>
    [HttpGet("public")]
    public IActionResult Public()
    {
        return Ok(new { message = "This endpoint is public - anyone can access it" });
    }

    /// <summary>
    /// Requires any authenticated user
    /// </summary>
    [Authorize]
    [HttpGet("authenticated")]
    public IActionResult Authenticated()
    {
        return Ok(new
        {
            message = "You are authenticated!",
            userId = _currentUser.UserId,
            userName = _currentUser.UserName,
            email = _currentUser.Email,
            roles = _currentUser.Roles
        });
    }

    /// <summary>
    /// Requires the 'user' role
    /// </summary>
    [Authorize(Policy = "user")]
    [HttpGet("user-only")]
    public IActionResult UserOnly()
    {
        return Ok(new
        {
            message = "You have the 'user' role",
            userName = _currentUser.UserName
        });
    }

    /// <summary>
    /// Requires the 'admin' role
    /// </summary>
    [Authorize(Policy = "admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly()
    {
        return Ok(new
        {
            message = "You have the 'admin' role - full access granted",
            userName = _currentUser.UserName
        });
    }

    /// <summary>
    /// Requires the ReadResX capability
    /// </summary>
    [Authorize(Policy = nameof(Capability.ReadResX))]
    [HttpGet("resx")]
    public IActionResult ReadResX()
    {
        return Ok(new
        {
            message = "You have ReadResX capability",
            userName = _currentUser.UserName,
            data = "Resource X data here..."
        });
    }

    /// <summary>
    /// Requires the WriteResX capability
    /// </summary>
    [Authorize(Policy = nameof(Capability.WriteResX))]
    [HttpPost("resx")]
    public IActionResult WriteResX()
    {
        return Ok(new
        {
            message = "You have WriteResX capability - write operation allowed",
            userName = _currentUser.UserName
        });
    }

    /// <summary>
    /// Requires the ReadResY capability
    /// </summary>
    [Authorize(Policy = nameof(Capability.ReadResY))]
    [HttpGet("resy")]
    public IActionResult ReadResY()
    {
        return Ok(new
        {
            message = "You have ReadResY capability",
            userName = _currentUser.UserName,
            data = "Resource Y data here..."
        });
    }

    /// <summary>
    /// Requires the WriteResY capability
    /// </summary>
    [Authorize(Policy = nameof(Capability.WriteResY))]
    [HttpPost("resy")]
    public IActionResult WriteResY()
    {
        return Ok(new
        {
            message = "You have WriteResY capability - write operation allowed",
            userName = _currentUser.UserName
        });
    }
}
