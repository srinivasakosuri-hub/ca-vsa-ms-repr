using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TemplateMs.Application.Common.Interfaces;

namespace TemplateMs.Infrastructure.Messaging.Amq;

/// <summary>
/// Publishes integration events to an ActiveMQ topic using Apache.NMS.
/// Implements <see cref="IMessageBusPublisher"/> as the single outbound gateway to ActiveMQ.
/// </summary>
public sealed class ActiveMqPublisher : IMessageBusPublisher, IAsyncDisposable
{
    private static class LogMessages
    {
        public const string Publishing = "Publishing message {MessageType} to queue {Queue}";
        public const string Published = "Message {MessageType} published successfully to queue {Queue}";
        public const string PublishFailed = "Failed to publish message {MessageType} to queue {Queue}";
    }

    private readonly ActiveMqConfiguration _config;
    private readonly ILogger<ActiveMqPublisher> _logger;
    private bool _disposed;

    /// <summary>Initializes a new <see cref="ActiveMqPublisher"/>.</summary>
    public ActiveMqPublisher(IOptions<ActiveMqConfiguration> config, ILogger<ActiveMqPublisher> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var queueName = _config.OrderEventsQueue;
        var messageType = typeof(T).FullName ?? typeof(T).Name;

        _logger.LogInformation(LogMessages.Publishing, messageType, queueName);

        var factory = new ConnectionFactory(_config.BrokerUrl);
        using var connection = await Task.Run(() => factory.CreateConnection(_config.Username, _config.Password), cancellationToken);
        connection.Start();

        using var session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        var destination = session.GetQueue(queueName);
        using var producer = session.CreateProducer(destination);

        var json = JsonSerializer.Serialize(message);
        var nmsMessage = session.CreateTextMessage(json);
        nmsMessage.Properties.SetString("MessageType", messageType);

        producer.Send(nmsMessage);

        _logger.LogInformation(LogMessages.Published, messageType, queueName);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await Task.CompletedTask;
    }
}
