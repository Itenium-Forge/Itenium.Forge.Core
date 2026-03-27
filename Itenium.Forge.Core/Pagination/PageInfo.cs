namespace Itenium.Forge.Core;

/// <summary>
/// Pagination metadata for a single page of results.
/// </summary>
public sealed class PageInfo
{
    /// <summary>The current page number (1-based).</summary>
    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    /// <summary>The total number of pages, rounded up to fit all items.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    public PageInfo(int page, int pageSize, int totalCount)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
