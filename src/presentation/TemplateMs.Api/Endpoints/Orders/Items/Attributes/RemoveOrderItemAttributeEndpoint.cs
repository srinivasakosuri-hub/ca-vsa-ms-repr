using FastEndpoints;
using TemplateMs.Application.Features.Order.Commands;
using Wolverine;

namespace TemplateMs.Api.Endpoints.Orders.Items.Attributes;

/// <summary>Request for removing an attribute from an order item.</summary>
public sealed class RemoveOrderItemAttributeRequest
{
    /// <summary>The order that owns the item (from route).</summary>
    public int OrderId { get; init; }
    /// <summary>The item that owns the attribute (from route).</summary>
    public int ItemId { get; init; }
    /// <summary>The attribute to soft-delete (from route).</summary>
    public int AttributeId { get; init; }
}

/// <summary>Soft-deletes an attribute from an order item.</summary>
/// <remarks>DELETE api/orders/{orderId}/items/{itemId}/attributes/{attributeId}</remarks>
public sealed class RemoveOrderItemAttributeEndpoint : Endpoint<RemoveOrderItemAttributeRequest>
{
    private readonly IMessageBus _bus;

    /// <summary>Initializes a new instance of the <see cref="RemoveOrderItemAttributeEndpoint"/> class.</summary>
    /// <param name="bus">The message bus for dispatching commands.</param>
    public RemoveOrderItemAttributeEndpoint(IMessageBus bus) => _bus = bus;

    /// <summary>Configures the endpoint route and behavior.</summary>
    public override void Configure()
    {
        Delete("api/orders/{orderId}/items/{itemId}/attributes/{attributeId}");
        AllowAnonymous();
        Description(b => b
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound));
    }

    /// <summary>Handles the request to remove an attribute from an order item.</summary>
    /// <param name="req">The request containing order, item, and attribute IDs.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(RemoveOrderItemAttributeRequest req, CancellationToken ct)
    {
        var command = new RemoveOrderItemAttributeCommand(req.OrderId, req.ItemId, req.AttributeId);
        await _bus.InvokeAsync(command, ct);
        HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }
}
