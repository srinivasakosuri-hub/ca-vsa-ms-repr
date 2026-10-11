using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Exceptions;

namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>WolverineFX handler for <see cref="RemoveOrderItemAttributeCommand"/>.</summary>
public sealed class RemoveOrderItemAttributeCommandHandler
{
    private readonly IOrderReadRepository _readRepository;
    private readonly IOrderItemAttributeWriteRepository _attributeWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new <see cref="RemoveOrderItemAttributeCommandHandler"/>.</summary>
    public RemoveOrderItemAttributeCommandHandler(
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

    /// <summary>Handles the command by locating the item/attribute on the aggregate and soft-deleting.</summary>
    public async Task Handle(RemoveOrderItemAttributeCommand command, CancellationToken cancellationToken)
    {
        var order = await _readRepository.GetAggregateByIdAsync(command.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(command.OrderId);

        var now = _dateTimeProvider.UtcNow;
        order.RemoveAttributeFromItem(command.OrderItemId, command.OrderItemAttributeId, now);

        _unitOfWork.RegisterAggregate(order);
        await _attributeWriteRepository.DeactivateAsync(command.OrderItemAttributeId, now, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
