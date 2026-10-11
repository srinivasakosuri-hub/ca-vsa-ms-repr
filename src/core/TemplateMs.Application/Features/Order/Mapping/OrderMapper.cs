using TemplateMs.Application.Features.Order.Dtos;
using DomainEntities = TemplateMs.Domain.Entities;

namespace TemplateMs.Application.Features.Order.Mapping;

/// <summary>
/// Manual mapper between domain aggregates/entities and their DTOs.
/// No AutoMapper or reflection-based mapping is used.
/// </summary>
public static class OrderMapper
{
    /// <summary>Maps a domain <see cref="DomainEntities.Order"/> to an <see cref="OrderDto"/>.</summary>
    public static OrderDto ToDto(DomainEntities.Order order) =>
        new(
            order.Id,
            order.CustomerName,
            order.OrderDate,
            order.IsActive,
            order.Items.Select(ToItemDto).ToList().AsReadOnly(),
            order.CreatedAt,
            order.UpdatedAt,
            order.CreatedBy,
            order.UpdatedBy);

    /// <summary>Maps a domain <see cref="DomainEntities.OrderItem"/> to an <see cref="OrderItemDto"/>.</summary>
    public static OrderItemDto ToItemDto(DomainEntities.OrderItem item) =>
        new(
            item.Id,
            item.OrderId,
            item.ProductName,
            item.Quantity,
            item.UnitPrice,
            item.TotalAmount,
            item.IsActive,
            item.Attributes.Select(ToAttributeDto).ToList().AsReadOnly(),
            item.CreatedAt,
            item.UpdatedAt,
            item.CreatedBy,
            item.UpdatedBy);

    /// <summary>Maps a domain <see cref="DomainEntities.OrderItemAttribute"/> to an <see cref="OrderItemAttributeDto"/>.</summary>
    public static OrderItemAttributeDto ToAttributeDto(DomainEntities.OrderItemAttribute attr) =>
        new(
            attr.Id,
            attr.OrderItemId,
            attr.AttributeName,
            attr.AttributeValue,
            attr.IsActive,
            attr.CreatedAt,
            attr.UpdatedAt,
            attr.CreatedBy,
            attr.UpdatedBy);
}
