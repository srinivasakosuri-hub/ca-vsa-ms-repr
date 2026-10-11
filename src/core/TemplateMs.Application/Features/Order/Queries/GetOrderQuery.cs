namespace TemplateMs.Application.Features.Order.Queries;

/// <summary>Query to retrieve an order with all its items and attributes by domain identity. Dispatched via WolverineFX.</summary>
/// <param name="OrderId">The domain identity (GUID) of the order to retrieve.</param>
public sealed record GetOrderQuery(int OrderId);
