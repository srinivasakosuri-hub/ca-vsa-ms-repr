namespace TemplateMs.Application.Features.Order.Dtos;

/// <summary>Data transfer object representing an Order for read operations and API responses.</summary>
/// <param name="Id">The database primary key of the order.</param>
/// <param name="CustomerName">The full name of the customer.</param>
/// <param name="OrderDate">The date the order was placed.</param>
/// <param name="IsActive">Whether the order is active.</param>
/// <param name="Items">The active line items on the order.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
/// <param name="UpdatedAt">The UTC last-update timestamp.</param>
/// <param name="CreatedBy">The ID of the user who created the order.</param>
/// <param name="UpdatedBy">The ID of the user who last updated the order.</param>
public sealed record OrderDto(
    int Id,
    string CustomerName,
    DateTime OrderDate,
    bool IsActive,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid CreatedBy,
    Guid UpdatedBy);
