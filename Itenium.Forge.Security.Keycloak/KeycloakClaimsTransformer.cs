using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Itenium.Forge.Security.Keycloak;

/// <summary>
/// Transforms Keycloak JWT claims to standard .NET claims.
/// Specifically handles the nested realm_access.roles structure.
/// </summary>
internal class KeycloakClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        // Extract roles from Keycloak's realm_access claim
        var realmAccessClaim = identity.FindFirst("realm_access");
        if (realmAccessClaim != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                {
                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrEmpty(roleName) && !identity.HasClaim(ClaimTypes.Role, roleName))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed realm_access claims
            }
        }

        // Also handle resource_access for client-specific roles
        var resourceAccessClaim = identity.FindFirst("resource_access");
        if (resourceAccessClaim != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(resourceAccessClaim.Value);
                foreach (var client in doc.RootElement.EnumerateObject())
                {
                    if (client.Value.TryGetProperty("roles", out var rolesElement))
                    {
                        foreach (var role in rolesElement.EnumerateArray())
                        {
                            var roleName = role.GetString();
                            if (!string.IsNullOrEmpty(roleName))
                            {
                                var qualifiedRole = $"{client.Name}:{roleName}";
                                if (!identity.HasClaim(ClaimTypes.Role, qualifiedRole))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, qualifiedRole));
                                }
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed resource_access claims
            }
        }

        return Task.FromResult(principal);
    }
}
