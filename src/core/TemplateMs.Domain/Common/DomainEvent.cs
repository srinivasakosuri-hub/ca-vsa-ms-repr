namespace TemplateMs.Domain.Common;

/// <summary>Marker base record for all domain events. Domain events represent something meaningful that happened within the domain.</summary>
public abstract record DomainEvent : BaseDomainEvent
{
    /// <summary>Gets the unique identifier for this event occurrence.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>Gets the UTC timestamp when this event was raised.</summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
