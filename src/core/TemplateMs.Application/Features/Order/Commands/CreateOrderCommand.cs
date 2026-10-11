namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>Command to create a new order, optionally with line items and their attributes in one shot.</summary>
/// <param name="CustomerName">The full name of the customer placing the order.</param>
/// <param name="OrderDate">The date the order was placed.</param>
/// <param name="Items">Optional line items to create together with the order.</param>
public sealed record CreateOrderCommand(
    string CustomerName,
    DateTime OrderDate,
    IReadOnlyList<CreateOrderItemData>? Items = null);

/// <summary>Line-item data carried inside <see cref="CreateOrderCommand"/>.</summary>
/// <param name="ProductName">The name of the product.</param>
/// <param name="Quantity">The number of units. Must be positive.</param>
/// <param name="UnitPrice">The price per unit. Must be non-negative.</param>
/// <param name="Attributes">Optional attributes for this item.</param>
public sealed record CreateOrderItemData(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    IReadOnlyList<CreateOrderItemAttributeData>? Attributes = null);

/// <summary>Attribute data carried inside <see cref="CreateOrderItemData"/>.</summary>
/// <param name="AttributeName">The attribute name (e.g., Color).</param>
/// <param name="AttributeValue">The attribute value (e.g., Blue).</param>
public sealed record CreateOrderItemAttributeData(string AttributeName, string AttributeValue);
