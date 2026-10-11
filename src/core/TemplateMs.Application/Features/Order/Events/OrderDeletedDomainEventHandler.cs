using Microsoft.Extensions.Logging;
using System.Text.Json;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Events;
using TemplateMs.Domain.Events;

namespace TemplateMs.Application.Features.Order.Events;

/// <summary>
/// WolverineFX domain event handler for <see cref="OrderDeletedEvent"/>.
/// Queues an outbox message so the outbox processor can publish the integration event to ActiveMQ.
/// </summary>
public sealed class OrderDeletedDomainEventHandler
{
    private static class LogMessages
    {
        public const string HandlingEvent = "Handling OrderDeletedEvent for order {OrderId}";
        public const string OutboxQueued = "Outbox message queued for OrderDeletedIntegrationEvent, order {OrderId}";
    }

    private static class MessageTypes
    {
        public const string OrderDeletedIntegration = "TemplateMs.Application.Events.OrderDeletedIntegrationEvent";
    }

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderDeletedDomainEventHandler> _logger;

    /// <summary>Initializes a new <see cref="OrderDeletedDomainEventHandler"/>.</summary>
    public OrderDeletedDomainEventHandler(IUnitOfWork unitOfWork, ILogger<OrderDeletedDomainEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="OrderDeletedEvent"/>. Creates and queues an
    /// <see cref="OrderDeletedIntegrationEvent"/> outbox message for AMQ publishing.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public async Task Handle(OrderDeletedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogMessages.HandlingEvent, domainEvent.OrderId);

        var integrationEvent = new OrderDeletedIntegrationEvent(
            domainEvent.OrderId,
            domainEvent.DeletedAt,
            domainEvent.EventId);

        var payload = JsonSerializer.Serialize(integrationEvent);
        await _unitOfWork.QueueOutboxMessageAsync(MessageTypes.OrderDeletedIntegration, payload, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(LogMessages.OutboxQueued, domainEvent.OrderId);
    }
}
