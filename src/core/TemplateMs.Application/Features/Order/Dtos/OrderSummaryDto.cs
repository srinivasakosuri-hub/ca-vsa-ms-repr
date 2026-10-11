namespace TemplateMs.Application.Features.Order.Dtos;

/// <summary>
/// Flat data transfer object for the order-summary grid.
/// Each row represents one order line item together with its parent order header,
/// so the grid can show Order / Customer / Product / Quantity / Unit Price / Total Amount
/// in a single denormalised projection.
/// </summary>
/// <param name="OrderId">The database primary key of the order.</param>
/// <param name="CustomerName">The full name of the customer who placed the order.</param>
/// <param name="OrderDate">The date the order was placed.</param>
/// <param name="OrderItemId">The database primary key of the order line item.</param>
/// <param name="ProductName">The name of the product on this line item.</param>
/// <param name="Quantity">The quantity ordered.</param>
/// <param name="UnitPrice">The price per unit.</param>
/// <param name="TotalAmount">The computed total (Quantity × UnitPrice).</param>
public sealed record OrderSummaryDto(
    int OrderId,
    string CustomerName,
    DateTime OrderDate,
    int OrderItemId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount);
