using FluentAssertions;
using NSubstitute;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Commands;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Entities;
using TemplateMs.Domain.Exceptions;
using Xunit;

namespace TemplateMs.Application.Tests;

/// <summary>Unit tests for <see cref="AddOrderItemAttributeCommandHandler"/>.</summary>
public sealed class AddOrderItemAttributeCommandHandlerTests
{
    private readonly IOrderReadRepository _readRepository = Substitute.For<IOrderReadRepository>();
    private readonly IOrderItemAttributeWriteRepository _attributeWriteRepository = Substitute.For<IOrderItemAttributeWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly AddOrderItemAttributeCommandHandler _sut;

    private static readonly DateTime FixedNow = new(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedOrderDate = new(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc);

    public AddOrderItemAttributeCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _sut = new AddOrderItemAttributeCommandHandler(
            _readRepository,
            _attributeWriteRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    private static (Order order, int itemId) BuildOrderWithItem()
    {
        var order = Order.Create("Jane Doe", FixedOrderDate, FixedNow);
        var item = order.AddItem("Widget", 1, 5m, FixedNow.AddMinutes(1));
        return (order, item.Id);
    }

    [Fact]
    public async Task HandleWithValidCommandReturnsOrderItemAttributeDto()
    {
        var (order, itemId) = BuildOrderWithItem();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemAttributeCommand(order.Id, itemId, "Color", "Blue");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<OrderItemAttributeDto>();
        result.AttributeName.Should().Be("Color");
        result.AttributeValue.Should().Be("Blue");
    }

    [Fact]
    public async Task HandleWithValidCommandCallsAddAsyncOnAttributeRepository()
    {
        var (order, itemId) = BuildOrderWithItem();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemAttributeCommand(order.Id, itemId, "Color", "Blue");

        await _sut.Handle(command, CancellationToken.None);

        await _attributeWriteRepository.Received(1).AddAsync(
            Arg.Any<OrderItemAttribute>(), itemId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithValidCommandCommitsUnitOfWork()
    {
        var (order, itemId) = BuildOrderWithItem();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemAttributeCommand(order.Id, itemId, "Color", "Blue");

        await _sut.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithValidCommandRegistersAggregateOnUnitOfWork()
    {
        var (order, itemId) = BuildOrderWithItem();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemAttributeCommand(order.Id, itemId, "Color", "Blue");

        await _sut.Handle(command, CancellationToken.None);

        _unitOfWork.Received(1).RegisterAggregate(order);
    }

    [Fact]
    public async Task HandleWhenOrderNotFoundThrowsOrderNotFoundException()
    {
        _readRepository.GetAggregateByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Order?)null);
        var command = new AddOrderItemAttributeCommand(9999, 1, "Color", "Blue");

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [Fact]
    public async Task HandleWhenItemNotFoundThrowsDomainException()
    {
        var order = Order.Create("Jane Doe", FixedOrderDate, FixedNow);
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemAttributeCommand(order.Id, 9999, "Color", "Blue");

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task HandleReturnedDtoHasIsActiveTrue()
    {
        var (order, itemId) = BuildOrderWithItem();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemAttributeCommand(order.Id, itemId, "Size", "Large");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsActive.Should().BeTrue();
    }
}
