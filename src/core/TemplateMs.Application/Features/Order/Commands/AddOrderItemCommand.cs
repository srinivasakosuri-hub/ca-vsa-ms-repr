namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>Command to add a line item to an existing order.</summary>
/// <param name="OrderId">The domain identity of the order.</param>
/// <param name="ProductName">The name of the product.</param>
/// <param name="Quantity">The number of units. Must be positive.</param>
/// <param name="UnitPrice">The price per unit. Must be non-negative.</param>
public sealed record AddOrderItemCommand(
    int OrderId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
