namespace TemplateMs.Application.Features.Order.Interfaces;

/// <summary>Write repository for OrderItemAttribute entities. Operations execute within the command transaction.</summary>
public interface IOrderItemAttributeWriteRepository
{
    /// <summary>Persists a new order item attribute and populates the DB surrogate key.</summary>
    /// <param name="attribute">The attribute to persist.</param>
    /// <param name="dbOrderItemId">The database surrogate OrderItemID of the parent item.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task AddAsync(Domain.Entities.OrderItemAttribute attribute, int dbOrderItemId, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes an attribute by its DB surrogate key (IsActive=0).</summary>
    Task DeactivateAsync(int dbOrderItemAttributeId, DateTime updatedAt, CancellationToken cancellationToken = default);
}
