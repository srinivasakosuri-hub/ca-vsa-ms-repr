using TemplateMs.Domain.Common;

namespace TemplateMs.Domain.Events;

/// <summary>Domain event raised when a line item is soft-deleted from an order.</summary>
public sealed record OrderItemRemovedEvent(
    int OrderId,
    int OrderItemId,
    DateTime RemovedAt) : DomainEvent;
