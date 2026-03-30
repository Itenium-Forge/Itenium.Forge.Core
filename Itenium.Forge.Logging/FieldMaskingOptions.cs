namespace Itenium.Forge.Logging;

/// <summary>
/// Configures which JSON body/query-string field names, header names, and object properties
/// are masked in request logs. Matching is case-insensitive. Values are replaced with <c>***</c>.
/// </summary>
public sealed class FieldMaskingOptions
{
    /// <summary>Field names masked in JSON bodies and query strings when no custom configuration is provided.</summary>
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

    /// <summary>Header names masked when no custom configuration is provided.</summary>
    public static readonly IReadOnlySet<string> DefaultMaskedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key"
    };

    /// <summary>
    /// Headers that will be logged when no custom configuration is provided.
    /// Only headers in this set are logged; all others are silently skipped.
    /// </summary>
    public static readonly IReadOnlySet<string> DefaultAllowedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Content-Type",
        "Accept",
        "X-Forwarded-For",
        "traceparent"
    };

    private HashSet<string> _maskedFields = new(DefaultFields, StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _maskedHeaders = new(DefaultMaskedHeaders, StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _allowedHeaders = new(DefaultAllowedHeaders, StringComparer.OrdinalIgnoreCase);

    /// <summary>The active set of body/query field names that will be masked.</summary>
    public IReadOnlySet<string> MaskedFields => _maskedFields;

    /// <summary>The active set of header names that will be masked.</summary>
    public IReadOnlySet<string> MaskedHeaders => _maskedHeaders;

    /// <summary>
    /// The active set of header names that will be logged.
    /// Only headers in this set are included in log output; all others are silently skipped.
    /// A header that is also in <see cref="MaskedHeaders"/> is logged as <c>***</c>.
    /// </summary>
    public IReadOnlySet<string> AllowedHeaders => _allowedHeaders;

    /// <summary>Adds <paramref name="fields"/> to the default masked-field list.</summary>
    public void AddMaskedFields(params string[] fields) => _maskedFields.UnionWith(fields);

    /// <summary>Replaces the default masked-field list entirely with <paramref name="fields"/>.</summary>
    public void SetMaskedFields(params string[] fields) =>
        _maskedFields = new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);

    /// <summary>Adds <paramref name="headers"/> to the default masked-header list.</summary>
    public void AddMaskedHeaders(params string[] headers) => _maskedHeaders.UnionWith(headers);

    /// <summary>Replaces the default masked-header list entirely with <paramref name="headers"/>.</summary>
    public void SetMaskedHeaders(params string[] headers) =>
        _maskedHeaders = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);

    /// <summary>Adds <paramref name="headers"/> to the default allowed-header list.</summary>
    public void AddAllowedHeaders(params string[] headers) => _allowedHeaders.UnionWith(headers);

    /// <summary>Replaces the default allowed-header list entirely with <paramref name="headers"/>.</summary>
    public void SetAllowedHeaders(params string[] headers) =>
        _allowedHeaders = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);
}
