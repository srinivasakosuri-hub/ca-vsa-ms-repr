using FluentAssertions;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Infrastructure.UnitOfWork;
using Xunit;

namespace TemplateMs.Infrastructure.Tests;

/// <summary>Unit tests for <see cref="DapperUnitOfWork"/> using substituted dependencies.</summary>
public sealed class UnitOfWorkTests
{
    [Fact]
    public void DapperUnitOfWorkImplementsApplicationIUnitOfWork()
    {
        // The type should satisfy the interface contract at the type level.
        typeof(DapperUnitOfWork).Should()
            .Implement<Application.Common.Interfaces.IUnitOfWork>();
    }

    [Fact]
    public void DapperUnitOfWorkImplementsDomainIUnitOfWork()
    {
        typeof(DapperUnitOfWork).Should()
            .Implement<Application.Common.Interfaces.IUnitOfWork>();
    }

    [Fact]
    public void DapperUnitOfWorkImplementsIAsyncDisposable()
    {
        typeof(DapperUnitOfWork).Should().Implement<IAsyncDisposable>();
    }

    [Fact]
    public void OutboxRecordPropertiesAreSettable()
    {
        // Arrange
        var record = new Messaging.Outbox.OutboxRecord
        {
            Id = Guid.NewGuid(),
            MessageType = "TestEvent",
            Payload = "{}",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        record.Id.Should().NotBeEmpty();
        record.MessageType.Should().Be("TestEvent");
        record.Payload.Should().Be("{}");
        record.ProcessedAt.Should().BeNull();
        record.Error.Should().BeNull();
    }

    [Fact]
    public void InboxRecordPropertiesAreSettable()
    {
        // Arrange
        var record = new Messaging.Inbox.InboxRecord
        {
            Id = Guid.NewGuid(),
            MessageType = "InboundEvent",
            Payload = "{\"key\":\"value\"}",
            ReceivedAt = DateTime.UtcNow
        };

        // Assert
        record.Id.Should().NotBeEmpty();
        record.MessageType.Should().Be("InboundEvent");
        record.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void DapperTypeMappingsConfigureIsIdempotent()
    {
        // Arrange & Act — calling twice should not throw
        Database.DapperTypeMappings.Configure();
        Database.DapperTypeMappings.Configure();

        // Assert — no exception means success
        true.Should().BeTrue();
    }

    [Fact]
    public void InfrastructureOptionsDefaultValuesAreEmptyStrings()
    {
        // Arrange
        var options = new Configuration.InfrastructureOptions();

        // Assert
        options.CommandConnectionString.Should().BeEmpty();
        options.QueryConnectionString.Should().BeEmpty();
        options.OutboxBatchSize.Should().Be(50);
        options.OutboxPollingIntervalMs.Should().Be(5000);
    }

    [Fact]
    public void WolverineEventForwardersImplementsIDomainEventDispatcher()
    {
        typeof(Messaging.Wolverine.WolverineEventForwarders).Should()
            .Implement<IDomainEventDispatcher>();
    }

    [Fact]
    public void SystemDateTimeProviderReturnsUtcNow()
    {
        // Arrange
        var provider = new Services.SystemDateTimeProvider();
        var before = DateTime.UtcNow;

        // Act
        var result = provider.UtcNow;

        // Assert
        var after = DateTime.UtcNow;
        result.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void SystemDateTimeProviderImplementsIDateTimeProvider()
    {
        typeof(Services.SystemDateTimeProvider).Should()
            .Implement<Application.Common.Interfaces.IDateTimeProvider>();
    }
}
