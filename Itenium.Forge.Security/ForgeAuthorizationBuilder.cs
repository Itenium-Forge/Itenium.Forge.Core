using Microsoft.AspNetCore.Authorization;

namespace Itenium.Forge.Security;

/// <summary>
/// Fluent builder for configuring the Forge authorization default policy.
/// Exactly one of <see cref="RequireAuthenticatedByDefault"/> or
/// <see cref="AllowAnonymousByDefault"/> must be called; omitting both
/// causes a hard crash at startup in Development.
/// </summary>
public class ForgeAuthorizationBuilder
{
    private readonly ForgeAuthorizationOptions _options;

    internal ForgeAuthorizationBuilder(ForgeAuthorizationOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// All endpoints require an authenticated user by default.
    /// At least one named policy must also be defined via <see cref="AddPolicy"/>.
    /// </summary>
    public ForgeAuthorizationBuilder RequireAuthenticatedByDefault()
    {
        _options.Mode = ForgeAuthorizationMode.RequireAuthenticated;
        _options.IsConfigured = true;
        return this;
    }

    /// <summary>
    /// No authentication is required by default; individual endpoints opt in via [Authorize].
    /// Emits a startup warning in non-Development environments.
    /// </summary>
    public ForgeAuthorizationBuilder AllowAnonymousByDefault()
    {
        _options.Mode = ForgeAuthorizationMode.AllowAnonymous;
        _options.IsConfigured = true;
        return this;
    }

    /// <summary>
    /// Registers a named authorization policy, e.g. for use with [Authorize(Policy = "admin")].
    /// Required when <see cref="RequireAuthenticatedByDefault"/> is used.
    /// </summary>
    public ForgeAuthorizationBuilder AddPolicy(string name, Action<AuthorizationPolicyBuilder> configure)
    {
        _options.Policies.Add((name, configure));
        return this;
    }
}
