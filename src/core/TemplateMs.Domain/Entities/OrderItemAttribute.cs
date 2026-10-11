using TemplateMs.Domain.Common;
using TemplateMs.Domain.Exceptions;

namespace TemplateMs.Domain.Entities;

/// <summary>
/// Represents a key/value attribute on an <see cref="OrderItem"/> (e.g., Color=Blue).
/// Not an aggregate root — created and mutated exclusively through <see cref="OrderItem"/>.
/// </summary>
public sealed class OrderItemAttribute : BaseAuditableEntity
{
    private static class ErrorMessages
    {
        public const string EmptyAttributeName = "AttributeName cannot be null or empty.";
        public const string EmptyAttributeValue = "AttributeValue cannot be null or empty.";
    }

    /// <summary>Gets the domain identity of the parent order item (FK).</summary>
    public int OrderItemId { get; private set; }

    /// <summary>Gets the attribute name (e.g., Color).</summary>
    public string AttributeName { get; private set; } = string.Empty;

    /// <summary>Gets the attribute value (e.g., Blue).</summary>
    public string AttributeValue { get; private set; } = string.Empty;

    /// <summary>Gets whether this attribute is active.</summary>
    public bool IsActive { get; private set; }

    // Parameterless constructor required by Dapper
    private OrderItemAttribute() { }

    /// <summary>Creates a new <see cref="OrderItemAttribute"/>. Called only from <see cref="OrderItem.AddAttribute"/>.</summary>
    internal static OrderItemAttribute Create(int orderItemId, string attributeName, string attributeValue, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(attributeName)) throw new DomainException(ErrorMessages.EmptyAttributeName);
        if (string.IsNullOrWhiteSpace(attributeValue)) throw new DomainException(ErrorMessages.EmptyAttributeValue);

        return new OrderItemAttribute
        {
            OrderItemId = orderItemId,
            AttributeName = attributeName.Trim(),
            AttributeValue = attributeValue.Trim(),
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    /// <summary>Soft-deletes this attribute. Called from <see cref="OrderItem.RemoveAttribute"/>.</summary>
    internal void Deactivate(DateTime updatedAt)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Reconstitutes an <see cref="OrderItemAttribute"/> from persisted DB row data.
    /// Bypasses domain invariants — use only in the read-side infrastructure layer.
    /// </summary>
    public static OrderItemAttribute Reconstitute(
        int id,
        int orderItemId,
        string attributeName,
        string attributeValue,
        bool isActive,
        Guid createdBy,
        DateTime createdAt,
        Guid updatedBy,
        DateTime updatedAt)
    {
        return new OrderItemAttribute
        {
            Id = id,
            OrderItemId = orderItemId,
            AttributeName = attributeName,
            AttributeValue = attributeValue,
            IsActive = isActive,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            UpdatedBy = updatedBy,
            UpdatedAt = updatedAt
        };
    }

}
