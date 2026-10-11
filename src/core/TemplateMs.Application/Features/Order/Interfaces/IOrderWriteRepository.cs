namespace TemplateMs.Application.Features.Order.Interfaces;

/// <summary>Write repository for the Order aggregate. Operations execute within the command transaction.</summary>
public interface IOrderWriteRepository
{
    /// <summary>Persists a new order to the command database and populates the DB surrogate key.</summary>
    Task AddAsync(Domain.Entities.Order order, CancellationToken cancellationToken = default);

    /// <summary>Updates mutable fields (IsActive, UpdatedAt) of an existing order.</summary>
    Task UpdateAsync(Domain.Entities.Order order, CancellationToken cancellationToken = default);
}
