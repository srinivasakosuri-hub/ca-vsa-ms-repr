using FluentAssertions;
using TemplateMs.Domain.Common;
using TemplateMs.Domain.Entities;
using TemplateMs.Domain.Events;
using Xunit;

namespace TemplateMs.Domain.Tests;

/// <summary>Unit tests for domain event infrastructure (raising, clearing, and record structure).</summary>
public sealed class DomainEventTests
{
    private static readonly DateTime FixedNow = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void DomainEventHasUniqueEventId()
    {
        var order1 = Order.Create("Alice", FixedNow, FixedNow);
        order1.RaiseCreatedEvent(FixedNow);
        var order2 = Order.Create("Bob", FixedNow, FixedNow);
        order2.RaiseCreatedEvent(FixedNow);

        order1.DomainEvents[0].EventId.Should().NotBe(order2.DomainEvents[0].EventId);
    }

    [Fact]
    public void DomainEventOccurredAtIsSet()
    {
        var before = DateTime.UtcNow;
        var order = Order.Create("Alice", FixedNow, FixedNow);
        order.RaiseCreatedEvent(FixedNow);
        var after = DateTime.UtcNow;
        var evt = order.DomainEvents[0];

        evt.OccurredAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void AggregateRootInitiallyHasNoDomainEventsAfterClear()
    {
        var order = Order.Create("Alice", FixedNow, FixedNow);
        order.ClearDomainEvents();
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddItemRaisesOrderItemAddedEvent()
    {
        var order = Order.Create("Alice", FixedNow, FixedNow);
        order.ClearDomainEvents();

        var item = order.AddItem("Widget", 2, 9.99m, FixedNow.AddMinutes(1));
        order.RaiseItemAddedEvent(item);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderItemAddedEvent>();
    }

    [Fact]
    public void RemoveItemRaisesOrderItemRemovedEvent()
    {
        var order = Order.Create("Alice", FixedNow, FixedNow);
        var item = order.AddItem("Widget", 2, 9.99m, FixedNow.AddMinutes(1));
        order.ClearDomainEvents();

        order.RemoveItem(item.Id, FixedNow.AddMinutes(2));

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderItemRemovedEvent>();
    }

    [Fact]
    public void OrderCreatedEventIsOfTypeDomainEvent()
    {
        var order = Order.Create("Alice", FixedNow, FixedNow);
        order.RaiseCreatedEvent(FixedNow);
        order.DomainEvents[0].Should().BeAssignableTo<DomainEvent>();
    }

    [Fact]
    public void DomainEventsAreReadOnlyCannotBeModifiedExternally()
    {
        var order = Order.Create("Alice", FixedNow, FixedNow);
        order.DomainEvents.Should().BeAssignableTo<IReadOnlyList<DomainEvent>>();
    }

    [Fact]
    public void UpdateRaisesOrderUpdatedEvent()
    {
        var order = Order.Create("Alice", FixedNow, FixedNow);
        order.ClearDomainEvents();

        order.Update("Alice Updated", FixedNow.AddDays(1), FixedNow.AddMinutes(1));

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderUpdatedEvent>();
    }

    [Fact]
    public void DeactivateRaisesOrderDeletedEvent()
    {
        var order = Order.Create("Alice", FixedNow, FixedNow);
        order.ClearDomainEvents();

        order.Deactivate(FixedNow.AddMinutes(1));

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderDeletedEvent>();
    }

    [Fact]
    public void AddAttributeToItemRaisesOrderItemAttributeAddedEvent()
    {
        var order = Order.Create("Alice", FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        order.ClearDomainEvents();

        var attr = order.AddAttributeToItem(item.Id, "Color", "Blue", FixedNow.AddMinutes(2));
        order.RaiseAttributeAddedEvent(item.Id, attr);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderItemAttributeAddedEvent>();
    }

    [Fact]
    public void RemoveAttributeFromItemRaisesOrderItemAttributeRemovedEvent()
    {
        var order = Order.Create("Alice", FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        var attr = order.AddAttributeToItem(item.Id, "Color", "Blue", FixedNow.AddMinutes(2));
        order.ClearDomainEvents();

        order.RemoveAttributeFromItem(item.Id, attr.Id, FixedNow.AddMinutes(3));

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderItemAttributeRemovedEvent>();
    }
}
