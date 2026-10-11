using System.Data;
using TemplateMs.Domain.Common;

namespace TemplateMs.Application.Common.Interfaces;

/// <summary>
/// Application-level unit of work. Coordinates the command transaction, domain event dispatch,
/// and outbox message persistence. Implemented by <c>DapperUnitOfWork</c> in Infrastructure.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Gets the open SQL connection associated with the current transaction.</summary>
    IDbConnection Connection { get; }

    /// <summary>Gets the active SQL transaction.</summary>
    IDbTransaction Transaction { get; }

    /// <summary>Rolls back the current transaction without persisting any changes.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits the current transaction, persisting all pending changes.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Registers an aggregate root so its domain events are dispatched on commit.</summary>
    /// <param name="aggregate">The aggregate root to register.</param>
    void RegisterAggregate(AggregateRoot aggregate);

    /// <summary>
    /// Queues an outbox message to be persisted within the current transaction.
    /// The outbox processor will later publish the message to ActiveMQ.
    /// </summary>
    /// <param name="messageType">The fully qualified type name of the integration event.</param>
    /// <param name="payload">The serialized JSON payload of the integration event.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task QueueOutboxMessageAsync(string messageType, string payload, CancellationToken cancellationToken = default);
}
