using TemplateMs.Domain.Common;

namespace TemplateMs.Application.Common.Interfaces;

/// <summary>
/// Dispatches domain events that have been collected on aggregate roots.
/// Implemented in the Infrastructure layer using WolverineFX.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>Dispatches all domain events raised by the provided aggregate roots.</summary>
    /// <param name="aggregates">The aggregate roots whose events should be dispatched.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task DispatchAsync(IEnumerable<AggregateRoot> aggregates, CancellationToken cancellationToken = default);
}
