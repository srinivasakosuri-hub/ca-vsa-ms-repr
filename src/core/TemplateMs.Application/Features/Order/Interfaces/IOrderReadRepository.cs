using TemplateMs.Application.Common;
using TemplateMs.Application.Features.Order.Dtos;

namespace TemplateMs.Application.Features.Order.Interfaces;

/// <summary>Read-only repository for the Order feature. Queries use the query database connection.</summary>
public interface IOrderReadRepository
{
    /// <summary>
    /// Returns the full <see cref="Domain.Entities.Order"/> aggregate (with items and attributes) for the given
    /// domain identity, or null if not found. Used by command handlers that need the live aggregate.
    /// </summary>
    Task<Domain.Entities.Order?> GetAggregateByIdAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Returns the full order DTO (with items and attributes) for the given domain identity, or null if not found.</summary>
    Task<OrderDto?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Returns all orders as DTOs, ordered by creation date descending.</summary>
    Task<IEnumerable<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single page of order-summary rows for the UI grid, together with
    /// the total record count needed to render paging controls.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of rows per page.</param>
    /// <param name="sortColumn">Column name to sort by (defaults to <c>orderDate</c>).</param>
    /// <param name="sortDirection">Sort direction: <c>asc</c> or <c>desc</c> (defaults to <c>desc</c>).</param>
    /// <param name="searchTerm">
    /// Optional free-text filter. When supplied, only rows where <c>CustomerName</c>,
    /// <c>ProductName</c>, or <c>OrderDate</c> (yyyy-MM-dd) contain the term are returned.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PagedResult<OrderSummaryDto>> GetPagedSummaryAsync(
        int page,
        int pageSize,
        string? sortColumn,
        string? sortDirection,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all order-summary rows matching the optional search filter, with no paging.
    /// Intended for CSV export where the caller needs every result in one shot.
    /// </summary>
    /// <param name="sortColumn">Column name to sort by (defaults to <c>orderDate</c>).</param>
    /// <param name="sortDirection">Sort direction: <c>asc</c> or <c>desc</c> (defaults to <c>desc</c>).</param>
    /// <param name="searchTerm">
    /// Optional free-text filter. When supplied, only rows where <c>CustomerName</c>,
    /// <c>ProductName</c>, or <c>OrderDate</c> (yyyy-MM-dd) contain the term are returned.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OrderSummaryDto>> GetAllSummaryAsync(
        string? sortColumn,
        string? sortDirection,
        string? searchTerm,
        CancellationToken cancellationToken = default);
}

