using TemplateMs.Domain.Common;

namespace TemplateMs.Domain.Events;

/// <summary>Domain event raised when a new line item is added to an order.</summary>
public sealed record OrderItemAddedEvent(
    int OrderId,
    int OrderItemId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    DateTime CreatedAt) : DomainEvent;
