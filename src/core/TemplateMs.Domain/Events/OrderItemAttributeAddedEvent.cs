using TemplateMs.Domain.Common;

namespace TemplateMs.Domain.Events;

/// <summary>Domain event raised when an attribute is added to an order item.</summary>
public sealed record OrderItemAttributeAddedEvent(
    int OrderItemId,
    int OrderItemAttributeId,
    string AttributeName,
    string AttributeValue,
    DateTime CreatedAt) : DomainEvent;
