namespace TemplateMs.Application.Events;

/// <summary>
/// Integration event published to ActiveMQ when an OrderItem is soft-deleted from an order.
/// </summary>
/// <param name="OrderId">The identifier of the parent order.</param>
/// <param name="OrderItemId">The identifier of the removed order item.</param>
/// <param name="RemovedAt">The UTC timestamp when the item was deactivated.</param>
/// <param name="EventId">Unique identifier for this event occurrence (for idempotency).</param>
public sealed record OrderItemRemovedIntegrationEvent(
    int OrderId,
    int OrderItemId,
    DateTime RemovedAt,
    Guid EventId);
