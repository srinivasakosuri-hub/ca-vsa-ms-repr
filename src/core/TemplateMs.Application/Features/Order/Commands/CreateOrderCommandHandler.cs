using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Application.Features.Order.Mapping;

namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>
/// WolverineFX handler for <see cref="CreateOrderCommand"/>.
/// Wolverine discovers this handler by convention: the class ends in "Handler" and the method is named "Handle".
/// </summary>
public sealed class CreateOrderCommandHandler
{
    private readonly IOrderWriteRepository _writeRepository;
    private readonly IOrderItemWriteRepository _itemWriteRepository;
    private readonly IOrderItemAttributeWriteRepository _attributeWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new <see cref="CreateOrderCommandHandler"/> with its dependencies.</summary>
    public CreateOrderCommandHandler(
        IOrderWriteRepository writeRepository,
        IOrderItemWriteRepository itemWriteRepository,
        IOrderItemAttributeWriteRepository attributeWriteRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _writeRepository = writeRepository;
        _itemWriteRepository = itemWriteRepository;
        _attributeWriteRepository = attributeWriteRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// Handles the <see cref="CreateOrderCommand"/>. Creates the Order aggregate with any supplied
    /// line items and their attributes, persists everything, and commits the unit of work.
    /// </summary>
    public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var order = Domain.Entities.Order.Create(command.CustomerName, command.OrderDate, now);

        _unitOfWork.RegisterAggregate(order);
        await _writeRepository.AddAsync(order, cancellationToken);

        foreach (var itemData in command.Items ?? [])
        {
            var item = order.AddItem(itemData.ProductName, itemData.Quantity, itemData.UnitPrice, now);
            await _itemWriteRepository.AddAsync(item, order.Id, cancellationToken);
            order.RaiseItemAddedEvent(item);

            foreach (var attrData in itemData.Attributes ?? [])
            {
                var attr = item.AddAttribute(attrData.AttributeName, attrData.AttributeValue, now);
                await _attributeWriteRepository.AddAsync(attr, item.Id, cancellationToken);
                order.RaiseAttributeAddedEvent(item.Id, attr);
            }
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        return OrderMapper.ToDto(order);
    }
}
