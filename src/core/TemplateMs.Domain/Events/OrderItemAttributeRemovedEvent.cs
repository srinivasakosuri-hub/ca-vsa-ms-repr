using TemplateMs.Domain.Common;

namespace TemplateMs.Domain.Events;

/// <summary>Domain event raised when an attribute is soft-deleted from an order item.</summary>
public sealed record OrderItemAttributeRemovedEvent(
    int OrderItemId,
    int OrderItemAttributeId,
    DateTime RemovedAt) : DomainEvent;
