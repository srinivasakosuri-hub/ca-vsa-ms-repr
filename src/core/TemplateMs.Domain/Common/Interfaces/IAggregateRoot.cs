namespace TemplateMs.Domain.Common.Interfaces;

/// <summary>
/// Interface to indicate that an entity is an aggregate root in the domain model.
/// </summary>
/// <remarks>
/// This interface is used in Domain-Driven Design (DDD) to mark aggregate root entities.
/// </remarks>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets the collection of domain events associated with this aggregate root.
    /// </summary>
    IReadOnlyCollection<BaseDomainEvent> DomainEvents { get; }
}
