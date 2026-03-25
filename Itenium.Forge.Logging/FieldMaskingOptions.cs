namespace Itenium.Forge.Logging;

/// <summary>
/// Configures which JSON body and query-string field names are masked in request logs.
/// Matching is case-insensitive. Values are replaced with <c>***</c>.
/// </summary>
public sealed class FieldMaskingOptions
{
    /// <summary>
    /// Field names masked when no custom configuration is provided.
    /// </summary>
    public static readonly IReadOnlySet<string> DefaultFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwd",
        "token",
        "secret",
        "authorization",
        "client_secret",
        "api_key",
        "access_token",
        "refresh_token"
    };

    private HashSet<string> _maskedFields = new(DefaultFields, StringComparer.OrdinalIgnoreCase);

    /// <summary>The active set of field names that will be masked.</summary>
    public IReadOnlySet<string> MaskedFields => _maskedFields;

    /// <summary>
    /// Adds <paramref name="fields"/> to the default masked-field list.
    /// </summary>
    public void AddFields(params string[] fields)
    {
        _maskedFields.UnionWith(fields);
    }

    /// <summary>
    /// Replaces the default masked-field list entirely with <paramref name="fields"/>.
    /// </summary>
    public void SetFields(params string[] fields)
    {
        _maskedFields = new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
    }
}
