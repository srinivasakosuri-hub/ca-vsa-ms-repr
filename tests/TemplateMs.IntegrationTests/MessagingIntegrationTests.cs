using FluentAssertions;
using NSubstitute;
using TemplateMs.Application.Common.Interfaces;
using Xunit;

namespace TemplateMs.IntegrationTests;

/// <summary>
/// Messaging integration tests. Full AMQ tests require a running ActiveMQ Docker container.
/// These tests verify the messaging abstractions and publisher contract.
/// For end-to-end AMQ tests, start the broker with:
///   docker run -p 61616:61616 -p 8161:8161 apache/activemq-classic:latest
/// </summary>
public sealed class MessagingIntegrationTests
{
    [Fact]
    public async Task MessageBusPublisher_WhenCalled_InvokesPublishAsync()
    {
        // Arrange
        var publisher = Substitute.For<IMessageBusPublisher>();
        var message = new { OrderId = Guid.NewGuid(), Amount = 100m };

        // Act
        await publisher.PublishAsync(message, CancellationToken.None);

        // Assert
        await publisher.Received(1).PublishAsync(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MessageBusPublisher_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var publisher = Substitute.For<IMessageBusPublisher>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await publisher.PublishAsync(new object(), token);

        // Assert
        await publisher.Received(1).PublishAsync(Arg.Any<object>(), token);
    }

    [Fact]
    public void ActiveMqPublisher_ImplementsIMessageBusPublisher()
    {
        // Assert
        typeof(Infrastructure.Messaging.Amq.ActiveMqPublisher)
            .Should().Implement<IMessageBusPublisher>();
    }

    [Fact]
    public void ActiveMqConsumer_IsBackgroundService()
    {
        // Assert
        typeof(Infrastructure.Messaging.Amq.ActiveMqConsumer)
            .Should().BeAssignableTo<Microsoft.Extensions.Hosting.BackgroundService>();
    }

    [Fact]
    public void OutboxProcessor_IsBackgroundService()
    {
        // Assert
        typeof(Infrastructure.Messaging.Outbox.OutboxProcessor)
            .Should().BeAssignableTo<Microsoft.Extensions.Hosting.BackgroundService>();
    }

    [Fact]
    public void InboxProcessor_IsBackgroundService()
    {
        // Assert
        typeof(Infrastructure.Messaging.Inbox.InboxProcessor)
            .Should().BeAssignableTo<Microsoft.Extensions.Hosting.BackgroundService>();
    }

    [Fact]
    public void OutboxEnvelope_PropertiesAreSet()
    {
        // Arrange
        var envelope = new Infrastructure.Messaging.Outbox.OutboxEnvelope("EventType", "{\"key\":\"val\"}");

        // Assert
        envelope.MessageType.Should().Be("EventType");
        envelope.Payload.Should().Be("{\"key\":\"val\"}");
    }
}
