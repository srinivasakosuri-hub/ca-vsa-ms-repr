using Dapper;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Entities;
using TemplateMs.Sql.Features.Order.Commands;

namespace TemplateMs.Infrastructure.Repositories;

/// <summary>Write repository for <see cref="OrderItem"/> entities.</summary>
public sealed class OrderItemWriteRepository : IOrderItemWriteRepository
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserProvider _currentUser;

    /// <summary>Initializes a new <see cref="OrderItemWriteRepository"/>.</summary>
    public OrderItemWriteRepository(IUnitOfWork unitOfWork, ICurrentUserProvider currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task AddAsync(OrderItem item, int dbOrderId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;

        var id = await _unitOfWork.Connection.ExecuteScalarAsync<int>(
            OrderItemCommandSql.Insert,
            new
            {
                OrderId = dbOrderId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.IsActive,
                CreatedBy = userId,
                item.CreatedAt,
                UpdatedBy = userId,
                item.UpdatedAt
            },
            _unitOfWork.Transaction);

        item.Id = id;
        item.CreatedBy = userId;
        item.UpdatedBy = userId;
    }

    /// <inheritdoc />
    public async Task DeactivateAsync(int dbOrderItemId, DateTime updatedAt, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Connection.ExecuteAsync(
            OrderItemCommandSql.Deactivate,
            new { Id = dbOrderItemId, UpdatedAt = updatedAt },
            _unitOfWork.Transaction);
    }
}
