namespace TemplateMs.Application.Events;

/// <summary>
/// Integration event published to ActiveMQ when an OrderItem is added to an order.
/// </summary>
/// <param name="OrderId">The identifier of the parent order.</param>
/// <param name="OrderItemId">The identifier of the newly added order item.</param>
/// <param name="ProductName">The name of the product.</param>
/// <param name="Quantity">The quantity ordered.</param>
/// <param name="UnitPrice">The price per unit.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
/// <param name="EventId">Unique identifier for this event occurrence (for idempotency).</param>
public sealed record OrderItemAddedIntegrationEvent(
    int OrderId,
    int OrderItemId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    DateTime CreatedAt,
    Guid EventId);
