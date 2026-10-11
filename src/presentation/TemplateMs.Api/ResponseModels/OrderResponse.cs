using TemplateMs.Application.Features.Order.Dtos;

namespace TemplateMs.Api.ResponseModels;

/// <summary>HTTP response body for order endpoints.</summary>
public sealed record OrderResponse(
    int Id,
    string CustomerName,
    DateTime OrderDate,
    bool IsActive,
    IReadOnlyList<OrderItemResponse> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid CreatedBy,
    Guid UpdatedBy)
{
    /// <summary>Maps an <see cref="OrderDto"/> to an <see cref="OrderResponse"/>.</summary>
    public static OrderResponse FromDto(OrderDto dto) =>
        new(dto.Id,
            dto.CustomerName,
            dto.OrderDate,
            dto.IsActive,
            dto.Items.Select(OrderItemResponse.FromDto).ToList().AsReadOnly(),
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.CreatedBy,
            dto.UpdatedBy);
}

/// <summary>HTTP response body for a single order line item.</summary>
public sealed record OrderItemResponse(
    int Id,
    int OrderId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    bool IsActive,
    IReadOnlyList<OrderItemAttributeResponse> Attributes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid CreatedBy,
    Guid UpdatedBy)
{
    /// <summary>Maps an <see cref="OrderItemDto"/> to an <see cref="OrderItemResponse"/>.</summary>
    public static OrderItemResponse FromDto(OrderItemDto dto) =>
        new(dto.Id,
            dto.OrderId,
            dto.ProductName,
            dto.Quantity,
            dto.UnitPrice,
            dto.TotalAmount,
            dto.IsActive,
            dto.Attributes.Select(OrderItemAttributeResponse.FromDto).ToList().AsReadOnly(),
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.CreatedBy,
            dto.UpdatedBy);
}

/// <summary>HTTP response body for a single order item attribute.</summary>
public sealed record OrderItemAttributeResponse(
    int Id,
    int OrderItemId,
    string AttributeName,
    string AttributeValue,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid CreatedBy,
    Guid UpdatedBy)
{
    /// <summary>Maps an <see cref="OrderItemAttributeDto"/> to an <see cref="OrderItemAttributeResponse"/>.</summary>
    public static OrderItemAttributeResponse FromDto(OrderItemAttributeDto dto) =>
        new(dto.Id,
            dto.OrderItemId,
            dto.AttributeName,
            dto.AttributeValue,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.CreatedBy,
            dto.UpdatedBy);
}
