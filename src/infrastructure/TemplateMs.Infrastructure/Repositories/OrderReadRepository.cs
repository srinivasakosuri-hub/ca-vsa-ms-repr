using Dapper;
using System.Data;
using TemplateMs.Application.Common;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Domain.Entities;
using TemplateMs.Sql.Features.Order.Queries;

namespace TemplateMs.Infrastructure.Repositories;

/// <summary>
/// Read repository for the Order feature. Uses the query database connection via
/// <see cref="IQueryDbConnectionFactory"/> and Dapper multi-result-set mapping.
/// </summary>
public sealed class OrderReadRepository : IOrderReadRepository
{
    // Whitelist mapping from API-facing column names to safe SQL column expressions.
    // This is the ONLY place where the sort column is resolved; it never flows as raw text.
    private static readonly IReadOnlyDictionary<string, string> SortColumnMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["orderId"]      = "o.Id",
            ["customerName"] = "o.CustomerName",
            ["orderDate"]    = "o.OrderDate",
            ["productName"]  = "i.ProductName",
            ["quantity"]     = "i.Quantity",
            ["unitPrice"]    = "i.UnitPrice",
            ["totalAmount"]  = "i.Quantity * i.UnitPrice",
        };

    private readonly IQueryDbConnectionFactory _queryConnectionFactory;

    /// <summary>Initializes a new <see cref="OrderReadRepository"/>.</summary>
    public OrderReadRepository(IQueryDbConnectionFactory connectionFactory) =>
        _queryConnectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<Order?> GetAggregateByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = _queryConnectionFactory.CreateConnection();

        using var multi = await connection.QueryMultipleAsync(
            OrderQuerySql.GetById, new { Id = orderId });

        var orderRow = await multi.ReadSingleOrDefaultAsync<OrderRow>();
        if (orderRow is null)
        {
            return null;
        }

        var itemRows = (await multi.ReadAsync<OrderItemRow>()).ToList();
        var attrRows = (await multi.ReadAsync<OrderItemAttributeRow>()).ToList();

        return ReconstitutAggregate(orderRow, itemRows, attrRows);
    }

    /// <inheritdoc />
    public async Task<OrderDto?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = _queryConnectionFactory.CreateConnection();

        using var multi = await connection.QueryMultipleAsync(
            OrderQuerySql.GetById, new { Id = orderId });

        var orderRow = await multi.ReadSingleOrDefaultAsync<OrderRow>();
        if (orderRow is null)
        {
            return null;
        }

        var itemRows = (await multi.ReadAsync<OrderItemRow>()).ToList();
        var attrRows = (await multi.ReadAsync<OrderItemAttributeRow>()).ToList();

        return MapToDto(orderRow, itemRows, attrRows);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = _queryConnectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<OrderRow>(OrderQuerySql.GetAll);
        return rows.Select(r => MapToDto(r, [], []));
    }

    /// <inheritdoc />
    public async Task<PagedResult<OrderSummaryDto>> GetPagedSummaryAsync(
        int page,
        int pageSize,
        string? sortColumn,
        string? sortDirection,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var orderBy       = BuildOrderByClause(sortColumn, sortDirection);
        var searchClause  = string.IsNullOrWhiteSpace(searchTerm)
            ? OrderQuerySql.NoSearchPredicate
            : OrderQuerySql.SearchPredicate;

        var sql = string.Format(OrderQuerySql.GetPagedSummaryTemplate, orderBy, searchClause);

        using IDbConnection connection = _queryConnectionFactory.CreateConnection();

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            Offset     = (page - 1) * pageSize,
            PageSize   = pageSize,
            SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : $"%{searchTerm.Trim()}%",
        });

        var totalCount = (int)(await multi.ReadSingleAsync<long>());
        var items      = (await multi.ReadAsync<OrderSummaryRow>())
                             .Select(MapToSummaryDto)
                             .ToList()
                             .AsReadOnly();

        return new PagedResult<OrderSummaryDto>(items, totalCount, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderSummaryDto>> GetAllSummaryAsync(
        string? sortColumn,
        string? sortDirection,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var orderBy      = BuildOrderByClause(sortColumn, sortDirection);
        var searchClause = string.IsNullOrWhiteSpace(searchTerm)
            ? OrderQuerySql.NoSearchPredicate
            : OrderQuerySql.SearchPredicate;

        var sql = string.Format(OrderQuerySql.GetAllSummaryTemplate, orderBy, searchClause);

        using IDbConnection connection = _queryConnectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<OrderSummaryRow>(sql, new
        {
            SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : $"%{searchTerm.Trim()}%",
        });

        return rows.Select(MapToSummaryDto).ToList().AsReadOnly();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the ORDER BY clause from the whitelisted column map.
    /// Falls back to <c>o.OrderDate DESC</c> for any unknown or null input.
    /// The sort direction is also validated against a fixed set so no raw text escapes.
    /// </summary>
    private static string BuildOrderByClause(string? sortColumn, string? sortDirection)
    {
        var col = (sortColumn is not null && SortColumnMap.TryGetValue(sortColumn, out var mapped))
            ? mapped
            : "o.OrderDate";

        var dir = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : "DESC";

        return $"{col} {dir}";
    }

    private static OrderSummaryDto MapToSummaryDto(OrderSummaryRow row) =>
        new(row.OrderId,
            row.CustomerName,
            row.OrderDate,
            row.OrderItemId,
            row.ProductName,
            row.Quantity,
            row.UnitPrice,
            row.TotalAmount);

    private static OrderDto MapToDto(
        OrderRow order,
        List<OrderItemRow> items,
        List<OrderItemAttributeRow> attrs)
    {
        var itemDtos = items.Select(i =>
        {
            var attrDtos = attrs
                .Where(a => a.OrderItemId == i.Id)
                .Select(a => new OrderItemAttributeDto(
                    a.Id,
                    a.OrderItemId,
                    a.AttributeName,
                    a.AttributeValue,
                    a.IsActive,
                    a.CreatedAt,
                    a.UpdatedAt,
                    a.CreatedBy,
                    a.UpdatedBy))
                .ToList()
                .AsReadOnly();

            return new OrderItemDto(
                i.Id,
                i.OrderId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.Quantity * i.UnitPrice,
                i.IsActive,
                attrDtos,
                i.CreatedAt,
                i.UpdatedAt,
                i.CreatedBy,
                i.UpdatedBy);
        }).ToList().AsReadOnly();

        return new OrderDto(
            order.Id,
            order.CustomerName,
            order.OrderDate,
            order.IsActive,
            itemDtos,
            order.CreatedAt,
            order.UpdatedAt,
            order.CreatedBy,
            order.UpdatedBy);
    }

    private static Order ReconstitutAggregate(
        OrderRow order,
        List<OrderItemRow> items,
        List<OrderItemAttributeRow> attrs)
    {
        var itemEntities = items.Select(i =>
        {
            var attrEntities = attrs
                .Where(a => a.OrderItemId == i.Id)
                .Select(a => OrderItemAttribute.Reconstitute(
                    a.Id,
                    a.OrderItemId,
                    a.AttributeName,
                    a.AttributeValue,
                    a.IsActive,
                    a.CreatedBy,
                    a.CreatedAt,
                    a.UpdatedBy,
                    a.UpdatedAt));

            return OrderItem.Reconstitute(
                i.Id,
                i.OrderId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.IsActive,
                i.CreatedBy,
                i.CreatedAt,
                i.UpdatedBy,
                i.UpdatedAt,
                attrEntities);
        });

        return Order.Reconstitute(
            order.Id,
            order.CustomerName,
            order.OrderDate,
            order.IsActive,
            order.CreatedBy,
            order.CreatedAt,
            order.UpdatedBy,
            order.UpdatedAt,
            itemEntities);
    }

    // ── Private row types ────────────────────────────────────────────────────

    private sealed class OrderRow
    {
        public int Id { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public DateTime OrderDate { get; init; }
        public bool IsActive { get; init; }
        public Guid CreatedBy { get; init; }
        public DateTime CreatedAt { get; init; }
        public Guid UpdatedBy { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    private sealed class OrderItemRow
    {
        public int Id { get; init; }
        public int OrderId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public bool IsActive { get; init; }
        public Guid CreatedBy { get; init; }
        public DateTime CreatedAt { get; init; }
        public Guid UpdatedBy { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    private sealed class OrderItemAttributeRow
    {
        public int Id { get; init; }
        public int OrderItemId { get; init; }
        public string AttributeName { get; init; } = string.Empty;
        public string AttributeValue { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public Guid CreatedBy { get; init; }
        public DateTime CreatedAt { get; init; }
        public Guid UpdatedBy { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    private sealed class OrderSummaryRow
    {
        public int OrderId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public DateTime OrderDate { get; init; }
        public int OrderItemId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal TotalAmount { get; init; }
    }
}
