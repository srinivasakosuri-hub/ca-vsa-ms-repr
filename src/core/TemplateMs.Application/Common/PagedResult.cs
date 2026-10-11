namespace TemplateMs.Application.Common;

/// <summary>
/// Wraps a page of results together with the total record count, enabling UI grids
/// to display paging controls without a second round-trip.
/// </summary>
/// <typeparam name="T">The type of each item in the page.</typeparam>
/// <param name="Items">The items for the current page.</param>
/// <param name="TotalCount">The total number of records across all pages.</param>
/// <param name="Page">The 1-based current page number.</param>
/// <param name="PageSize">The number of records per page.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
