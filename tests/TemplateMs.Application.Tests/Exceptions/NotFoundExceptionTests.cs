using FluentAssertions;
using TemplateMs.Domain.Exceptions;
using Xunit;

namespace TemplateMs.Application.Tests.Exceptions;

/// <summary>Unit tests for <see cref="NotFoundException"/>, <see cref="OrderNotFoundException"/>,
/// and <see cref="OrderItemNotFoundException"/>.</summary>
public sealed class NotFoundExceptionTests
{
    // ── NotFoundException (abstract base) ────────────────────────────────────

    [Fact]
    public void NotFoundException_IsAbstract()
    {
        typeof(NotFoundException).IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void NotFoundException_InheritsFromDomainException()
    {
        typeof(NotFoundException).BaseType.Should().Be(typeof(DomainException));
    }

    // ── OrderNotFoundException ────────────────────────────────────────────────

    [Fact]
    public void OrderNotFoundException_InheritsFromNotFoundException()
    {
        typeof(OrderNotFoundException).BaseType.Should().Be(typeof(NotFoundException));
    }

    [Fact]
    public void OrderNotFoundException_InheritsFromDomainException()
    {
        new OrderNotFoundException(1).Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void OrderNotFoundException_InheritsFromException()
    {
        new OrderNotFoundException(1).Should().BeAssignableTo<Exception>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(999)]
    public void OrderNotFoundException_Message_ContainsOrderId(int orderId)
    {
        var sut = new OrderNotFoundException(orderId);

        sut.Message.Should().Contain(orderId.ToString());
    }

    [Fact]
    public void OrderNotFoundException_IsSealed()
    {
        typeof(OrderNotFoundException).IsSealed.Should().BeTrue();
    }

    // ── OrderItemNotFoundException ────────────────────────────────────────────

    [Fact]
    public void OrderItemNotFoundException_InheritsFromNotFoundException()
    {
        typeof(OrderItemNotFoundException).BaseType.Should().Be(typeof(NotFoundException));
    }

    [Fact]
    public void OrderItemNotFoundException_InheritsFromDomainException()
    {
        new OrderItemNotFoundException(1).Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void OrderItemNotFoundException_InheritsFromException()
    {
        new OrderItemNotFoundException(1).Should().BeAssignableTo<Exception>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(100)]
    public void OrderItemNotFoundException_Message_ContainsOrderItemId(int orderItemId)
    {
        var sut = new OrderItemNotFoundException(orderItemId);

        sut.Message.Should().Contain(orderItemId.ToString());
    }

    [Fact]
    public void OrderItemNotFoundException_IsSealed()
    {
        typeof(OrderItemNotFoundException).IsSealed.Should().BeTrue();
    }
}
