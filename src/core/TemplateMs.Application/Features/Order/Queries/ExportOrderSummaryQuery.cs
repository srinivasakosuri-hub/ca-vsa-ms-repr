namespace TemplateMs.Application.Features.Order.Queries;

/// <summary>
/// Query that retrieves all order-summary rows (no paging) after applying
/// an optional sort and free-text search filter, for use in CSV export.
/// </summary>
/// <param name="SortColumn">Column name to sort by. Null defaults to <c>orderDate</c>.</param>
/// <param name="SortDirection"><c>asc</c> or <c>desc</c>. Null defaults to <c>desc</c>.</param>
/// <param name="SearchTerm">
/// Optional free-text search across <c>CustomerName</c>, <c>ProductName</c>,
/// and <c>OrderDate</c> (formatted as yyyy-MM-dd).
/// </param>
public sealed record ExportOrderSummaryQuery(
    string? SortColumn,
    string? SortDirection,
    string? SearchTerm);
