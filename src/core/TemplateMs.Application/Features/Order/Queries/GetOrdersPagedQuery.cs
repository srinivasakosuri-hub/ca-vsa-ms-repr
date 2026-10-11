using TemplateMs.Application.Common;
using TemplateMs.Application.Features.Order.Dtos;

namespace TemplateMs.Application.Features.Order.Queries;

/// <summary>
/// Query to retrieve a paged, sorted, optionally-filtered list of order-summary rows for a UI grid.
/// Dispatched via WolverineFX.
/// </summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Number of items per page. Supported values: 10, 25, 50; any positive integer is accepted.</param>
/// <param name="SortColumn">
/// Column to sort by. Allowed values (case-insensitive):
/// <c>orderId</c>, <c>customerName</c>, <c>orderDate</c>,
/// <c>productName</c>, <c>quantity</c>, <c>unitPrice</c>, <c>totalAmount</c>.
/// Defaults to <c>orderDate</c> when omitted.
/// </param>
/// <param name="SortDirection">Sort direction: <c>asc</c> or <c>desc</c>. Defaults to <c>desc</c>.</param>
/// <param name="SearchTerm">
/// Optional free-text filter applied as a case-insensitive LIKE across
/// <c>CustomerName</c>, <c>ProductName</c>, and <c>OrderDate</c> (formatted as yyyy-MM-dd).
/// Omit or pass <c>null</c> to return all records.
/// </param>
public sealed record GetOrdersPagedQuery(
    int Page,
    int PageSize,
    string? SortColumn,
    string? SortDirection,
    string? SearchTerm);
