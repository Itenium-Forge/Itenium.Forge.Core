using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.Security;

/// <summary>
/// Implementation of ICurrentUser that extracts user information from the HTTP context.
/// </summary>
internal class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("sub");

    public string? UserName => User?.FindFirstValue("preferred_username")
        ?? User?.FindFirstValue(ClaimTypes.Name)
        ?? User?.Identity?.Name;

    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email");

    public IEnumerable<string> Roles
    {
        get
        {
            if (User == null)
                return [];

            // Try standard role claims first
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            // Also check Keycloak's realm_access.roles structure (flattened by our claim transformer)
            roles.AddRange(User.FindAll("roles").Select(c => c.Value));

            return roles.Distinct();
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
