namespace Itenium.Forge.Core;

/// <summary>
/// Default implementation of <see cref="IForgePagedResult{T}"/>.
/// Construct from a pre-fetched page of items, the total item count, and the query parameters.
/// </summary>
public sealed class ForgePagedResult<T> : IForgePagedResult<T>
{
    /// <inheritdoc/>
    public IReadOnlyList<T> Items { get; }

    /// <inheritdoc/>
    public int Page { get; }

    /// <inheritdoc/>
    public int PageSize { get; }

    /// <inheritdoc/>
    public int TotalCount { get; }

    /// <inheritdoc/>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <inheritdoc/>
    public bool HasPreviousPage => Page > 1;

    /// <inheritdoc/>
    public bool HasNextPage => Page < TotalPages;

    /// <param name="items">The items on this page. Must not be null.</param>
    /// <param name="totalCount">Total number of items across all pages. Must be non-negative.</param>
    /// <param name="page">Current page number (1-based). Must be at least 1.</param>
    /// <param name="pageSize">Maximum items per page. Must be at least 1.</param>
    public ForgePagedResult(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        Items = items.ToList();
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
