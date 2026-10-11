using TemplateMs.Application.Common;
using TemplateMs.Application.Features.Order.Dtos;

namespace TemplateMs.Api.ResponseModels;

/// <summary>HTTP response body for a single row in the order-summary grid.</summary>
/// <param name="OrderId">The database primary key of the order.</param>
/// <param name="CustomerName">The full name of the customer.</param>
/// <param name="OrderDate">The date the order was placed.</param>
/// <param name="OrderItemId">The database primary key of the order line item.</param>
/// <param name="ProductName">The name of the product on this line.</param>
/// <param name="Quantity">The quantity ordered.</param>
/// <param name="UnitPrice">The price per unit.</param>
/// <param name="TotalAmount">The computed total (Quantity × UnitPrice).</param>
public sealed record OrderSummaryItemResponse(
    int OrderId,
    string CustomerName,
    DateTime OrderDate,
    int OrderItemId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount)
{
    /// <summary>Maps an <see cref="OrderSummaryDto"/> to an <see cref="OrderSummaryItemResponse"/>.</summary>
    public static OrderSummaryItemResponse FromDto(OrderSummaryDto dto) =>
        new(dto.OrderId,
            dto.CustomerName,
            dto.OrderDate,
            dto.OrderItemId,
            dto.ProductName,
            dto.Quantity,
            dto.UnitPrice,
            dto.TotalAmount);
}

/// <summary>HTTP response body for the paged order-summary grid endpoint.</summary>
/// <param name="Items">The order-summary rows for the current page.</param>
/// <param name="TotalCount">The total number of records across all pages.</param>
/// <param name="Page">The 1-based current page number.</param>
/// <param name="PageSize">The number of records per page.</param>
/// <param name="TotalPages">The total number of pages.</param>
public sealed record OrderSummaryPagedResponse(
    IReadOnlyList<OrderSummaryItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages)
{
    /// <summary>Maps a <see cref="PagedResult{OrderSummaryDto}"/> to an <see cref="OrderSummaryPagedResponse"/>.</summary>
    public static OrderSummaryPagedResponse FromPagedResult(PagedResult<OrderSummaryDto> result) =>
        new(result.Items.Select(OrderSummaryItemResponse.FromDto).ToList().AsReadOnly(),
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages);
}
