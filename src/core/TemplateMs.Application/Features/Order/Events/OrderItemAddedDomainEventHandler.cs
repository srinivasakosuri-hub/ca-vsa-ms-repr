using Microsoft.Extensions.Logging;
using System.Text.Json;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Events;
using TemplateMs.Domain.Events;

namespace TemplateMs.Application.Features.Order.Events;

/// <summary>
/// WolverineFX domain event handler for <see cref="OrderItemAddedEvent"/>.
/// Queues an outbox message so the outbox processor can publish the integration event to ActiveMQ.
/// </summary>
public sealed class OrderItemAddedDomainEventHandler
{
    private static class LogMessages
    {
        public const string HandlingEvent = "Handling OrderItemAddedEvent for order {OrderId}, item {OrderItemId}";
        public const string OutboxQueued = "Outbox message queued for OrderItemAddedIntegrationEvent, order {OrderId}, item {OrderItemId}";
    }

    private static class MessageTypes
    {
        public const string OrderItemAddedIntegration = "TemplateMs.Application.Events.OrderItemAddedIntegrationEvent";
    }

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderItemAddedDomainEventHandler> _logger;

    /// <summary>Initializes a new <see cref="OrderItemAddedDomainEventHandler"/>.</summary>
    public OrderItemAddedDomainEventHandler(IUnitOfWork unitOfWork, ILogger<OrderItemAddedDomainEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="OrderItemAddedEvent"/>. Creates and queues an
    /// <see cref="OrderItemAddedIntegrationEvent"/> outbox message for AMQ publishing.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public async Task Handle(OrderItemAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogMessages.HandlingEvent, domainEvent.OrderId, domainEvent.OrderItemId);

        var integrationEvent = new OrderItemAddedIntegrationEvent(
            domainEvent.OrderId,
            domainEvent.OrderItemId,
            domainEvent.ProductName,
            domainEvent.Quantity,
            domainEvent.UnitPrice,
            domainEvent.CreatedAt,
            domainEvent.EventId);

        var payload = JsonSerializer.Serialize(integrationEvent);
        await _unitOfWork.QueueOutboxMessageAsync(MessageTypes.OrderItemAddedIntegration, payload, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(LogMessages.OutboxQueued, domainEvent.OrderId, domainEvent.OrderItemId);
    }
}
