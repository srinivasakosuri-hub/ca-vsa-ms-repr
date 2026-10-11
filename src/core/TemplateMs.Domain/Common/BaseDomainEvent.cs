namespace TemplateMs.Domain.Common;

/// <summary>
/// Represents the base class for all domain events.
/// Contains the date and time when the event occurred.
/// </summary>
public abstract record BaseDomainEvent
{
    /// <summary>
    /// Gets the date and time when the domain event occurred.
    /// </summary>
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
}