namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>Command to soft-delete an attribute from an order item.</summary>
/// <param name="OrderId">The domain identity of the parent order.</param>
/// <param name="OrderItemId">The domain identity of the parent item.</param>
/// <param name="OrderItemAttributeId">The domain identity of the attribute to remove.</param>
public sealed record RemoveOrderItemAttributeCommand(
    int OrderId,
    int OrderItemId,
    int OrderItemAttributeId);
