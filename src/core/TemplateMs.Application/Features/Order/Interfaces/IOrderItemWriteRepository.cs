using TemplateMs.Domain.Entities;

namespace TemplateMs.Application.Features.Order.Interfaces;

/// <summary>Write repository for OrderItem entities. Operations execute within the command transaction.</summary>
public interface IOrderItemWriteRepository
{
    /// <summary>Persists a new order item to the command database and populates the DB surrogate key.</summary>
    /// <param name="item">The order item to persist.</param>
    /// <param name="dbOrderId">The database surrogate OrderID of the parent order.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task AddAsync(OrderItem item, int dbOrderId, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes an order item by its DB surrogate key (IsActive=0).</summary>
    Task DeactivateAsync(int dbOrderItemId, DateTime updatedAt, CancellationToken cancellationToken = default);
}
