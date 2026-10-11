using FluentAssertions;
using TemplateMs.Domain.Entities;
using TemplateMs.Domain.Events;
using TemplateMs.Domain.Exceptions;
using Xunit;

namespace TemplateMs.Domain.Tests;

/// <summary>Unit tests for the <see cref="Order"/> aggregate root.</summary>
public sealed class OrderTests
{
    private static readonly DateTime FixedNow = new(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private const string ValidCustomerName = "Jane Doe";

    // Order.Create 

    [Fact]
    public void CreateOrderWithValidInputsSetsPropertiesCorrectly()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);

        order.Id.Should().BeGreaterThanOrEqualTo(0);
        order.CustomerName.Should().Be(ValidCustomerName);
        order.OrderDate.Should().Be(FixedNow);
        order.IsActive.Should().BeTrue();
        order.CreatedAt.Should().Be(FixedNow);
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void CreateOrderWithValidInputsRaisesOrderCreatedEvent()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        order.RaiseCreatedEvent(FixedNow);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedEvent>();
    }

    [Fact]
    public void CreateOrderWithEmptyCustomerNameThrowsDomainException()
    {
        var act = () => Order.Create(string.Empty, FixedNow, FixedNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateOrderWithWhitespaceCustomerNameThrowsDomainException()
    {
        var act = () => Order.Create("   ", FixedNow, FixedNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void OrderCreatedEventContainsCorrectData()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        order.RaiseCreatedEvent(FixedNow);
        var evt = order.DomainEvents.OfType<OrderCreatedEvent>().Single();

        evt.OrderId.Should().Be(order.Id);
        evt.CustomerName.Should().Be(ValidCustomerName);
        evt.OrderDate.Should().Be(FixedNow);
    }

    [Fact]
    public void TwoOrdersCreatedSeparatelyAreNotSameReference()
    {
        var order1 = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var order2 = Order.Create(ValidCustomerName, FixedNow, FixedNow);

        order1.Should().NotBeSameAs(order2);
    }

    // Order.AddItem─

    [Fact]
    public void AddItemWithValidInputsAddsItemToOrder()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);

        order.AddItem("Widget", 3, 10m, FixedNow.AddMinutes(1));

        order.Items.Should().ContainSingle(i => i.ProductName == "Widget");
    }

    [Fact]
    public void AddItemWithValidInputsRaisesOrderItemAddedEvent()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        order.ClearDomainEvents();

        var item = order.AddItem("Widget", 3, 10m, FixedNow.AddMinutes(1));
        order.RaiseItemAddedEvent(item);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderItemAddedEvent>();
    }

    [Fact]
    public void AddItemWithZeroQuantityThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var act = () => order.AddItem("Widget", 0, 10m, FixedNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddItemWithNegativeUnitPriceThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var act = () => order.AddItem("Widget", 1, -1m, FixedNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddItemWithEmptyProductNameThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var act = () => order.AddItem(string.Empty, 1, 10m, FixedNow);
        act.Should().Throw<DomainException>();
    }

    // Order.RemoveItem ─

    [Fact]
    public void RemoveItemWhenItemExistsSetsItemInactive()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));

        order.RemoveItem(item.Id, FixedNow.AddMinutes(2));

        order.Items.First(i => i.Id == item.Id).IsActive.Should().BeFalse();
    }

    [Fact]
    public void RemoveItemWhenItemExistsRaisesOrderItemRemovedEvent()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        order.ClearDomainEvents();

        order.RemoveItem(item.Id, FixedNow.AddMinutes(2));

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderItemRemovedEvent>();
    }

    [Fact]
    public void RemoveItemWhenItemNotFoundThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var act = () => order.RemoveItem(1, FixedNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RemoveItemWhenAlreadyInactiveThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        order.RemoveItem(item.Id, FixedNow.AddMinutes(2));

        var act = () => order.RemoveItem(item.Id, FixedNow.AddMinutes(3));
        act.Should().Throw<DomainException>();
    }

    // Order.Update 

    [Fact]
    public void UpdateWithValidInputsUpdatesProperties()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var newDate = FixedNow.AddDays(1);
        order.ClearDomainEvents();

        order.Update("New Name", newDate, FixedNow.AddMinutes(5));

        order.CustomerName.Should().Be("New Name");
        order.OrderDate.Should().Be(newDate);
    }

    [Fact]
    public void UpdateWithValidInputsRaisesOrderUpdatedEvent()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        order.ClearDomainEvents();

        order.Update("New Name", FixedNow.AddDays(1), FixedNow.AddMinutes(5));

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderUpdatedEvent>();
    }

    [Fact]
    public void UpdateWithEmptyCustomerNameThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var act = () => order.Update(string.Empty, FixedNow, FixedNow.AddMinutes(1));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateEventContainsCorrectData()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var newDate = FixedNow.AddDays(2);
        var updatedAt = FixedNow.AddMinutes(10);
        order.ClearDomainEvents();

        order.Update("Updated Name", newDate, updatedAt);

        var evt = order.DomainEvents.OfType<OrderUpdatedEvent>().Single();
        evt.OrderId.Should().Be(order.Id);
        evt.CustomerName.Should().Be("Updated Name");
        evt.OrderDate.Should().Be(newDate);
        evt.UpdatedAt.Should().Be(updatedAt);
    }

    // Order.Deactivate ─

    [Fact]
    public void DeactivateSetsIsActiveFalse()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        order.ClearDomainEvents();

        order.Deactivate(FixedNow.AddMinutes(1));

        order.IsActive.Should().BeFalse();
    }

    [Fact]
    public void DeactivateRaisesOrderDeletedEvent()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        order.ClearDomainEvents();

        order.Deactivate(FixedNow.AddMinutes(1));

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderDeletedEvent>();
    }

    [Fact]
    public void DeactivateWhenAlreadyInactiveThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        order.Deactivate(FixedNow.AddMinutes(1));

        var act = () => order.Deactivate(FixedNow.AddMinutes(2));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void DeactivateEventContainsCorrectData()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var deletedAt = FixedNow.AddMinutes(5);
        order.ClearDomainEvents();

        order.Deactivate(deletedAt);

        var evt = order.DomainEvents.OfType<OrderDeletedEvent>().Single();
        evt.OrderId.Should().Be(order.Id);
        evt.DeletedAt.Should().Be(deletedAt);
    }

    // Order.AddAttributeToItem 

    [Fact]
    public void AddAttributeToItemWithValidInputsAddsAttributeToItem()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        order.ClearDomainEvents();

        order.AddAttributeToItem(item.Id, "Color", "Blue", FixedNow.AddMinutes(2));

        item.Attributes.Should().ContainSingle(a => a.AttributeName == "Color" && a.AttributeValue == "Blue");
    }

    [Fact]
    public void AddAttributeToItemRaisesOrderItemAttributeAddedEvent()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        order.ClearDomainEvents();

        var attr = order.AddAttributeToItem(item.Id, "Color", "Blue", FixedNow.AddMinutes(2));
        order.RaiseAttributeAddedEvent(item.Id, attr);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderItemAttributeAddedEvent>();
    }

    [Fact]
    public void AddAttributeToItemWhenItemNotFoundThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var act = () => order.AddAttributeToItem(999, "Color", "Blue", FixedNow.AddMinutes(1));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddAttributeToItemEventContainsCorrectData()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        order.ClearDomainEvents();
        var createdAt = FixedNow.AddMinutes(2);

        var attr = order.AddAttributeToItem(item.Id, "Size", "Large", createdAt);
        order.RaiseAttributeAddedEvent(item.Id, attr);

        var evt = order.DomainEvents.OfType<OrderItemAttributeAddedEvent>().Single();
        evt.OrderItemId.Should().Be(item.Id);
        evt.OrderItemAttributeId.Should().Be(attr.Id);
        evt.AttributeName.Should().Be("Size");
        evt.AttributeValue.Should().Be("Large");
        evt.CreatedAt.Should().Be(createdAt);
    }

    // Order.RemoveAttributeFromItem                     

    [Fact]
    public void RemoveAttributeFromItemSetsAttributeInactive()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        var attr = order.AddAttributeToItem(item.Id, "Color", "Blue", FixedNow.AddMinutes(2));
        order.ClearDomainEvents();

        order.RemoveAttributeFromItem(item.Id, attr.Id, FixedNow.AddMinutes(3));

        item.Attributes.First(a => a.Id == attr.Id).IsActive.Should().BeFalse();
    }

    [Fact]
    public void RemoveAttributeFromItemRaisesOrderItemAttributeRemovedEvent()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        var attr = order.AddAttributeToItem(item.Id, "Color", "Blue", FixedNow.AddMinutes(2));
        order.ClearDomainEvents();

        order.RemoveAttributeFromItem(item.Id, attr.Id, FixedNow.AddMinutes(3));

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderItemAttributeRemovedEvent>();
    }

    [Fact]
    public void RemoveAttributeFromItemWhenItemNotFoundThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var act = () => order.RemoveAttributeFromItem(999, 1, FixedNow.AddMinutes(1));
        act.Should().Throw<DomainException>();
    }

    // OrderItem.AddAttribute (direct)                   ─

    [Fact]
    public void AddAttributeWithValidInputsAddsAttributeToItem()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));

        item.AddAttribute("Color", "Blue", FixedNow.AddMinutes(2));

        item.Attributes.Should().ContainSingle(a => a.AttributeName == "Color" && a.AttributeValue == "Blue");
    }

    [Fact]
    public void AddAttributeWithEmptyNameThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));

        var act = () => item.AddAttribute(string.Empty, "Blue", FixedNow.AddMinutes(2));
        act.Should().Throw<DomainException>();
    }

    // OrderItem.RemoveAttribute (direct)                  ─

    [Fact]
    public void RemoveAttributeWhenExistsSetsAttributeInactive()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        var attr = item.AddAttribute("Color", "Blue", FixedNow.AddMinutes(2));

        item.RemoveAttribute(attr.Id, FixedNow.AddMinutes(3));

        item.Attributes.First(a => a.Id == attr.Id).IsActive.Should().BeFalse();
    }

    [Fact]
    public void RemoveAttributeWhenNotFoundThrowsDomainException()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));

        var act = () => item.RemoveAttribute(999, FixedNow.AddMinutes(2));
        act.Should().Throw<DomainException>();
    }

    // Misc

    [Fact]
    public void ClearDomainEventsRemovesAllEvents()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        order.RaiseCreatedEvent(FixedNow);
        order.DomainEvents.Should().NotBeEmpty();

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void OrderItemTotalAmountIsQuantityTimesUnitPrice()
    {
        var order = Order.Create(ValidCustomerName, FixedNow, FixedNow);
        var item = order.AddItem("Widget", 4, 2.50m, FixedNow.AddMinutes(1));

        item.TotalAmount.Should().Be(10m);
    }
}
