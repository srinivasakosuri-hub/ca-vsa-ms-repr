using TemplateMs.Domain.Common;
using TemplateMs.Domain.Events;
using TemplateMs.Domain.Exceptions;

namespace TemplateMs.Domain.Entities;

/// <summary>Order aggregate root. Owns a collection of <see cref="OrderItem"/>s and encapsulates order business rules.</summary>
public sealed class Order : AggregateRoot
{
    private static class ErrorMessages
    {
        public const string EmptyCustomerName = "CustomerName cannot be null or empty.";
        public const string ItemNotFound = "OrderItem with id '{0}' was not found on this order.";
        public const string ItemAlreadyInactive = "OrderItem with id '{0}' is already inactive.";
        public const string OrderAlreadyInactive = "Order is already inactive.";
    }

    private readonly List<OrderItem> _items = [];

    /// <summary>Gets the full name of the customer who placed this order.</summary>
    public string CustomerName { get; private set; } = string.Empty;

    /// <summary>Gets the date the order was placed.</summary>
    public DateTime OrderDate { get; private set; }

    /// <summary>Gets whether this order is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the line items belonging to this order.</summary>
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    // Parameterless constructor required by Dapper
    private Order() { }

    /// <summary>
    /// Factory method that creates a new <see cref="Order"/>.
    /// Does NOT raise <see cref="OrderCreatedEvent"/> — call <see cref="RaiseCreatedEvent"/> after
    /// the repository has assigned the DB-generated Id.
    /// </summary>
    public static Order Create(string customerName, DateTime orderDate, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(customerName)) throw new DomainException(ErrorMessages.EmptyCustomerName);

        return new Order
        {
            CustomerName = customerName.Trim(),
            OrderDate = orderDate,
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    /// <summary>
    /// Raises <see cref="OrderCreatedEvent"/> with the DB-assigned Id.
    /// Call from <c>OrderWriteRepository.AddAsync</c> immediately after setting <c>order.Id</c>.
    /// </summary>
    public void RaiseCreatedEvent(DateTime createdAt) =>
        AddDomainEvent(new OrderCreatedEvent(Id, CustomerName, OrderDate, createdAt));

    /// <summary>
    /// Adds a new line item to the order.
    /// Does NOT raise <see cref="OrderItemAddedEvent"/> — call <see cref="RaiseItemAddedEvent"/> after
    /// the repository has assigned the DB-generated Id to the returned item.
    /// </summary>
    public OrderItem AddItem(string productName, int quantity, decimal unitPrice, DateTime createdAt)
    {
        var item = OrderItem.Create(Id, productName, quantity, unitPrice, createdAt);
        _items.Add(item);
        UpdatedAt = createdAt;
        return item;
    }

    /// <summary>
    /// Raises <see cref="OrderItemAddedEvent"/> with the DB-assigned Id on the item.
    /// Call from the command handler immediately after <c>OrderItemWriteRepository.AddAsync</c>.
    /// </summary>
    public void RaiseItemAddedEvent(OrderItem item) =>
        AddDomainEvent(new OrderItemAddedEvent(Id, item.Id, item.ProductName, item.Quantity, item.UnitPrice, item.CreatedAt));

    /// <summary>Soft-deletes a line item by setting its <c>IsActive</c> flag to false.</summary>
    /// <param name="itemId">The domain identity (<see cref="BaseEntity.Id"/>) of the item to remove.</param>
    /// <param name="updatedAt">The timestamp for audit fields.</param>
    /// <exception cref="DomainException">Thrown when the item is not found or already inactive.</exception>
    public void RemoveItem(int itemId, DateTime updatedAt)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException(string.Format(ErrorMessages.ItemNotFound, itemId));

        if (!item.IsActive)
            throw new DomainException(string.Format(ErrorMessages.ItemAlreadyInactive, itemId));

        item.Deactivate(updatedAt);
        UpdatedAt = updatedAt;
        AddDomainEvent(new OrderItemRemovedEvent(Id, itemId, updatedAt));
    }

