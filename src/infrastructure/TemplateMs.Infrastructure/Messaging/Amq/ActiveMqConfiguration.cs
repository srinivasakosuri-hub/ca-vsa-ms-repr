namespace TemplateMs.Infrastructure.Messaging.Amq;

/// <summary>Strongly typed configuration for the ActiveMQ broker connection.</summary>
public sealed class ActiveMqConfiguration
{
    /// <summary>Configuration section key in <c>appsettings.json</c>.</summary>
    public const string SectionName = "ActiveMQ";

    /// <summary>Gets or sets the broker URL (e.g., <c>activemq:tcp://localhost:61616</c>).</summary>
    public string BrokerUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the ActiveMQ username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the ActiveMQ password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Gets or sets the anycast queue name for publishing order integration events.</summary>
    public string OrderEventsQueue { get; set; } = "order.events";
}
