using Microsoft.Extensions.Logging;
using NSubstitute;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Events;
using TemplateMs.Domain.Events;
using Xunit;

namespace TemplateMs.Application.Tests;

/// <summary>Unit tests for all Order domain event handlers (outbox-queuing pipeline).</summary>
public sealed class OrderDomainEventHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly DateTime FixedNow = new(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedOrderDate = new(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc);

    // OrderDomainEventHandler (OrderCreatedEvent)            

    [Fact]
    public async Task OrderCreatedHandleQueuesOutboxMessage()
    {
        var handler = new OrderDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderDomainEventHandler>>());
        var domainEvent = new OrderCreatedEvent(1, "Jane Doe", FixedOrderDate, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).QueueOutboxMessageAsync(
            "TemplateMs.Application.Events.OrderCreatedIntegrationEvent",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrderCreatedHandleCommitsUnitOfWork()
    {
        var handler = new OrderDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderDomainEventHandler>>());
        var domainEvent = new OrderCreatedEvent(1, "Jane Doe", FixedOrderDate, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // OrderItemAddedDomainEventHandler                 

    [Fact]
    public async Task OrderItemAddedHandleQueuesOutboxMessage()
    {
        var handler = new OrderItemAddedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderItemAddedDomainEventHandler>>());
        var domainEvent = new OrderItemAddedEvent(1, 10, "Widget", 2, 9.99m, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).QueueOutboxMessageAsync(
            "TemplateMs.Application.Events.OrderItemAddedIntegrationEvent",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrderItemAddedHandleCommitsUnitOfWork()
    {
        var handler = new OrderItemAddedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderItemAddedDomainEventHandler>>());
        var domainEvent = new OrderItemAddedEvent(1, 10, "Widget", 2, 9.99m, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // OrderItemRemovedDomainEventHandler                ─

    [Fact]
    public async Task OrderItemRemovedHandleQueuesOutboxMessage()
    {
        var handler = new OrderItemRemovedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderItemRemovedDomainEventHandler>>());
        var domainEvent = new OrderItemRemovedEvent(1, 10, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).QueueOutboxMessageAsync(
            "TemplateMs.Application.Events.OrderItemRemovedIntegrationEvent",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrderItemRemovedHandleCommitsUnitOfWork()
    {
        var handler = new OrderItemRemovedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderItemRemovedDomainEventHandler>>());
        var domainEvent = new OrderItemRemovedEvent(1, 10, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // OrderItemAttributeAddedDomainEventHandler             

    [Fact]
    public async Task OrderItemAttributeAddedHandleQueuesOutboxMessage()
    {
        var handler = new OrderItemAttributeAddedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderItemAttributeAddedDomainEventHandler>>());
        var domainEvent = new OrderItemAttributeAddedEvent(10, 100, "Color", "Blue", FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).QueueOutboxMessageAsync(
            "TemplateMs.Application.Events.OrderItemAttributeAddedIntegrationEvent",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrderItemAttributeAddedHandleCommitsUnitOfWork()
    {
        var handler = new OrderItemAttributeAddedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderItemAttributeAddedDomainEventHandler>>());
        var domainEvent = new OrderItemAttributeAddedEvent(10, 100, "Color", "Blue", FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // OrderItemAttributeRemovedDomainEventHandler            

    [Fact]
    public async Task OrderItemAttributeRemovedHandleQueuesOutboxMessage()
    {
        var handler = new OrderItemAttributeRemovedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderItemAttributeRemovedDomainEventHandler>>());
        var domainEvent = new OrderItemAttributeRemovedEvent(10, 100, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).QueueOutboxMessageAsync(
            "TemplateMs.Application.Events.OrderItemAttributeRemovedIntegrationEvent",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrderItemAttributeRemovedHandleCommitsUnitOfWork()
    {
        var handler = new OrderItemAttributeRemovedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderItemAttributeRemovedDomainEventHandler>>());
        var domainEvent = new OrderItemAttributeRemovedEvent(10, 100, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // OrderUpdatedDomainEventHandler                  ─

    [Fact]
    public async Task OrderUpdatedHandleQueuesOutboxMessage()
    {
        var handler = new OrderUpdatedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderUpdatedDomainEventHandler>>());
        var domainEvent = new OrderUpdatedEvent(1, "Jane Doe", FixedOrderDate, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).QueueOutboxMessageAsync(
            "TemplateMs.Application.Events.OrderUpdatedIntegrationEvent",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrderUpdatedHandleCommitsUnitOfWork()
    {
        var handler = new OrderUpdatedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderUpdatedDomainEventHandler>>());
        var domainEvent = new OrderUpdatedEvent(1, "Jane Doe", FixedOrderDate, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // OrderDeletedDomainEventHandler                  ─

    [Fact]
    public async Task OrderDeletedHandleQueuesOutboxMessage()
    {
        var handler = new OrderDeletedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderDeletedDomainEventHandler>>());
        var domainEvent = new OrderDeletedEvent(1, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).QueueOutboxMessageAsync(
            "TemplateMs.Application.Events.OrderDeletedIntegrationEvent",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrderDeletedHandleCommitsUnitOfWork()
    {
        var handler = new OrderDeletedDomainEventHandler(_unitOfWork,
            Substitute.For<ILogger<OrderDeletedDomainEventHandler>>());
        var domainEvent = new OrderDeletedEvent(1, FixedNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
