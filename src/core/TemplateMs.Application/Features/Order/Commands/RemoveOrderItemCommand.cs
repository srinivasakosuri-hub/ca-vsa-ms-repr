namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>Command to soft-delete a line item from an order.</summary>
/// <param name="OrderId">The domain identity of the order.</param>
/// <param name="OrderItemId">The domain identity of the item to remove.</param>
public sealed record RemoveOrderItemCommand(int OrderId, int OrderItemId);
