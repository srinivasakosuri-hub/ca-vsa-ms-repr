using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Infrastructure.Configuration;

namespace TemplateMs.Infrastructure.Messaging.Outbox;

/// <summary>
/// Background service that polls the outbox table and publishes pending integration events to ActiveMQ.
/// Runs on a configurable interval and processes records in batches.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private static class LogMessages
    {
        public const string StartingProcessor = "OutboxProcessor starting.";
        public const string StoppingProcessor = "OutboxProcessor stopping.";
        public const string ProcessingBatch = "Processing {Count} outbox message(s).";
        public const string MessagePublished = "Outbox message {Id} ({MessageType}) published successfully.";
        public const string MessageFailed = "Outbox message {Id} ({MessageType}) failed to publish.";
        public const string BatchFailed = "OutboxProcessor batch failed. Retrying after interval.";
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _pollingInterval;

    /// <summary>Initializes a new <see cref="OutboxProcessor"/>.</summary>
    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger,
        IOptions<InfrastructureOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pollingInterval = TimeSpan.FromMilliseconds(options.Value.OutboxPollingIntervalMs);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogMessages.StartingProcessor);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LogMessages.BatchFailed);
            }
        }

        _logger.LogInformation(LogMessages.StoppingProcessor);
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<OutboxStorage>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessageBusPublisher>();

        var records = (await storage.GetUnprocessedAsync()).ToList();
        if (records.Count == 0) return;

        _logger.LogInformation(LogMessages.ProcessingBatch, records.Count);

        foreach (var record in records)
        {
            await PublishRecordAsync(record, storage, publisher, cancellationToken);
        }
    }

    private async Task PublishRecordAsync(
        OutboxRecord record,
        OutboxStorage storage,
        IMessageBusPublisher publisher,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = new OutboxEnvelope(record.MessageType, record.Payload);
            await publisher.PublishAsync(message, cancellationToken);
            await storage.MarkProcessedAsync(record.Id);
            _logger.LogInformation(LogMessages.MessagePublished, record.Id, record.MessageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.MessageFailed, record.Id, record.MessageType);
            await storage.MarkFailedAsync(record.Id, ex.Message);
        }
    }
}

/// <summary>Envelope wrapping an outbox record for transport over ActiveMQ.</summary>
/// <param name="MessageType">The fully qualified integration event type name.</param>
/// <param name="Payload">The JSON-serialized integration event payload.</param>
public sealed record OutboxEnvelope(string MessageType, string Payload);
