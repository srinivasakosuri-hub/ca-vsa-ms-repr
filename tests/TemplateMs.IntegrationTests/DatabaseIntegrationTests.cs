using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using TemplateMs.Sql.Features.Order.Commands;
using TemplateMs.Sql.Features.Order.Queries;
using TemplateMs.Sql.Infrastructure.Outbox;
using Xunit;

namespace TemplateMs.IntegrationTests;

/// <summary>
/// Integration tests that exercise SQL operations against a single shared SQL Server
/// container.  The container is started once via <see cref="SqlServerFixture"/> (wired
/// through <see cref="IntegrationTestCollection"/>) and kept running for all tests in
/// both this class and <see cref="EndToEndTests"/>.
/// Requires Docker to be running.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class DatabaseIntegrationTests
{
    private readonly SqlServerFixture _fixture;

    public DatabaseIntegrationTests(SqlServerFixture fixture) => _fixture = fixture;

    private string ConnectionString => _fixture.ConnectionString;

    [Fact]
    public async Task InsertOrder_WithValidData_SuccessfullyPersists()
    {
        using var connection = new SqlConnection(ConnectionString);

        var dbOrderId = await connection.ExecuteScalarAsync<int>(
            OrderCommandSql.Insert,
            new
            {
                CustomerName = "Jane Doe",
                OrderDate = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.NewGuid(),
                UpdatedAt = DateTime.UtcNow
            });

        dbOrderId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetOrderById_AfterInsert_ReturnsCorrectRow()
    {
        using var connection = new SqlConnection(ConnectionString);

        var dbOrderId = await InsertOrderAsync(connection, "Alice");

        using var multi = await connection.QueryMultipleAsync(
            OrderQuerySql.GetById, new { Id = dbOrderId });
        var row = await multi.ReadSingleOrDefaultAsync<dynamic>();

        ((object?)row).Should().NotBeNull();
        ((int)row!.Id).Should().Be(dbOrderId);
        ((string)row.CustomerName).Should().Be("Alice");
    }

    [Fact]
    public async Task GetAllOrders_AfterInsertingTwo_ReturnsBothRows()
    {
        using var connection = new SqlConnection(ConnectionString);
        await InsertOrderAsync(connection, "Bob");
        await InsertOrderAsync(connection, "Charlie");

        var rows = (await connection.QueryAsync<dynamic>(OrderQuerySql.GetAll)).ToList();

        rows.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task UpdateOrderStatus_ChangesIsActiveCorrectly()
    {
        using var connection = new SqlConnection(ConnectionString);
        var dbOrderId = await InsertOrderAsync(connection, "Dave");

        await connection.ExecuteAsync(
            OrderCommandSql.UpdateStatus,
            new { IsActive = false, UpdatedBy = Guid.NewGuid(), UpdatedAt = DateTime.UtcNow, Id = dbOrderId });

        using var multi = await connection.QueryMultipleAsync(
            OrderQuerySql.GetById, new { Id = dbOrderId });
        var updated = await multi.ReadSingleAsync<dynamic>();

        ((bool)updated.IsActive).Should().BeFalse();
    }

    [Fact]
    public async Task InsertOrderItem_WithValidData_SuccessfullyPersists()
    {
        using var connection = new SqlConnection(ConnectionString);
        var dbOrderId = await InsertOrderAsync(connection, "Eve");

        var dbItemId = await connection.ExecuteScalarAsync<int>(
            OrderItemCommandSql.Insert,
            new
            {
                OrderId = dbOrderId,
                ProductName = "Widget",
                Quantity = 3,
                UnitPrice = 9.99m,
                IsActive = true,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.NewGuid(),
                UpdatedAt = DateTime.UtcNow
            });

        dbItemId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeactivateOrderItem_SetsIsActiveFalse()
    {
        using var connection = new SqlConnection(ConnectionString);
        var dbOrderId = await InsertOrderAsync(connection, "Frank");
        var dbItemId = await InsertOrderItemAsync(connection, dbOrderId, "Gadget");

        await connection.ExecuteAsync(
            OrderItemCommandSql.Deactivate,
            new { Id = dbItemId, UpdatedAt = DateTime.UtcNow });

        using var multi = await connection.QueryMultipleAsync(
            OrderQuerySql.GetById, new { Id = dbOrderId });
        await multi.ReadAsync<dynamic>(); // skip order row
        var items = (await multi.ReadAsync<dynamic>()).ToList();

        items.Should().NotBeEmpty();
        var deactivatedItem = items.FirstOrDefault(i => (int)i.Id == dbItemId);
        ((object?)deactivatedItem).Should().NotBeNull();
        ((bool)deactivatedItem!.IsActive).Should().BeFalse();
    }

    [Fact]
    public async Task InsertOrderItemAttribute_WithValidData_SuccessfullyPersists()
    {
        using var connection = new SqlConnection(ConnectionString);
        var dbOrderId = await InsertOrderAsync(connection, "Grace");
        var dbItemId = await InsertOrderItemAsync(connection, dbOrderId, "Widget");

        var dbAttrId = await connection.ExecuteScalarAsync<int>(
            OrderItemAttributeCommandSql.Insert,
            new
            {
                OrderItemId = dbItemId,
                AttributeName = "Color",
                AttributeValue = "Blue",
                IsActive = true,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.NewGuid(),
                UpdatedAt = DateTime.UtcNow
            });

        dbAttrId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task InsertOutboxMessage_WithValidData_SuccessfullyPersists()
    {
        using var connection = new SqlConnection(ConnectionString);

        var rows = await connection.ExecuteAsync(
            OutboxSql.Insert,
            new
            {
                Id = Guid.NewGuid(),
                MessageType = "TemplateMs.Application.Events.OrderCreatedIntegrationEvent",
                Payload = "{\"OrderId\":\"" + Guid.NewGuid() + "\"}",
                CreatedAt = DateTime.UtcNow
            });

        rows.Should().Be(1);
    }

    [Fact]
    public async Task SelectUnprocessedOutbox_AfterInsert_ReturnsRecord()
    {
        using var connection = new SqlConnection(ConnectionString);
        var messageId = Guid.NewGuid();
        await connection.ExecuteAsync(OutboxSql.Insert, new
        {
            Id = messageId,
            MessageType = "TestEvent",
            Payload = "{}",
            CreatedAt = DateTime.UtcNow
        });

        var records = (await connection.QueryAsync<dynamic>(
            OutboxSql.SelectUnprocessed, new { BatchSize = 10 })).ToList();

        records.Any(r => (Guid)r.Id == messageId).Should().BeTrue();
    }

    [Fact]
    public async Task MarkProcessedOutbox_SetsProcessedAt()
    {
        using var connection = new SqlConnection(ConnectionString);
        var messageId = Guid.NewGuid();
        await connection.ExecuteAsync(OutboxSql.Insert, new
        {
            Id = messageId,
            MessageType = "TestEvent2",
            Payload = "{}",
            CreatedAt = DateTime.UtcNow
        });

        await connection.ExecuteAsync(OutboxSql.MarkProcessed,
            new { Id = messageId, ProcessedAt = DateTime.UtcNow });

        var record = await connection.QuerySingleAsync<dynamic>(
            "SELECT Id, ProcessedAt FROM OutboxMessages WHERE Id = @Id", new { Id = messageId });

        ((DateTime?)record.ProcessedAt).Should().NotBeNull();
    }

    private async Task<int> InsertOrderAsync(SqlConnection connection, string customerName)
    {
        return await connection.ExecuteScalarAsync<int>(
            OrderCommandSql.Insert,
            new
            {
                CustomerName = customerName,
                OrderDate = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.NewGuid(),
                UpdatedAt = DateTime.UtcNow
            });
    }

    private async Task<int> InsertOrderItemAsync(SqlConnection connection, int dbOrderId, string productName)
    {
        return await connection.ExecuteScalarAsync<int>(
            OrderItemCommandSql.Insert,
            new
            {
                OrderId = dbOrderId,
                ProductName = productName,
                Quantity = 1,
                UnitPrice = 10m,
                IsActive = true,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.NewGuid(),
                UpdatedAt = DateTime.UtcNow
            });
    }
}
