using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Exceptions;

namespace TemplateMs.Application.Features.Order.Queries;

/// <summary>
/// WolverineFX handler for <see cref="GetOrderQuery"/>.
/// Returns the full order with items and attributes using the read repository.
/// </summary>
public sealed class GetOrderQueryHandler
{
    private readonly IOrderReadRepository _readRepository;

    /// <summary>Initializes a new <see cref="GetOrderQueryHandler"/>.</summary>
    public GetOrderQueryHandler(IOrderReadRepository readRepository) =>
        _readRepository = readRepository;

    /// <summary>
    /// Handles the <see cref="GetOrderQuery"/>. Returns the <see cref="OrderDto"/> or throws
    /// <see cref="OrderNotFoundException"/> when not found.
    /// </summary>
    public async Task<OrderDto> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await _readRepository.GetByIdAsync(query.OrderId, cancellationToken);

        if (order is null)
            throw new OrderNotFoundException(query.OrderId);

        return order;
    }
}
