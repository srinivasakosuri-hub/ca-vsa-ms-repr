namespace TemplateMs.Application.Features.Order.Dtos;

/// <summary>Data transfer object representing an OrderItem for read operations.</summary>
/// <param name="Id">The database primary key of the item.</param>
/// <param name="OrderId">The database primary key of the parent order.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="Quantity">The quantity ordered.</param>
/// <param name="UnitPrice">The price per unit.</param>
/// <param name="TotalAmount">The computed total (Quantity × UnitPrice).</param>
/// <param name="IsActive">Whether the item is active.</param>
/// <param name="Attributes">The key/value attributes on this item.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC last-update timestamp.</param>
/// <param name="CreatedBy">The ID of the user who created the item.</param>
/// <param name="UpdatedBy">The ID of the user who last updated the item.</param>
public sealed record OrderItemDto(
    int Id,
    int OrderId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    bool IsActive,
    IReadOnlyList<OrderItemAttributeDto> Attributes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid CreatedBy,
    Guid UpdatedBy);
