using FluentAssertions;
using NSubstitute;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Application.Features.Order.Queries;
using TemplateMs.Domain.Exceptions;
using Xunit;

namespace TemplateMs.Application.Tests;

/// <summary>Unit tests for <see cref="GetOrderQueryHandler"/>.</summary>
public sealed class GetOrderQueryHandlerTests
{
    private readonly IOrderReadRepository _readRepository = Substitute.For<IOrderReadRepository>();
    private readonly GetOrderQueryHandler _sut;

    private static readonly Guid SampleUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static readonly OrderDto SampleDto = new(
        42,
        "Jane Doe",
        new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc),
        true,
        [],
        new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc),
        new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc),
        SampleUserId,
        SampleUserId);

    public GetOrderQueryHandlerTests()
    {
        _sut = new GetOrderQueryHandler(_readRepository);
    }

    [Fact]
    public async Task HandleWhenOrderExistsReturnsOrderDto()
    {
        var query = new GetOrderQuery(SampleDto.Id);
        _readRepository.GetByIdAsync(SampleDto.Id, Arg.Any<CancellationToken>()).Returns(SampleDto);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(SampleDto.Id);
    }

    [Fact]
    public async Task HandleWhenOrderDoesNotExistThrowsOrderNotFoundException()
    {
        var missingId = 9999;
        var query = new GetOrderQuery(missingId);
        _readRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((OrderDto?)null);

        var act = async () => await _sut.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [Fact]
    public async Task HandleCallsReadRepositoryWithCorrectId()
    {
        var id = SampleDto.Id;
        var query = new GetOrderQuery(id);
        _readRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(SampleDto);

        await _sut.Handle(query, CancellationToken.None);

        await _readRepository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWhenOrderExistsReturnsDtoWithCorrectCustomerName()
    {
        var query = new GetOrderQuery(SampleDto.Id);
        _readRepository.GetByIdAsync(SampleDto.Id, Arg.Any<CancellationToken>()).Returns(SampleDto);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.CustomerName.Should().Be(SampleDto.CustomerName);
    }

    [Fact]
    public async Task HandleWhenOrderExistsReturnsDtoWithCorrectIsActive()
    {
        var query = new GetOrderQuery(SampleDto.Id);
        _readRepository.GetByIdAsync(SampleDto.Id, Arg.Any<CancellationToken>()).Returns(SampleDto);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task HandleWhenOrderExistsReturnsDtoWithCorrectOrderDate()
    {
        var query = new GetOrderQuery(SampleDto.Id);
        _readRepository.GetByIdAsync(SampleDto.Id, Arg.Any<CancellationToken>()).Returns(SampleDto);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.OrderDate.Should().Be(SampleDto.OrderDate);
    }
}
