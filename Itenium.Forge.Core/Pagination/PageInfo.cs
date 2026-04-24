using System.Text.Json.Serialization;

namespace Itenium.Forge.Core;

/// <summary>
/// Pagination metadata for a single page of results.
/// </summary>
public sealed class PageInfo
{
    /// <summary>The current page number (1-based).</summary>
    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    /// <summary>The total number of pages, rounded up to fit all items.</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public PageInfo()
    {
    }

    public PageInfo(int page, int pageSize, int totalCount)
    {
        if (page < 1 && totalCount > 0) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize < 1 && totalCount > 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
        if (totalCount < 0) throw new ArgumentOutOfRangeException(nameof(totalCount));

        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
