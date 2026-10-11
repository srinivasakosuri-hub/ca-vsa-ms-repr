namespace TemplateMs.Application.Events;

/// <summary>
/// Integration event published to ActiveMQ when an attribute is soft-deleted from an order item.
/// </summary>
/// <param name="OrderItemId">The identifier of the parent order item.</param>
/// <param name="OrderItemAttributeId">The identifier of the removed attribute.</param>
/// <param name="RemovedAt">The UTC timestamp when the attribute was deactivated.</param>
/// <param name="EventId">Unique identifier for this event occurrence (for idempotency).</param>
public sealed record OrderItemAttributeRemovedIntegrationEvent(
    int OrderItemId,
    int OrderItemAttributeId,
    DateTime RemovedAt,
    Guid EventId);
