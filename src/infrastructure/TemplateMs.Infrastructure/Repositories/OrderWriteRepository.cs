using Dapper;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Entities;
using TemplateMs.Sql.Features.Order.Commands;

namespace TemplateMs.Infrastructure.Repositories;

/// <summary>
/// Write repository for the Order aggregate. Uses the transactional connection from
/// <see cref="IUnitOfWork"/> so all writes participate in the command transaction.
/// </summary>
public sealed class OrderWriteRepository : IOrderWriteRepository
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserProvider _currentUser;

    /// <summary>Initializes a new <see cref="OrderWriteRepository"/>.</summary>
    public OrderWriteRepository(IUnitOfWork unitOfWork, ICurrentUserProvider currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;

        var id = await _unitOfWork.Connection.ExecuteScalarAsync<int>(
            OrderCommandSql.Insert,
            new
            {
                order.CustomerName,
                order.OrderDate,
                order.IsActive,
                CreatedBy = userId,
                order.CreatedAt,
                UpdatedBy = userId,
                order.UpdatedAt
            },
            _unitOfWork.Transaction);

        order.Id = id;
        order.CreatedBy = userId;
        order.UpdatedBy = userId;
        order.RaiseCreatedEvent(order.CreatedAt);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;

        await _unitOfWork.Connection.ExecuteAsync(
            OrderCommandSql.UpdateStatus,
            new
            {
                order.IsActive,
                UpdatedBy = userId,
                order.UpdatedAt,
                order.Id
            },
            _unitOfWork.Transaction);

        order.UpdatedBy = userId;
    }
}
