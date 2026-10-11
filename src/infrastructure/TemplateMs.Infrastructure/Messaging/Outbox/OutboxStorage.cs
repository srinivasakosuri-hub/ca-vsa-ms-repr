using Dapper;
using Microsoft.Extensions.Options;
using System.Data;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Infrastructure.Configuration;
using TemplateMs.Sql.Infrastructure.Outbox;

namespace TemplateMs.Infrastructure.Messaging.Outbox;

/// <summary>
/// Reads <see cref="OutboxRecord"/> rows from the database and delegates them to
/// <see cref="OutboxProcessor"/> for publishing to ActiveMQ.
/// This class is responsible solely for data access; <see cref="OutboxProcessor"/> orchestrates the flow.
/// </summary>
public sealed class OutboxStorage
{
    private readonly ICommandDbConnectionFactory _connectionFactory;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly int _batchSize;

    /// <summary>Initializes a new <see cref="OutboxStorage"/>.</summary>
    public OutboxStorage(ICommandDbConnectionFactory connectionFactory, IOptions<InfrastructureOptions> options, IDateTimeProvider dateTimeProvider)
    {
        _connectionFactory = connectionFactory;
        _dateTimeProvider = dateTimeProvider;
        _batchSize = options.Value.OutboxBatchSize;
    }

    /// <summary>Returns unprocessed outbox records up to the configured batch size.</summary>
    public async Task<IEnumerable<OutboxRecord>> GetUnprocessedAsync()
    {
        using IDbConnection connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<OutboxRecord>(
            OutboxSql.SelectUnprocessed,
            new { BatchSize = _batchSize });
    }

    /// <summary>Marks the given record as successfully processed.</summary>
    public async Task MarkProcessedAsync(Guid messageId)
    {
        using IDbConnection connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(OutboxSql.MarkProcessed, new { Id = messageId, ProcessedAt = _dateTimeProvider.UtcNow });
    }

    /// <summary>Records an error against the given outbox record.</summary>
    public async Task MarkFailedAsync(Guid messageId, string error)
    {
        using IDbConnection connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(OutboxSql.MarkFailed, new { Id = messageId, Error = error });
    }
}
