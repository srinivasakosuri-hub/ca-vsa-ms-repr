namespace TemplateMs.Application.Events;

/// <summary>
/// Integration event published to ActiveMQ when an attribute is added to an order item.
/// </summary>
/// <param name="OrderItemId">The identifier of the parent order item.</param>
/// <param name="OrderItemAttributeId">The identifier of the newly added attribute.</param>
/// <param name="AttributeName">The attribute name.</param>
/// <param name="AttributeValue">The attribute value.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
/// <param name="EventId">Unique identifier for this event occurrence (for idempotency).</param>
public sealed record OrderItemAttributeAddedIntegrationEvent(
    int OrderItemId,
    int OrderItemAttributeId,
    string AttributeName,
    string AttributeValue,
    DateTime CreatedAt,
    Guid EventId);
