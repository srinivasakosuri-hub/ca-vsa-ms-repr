using FluentAssertions;
using NSubstitute;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Commands;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Entities;
using Xunit;

namespace TemplateMs.Application.Tests;

/// <summary>Unit tests for <see cref="CreateOrderCommandHandler"/>.</summary>
public sealed class CreateOrderCommandHandlerTests
{
    private readonly IOrderWriteRepository _writeRepository = Substitute.For<IOrderWriteRepository>();
    private readonly IOrderItemWriteRepository _itemWriteRepository = Substitute.For<IOrderItemWriteRepository>();
    private readonly IOrderItemAttributeWriteRepository _attributeWriteRepository = Substitute.For<IOrderItemAttributeWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly CreateOrderCommandHandler _sut;
    private static readonly DateTime FixedNow = new(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedOrderDate = new(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc);

    public CreateOrderCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _sut = new CreateOrderCommandHandler(
            _writeRepository,
            _itemWriteRepository,
            _attributeWriteRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task HandleWithValidCommandReturnsOrderDto()
    {
        var command = new CreateOrderCommand("John Smith", FixedOrderDate);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<OrderDto>();
        result.CustomerName.Should().Be("John Smith");
        result.OrderDate.Should().Be(FixedOrderDate);
    }

    [Fact]
    public async Task HandleWithValidCommandCallsAddAsyncOnRepository()
    {
        var command = new CreateOrderCommand("Jane Doe", FixedOrderDate);

        await _sut.Handle(command, CancellationToken.None);

        await _writeRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithValidCommandCommitsUnitOfWork()
    {
        var command = new CreateOrderCommand("Alice", FixedOrderDate);

        await _sut.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithValidCommandRegistersAggregateOnUnitOfWork()
    {
        var command = new CreateOrderCommand("Bob", FixedOrderDate);

        await _sut.Handle(command, CancellationToken.None);

        _unitOfWork.Received(1).RegisterAggregate(Arg.Any<Domain.Common.AggregateRoot>());
    }

    [Fact]
    public async Task HandleWithValidCommandReturnedDtoHasIsActiveTrue()
    {
        var command = new CreateOrderCommand("Charlie", FixedOrderDate);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task HandleWithValidCommandReturnedDtoHasCreatedAt()
    {
        var command = new CreateOrderCommand("Dave", FixedOrderDate);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.CreatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task HandleWhenRepositoryThrowsExceptionPropagates()
    {
        var command = new CreateOrderCommand("Eve", FixedOrderDate);
        _writeRepository.AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB error")));

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleWithItemsPersistsEachItem()
    {
        var command = new CreateOrderCommand("Frank", FixedOrderDate,
        [
            new CreateOrderItemData("Widget", 2, 9.99m),
            new CreateOrderItemData("Gadget", 1, 49.99m)
        ]);

        await _sut.Handle(command, CancellationToken.None);

        await _itemWriteRepository.Received(2).AddAsync(Arg.Any<OrderItem>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithItemsAndAttributesPersistsEachAttribute()
    {
        var command = new CreateOrderCommand("Grace", FixedOrderDate,
        [
            new CreateOrderItemData("Widget", 1, 5m,
            [
                new CreateOrderItemAttributeData("Color", "Red"),
                new CreateOrderItemAttributeData("Size", "Large")
            ])
        ]);

        await _sut.Handle(command, CancellationToken.None);

        await _attributeWriteRepository.Received(2).AddAsync(
            Arg.Any<Domain.Entities.OrderItemAttribute>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithItemsReturnedDtoContainsItems()
    {
        var command = new CreateOrderCommand("Hank", FixedOrderDate,
        [
            new CreateOrderItemData("Sprocket", 3, 2.50m)
        ]);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].ProductName.Should().Be("Sprocket");
        result.Items[0].Quantity.Should().Be(3);
    }
}
