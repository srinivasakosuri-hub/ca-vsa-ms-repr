using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TemplateMs.Api.Endpoints.Orders;
using TemplateMs.Api.Middleware;
using TemplateMs.Api.ResponseModels;
using Xunit;

namespace TemplateMs.Api.Tests;

/// <summary>Unit tests for endpoints and API middleware.</summary>
public sealed class ControllerTests
{
    private static readonly DateTime FixedNow = new(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedOrderDate = new(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Guid SampleUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static Application.Features.Order.Dtos.OrderDto BuildOrderDto(
        int id = 99,
        string customerName = "Test Customer",
        bool isActive = true,
        DateTime? updatedAt = null) =>
        new(
            id,
            customerName,
            FixedOrderDate,
            isActive,
            [],
            FixedNow,
            updatedAt ?? FixedNow,
            SampleUserId,
            SampleUserId);

    [Fact]
    public void OrderResponseFromDtoMapsCustomerName()
    {
        var dto = BuildOrderDto(customerName: "Jane Doe");

        var response = OrderResponse.FromDto(dto);

        response.CustomerName.Should().Be("Jane Doe");
        response.IsActive.Should().BeTrue();
    }

    [Fact]
    public void OrderResponseFromDtoMapsAllFields()
    {
        var dto = BuildOrderDto(id: 42);

        var response = OrderResponse.FromDto(dto);

        response.Id.Should().Be(42);
        response.CustomerName.Should().Be("Test Customer");
        response.OrderDate.Should().Be(FixedOrderDate);
        response.IsActive.Should().BeTrue();
        response.CreatedAt.Should().Be(FixedNow);
        response.Items.Should().BeEmpty();
    }

    [Fact]
    public void OrderResponseUpdatedAtIsPreservedWhenSet()
    {
        var updatedAt = FixedNow.AddHours(1);
        var dto = BuildOrderDto(updatedAt: updatedAt);

        var response = OrderResponse.FromDto(dto);

        response.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void CreateOrderEndpointExists()
    {
        // Assert
        typeof(CreateOrderEndpoint).Should().NotBeNull();
    }

    [Fact]
    public async Task CorrelationIdMiddlewareWhenHeaderProvidedUsesExistingCorrelationId()
    {
        var correlationId = "test-correlation-id-123";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = correlationId;
        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new CorrelationIdMiddleware(next);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Items["CorrelationId"]!.ToString().Should().Be(correlationId);
        context.Response.Headers["X-Correlation-Id"].ToString().Should().Be(correlationId);
    }

    [Fact]
    public async Task CorrelationIdMiddlewareWhenNoHeaderGeneratesNewCorrelationId()
    {
        var context = new DefaultHttpContext();
        RequestDelegate next = ctx => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next);

        await middleware.InvokeAsync(context);

        var correlationId = context.Items["CorrelationId"]?.ToString();
        correlationId.Should().NotBeNullOrEmpty();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CorrelationIdMiddlewareSetsResponseHeader()
    {
        var context = new DefaultHttpContext();
        RequestDelegate next = ctx => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next);

        await middleware.InvokeAsync(context);

        context.Response.Headers.ContainsKey("X-Correlation-Id").Should().BeTrue();
    }

    [Fact]
    public void GlobalExceptionFilterCanBeInstantiated()
    {
        var logger = Substitute.For<ILogger<Filters.GlobalExceptionFilter>>();

        var filter = new Filters.GlobalExceptionFilter(logger);

        filter.Should().NotBeNull();
    }
}
