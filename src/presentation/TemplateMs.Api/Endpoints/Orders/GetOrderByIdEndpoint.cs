using FastEndpoints;
using TemplateMs.Api.ResponseModels;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Queries;
using Wolverine;

namespace TemplateMs.Api.Endpoints.Orders;

/// <summary>Request for retrieving an order by its identity.</summary>
public sealed class GetOrderByIdRequest
{
    /// <summary>Gets the order identifier.</summary>
    public int Id { get; init; }
}

/// <summary>Retrieves an order with all its items and attributes by domain identity.</summary>
/// <remarks>GET api/orders/{id}</remarks>
public sealed class GetOrderByIdEndpoint : Endpoint<GetOrderByIdRequest, OrderResponse>
{
    private readonly IMessageBus _bus;

    /// <summary>Initializes a new instance of the <see cref="GetOrderByIdEndpoint"/> class.</summary>
    /// <param name="bus">The message bus for dispatching queries.</param>
    public GetOrderByIdEndpoint(IMessageBus bus) => _bus = bus;

    /// <summary>Configures the endpoint route and behavior.</summary>
    public override void Configure()
    {
        Get("api/orders/{id}");
        AllowAnonymous();
        Description(b => b
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound));
    }

    /// <summary>Handles the request to retrieve an order by ID.</summary>
    /// <param name="req">The request containing the order ID.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(GetOrderByIdRequest req, CancellationToken ct)
    {
        var query = new GetOrderQuery(req.Id);
        var dto = await _bus.InvokeAsync<OrderDto>(query, ct);
        HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        await HttpContext.Response.WriteAsJsonAsync(OrderResponse.FromDto(dto!), cancellationToken: ct);
    }
}
