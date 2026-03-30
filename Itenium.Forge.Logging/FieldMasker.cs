using System.Text.Json;
using System.Text.Json.Nodes;

namespace Itenium.Forge.Logging;

/// <summary>
/// Masks sensitive field values before they are written to a log sink.
/// </summary>
internal static class FieldMasker
{
    private const string MaskValue = "***";

    /// <summary>
    /// Parses <paramref name="json"/> and replaces values of any property whose name
    /// matches a field in <paramref name="maskedFields"/> with <c>***</c>.
    /// Traversal is recursive — nested objects and arrays are also processed.
    /// Returns the original string unchanged when it is not valid JSON.
    /// </summary>
    public static string MaskJsonBody(string json, IReadOnlySet<string> maskedFields)
    {
        if (string.IsNullOrWhiteSpace(json) || maskedFields.Count == 0)
            return json;

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return json;
        }

        if (node is null)
            return json;

        MaskNode(node, maskedFields);
        return node.ToJsonString();
    }

    /// <summary>
    /// Returns a dictionary with values replaced by <c>***</c> for keys that match a field
    /// in <paramref name="maskedFields"/>. When <paramref name="maskedFields"/> is empty the
    /// original dictionary is returned unchanged. Otherwise a new dictionary is returned and
    /// the original is not mutated.
    /// </summary>
    public static Dictionary<string, string> MaskQueryParams(
        Dictionary<string, string> query,
        IReadOnlySet<string> maskedFields)
    {
        if (maskedFields.Count == 0)
            return query;

        var result = new Dictionary<string, string>(query.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in query)
            result[key] = maskedFields.Contains(key) ? MaskValue : value;

        return result;
    }

    /// <summary>
    /// Returns a dictionary with values replaced by <c>***</c> for header names that match
    /// a field in <paramref name="maskedHeaderFields"/>. Same contract as
    /// <see cref="MaskQueryParams"/> — original is not mutated.
    /// </summary>
    public static Dictionary<string, string> MaskHeaders(
        Dictionary<string, string> headers,
        IReadOnlySet<string> maskedHeaderFields)
    {
        if (maskedHeaderFields.Count == 0)
            return headers;

        var result = new Dictionary<string, string>(headers.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in headers)
            result[key] = maskedHeaderFields.Contains(key) ? MaskValue : value;

        return result;
    }

    // -------------------------------------------------------------------------

    private static void MaskNode(JsonNode node, IReadOnlySet<string> maskedFields)
    {
        if (node is JsonObject obj)
            MaskObject(obj, maskedFields);
        else if (node is JsonArray arr)
            foreach (var item in arr)
                if (item is not null)
                    MaskNode(item, maskedFields);
    }

    private static void MaskObject(JsonObject obj, IReadOnlySet<string> maskedFields)
    {
        foreach (var key in obj.Select(kv => kv.Key).ToList())
        {
            if (maskedFields.Contains(key))
                obj[key] = MaskValue;
            else if (obj[key] is JsonObject or JsonArray)
                MaskNode(obj[key]!, maskedFields);
        }
    }
}
