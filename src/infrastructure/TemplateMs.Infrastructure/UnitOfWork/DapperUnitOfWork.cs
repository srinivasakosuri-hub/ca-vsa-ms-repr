using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Domain.Common;
using TemplateMs.Infrastructure.Messaging.Outbox;
using TemplateMs.Sql.Infrastructure.Outbox;

namespace TemplateMs.Infrastructure.UnitOfWork;

/// <summary>
/// Dapper-based unit of work. Manages a single <see cref="SqlTransaction"/> on the command database.
/// On <see cref="CommitAsync"/>: persists outbox messages, commits the transaction, then dispatches domain events.
/// </summary>
public sealed class DapperUnitOfWork : IUnitOfWork, IDisposable
{
    private readonly List<AggregateRoot> _trackedAggregates = [];
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private bool _disposed;

    /// <summary>Gets the open SQL connection associated with the current transaction.</summary>
    public IDbConnection Connection { get; }

    /// <summary>Gets the active SQL transaction.</summary>
    public IDbTransaction Transaction { get; }

    /// <summary>Initializes a new <see cref="DapperUnitOfWork"/> by opening a connection and beginning a transaction.</summary>
    /// <param name="connectionFactory">Factory for creating command database connections.</param>
    /// <param name="dispatcher">Domain event dispatcher.</param>
    /// <param name="dateTimeProvider">Provider for the current UTC timestamp.</param>
    public DapperUnitOfWork(ICommandDbConnectionFactory connectionFactory, IDomainEventDispatcher dispatcher, IDateTimeProvider dateTimeProvider)
    {
        _dispatcher = dispatcher;
        _dateTimeProvider = dateTimeProvider;
        Connection = connectionFactory.CreateConnection();
        Transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
    }

    /// <inheritdoc />
    public void RegisterAggregate(AggregateRoot aggregate) => _trackedAggregates.Add(aggregate);

    /// <inheritdoc />
    public async Task QueueOutboxMessageAsync(string messageType, string payload, CancellationToken cancellationToken = default)
    {
        var record = new OutboxRecord
        {
            Id = Guid.NewGuid(),
            MessageType = messageType,
            Payload = payload,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        await Connection.ExecuteAsync(OutboxSql.Insert, record, Transaction);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Transaction.Commit();
        var aggregates = _trackedAggregates.ToList();
        _trackedAggregates.Clear();
        await _dispatcher.DispatchAsync(aggregates, cancellationToken);
    }

    /// <inheritdoc />
    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        Transaction.Rollback();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Transaction.Dispose();
        Connection.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        Transaction.Dispose();
        Connection.Dispose();
        await Task.CompletedTask;
    }
}
