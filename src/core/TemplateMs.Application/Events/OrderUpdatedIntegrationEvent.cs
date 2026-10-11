namespace TemplateMs.Application.Events;

/// <summary>
/// Integration event published to ActiveMQ when an Order is updated.
/// </summary>
/// <param name="OrderId">The identifier of the updated order.</param>
/// <param name="CustomerName">The updated customer name.</param>
/// <param name="OrderDate">The updated order date.</param>
/// <param name="UpdatedAt">The UTC timestamp of the update.</param>
/// <param name="EventId">Unique identifier for this event occurrence (for idempotency).</param>
public sealed record OrderUpdatedIntegrationEvent(
    int OrderId,
    string CustomerName,
    DateTime OrderDate,
    DateTime UpdatedAt,
    Guid EventId);
