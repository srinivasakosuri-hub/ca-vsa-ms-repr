using TemplateMs.Domain.Common;

namespace TemplateMs.Domain.Events;

/// <summary>Domain event raised when a new Order aggregate is created.</summary>
/// <param name="OrderId">The domain identity of the newly created order.</param>
/// <param name="CustomerName">The full name of the customer who placed the order.</param>
/// <param name="OrderDate">The date the order was placed.</param>
/// <param name="CreatedAt">The UTC timestamp when the order was created.</param>
public sealed record OrderCreatedEvent(
    int OrderId,
    string CustomerName,
    DateTime OrderDate,
    DateTime CreatedAt) : DomainEvent;
