using Microsoft.Extensions.Logging;
using System.Text.Json;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Events;
using TemplateMs.Domain.Events;

namespace TemplateMs.Application.Features.Order.Events;

/// <summary>
/// WolverineFX domain event handler for <see cref="OrderCreatedEvent"/>.
/// Queues an outbox message so the outbox processor can publish the integration event to ActiveMQ.
/// </summary>
public sealed class OrderDomainEventHandler
{
    private static class LogMessages
    {
        public const string HandlingEvent = "Handling OrderCreatedEvent for order {OrderId}";
        public const string OutboxQueued = "Outbox message queued for OrderCreatedIntegrationEvent, order {OrderId}";
    }

    private static class MessageTypes
    {
        public const string OrderCreatedIntegration = "TemplateMs.Application.Events.OrderCreatedIntegrationEvent";
    }

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderDomainEventHandler> _logger;

    /// <summary>Initializes a new <see cref="OrderDomainEventHandler"/>.</summary>
    public OrderDomainEventHandler(IUnitOfWork unitOfWork, ILogger<OrderDomainEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="OrderCreatedEvent"/>. Creates and queues an
    /// <see cref="OrderCreatedIntegrationEvent"/> outbox message for AMQ publishing.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public async Task Handle(OrderCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogMessages.HandlingEvent, domainEvent.OrderId);

        var integrationEvent = new OrderCreatedIntegrationEvent(
            domainEvent.OrderId,
            domainEvent.CustomerName,
            domainEvent.OrderDate,
            domainEvent.CreatedAt,
            domainEvent.EventId);

        var payload = JsonSerializer.Serialize(integrationEvent);
        await _unitOfWork.QueueOutboxMessageAsync(MessageTypes.OrderCreatedIntegration, payload, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(LogMessages.OutboxQueued, domainEvent.OrderId);
    }
}
