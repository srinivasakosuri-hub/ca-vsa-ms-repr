using FluentAssertions;
using Xunit;

namespace TemplateMs.Infrastructure.Tests;

/// <summary>Unit tests for the SQL constants in <see cref="Sql.Features.Order.Commands.OrderCommandSql"/> and queries.</summary>
public sealed class RepositoryTests
{
    [Fact]
    public void OrderCommandSqlInsertIsNotNullOrEmpty()
    {
        // Assert
        Sql.Features.Order.Commands.OrderCommandSql.Insert.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void OrderCommandSqlUpdateStatusIsNotNullOrEmpty()
    {
        // Assert
        Sql.Features.Order.Commands.OrderCommandSql.UpdateStatus.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void OrderQuerySqlGetByIdIsNotNullOrEmpty()
    {
        // Assert
        Sql.Features.Order.Queries.OrderQuerySql.GetById.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void OrderQuerySqlGetAllIsNotNullOrEmpty()
    {
        // Assert
        Sql.Features.Order.Queries.OrderQuerySql.GetAll.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void OutboxSqlInsertContainsInsertKeyword()
    {
        // Assert
        Sql.Infrastructure.Outbox.OutboxSql.Insert.Should().ContainEquivalentOf("INSERT");
    }

    [Fact]
    public void OutboxSqlSelectUnprocessedContainsSelectKeyword()
    {
        // Assert
        Sql.Infrastructure.Outbox.OutboxSql.SelectUnprocessed.Should().ContainEquivalentOf("SELECT");
    }

    [Fact]
    public void OutboxSqlMarkProcessedContainsUpdateKeyword()
    {
        // Assert
        Sql.Infrastructure.Outbox.OutboxSql.MarkProcessed.Should().ContainEquivalentOf("UPDATE");
    }

    [Fact]
    public void OrderCommandSqlInsertContainsExpectedColumns()
    {
        // Assert
        Sql.Features.Order.Commands.OrderCommandSql.Insert
            .Should().Contain("CustomerName")
            .And.Contain("OrderDate")
            .And.Contain("IsActive");
    }

    [Fact]
    public void OrderQuerySqlGetByIdContainsWhereClause()
    {
        // Assert
        Sql.Features.Order.Queries.OrderQuerySql.GetById.Should().ContainEquivalentOf("WHERE");
    }

    [Fact]
    public void ActiveMqConfigurationSectionNameIsCorrect()
    {
        // Assert
        Messaging.Amq.ActiveMqConfiguration.SectionName.Should().Be("ActiveMQ");
    }

    [Fact]
    public void InfrastructureOptionsSectionNameIsCorrect()
    {
        // Assert
        Configuration.InfrastructureOptions.SectionName.Should().Be("Infrastructure");
    }
}
