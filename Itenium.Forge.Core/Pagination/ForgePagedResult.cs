using System.Text.Json.Serialization;

namespace Itenium.Forge.Core;

/// <summary>
/// Default implementation of <see cref="IForgePagedResult{T}"/>.
/// Construct from a pre-fetched page of items, the total item count, and the query parameters.
/// </summary>
public sealed class ForgePagedResult<T> : IForgePagedResult<T>
{
    /// <inheritdoc/>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <inheritdoc/>
    public PageInfo Page { get; init; } = null!;

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public ForgePagedResult()
    {
    }

    /// <summary>
    /// Constructor for manual instantiation.
    /// </summary>
    public ForgePagedResult(IEnumerable<T> items, PageInfo page)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(page);

        Items = items.ToList();
        Page = page;
    }

    /// <param name="items">The items on this page. Must not be null.</param>
    /// <param name="totalCount">Total number of items across all pages. Must be non-negative.</param>
    /// <param name="page">Current page number (1-based). Must be at least 1.</param>
    /// <param name="pageSize">Maximum items per page. Must be at least 1.</param>
    public ForgePagedResult(IEnumerable<T> items, int totalCount, int page, int pageSize)
        : this(items, new PageInfo(page, pageSize, totalCount))
    {
    }
}
