using TemplateMs.Domain.Common;

namespace TemplateMs.Domain.Events;

/// <summary>Domain event raised when an Order is soft-deleted.</summary>
/// <param name="OrderId">The identifier of the deleted order.</param>
/// <param name="DeletedAt">The UTC timestamp when the order was deactivated.</param>
public sealed record OrderDeletedEvent(
    int OrderId,
    DateTime DeletedAt) : DomainEvent;
