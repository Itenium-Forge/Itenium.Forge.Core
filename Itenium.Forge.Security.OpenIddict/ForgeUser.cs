using Microsoft.AspNetCore.Identity;

namespace Itenium.Forge.Security.OpenIddict;

/// <summary>
/// Application user entity for ASP.NET Core Identity.
/// Extend this class to add custom user properties.
/// </summary>
public class ForgeUser : IdentityUser
{
    /// <summary>
    /// User's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Full display name.
    /// </summary>
    public string DisplayName => $"{FirstName} {LastName}".Trim();
}
