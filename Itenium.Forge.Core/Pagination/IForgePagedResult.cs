namespace Itenium.Forge.Core;

/// <summary>
/// A single page of results from a paginated query.
/// Page numbers are 1-based; <see cref="PageInfo.Page"/> 1 is the first page.
/// </summary>
public interface IForgePagedResult<out T>
{
    IReadOnlyList<T> Items { get; }

    PageInfo Page { get; }
}
