namespace Itenium.Forge.Core;

/// <summary>
/// Query parameters for paginated endpoints. Bind with [FromQuery] on controller actions.
/// Page numbers are 1-based: <see cref="Page"/> 1 is the first page.
/// </summary>
public class ForgePageQuery
{
    /// <summary>
    /// The page to retrieve. Defaults to 1 (the first page).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// The maximum number of items per page. Defaults to 20.
    /// </summary>
    public int PageSize { get; set; } = 20;
}
