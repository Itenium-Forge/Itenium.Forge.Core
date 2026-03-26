namespace Itenium.Forge.SecurityHeaders;

/// <summary>
/// A keyed collection of <see cref="IHeaderPolicy"/> instances, one per response header.
/// The dictionary key is the HTTP header name; assigning a new policy for the same key
/// replaces the previous one, preventing duplicate headers.
/// </summary>
public class HeaderPolicyCollection : Dictionary<string, IHeaderPolicy>
{
}
