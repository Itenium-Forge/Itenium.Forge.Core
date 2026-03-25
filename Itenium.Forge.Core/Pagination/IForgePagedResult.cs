namespace Itenium.Forge.Core;

/// <summary>
/// A single page of results from a paginated query.
/// Page numbers are 1-based; <see cref="Page"/> 1 is the first page.
/// </summary>
public interface IForgePagedResult<out T>
{
    /// <summary>The items on this page.</summary>
    IReadOnlyList<T> Items { get; }

    /// <summary>The current page number (1-based).</summary>
    int Page { get; }

    /// <summary>The maximum number of items per page.</summary>
    int PageSize { get; }

    /// <summary>The total number of items across all pages.</summary>
    int TotalCount { get; }

    /// <summary>The total number of pages, rounded up to fit all items.</summary>
    int TotalPages { get; }

    /// <summary>True when a previous page exists, i.e. <see cref="Page"/> is greater than 1.</summary>
    bool HasPreviousPage { get; }

    /// <summary>True when a next page exists, i.e. <see cref="Page"/> is less than <see cref="TotalPages"/>.</summary>
    bool HasNextPage { get; }
}
