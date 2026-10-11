namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>Command to add an attribute to an existing order item.</summary>
/// <param name="OrderId">The domain identity of the parent order.</param>
/// <param name="OrderItemId">The domain identity of the order item.</param>
/// <param name="AttributeName">The attribute name (e.g., Color).</param>
/// <param name="AttributeValue">The attribute value (e.g., Blue).</param>
public sealed record AddOrderItemAttributeCommand(
    int OrderId,
    int OrderItemId,
    string AttributeName,
    string AttributeValue);
