using Microsoft.Extensions.Logging;
using System.Text.Json;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Events;
using TemplateMs.Domain.Events;

namespace TemplateMs.Application.Features.Order.Events;

/// <summary>
/// WolverineFX domain event handler for <see cref="OrderItemAttributeRemovedEvent"/>.
/// Queues an outbox message so the outbox processor can publish the integration event to ActiveMQ.
/// </summary>
public sealed class OrderItemAttributeRemovedDomainEventHandler
{
    private static class LogMessages
    {
        public const string HandlingEvent = "Handling OrderItemAttributeRemovedEvent for item {OrderItemId}, attribute {OrderItemAttributeId}";
        public const string OutboxQueued = "Outbox message queued for OrderItemAttributeRemovedIntegrationEvent, item {OrderItemId}, attribute {OrderItemAttributeId}";
    }

    private static class MessageTypes
    {
        public const string OrderItemAttributeRemovedIntegration = "TemplateMs.Application.Events.OrderItemAttributeRemovedIntegrationEvent";
    }

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderItemAttributeRemovedDomainEventHandler> _logger;

    /// <summary>Initializes a new <see cref="OrderItemAttributeRemovedDomainEventHandler"/>.</summary>
    public OrderItemAttributeRemovedDomainEventHandler(IUnitOfWork unitOfWork, ILogger<OrderItemAttributeRemovedDomainEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="OrderItemAttributeRemovedEvent"/>. Creates and queues an
    /// <see cref="OrderItemAttributeRemovedIntegrationEvent"/> outbox message for AMQ publishing.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public async Task Handle(OrderItemAttributeRemovedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogMessages.HandlingEvent, domainEvent.OrderItemId, domainEvent.OrderItemAttributeId);

        var integrationEvent = new OrderItemAttributeRemovedIntegrationEvent(
            domainEvent.OrderItemId,
            domainEvent.OrderItemAttributeId,
            domainEvent.RemovedAt,
            domainEvent.EventId);

        var payload = JsonSerializer.Serialize(integrationEvent);
        await _unitOfWork.QueueOutboxMessageAsync(MessageTypes.OrderItemAttributeRemovedIntegration, payload, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(LogMessages.OutboxQueued, domainEvent.OrderItemId, domainEvent.OrderItemAttributeId);
    }
}
