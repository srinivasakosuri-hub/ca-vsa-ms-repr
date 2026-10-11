using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Domain.Common;
using Wolverine;

namespace TemplateMs.Infrastructure.Messaging.Wolverine;

/// <summary>
/// Dispatches domain events collected on aggregate roots through WolverineFX's <see cref="IMessageBus"/>.
/// Implements <see cref="IDomainEventDispatcher"/>.
/// </summary>
public sealed class WolverineEventForwarders : IDomainEventDispatcher
{
    private readonly IMessageBus _bus;

    /// <summary>Initializes a new <see cref="WolverineEventForwarders"/>.</summary>
    public WolverineEventForwarders(IMessageBus bus) => _bus = bus;

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<AggregateRoot> aggregates, CancellationToken cancellationToken = default)
    {
        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                await _bus.PublishAsync(domainEvent);
            }
            aggregate.ClearDomainEvents();
        }
    }
}
