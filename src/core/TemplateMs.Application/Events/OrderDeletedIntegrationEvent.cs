namespace TemplateMs.Application.Events;

/// <summary>
/// Integration event published to ActiveMQ when an Order is soft-deleted.
/// </summary>
/// <param name="OrderId">The identifier of the deleted order.</param>
/// <param name="DeletedAt">The UTC timestamp when the order was deactivated.</param>
/// <param name="EventId">Unique identifier for this event occurrence (for idempotency).</param>
public sealed record OrderDeletedIntegrationEvent(
    int OrderId,
    DateTime DeletedAt,
    Guid EventId);
