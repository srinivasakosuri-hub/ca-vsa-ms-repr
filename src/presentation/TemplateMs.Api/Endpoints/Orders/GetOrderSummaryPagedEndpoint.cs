using FastEndpoints;
using TemplateMs.Api.ResponseModels;
using TemplateMs.Application.Common;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Queries;
using Wolverine;

namespace TemplateMs.Api.Endpoints.Orders;

/// <summary>
/// Request body for the paged order-summary grid endpoint.
/// Accepts page, size, sort column/direction and a free-text search term.
/// </summary>
public sealed class GetOrderSummaryPagedRequest
{
    /// <summary>Gets the page number (1-indexed).</summary>
    public int     Page          { get; init; } = 1;

    /// <summary>Gets the number of items per page.</summary>
    public int     PageSize      { get; init; } = 10;

    /// <summary>Gets the column name to sort by (optional).</summary>
    public string? SortColumn    { get; init; }

    /// <summary>Gets the sort direction ('asc' or 'desc', optional).</summary>
    public string? SortDirection { get; init; }

    /// <summary>Gets the free-text search term to filter results (optional).</summary>
    public string? SearchTerm    { get; init; }
}

/// <summary>
/// Returns a paged, sortable, searchable list of order-summary rows for a UI grid.
/// Each row contains Order, Customer, Product, Quantity, Unit Price and Total Amount.
/// </summary>
/// <remarks>POST api/orders/summary</remarks>
/// <summary>Endpoint for retrieving a paged, sortable, searchable list of order summaries.</summary>
public sealed class GetOrderSummaryPagedEndpoint : Endpoint<GetOrderSummaryPagedRequest, OrderSummaryPagedResponse>
{
    private readonly IMessageBus _bus;

    /// <summary>Initializes a new instance of the <see cref="GetOrderSummaryPagedEndpoint"/> class.</summary>
    /// <param name="bus">The message bus for dispatching queries.</param>
    public GetOrderSummaryPagedEndpoint(IMessageBus bus) => _bus = bus;

    /// <summary>Configures the endpoint route and behavior.</summary>
    public override void Configure()
    {
        Post("api/orders/summary");
        AllowAnonymous();
        Description(b => b
            .Produces<OrderSummaryPagedResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    /// <summary>Handles the request to retrieve paged order summaries.</summary>
    /// <param name="req">The request containing page, filters, and sort parameters.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(GetOrderSummaryPagedRequest req, CancellationToken ct)
    {
        var query = new GetOrdersPagedQuery(
            req.Page,
            req.PageSize,
            req.SortColumn,
            req.SortDirection,
            req.SearchTerm);

        var result = await _bus.InvokeAsync<PagedResult<OrderSummaryDto>>(query, ct);
        HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        await HttpContext.Response.WriteAsJsonAsync(OrderSummaryPagedResponse.FromPagedResult(result!), cancellationToken: ct);
    }
}
