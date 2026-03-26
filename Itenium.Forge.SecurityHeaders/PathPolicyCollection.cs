namespace Itenium.Forge.SecurityHeaders;

/// <summary>
/// Maps path prefixes to <see cref="HeaderPolicyCollection"/> overrides.
/// The middleware selects the longest matching prefix per request, falling back to the default policy.
/// </summary>
public sealed class PathPolicyCollection
{
    private readonly List<(string Prefix, HeaderPolicyCollection Policy)> _entries = [];

    /// <summary>Registers a per-path override. Prefix matching is case-insensitive; longer prefixes take priority.</summary>
    public PathPolicyCollection ForPath(string prefix, Action<HeaderPolicyCollection> configure)
    {
        var policy = new HeaderPolicyCollection();
        configure(policy);
        _entries.Add((prefix, policy));
        _entries.Sort((a, b) => b.Prefix.Length.CompareTo(a.Prefix.Length));
        return this;
    }

    internal HeaderPolicyCollection? Resolve(string path)
    {
        foreach (var (prefix, policy) in _entries)
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return policy;
        return null;
    }
}
