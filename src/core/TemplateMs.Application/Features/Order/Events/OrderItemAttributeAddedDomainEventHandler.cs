using Microsoft.Extensions.Logging;
using System.Text.Json;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Events;
using TemplateMs.Domain.Events;

namespace TemplateMs.Application.Features.Order.Events;

/// <summary>
/// WolverineFX domain event handler for <see cref="OrderItemAttributeAddedEvent"/>.
/// Queues an outbox message so the outbox processor can publish the integration event to ActiveMQ.
/// </summary>
public sealed class OrderItemAttributeAddedDomainEventHandler
{
    private static class LogMessages
    {
        public const string HandlingEvent = "Handling OrderItemAttributeAddedEvent for item {OrderItemId}, attribute {OrderItemAttributeId}";
        public const string OutboxQueued = "Outbox message queued for OrderItemAttributeAddedIntegrationEvent, item {OrderItemId}, attribute {OrderItemAttributeId}";
    }

    private static class MessageTypes
    {
        public const string OrderItemAttributeAddedIntegration = "TemplateMs.Application.Events.OrderItemAttributeAddedIntegrationEvent";
    }

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderItemAttributeAddedDomainEventHandler> _logger;

    /// <summary>Initializes a new <see cref="OrderItemAttributeAddedDomainEventHandler"/>.</summary>
    public OrderItemAttributeAddedDomainEventHandler(IUnitOfWork unitOfWork, ILogger<OrderItemAttributeAddedDomainEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="OrderItemAttributeAddedEvent"/>. Creates and queues an
    /// <see cref="OrderItemAttributeAddedIntegrationEvent"/> outbox message for AMQ publishing.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public async Task Handle(OrderItemAttributeAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogMessages.HandlingEvent, domainEvent.OrderItemId, domainEvent.OrderItemAttributeId);

        var integrationEvent = new OrderItemAttributeAddedIntegrationEvent(
            domainEvent.OrderItemId,
            domainEvent.OrderItemAttributeId,
            domainEvent.AttributeName,
            domainEvent.AttributeValue,
            domainEvent.CreatedAt,
            domainEvent.EventId);

        var payload = JsonSerializer.Serialize(integrationEvent);
        await _unitOfWork.QueueOutboxMessageAsync(MessageTypes.OrderItemAttributeAddedIntegration, payload, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(LogMessages.OutboxQueued, domainEvent.OrderItemId, domainEvent.OrderItemAttributeId);
    }
}
