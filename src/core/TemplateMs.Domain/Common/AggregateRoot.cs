namespace TemplateMs.Domain.Common;

/// <summary>Base class for DDD aggregate roots. Tracks domain events raised during the aggregate's lifetime.</summary>
public abstract class AggregateRoot : BaseAuditableEntity
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>Gets the domain events raised by this aggregate since the last commit.</summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();


    /// <summary>Registers a domain event to be dispatched when the unit of work commits.</summary>
    /// <param name="domainEvent">The domain event to register.</param>
    protected void AddDomainEvent(DomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>Clears all registered domain events. Called by the unit of work after dispatch.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();


}
