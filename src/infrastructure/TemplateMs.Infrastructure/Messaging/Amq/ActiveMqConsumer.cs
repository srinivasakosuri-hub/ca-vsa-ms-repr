using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TemplateMs.Infrastructure.Messaging.Amq;

/// <summary>
/// Background consumer that subscribes to ActiveMQ topics and routes received messages
/// through the inbox pattern for idempotent processing.
/// Extend this class to add specific topic subscriptions per feature.
/// </summary>
public sealed class ActiveMqConsumer : BackgroundService
{
    private static class LogMessages
    {
        public const string Starting = "ActiveMqConsumer starting, connecting to {BrokerUrl}";
        public const string Stopping = "ActiveMqConsumer stopping.";
        public const string MessageReceived = "Received message of type {MessageType} from topic {Topic}";
        public const string ConnectionFailed = "ActiveMqConsumer failed to connect to broker.";
    }

    private readonly ActiveMqConfiguration _config;
    private readonly ILogger<ActiveMqConsumer> _logger;

    /// <summary>Initializes a new <see cref="ActiveMqConsumer"/>.</summary>
    public ActiveMqConsumer(IOptions<ActiveMqConfiguration> config, ILogger<ActiveMqConsumer> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogMessages.Starting, _config.BrokerUrl);

        try
        {
            // The consumer connection lifecycle is managed here.
            // Add topic subscriptions by creating IMessageConsumer instances within this scope.
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during host shutdown — stoppingToken was cancelled.
        }

        _logger.LogInformation(LogMessages.Stopping);
    }
}
