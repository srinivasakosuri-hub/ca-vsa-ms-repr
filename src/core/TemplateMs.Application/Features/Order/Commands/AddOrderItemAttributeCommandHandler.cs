using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Application.Features.Order.Mapping;
using TemplateMs.Domain.Exceptions;

namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>WolverineFX handler for <see cref="AddOrderItemAttributeCommand"/>.</summary>
public sealed class AddOrderItemAttributeCommandHandler
{
    private readonly IOrderReadRepository _readRepository;
    private readonly IOrderItemAttributeWriteRepository _attributeWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new <see cref="AddOrderItemAttributeCommandHandler"/>.</summary>
    public AddOrderItemAttributeCommandHandler(
        IOrderReadRepository readRepository,
        IOrderItemAttributeWriteRepository attributeWriteRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _readRepository = readRepository;
        _attributeWriteRepository = attributeWriteRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>Handles the command by locating the item on the aggregate, adding the attribute, and persisting.</summary>
    public async Task<OrderItemAttributeDto> Handle(AddOrderItemAttributeCommand command, CancellationToken cancellationToken)
    {
        var order = await _readRepository.GetAggregateByIdAsync(command.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(command.OrderId);

        var now = _dateTimeProvider.UtcNow;
        var attr = order.AddAttributeToItem(command.OrderItemId, command.AttributeName, command.AttributeValue, now);

        _unitOfWork.RegisterAggregate(order);
        await _attributeWriteRepository.AddAsync(attr, command.OrderItemId, cancellationToken);
        order.RaiseAttributeAddedEvent(command.OrderItemId, attr);
        await _unitOfWork.CommitAsync(cancellationToken);

        return OrderMapper.ToAttributeDto(attr);
    }
}
