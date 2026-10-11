using FastEndpoints;
using TemplateMs.Api.ResponseModels;
using TemplateMs.Application.Features.Order.Commands;
using TemplateMs.Application.Features.Order.Dtos;
using Wolverine;

namespace TemplateMs.Api.Endpoints.Orders;

/// <summary>Request body for creating a new order with optional line items.</summary>
public sealed class CreateOrderRequest
{
    /// <summary>Gets the name of the customer placing the order.</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>Gets the date the order was placed.</summary>
    public DateTime OrderDate { get; init; }

    /// <summary>Gets the optional collection of line items to include in the order.</summary>
    public IReadOnlyList<CreateOrderItemRequest>? Items { get; init; }
}

/// <summary>Represents a line item to create as part of an order.</summary>
public sealed class CreateOrderItemRequest
{
    /// <summary>Gets the name of the product for this line item.</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Gets the quantity of items ordered.</summary>
    public int Quantity { get; init; }

    /// <summary>Gets the unit price per item.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Gets the optional collection of attributes to attach to this line item.</summary>
    public IReadOnlyList<CreateOrderItemAttributeRequest>? Attributes { get; init; }
}

/// <summary>Represents an attribute to attach to a line item during order creation.</summary>
public sealed class CreateOrderItemAttributeRequest
{
    /// <summary>Gets the name of the attribute.</summary>
    public string AttributeName { get; init; } = string.Empty;

    /// <summary>Gets the value of the attribute.</summary>
    public string AttributeValue { get; init; } = string.Empty;
}

/// <summary>Creates a new order, optionally with line items and their attributes in one call.</summary>
/// <remarks>POST api/orders</remarks>
public sealed class CreateOrderEndpoint : Endpoint<CreateOrderRequest, OrderResponse>
{
    private readonly IMessageBus _bus;

    /// <summary>Initializes a new instance of the <see cref="CreateOrderEndpoint"/> class.</summary>
    /// <param name="bus">The message bus for dispatching commands.</param>
    public CreateOrderEndpoint(IMessageBus bus) => _bus = bus;

    /// <summary>Configures the endpoint route and behavior.</summary>
    public override void Configure()
    {
        Post("api/orders");
        AllowAnonymous();
        Description(b => b
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    /// <summary>Handles the request to create a new order with optional items and attributes.</summary>
    /// <param name="req">The request containing order details and optional items.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(CreateOrderRequest req, CancellationToken ct)
    {
        var items = req.Items?.Select(i =>
            new CreateOrderItemData(
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.Attributes?.Select(a => new CreateOrderItemAttributeData(a.AttributeName, a.AttributeValue))
                              .ToList()
                              .AsReadOnly()))
            .ToList()
            .AsReadOnly();

        var command = new CreateOrderCommand(req.CustomerName, req.OrderDate, items);
        var dto = await _bus.InvokeAsync<OrderDto>(command, ct);
        var response = OrderResponse.FromDto(dto!);

        HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        HttpContext.Response.Headers["Location"] = $"/api/orders/{response.Id}";
        await HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: ct);
    }
}
