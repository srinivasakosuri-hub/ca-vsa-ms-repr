namespace TemplateMs.Application.Features.Order.Dtos;

/// <summary>Data transfer object representing an OrderItemAttribute for read operations.</summary>
/// <param name="Id">The database primary key of the attribute.</param>
/// <param name="OrderItemId">The database primary key of the parent item.</param>
/// <param name="AttributeName">The attribute name (e.g., Color).</param>
/// <param name="AttributeValue">The attribute value (e.g., Blue).</param>
/// <param name="IsActive">Whether the attribute is active.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC last-update timestamp.</param>
/// <param name="CreatedBy">The ID of the user who created the attribute.</param>
/// <param name="UpdatedBy">The ID of the user who last updated the attribute.</param>
public sealed record OrderItemAttributeDto(
    int Id,
    int OrderItemId,
    string AttributeName,
    string AttributeValue,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid CreatedBy,
    Guid UpdatedBy);
