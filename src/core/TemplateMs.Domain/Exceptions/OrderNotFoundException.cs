namespace TemplateMs.Domain.Exceptions;

/// <summary>Thrown when a requested Order aggregate cannot be found in the data store.</summary>
public sealed class OrderNotFoundException : NotFoundException
{
    /// <summary>Initializes a new <see cref="OrderNotFoundException"/> for the given order identifier.</summary>
    /// <param name="orderId">The identifier of the missing order.</param>
    public OrderNotFoundException(int orderId)
        : base($"Order with id '{orderId}' was not found.") { }
}
