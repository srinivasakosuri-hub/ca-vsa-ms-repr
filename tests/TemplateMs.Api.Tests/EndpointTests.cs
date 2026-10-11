using FluentAssertions;
using TemplateMs.Api.Endpoints.Orders;
using TemplateMs.Api.ResponseModels;
using Xunit;

namespace TemplateMs.Api.Tests;

/// <summary>Structural tests verifying endpoint types and response model shapes.</summary>
public sealed class EndpointTests
{
    [Fact]
    public void CreateOrderEndpointExists()
    {
        // Assert
        typeof(CreateOrderEndpoint).Should().NotBeNull();
    }

    [Fact]
    public void GetOrderByIdEndpointExists()
    {
        // Assert
        typeof(GetOrderByIdEndpoint).Should().NotBeNull();
    }

    [Fact]
    public void GetOrderSummaryPagedEndpointExists()
    {
        // Assert
        typeof(GetOrderSummaryPagedEndpoint).Should().NotBeNull();
    }

    [Fact]
    public void ExportOrderSummaryCsvEndpointExists()
    {
        // Assert
        typeof(ExportOrderSummaryCsvEndpoint).Should().NotBeNull();
    }

    [Fact]
    public void OrderResponseIsRecord()
    {
        // Assert
        typeof(OrderResponse).Should().BeAssignableTo<IEquatable<OrderResponse>>();
    }

    [Fact]
    public void OrderSummaryPagedResponseExists()
    {
        // Assert
        typeof(OrderSummaryPagedResponse).Should().NotBeNull();
    }

    [Fact]
    public void ProgramIsPartialClass()
    {
        // Ensures the partial Program class is available for WebApplicationFactory
        typeof(global::Program).Should().NotBeNull();
    }

    [Fact]
    public void CorrelationIdMiddlewareIsSealed()
    {
        // Assert
        typeof(Middleware.CorrelationIdMiddleware).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void ErrorHandlingMiddlewareIsSealed()
    {
        // Assert
        typeof(Middleware.ErrorHandlingMiddleware).IsSealed.Should().BeTrue();
    }
}
