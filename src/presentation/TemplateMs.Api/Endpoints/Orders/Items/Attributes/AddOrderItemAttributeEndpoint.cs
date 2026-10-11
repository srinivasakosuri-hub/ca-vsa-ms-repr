using FastEndpoints;
using TemplateMs.Api.ResponseModels;
using TemplateMs.Application.Features.Order.Commands;
using TemplateMs.Application.Features.Order.Dtos;
using Wolverine;

namespace TemplateMs.Api.Endpoints.Orders.Items.Attributes;

/// <summary>Request for adding an attribute to an existing order item.</summary>
public sealed class AddOrderItemAttributeRequest
{
    /// <summary>The order that owns the item (from route).</summary>
    public int OrderId { get; init; }
    /// <summary>The item to attach the attribute to (from route).</summary>
    public int ItemId { get; init; }
    /// <summary>Gets the attribute name.</summary>
    public string AttributeName { get; init; } = string.Empty;
    /// <summary>Gets the attribute value.</summary>
    public string AttributeValue { get; init; } = string.Empty;
}

/// <summary>Adds an attribute to an existing order item.</summary>
/// <remarks>POST api/orders/{orderId}/items/{itemId}/attributes</remarks>
public sealed class AddOrderItemAttributeEndpoint : Endpoint<AddOrderItemAttributeRequest, OrderItemAttributeResponse>
{
    private readonly IMessageBus _bus;

    /// <summary>Initializes a new instance of the <see cref="AddOrderItemAttributeEndpoint"/> class.</summary>
    /// <param name="bus">The message bus for dispatching commands.</param>
    public AddOrderItemAttributeEndpoint(IMessageBus bus) => _bus = bus;

    /// <summary>Configures the endpoint route and behavior.</summary>
    public override void Configure()
    {
        Post("api/orders/{orderId}/items/{itemId}/attributes");
        AllowAnonymous();
        Description(b => b
            .Produces<OrderItemAttributeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound));
    }

    /// <summary>Handles the request to add an attribute to an order item.</summary>
    /// <param name="req">The request containing order, item, and attribute details.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(AddOrderItemAttributeRequest req, CancellationToken ct)
    {
        var command = new AddOrderItemAttributeCommand(req.OrderId, req.ItemId, req.AttributeName, req.AttributeValue);
        var dto = await _bus.InvokeAsync<OrderItemAttributeDto>(command, ct);
        HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        HttpContext.Response.Headers["Location"] = $"/api/orders/{req.OrderId}/items/{req.ItemId}/attributes/{dto!.Id}";
        await HttpContext.Response.WriteAsJsonAsync(OrderItemAttributeResponse.FromDto(dto!), cancellationToken: ct);
    }
}