    /// <summary>Updates the Order's mutable properties and raises <see cref="OrderUpdatedEvent"/>.</summary>
    /// <param name="customerName">The new customer name.</param>
    /// <param name="orderDate">The new order date.</param>
    /// <param name="updatedAt">The timestamp for audit fields.</param>
    /// <exception cref="DomainException">Thrown when business invariants are violated.</exception>
    public void Update(string customerName, DateTime orderDate, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(customerName)) throw new DomainException(ErrorMessages.EmptyCustomerName);

        CustomerName = customerName.Trim();
        OrderDate = orderDate;
        UpdatedAt = updatedAt;
        AddDomainEvent(new OrderUpdatedEvent(Id, CustomerName, OrderDate, updatedAt));
    }

    /// <summary>Soft-deletes this order and raises <see cref="OrderDeletedEvent"/>.</summary>
    /// <param name="deletedAt">The timestamp for audit fields.</param>
    /// <exception cref="DomainException">Thrown when the order is already inactive.</exception>
    public void Deactivate(DateTime deletedAt)
    {
        if (!IsActive) throw new DomainException(ErrorMessages.OrderAlreadyInactive);

        IsActive = false;
        UpdatedAt = deletedAt;
        AddDomainEvent(new OrderDeletedEvent(Id, deletedAt));
    }

    /// <summary>
    /// Adds an attribute to a line item.
    /// Does NOT raise <see cref="OrderItemAttributeAddedEvent"/> — call <see cref="RaiseAttributeAddedEvent"/> after
    /// the repository has assigned the DB-generated Id to the returned attribute.
    /// </summary>
    public OrderItemAttribute AddAttributeToItem(int itemId, string attributeName, string attributeValue, DateTime createdAt)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException(string.Format(ErrorMessages.ItemNotFound, itemId));

        var attr = item.AddAttribute(attributeName, attributeValue, createdAt);
        UpdatedAt = createdAt;
        return attr;
    }

    /// <summary>
    /// Raises <see cref="OrderItemAttributeAddedEvent"/> with the DB-assigned Id on the attribute.
    /// Call from the command handler immediately after <c>OrderItemAttributeWriteRepository.AddAsync</c>.
    /// </summary>
    public void RaiseAttributeAddedEvent(int itemId, OrderItemAttribute attr) =>
        AddDomainEvent(new OrderItemAttributeAddedEvent(itemId, attr.Id, attr.AttributeName, attr.AttributeValue, attr.CreatedAt));

    /// <summary>Removes an attribute from a line item and raises <see cref="OrderItemAttributeRemovedEvent"/>.</summary>
    /// <param name="itemId">The domain identity of the target line item.</param>
    /// <param name="attributeId">The domain identity of the attribute to remove.</param>
    /// <param name="updatedAt">The timestamp for audit fields.</param>
    /// <exception cref="DomainException">Thrown when the item or attribute is not found.</exception>
    public void RemoveAttributeFromItem(int itemId, int attributeId, DateTime updatedAt)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException(string.Format(ErrorMessages.ItemNotFound, itemId));

        item.RemoveAttribute(attributeId, updatedAt);
        UpdatedAt = updatedAt;
        AddDomainEvent(new OrderItemAttributeRemovedEvent(itemId, attributeId, updatedAt));
    }

    /// <summary>
    /// Reconstitutes an <see cref="Order"/> aggregate from persisted DB row data, including all items and attributes.
    /// Bypasses domain invariants — use only in the read-side infrastructure layer.
    /// </summary>
    public static Order Reconstitute(
        int id,
        string customerName,
        DateTime orderDate,
        bool isActive,
        Guid createdBy,
        DateTime createdAt,
        Guid updatedBy,
        DateTime updatedAt,
        IEnumerable<OrderItem> items)
    {
        var order = new Order
        {
            Id = id,
            CustomerName = customerName,
            OrderDate = orderDate,
            IsActive = isActive,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            UpdatedBy = updatedBy,
            UpdatedAt = updatedAt
        };
        order._items.AddRange(items);
        return order;
    }

}
