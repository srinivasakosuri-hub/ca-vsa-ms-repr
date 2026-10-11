using Microsoft.Extensions.Logging;
using System.Text.Json;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Events;
using TemplateMs.Domain.Events;

namespace TemplateMs.Application.Features.Order.Events;

/// <summary>
/// WolverineFX domain event handler for <see cref="OrderItemRemovedEvent"/>.
/// Queues an outbox message so the outbox processor can publish the integration event to ActiveMQ.
/// </summary>
public sealed class OrderItemRemovedDomainEventHandler
{
    private static class LogMessages
    {
        public const string HandlingEvent = "Handling OrderItemRemovedEvent for order {OrderId}, item {OrderItemId}";
        public const string OutboxQueued = "Outbox message queued for OrderItemRemovedIntegrationEvent, order {OrderId}, item {OrderItemId}";
    }

    private static class MessageTypes
    {
        public const string OrderItemRemovedIntegration = "TemplateMs.Application.Events.OrderItemRemovedIntegrationEvent";
    }

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderItemRemovedDomainEventHandler> _logger;

    /// <summary>Initializes a new <see cref="OrderItemRemovedDomainEventHandler"/>.</summary>
    public OrderItemRemovedDomainEventHandler(IUnitOfWork unitOfWork, ILogger<OrderItemRemovedDomainEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="OrderItemRemovedEvent"/>. Creates and queues an
    /// <see cref="OrderItemRemovedIntegrationEvent"/> outbox message for AMQ publishing.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public async Task Handle(OrderItemRemovedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogMessages.HandlingEvent, domainEvent.OrderId, domainEvent.OrderItemId);

        var integrationEvent = new OrderItemRemovedIntegrationEvent(
            domainEvent.OrderId,
            domainEvent.OrderItemId,
            domainEvent.RemovedAt,
            domainEvent.EventId);

        var payload = JsonSerializer.Serialize(integrationEvent);
        await _unitOfWork.QueueOutboxMessageAsync(MessageTypes.OrderItemRemovedIntegration, payload, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(LogMessages.OutboxQueued, domainEvent.OrderId, domainEvent.OrderItemId);
    }
}
