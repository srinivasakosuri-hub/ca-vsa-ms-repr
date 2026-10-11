using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Application.Features.Order.Mapping;
using TemplateMs.Domain.Exceptions;

namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>WolverineFX handler for <see cref="AddOrderItemCommand"/>.</summary>
public sealed class AddOrderItemCommandHandler
{
    private readonly IOrderReadRepository _readRepository;
    private readonly IOrderItemWriteRepository _itemWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new <see cref="AddOrderItemCommandHandler"/>.</summary>
    public AddOrderItemCommandHandler(
        IOrderReadRepository readRepository,
        IOrderItemWriteRepository itemWriteRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _readRepository = readRepository;
        _itemWriteRepository = itemWriteRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>Handles the command by loading the order aggregate, adding the item, and persisting.</summary>
    public async Task<OrderItemDto> Handle(AddOrderItemCommand command, CancellationToken cancellationToken)
    {
        var order = await _readRepository.GetAggregateByIdAsync(command.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(command.OrderId);

        var now = _dateTimeProvider.UtcNow;
        var item = order.AddItem(command.ProductName, command.Quantity, command.UnitPrice, now);

        _unitOfWork.RegisterAggregate(order);
        await _itemWriteRepository.AddAsync(item, order.Id, cancellationToken);
        order.RaiseItemAddedEvent(item);
        await _unitOfWork.CommitAsync(cancellationToken);

        return OrderMapper.ToItemDto(item);
    }
}
