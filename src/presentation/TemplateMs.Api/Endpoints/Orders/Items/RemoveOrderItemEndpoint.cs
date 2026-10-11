using FastEndpoints;
using TemplateMs.Application.Features.Order.Commands;
using Wolverine;

namespace TemplateMs.Api.Endpoints.Orders.Items;

/// <summary>Request for removing a line item from an existing order.</summary>
public sealed class RemoveOrderItemRequest
{
    /// <summary>The order that owns the item (from route).</summary>
    public int OrderId { get; init; }
    /// <summary>The item to soft-delete (from route).</summary>
    public int ItemId { get; init; }
}

/// <summary>Soft-deletes a line item from an existing order.</summary>
/// <remarks>DELETE api/orders/{orderId}/items/{itemId}</remarks>
public sealed class RemoveOrderItemEndpoint : Endpoint<RemoveOrderItemRequest>
{
    private readonly IMessageBus _bus;

    /// <summary>Initializes a new instance of the <see cref="RemoveOrderItemEndpoint"/> class.</summary>
    /// <param name="bus">The message bus for dispatching commands.</param>
    public RemoveOrderItemEndpoint(IMessageBus bus) => _bus = bus;

    /// <summary>Configures the endpoint route and behavior.</summary>
    public override void Configure()
    {
        Delete("api/orders/{orderId}/items/{itemId}");
        AllowAnonymous();
        Description(b => b
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound));
    }

    /// <summary>Handles the request to remove an item from an order.</summary>
    /// <param name="req">The request containing the order ID and item ID.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(RemoveOrderItemRequest req, CancellationToken ct)
    {
        var command = new RemoveOrderItemCommand(req.OrderId, req.ItemId);
        await _bus.InvokeAsync(command, ct);
        HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }
}
