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

/// <summary>Unit tests for <see cref="AddOrderItemCommandHandler"/>.</summary>
public sealed class AddOrderItemCommandHandlerTests
{
    private readonly IOrderReadRepository _readRepository = Substitute.For<IOrderReadRepository>();
    private readonly IOrderItemWriteRepository _itemWriteRepository = Substitute.For<IOrderItemWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly AddOrderItemCommandHandler _sut;

    private static readonly DateTime FixedNow = new(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedOrderDate = new(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc);

    private static Order BuildOrder()
    {
        var order = Order.Create("Jane Doe", FixedOrderDate, FixedNow);
        return order;
    }

    public AddOrderItemCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _sut = new AddOrderItemCommandHandler(
            _readRepository,
            _itemWriteRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task HandleWithValidCommandReturnsOrderItemDto()
    {
        var order = BuildOrder();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemCommand(order.Id, "Widget", 2, 9.99m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<OrderItemDto>();
        result.ProductName.Should().Be("Widget");
        result.Quantity.Should().Be(2);
        result.UnitPrice.Should().Be(9.99m);
    }

    [Fact]
    public async Task HandleWithValidCommandCallsAddAsyncOnItemRepository()
    {
        var order = BuildOrder();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemCommand(order.Id, "Widget", 1, 5m);

        await _sut.Handle(command, CancellationToken.None);

        await _itemWriteRepository.Received(1).AddAsync(Arg.Any<OrderItem>(), order.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithValidCommandCommitsUnitOfWork()
    {
        var order = BuildOrder();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemCommand(order.Id, "Widget", 1, 5m);

        await _sut.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithValidCommandRegistersAggregateOnUnitOfWork()
    {
        var order = BuildOrder();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemCommand(order.Id, "Widget", 1, 5m);

        await _sut.Handle(command, CancellationToken.None);

        _unitOfWork.Received(1).RegisterAggregate(order);
    }

    [Fact]
    public async Task HandleWhenOrderNotFoundThrowsOrderNotFoundException()
    {
        _readRepository.GetAggregateByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Order?)null);
        var command = new AddOrderItemCommand(9999, "Widget", 1, 5m);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [Fact]
    public async Task HandleReturnedDtoHasCorrectTotalAmount()
    {
        var order = BuildOrder();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemCommand(order.Id, "Gadget", 3, 10m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.TotalAmount.Should().Be(30m);
    }

    [Fact]
    public async Task HandleReturnedDtoHasIsActiveTrue()
    {
        var order = BuildOrder();
        _readRepository.GetAggregateByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var command = new AddOrderItemCommand(order.Id, "Widget", 1, 5m);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsActive.Should().BeTrue();
    }
}
