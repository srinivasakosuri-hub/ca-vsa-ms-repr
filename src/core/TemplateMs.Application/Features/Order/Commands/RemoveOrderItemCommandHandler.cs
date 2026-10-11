using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Exceptions;

namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>WolverineFX handler for <see cref="RemoveOrderItemCommand"/>.</summary>
public sealed class RemoveOrderItemCommandHandler
{
    private readonly IOrderReadRepository _readRepository;
    private readonly IOrderItemWriteRepository _itemWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new <see cref="RemoveOrderItemCommandHandler"/>.</summary>
    public RemoveOrderItemCommandHandler(
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

    /// <summary>Handles the command by loading the order, calling RemoveItem, and persisting the soft-delete.</summary>
    public async Task Handle(RemoveOrderItemCommand command, CancellationToken cancellationToken)
    {
        var order = await _readRepository.GetAggregateByIdAsync(command.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(command.OrderId);

        var now = _dateTimeProvider.UtcNow;
        order.RemoveItem(command.OrderItemId, now);

        // Find the DB surrogate key from the in-memory item before it was deactivated
        var removedItem = order.Items.First(i => i.Id == command.OrderItemId);

        _unitOfWork.RegisterAggregate(order);
        await _itemWriteRepository.DeactivateAsync(removedItem.Id, now, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
