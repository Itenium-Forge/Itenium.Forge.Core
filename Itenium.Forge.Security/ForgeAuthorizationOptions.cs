using Microsoft.AspNetCore.Authorization;

namespace Itenium.Forge.Security;

/// <summary>
/// Holds the authorization configuration built via <see cref="ForgeAuthorizationBuilder"/>.
/// Registered as a singleton in DI so <see cref="SecurityExtensions.UseForgeSecurity"/> can
/// validate the configuration at startup.
/// </summary>
internal class ForgeAuthorizationOptions
{
    internal bool IsConfigured { get; set; }
    internal ForgeAuthorizationMode Mode { get; set; } = ForgeAuthorizationMode.NotSet;
    internal List<(string Name, Action<AuthorizationPolicyBuilder> Configure)> Policies { get; } = [];
}

internal enum ForgeAuthorizationMode { NotSet, AllowAnonymous, RequireAuthenticated }
