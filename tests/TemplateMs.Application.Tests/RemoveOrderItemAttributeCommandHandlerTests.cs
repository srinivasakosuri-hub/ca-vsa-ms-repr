using FluentAssertions;
using NSubstitute;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Commands;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Entities;
using TemplateMs.Domain.Exceptions;
using Xunit;

namespace TemplateMs.Application.Tests;

/// <summary>Unit tests for <see cref="RemoveOrderItemAttributeCommandHandler"/>.</summary>
public sealed class RemoveOrderItemAttributeCommandHandlerTests
{
    private readonly IOrderReadRepository _readRepository = Substitute.For<IOrderReadRepository>();
    private readonly IOrderItemAttributeWriteRepository _attributeWriteRepository = Substitute.For<IOrderItemAttributeWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly RemoveOrderItemAttributeCommandHandler _sut;

    private static readonly DateTime FixedNow = new(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedOrderDate = new(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc);

    public RemoveOrderItemAttributeCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _sut = new RemoveOrderItemAttributeCommandHandler(
            _readRepository,
            _attributeWriteRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    private static (Order order, int itemId, int attributeId) BuildOrderWithItemAndAttribute()
    {
        var order = Order.Create("Jane Doe", FixedOrderDate, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        var attr = order.AddAttributeToItem(item.Id, "Color", "Blue", FixedNow.AddMinutes(2));
        return (order, item.Id, attr.Id);
    }

    [Fact]
    public async Task HandleWithValidCommandCallsDeactivateAsyncOnAttributeRepository()
    {
        var (order, itemId, attributeId) = BuildOrderWithItemAndAttribute();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemAttributeCommand(order.Id, itemId, attributeId);

        await _sut.Handle(command, CancellationToken.None);

        await _attributeWriteRepository.Received(1).DeactivateAsync(attributeId, FixedNow, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithValidCommandCommitsUnitOfWork()
    {
        var (order, itemId, attributeId) = BuildOrderWithItemAndAttribute();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemAttributeCommand(order.Id, itemId, attributeId);

        await _sut.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithValidCommandRegistersAggregateOnUnitOfWork()
    {
        var (order, itemId, attributeId) = BuildOrderWithItemAndAttribute();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemAttributeCommand(order.Id, itemId, attributeId);

        await _sut.Handle(command, CancellationToken.None);

        _unitOfWork.Received(1).RegisterAggregate(order);
    }

    [Fact]
    public async Task HandleWhenOrderNotFoundThrowsOrderNotFoundException()
    {
        _readRepository.GetAggregateByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Order?)null);
        var command = new RemoveOrderItemAttributeCommand(9999, 1, 1);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [Fact]
    public async Task HandleWhenItemNotFoundThrowsDomainException()
    {
        var order = Order.Create("Jane Doe", FixedOrderDate, FixedNow);
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemAttributeCommand(order.Id, 9999, 1);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task HandleWhenAttributeNotFoundThrowsDomainException()
    {
        var order = Order.Create("Jane Doe", FixedOrderDate, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemAttributeCommand(order.Id, item.Id, 9999);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task HandleSetsAttributeInactiveOnAggregate()
    {
        var (order, itemId, attributeId) = BuildOrderWithItemAndAttribute();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new RemoveOrderItemAttributeCommand(order.Id, itemId, attributeId);

        await _sut.Handle(command, CancellationToken.None);

        order.Items.First(i => i.Id == itemId)
             .Attributes.First(a => a.Id == attributeId)
             .IsActive.Should().BeFalse();
    }
}
