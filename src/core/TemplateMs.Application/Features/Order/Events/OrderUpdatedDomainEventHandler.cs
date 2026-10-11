using Microsoft.Extensions.Logging;
using System.Text.Json;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Events;
using TemplateMs.Domain.Events;

namespace TemplateMs.Application.Features.Order.Events;

/// <summary>
/// WolverineFX domain event handler for <see cref="OrderUpdatedEvent"/>.
/// Queues an outbox message so the outbox processor can publish the integration event to ActiveMQ.
/// </summary>
public sealed class OrderUpdatedDomainEventHandler
{
    private static class LogMessages
    {
        public const string HandlingEvent = "Handling OrderUpdatedEvent for order {OrderId}";
        public const string OutboxQueued = "Outbox message queued for OrderUpdatedIntegrationEvent, order {OrderId}";
    }

    private static class MessageTypes
    {
        public const string OrderUpdatedIntegration = "TemplateMs.Application.Events.OrderUpdatedIntegrationEvent";
    }

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderUpdatedDomainEventHandler> _logger;

    /// <summary>Initializes a new <see cref="OrderUpdatedDomainEventHandler"/>.</summary>
    public OrderUpdatedDomainEventHandler(IUnitOfWork unitOfWork, ILogger<OrderUpdatedDomainEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="OrderUpdatedEvent"/>. Creates and queues an
    /// <see cref="OrderUpdatedIntegrationEvent"/> outbox message for AMQ publishing.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public async Task Handle(OrderUpdatedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogMessages.HandlingEvent, domainEvent.OrderId);

        var integrationEvent = new OrderUpdatedIntegrationEvent(
            domainEvent.OrderId,
            domainEvent.CustomerName,
            domainEvent.OrderDate,
            domainEvent.UpdatedAt,
            domainEvent.EventId);

        var payload = JsonSerializer.Serialize(integrationEvent);
        await _unitOfWork.QueueOutboxMessageAsync(MessageTypes.OrderUpdatedIntegration, payload, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(LogMessages.OutboxQueued, domainEvent.OrderId);
    }
}
