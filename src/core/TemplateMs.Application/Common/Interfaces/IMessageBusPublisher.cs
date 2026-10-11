namespace TemplateMs.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the integration event message bus (ActiveMQ).
/// Implemented by <c>ActiveMqPublisher</c> in Infrastructure.
/// </summary>
public interface IMessageBusPublisher
{
    /// <summary>Publishes an integration event message to the configured message broker.</summary>
    /// <typeparam name="T">The integration event type.</typeparam>
    /// <param name="message">The integration event to publish.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
