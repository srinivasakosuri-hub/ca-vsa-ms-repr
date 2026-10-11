namespace TemplateMs.Domain.Exceptions;

/// <summary>Thrown when a requested OrderItem cannot be found in the data store.</summary>
public sealed class OrderItemNotFoundException : NotFoundException
{
    /// <summary>Initializes a new <see cref="OrderItemNotFoundException"/> for the given order item identifier.</summary>
    public OrderItemNotFoundException(int orderItemId)
        : base($"OrderItem with id '{orderItemId}' was not found.") { }
}
