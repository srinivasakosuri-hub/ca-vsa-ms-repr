using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using TemplateMs.Sql.Features.Order.Commands;
using TemplateMs.Sql.Features.Order.Queries;
using Xunit;

namespace TemplateMs.IntegrationTests;

/// <summary>
/// End-to-end integration tests verifying the full create-order flow against a real SQL
/// Server database.  The container is started once via <see cref="SqlServerFixture"/>
/// (wired through <see cref="IntegrationTestCollection"/>) and shared with
/// <see cref="DatabaseIntegrationTests"/>.
/// Requires Docker to be running.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class EndToEndTests
{
    private readonly SqlServerFixture _fixture;

    public EndToEndTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateAndRetrieveOrder_FullRoundTrip_SucceedsWithCorrectData()
    {
        using var connection = new SqlConnection(_fixture.ConnectionString);
        const string customerName = "John Smith";
        var orderDate = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc);

        var dbOrderId = await connection.ExecuteScalarAsync<int>(OrderCommandSql.Insert, new
        {
            CustomerName = customerName,
            OrderDate = orderDate,
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        });

        using var multi = await connection.QueryMultipleAsync(
            OrderQuerySql.GetById, new { Id = dbOrderId });
        var row = await multi.ReadSingleAsync<dynamic>();

        ((int)row.Id).Should().Be(dbOrderId);
        ((string)row.CustomerName).Should().Be(customerName);
        ((bool)row.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task CreateOrderWithItemsAndAttributes_FullRoundTrip_SucceedsWithCorrectData()
    {
        using var connection = new SqlConnection(_fixture.ConnectionString);

        var dbOrderId = await connection.ExecuteScalarAsync<int>(OrderCommandSql.Insert, new
        {
            CustomerName = "Alice",
            OrderDate = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        });

        var dbItemId = await connection.ExecuteScalarAsync<int>(OrderItemCommandSql.Insert, new
        {
            OrderId = dbOrderId,
            ProductName = "Widget",
            Quantity = 2,
            UnitPrice = 15m,
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        });

        var dbAttrId = await connection.ExecuteScalarAsync<int>(OrderItemAttributeCommandSql.Insert, new
        {
            OrderItemId = dbItemId,
            AttributeName = "Color",
            AttributeValue = "Red",
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        });

        using var multi = await connection.QueryMultipleAsync(
            OrderQuerySql.GetById, new { Id = dbOrderId });
        var orderRow = await multi.ReadSingleAsync<dynamic>();
        var itemRows = (await multi.ReadAsync<dynamic>()).ToList();
        var attrRows = (await multi.ReadAsync<dynamic>()).ToList();

        ((int)orderRow.Id).Should().Be(dbOrderId);
        var foundItem = itemRows.FirstOrDefault(i => (int)i.Id == dbItemId);
        ((object?)foundItem).Should().NotBeNull();
        var foundAttr = attrRows.FirstOrDefault(a => (int)a.Id == dbAttrId);
        ((object?)foundAttr).Should().NotBeNull();
    }

    [Fact]
    public async Task CreateOrderAndWriteOutbox_InSameTransaction_BothPersist()
    {
        using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var dbOrderId = await connection.ExecuteScalarAsync<int>(OrderCommandSql.Insert, new
        {
            CustomerName = "Bob",
            OrderDate = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        }, transaction);

        var outboxId = Guid.NewGuid();
        await connection.ExecuteAsync(Sql.Infrastructure.Outbox.OutboxSql.Insert, new
        {
            Id = outboxId,
            MessageType = "TemplateMs.Application.Events.OrderCreatedIntegrationEvent",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { OrderId = dbOrderId }),
            CreatedAt = DateTime.UtcNow
        }, transaction);

        transaction.Commit();

        // Assert order exists — dispose the GridReader before reusing the connection.
        dynamic? order;
        using (var readConnection = new SqlConnection(_fixture.ConnectionString))
        {
            using var multi = await readConnection.QueryMultipleAsync(
                OrderQuerySql.GetById, new { Id = dbOrderId });
            order = await multi.ReadSingleOrDefaultAsync<dynamic>();
        }

        // Assert outbox record exists on a fresh connection.
        dynamic? outbox;
        using (var readConnection = new SqlConnection(_fixture.ConnectionString))
        {
            outbox = await readConnection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT Id FROM OutboxMessages WHERE Id = @Id", new { Id = outboxId });
        }

        ((object?)order).Should().NotBeNull();
        ((object?)outbox).Should().NotBeNull();
    }

    [Fact]
    public async Task DeactivateOrder_ThenRetrieve_ReflectsInactiveStatus()
    {
        using var connection = new SqlConnection(_fixture.ConnectionString);

        var dbOrderId = await connection.ExecuteScalarAsync<int>(OrderCommandSql.Insert, new
        {
            CustomerName = "Charlie",
            OrderDate = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        });

        await connection.ExecuteAsync(OrderCommandSql.UpdateStatus, new
        {
            IsActive = false,
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow,
            Id = dbOrderId
        });

        using var multi = await connection.QueryMultipleAsync(
            OrderQuerySql.GetById, new { Id = dbOrderId });
        var updated = await multi.ReadSingleAsync<dynamic>();

        ((bool)updated.IsActive).Should().BeFalse();
    }

    [Fact]
    public async Task RolledBackTransaction_LeavesNoPersistenceTraces()
    {
        using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var dbOrderId = await connection.ExecuteScalarAsync<int>(OrderCommandSql.Insert, new
        {
            CustomerName = "Dave",
            OrderDate = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        }, transaction);

        transaction.Rollback();

        using var multi = await connection.QueryMultipleAsync(
            OrderQuerySql.GetById, new { Id = dbOrderId });
        var result = await multi.ReadSingleOrDefaultAsync<dynamic>();

        ((object?)result).Should().BeNull();
    }
}
