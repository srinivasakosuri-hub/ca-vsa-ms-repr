namespace TemplateMs.Application.Events;

/// <summary>
/// Integration event published to ActiveMQ when an Order is created.
/// This event crosses service boundaries and is serialized to JSON for AMQ transport.
/// </summary>
/// <param name="OrderId">The identifier of the newly created order.</param>
/// <param name="CustomerName">The name of the customer.</param>
/// <param name="OrderDate">The date of the order.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
/// <param name="EventId">Unique identifier for this event occurrence (for idempotency).</param>
public sealed record OrderCreatedIntegrationEvent(
    int OrderId,
    string CustomerName,
    DateTime OrderDate,
    DateTime CreatedAt,
    Guid EventId);
