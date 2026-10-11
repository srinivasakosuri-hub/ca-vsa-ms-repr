using TemplateMs.Domain.Common;

namespace TemplateMs.Domain.Events;

/// <summary>Domain event raised when an Order's properties are updated.</summary>
/// <param name="OrderId">The identifier of the updated order.</param>
/// <param name="CustomerName">The updated customer name.</param>
/// <param name="OrderDate">The updated order date.</param>
/// <param name="UpdatedAt">The UTC timestamp of the update.</param>
public sealed record OrderUpdatedEvent(
    int OrderId,
    string CustomerName,
    DateTime OrderDate,
    DateTime UpdatedAt) : DomainEvent;
