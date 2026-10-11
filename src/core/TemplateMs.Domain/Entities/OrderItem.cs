using TemplateMs.Domain.Common;
using TemplateMs.Domain.Exceptions;

namespace TemplateMs.Domain.Entities;

/// <summary>
/// Represents a line item within an <see cref="Order"/>.
/// Not an aggregate root — created and mutated exclusively through the <see cref="Order"/> aggregate.
/// </summary>
public sealed class OrderItem : BaseAuditableEntity
{
    private static class ErrorMessages
    {
        public const string EmptyProductName = "ProductName cannot be null or empty.";
        public const string InvalidQuantity = "Quantity must be greater than zero.";
        public const string NegativeUnitPrice = "UnitPrice cannot be negative.";
        public const string AttributeNotFound = "OrderItemAttribute with id '{0}' was not found on this item.";
        public const string AttributeAlreadyInactive = "OrderItemAttribute with id '{0}' is already inactive.";
    }

    private readonly List<OrderItemAttribute> _attributes = [];

    /// <summary>Gets the domain identity of the parent order (FK).</summary>
    public int OrderId { get; private set; }

    /// <summary>Gets the name of the product.</summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>Gets the number of units ordered.</summary>
    public int Quantity { get; private set; }

    /// <summary>Gets the price per unit.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Gets the computed total (Quantity × UnitPrice). Mirrors the DB computed column.</summary>
    public decimal TotalAmount => Quantity * UnitPrice;

    /// <summary>Gets whether this line item is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the attributes attached to this line item.</summary>
    public IReadOnlyList<OrderItemAttribute> Attributes => _attributes.AsReadOnly();

    // Parameterless constructor required by Dapper
    private OrderItem() { }

    /// <summary>Creates a new <see cref="OrderItem"/>. Called only from <see cref="Order.AddItem"/>.</summary>
    internal static OrderItem Create(int orderId, string productName, int quantity, decimal unitPrice, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(productName)) throw new DomainException(ErrorMessages.EmptyProductName);
        if (quantity <= 0) throw new DomainException(ErrorMessages.InvalidQuantity);
        if (unitPrice < 0m) throw new DomainException(ErrorMessages.NegativeUnitPrice);

        return new OrderItem
        {
            OrderId = orderId,
            ProductName = productName.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    /// <summary>Adds an attribute key/value pair to this line item.</summary>
    /// <param name="attributeName">The attribute name (e.g., Color).</param>
    /// <param name="attributeValue">The attribute value (e.g., Blue).</param>
    /// <param name="createdAt">The timestamp for audit fields.</param>
    /// <returns>The newly created <see cref="OrderItemAttribute"/>.</returns>
    public OrderItemAttribute AddAttribute(string attributeName, string attributeValue, DateTime createdAt)
    {
        var attr = OrderItemAttribute.Create(Id, attributeName, attributeValue, createdAt);
        _attributes.Add(attr);
        UpdatedAt = createdAt;
        return attr;
    }

    /// <summary>Soft-deletes an attribute by setting its <c>IsActive</c> flag to false.</summary>
    /// <param name="attributeId">The domain identity of the attribute to remove.</param>
    /// <param name="updatedAt">The timestamp for audit fields.</param>
    /// <exception cref="DomainException">Thrown when the attribute is not found or already inactive.</exception>
    public void RemoveAttribute(int attributeId, DateTime updatedAt)
    {
        var attr = _attributes.FirstOrDefault(a => a.Id == attributeId)
            ?? throw new DomainException(string.Format(ErrorMessages.AttributeNotFound, attributeId));

        if (!attr.IsActive)
            throw new DomainException(string.Format(ErrorMessages.AttributeAlreadyInactive, attributeId));

        attr.Deactivate(updatedAt);
        UpdatedAt = updatedAt;
    }

    /// <summary>Soft-deletes this item. Called from <see cref="Order.RemoveItem"/>.</summary>
    internal void Deactivate(DateTime updatedAt)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Reconstitutes an <see cref="OrderItem"/> from persisted DB row data, including its attributes.
    /// Bypasses domain invariants — use only in the read-side infrastructure layer.
    /// </summary>
    public static OrderItem Reconstitute(
        int id,
        int orderId,
        string productName,
        int quantity,
        decimal unitPrice,
        bool isActive,
        Guid createdBy,
        DateTime createdAt,
        Guid updatedBy,
        DateTime updatedAt,
        IEnumerable<OrderItemAttribute> attributes)
    {
        var item = new OrderItem
        {
            Id = id,
            OrderId = orderId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            IsActive = isActive,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            UpdatedBy = updatedBy,
            UpdatedAt = updatedAt
        };
        item._attributes.AddRange(attributes);
        return item;
    }

}
