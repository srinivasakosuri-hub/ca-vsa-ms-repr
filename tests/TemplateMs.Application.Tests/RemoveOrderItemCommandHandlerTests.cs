using FluentAssertions;
using NSubstitute;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Commands;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Entities;
using TemplateMs.Domain.Exceptions;
using Xunit;

namespace TemplateMs.Application.Tests;

/// <summary>Unit tests for <see cref="RemoveOrderItemCommandHandler"/>.</summary>
public sealed class RemoveOrderItemCommandHandlerTests
{
    private readonly IOrderReadRepository _readRepository = Substitute.For<IOrderReadRepository>();
    private readonly IOrderItemWriteRepository _itemWriteRepository = Substitute.For<IOrderItemWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly RemoveOrderItemCommandHandler _sut;

    private static readonly DateTime FixedNow = new(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedOrderDate = new(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc);

    public RemoveOrderItemCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _sut = new RemoveOrderItemCommandHandler(
            _readRepository,
            _itemWriteRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    private static Order BuildOrderWithItem(out int itemId)
    {
        var order = Order.Create("Jane Doe", FixedOrderDate, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        itemId = item.Id;
        return order;
    }

    [Fact]
    public async Task HandleWithValidCommandCallsDeactivateAsyncOnItemRepository()
    {
        var order = BuildOrderWithItem(out var itemId);
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemCommand(order.Id, itemId);

        await _sut.Handle(command, CancellationToken.None);

        await _itemWriteRepository.Received(1).DeactivateAsync(itemId, FixedNow, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithValidCommandCommitsUnitOfWork()
    {
        var order = BuildOrderWithItem(out var itemId);
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemCommand(order.Id, itemId);

        await _sut.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithValidCommandRegistersAggregateOnUnitOfWork()
    {
        var order = BuildOrderWithItem(out var itemId);
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemCommand(order.Id, itemId);

        await _sut.Handle(command, CancellationToken.None);

        _unitOfWork.Received(1).RegisterAggregate(order);
    }

    [Fact]
    public async Task HandleWhenOrderNotFoundThrowsOrderNotFoundException()
    {
        _readRepository.GetAggregateByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Order?)null);
        var command = new RemoveOrderItemCommand(9999, 1);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [Fact]
    public async Task HandleWhenItemNotFoundThrowsDomainException()
    {
        var order = Order.Create("Jane Doe", FixedOrderDate, FixedNow);
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemCommand(order.Id, 9999);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task HandleSetsItemInactiveOnAggregate()
    {
        var order = BuildOrderWithItem(out var itemId);
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemCommand(order.Id, itemId);

        await _sut.Handle(command, CancellationToken.None);

        order.Items.First(i => i.Id == itemId).IsActive.Should().BeFalse();
    }
}
