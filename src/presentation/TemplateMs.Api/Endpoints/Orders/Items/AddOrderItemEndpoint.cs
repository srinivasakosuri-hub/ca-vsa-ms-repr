using FastEndpoints;
using TemplateMs.Api.ResponseModels;
using TemplateMs.Application.Features.Order.Commands;
using TemplateMs.Application.Features.Order.Dtos;
using Wolverine;

namespace TemplateMs.Api.Endpoints.Orders.Items;

/// <summary>Request for adding a line item to an existing order.</summary>
public sealed class AddOrderItemRequest
{
    /// <summary>The order to add the item to (from route).</summary>
    public int OrderId { get; init; }
/// <summary> Gets the name of the product</summary>
    public string ProductName { get; init; } = string.Empty;
    /// <summary>Gets the quantity of items to add to the order.</summary>
    public int Quantity { get; init; }
    /// <summary>Gets the unit price of the product.</summary>
    public decimal UnitPrice { get; init; }
}

/// <summary>Adds a line item to an existing order.</summary>
/// <remarks>POST api/orders/{orderId}/items</remarks>
public sealed class AddOrderItemEndpoint : Endpoint<AddOrderItemRequest, OrderItemResponse>
{
    private readonly IMessageBus _bus;

    /// <summary>Initializes a new instance of the <see cref="AddOrderItemEndpoint"/> class.</summary>
    /// <param name="bus">The message bus for dispatching commands.</param>
    public AddOrderItemEndpoint(IMessageBus bus) => _bus = bus;

    /// <summary>Configures the endpoint route and behavior.</summary>
    public override void Configure()
    {
        Post("api/orders/{orderId}/items");
        AllowAnonymous();
        Description(b => b
            .Produces<OrderItemResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound));
    }

    /// <summary>Handles the request to add an item to an order.</summary>
    /// <param name="req">The request containing the order ID and item details.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(AddOrderItemRequest req, CancellationToken ct)
    {
        var command = new AddOrderItemCommand(req.OrderId, req.ProductName, req.Quantity, req.UnitPrice);
        var dto = await _bus.InvokeAsync<OrderItemDto>(command, ct);
        HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        HttpContext.Response.Headers["Location"] = $"/api/orders/{req.OrderId}/items/{dto!.Id}";
        await HttpContext.Response.WriteAsJsonAsync(OrderItemResponse.FromDto(dto!), cancellationToken: ct);
    }
}
