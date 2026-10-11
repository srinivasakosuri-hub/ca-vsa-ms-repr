using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TemplateMs.Infrastructure.Messaging.Inbox;

/// <summary>
/// Background service that processes received integration events from the inbox table.
/// Ensures idempotent handling of messages consumed from ActiveMQ.
/// </summary>
public sealed class InboxProcessor : BackgroundService
{
    private static class LogMessages
    {
        public const string Starting = "InboxProcessor starting.";
        public const string Stopping = "InboxProcessor stopping.";
    }

    private readonly ILogger<InboxProcessor> _logger;

    /// <summary>Initializes a new <see cref="InboxProcessor"/>.</summary>
    public InboxProcessor(ILogger<InboxProcessor> logger) => _logger = logger;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogMessages.Starting);

        try
        {
            // Inbox processing runs while the consumer is active.
            // Extend this service with inbox table polling when inbound AMQ consumers are registered.
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during host shutdown — stoppingToken was cancelled.
        }

        _logger.LogInformation(LogMessages.Stopping);
    }
}
