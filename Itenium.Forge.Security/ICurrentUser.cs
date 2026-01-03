namespace Itenium.Forge.Security;

/// <summary>
/// Provides access to the current authenticated user's information.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The unique identifier (sub claim) of the user.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// The username (preferred_username claim) of the user.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// The email address of the user.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if the user has the specified role.
    /// </summary>
    bool IsInRole(string role);
}
