using Dapper;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Entities;
using TemplateMs.Sql.Features.Order.Commands;

namespace TemplateMs.Infrastructure.Repositories;

/// <summary>Write repository for <see cref="OrderItemAttribute"/> entities.</summary>
public sealed class OrderItemAttributeWriteRepository : IOrderItemAttributeWriteRepository
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserProvider _currentUser;

    /// <summary>Initializes a new <see cref="OrderItemAttributeWriteRepository"/>.</summary>
    public OrderItemAttributeWriteRepository(IUnitOfWork unitOfWork, ICurrentUserProvider currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task AddAsync(OrderItemAttribute attribute, int dbOrderItemId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;

        var id = await _unitOfWork.Connection.ExecuteScalarAsync<int>(
            OrderItemAttributeCommandSql.Insert,
            new
            {
                OrderItemId = dbOrderItemId,
                attribute.AttributeName,
                attribute.AttributeValue,
                attribute.IsActive,
                CreatedBy = userId,
                attribute.CreatedAt,
                UpdatedBy = userId,
                attribute.UpdatedAt
            },
            _unitOfWork.Transaction);

        attribute.Id = id;
        attribute.CreatedBy = userId;
        attribute.UpdatedBy = userId;
    }

    /// <inheritdoc />
    public async Task DeactivateAsync(int dbOrderItemAttributeId, DateTime updatedAt, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Connection.ExecuteAsync(
            OrderItemAttributeCommandSql.Deactivate,
            new { Id = dbOrderItemAttributeId, UpdatedAt = updatedAt },
            _unitOfWork.Transaction);
    }
}
