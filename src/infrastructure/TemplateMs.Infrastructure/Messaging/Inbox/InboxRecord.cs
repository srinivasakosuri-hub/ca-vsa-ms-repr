namespace TemplateMs.Infrastructure.Messaging.Inbox;

/// <summary>Represents an inbox record that tracks received integration events for idempotent processing.</summary>
public sealed class InboxRecord
{
    /// <summary>Gets or sets the unique identifier for this inbox entry (typically the message id from the broker).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the fully qualified type name of the received integration event.</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON-serialized payload of the received event.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the message was received.</summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the message was successfully processed.</summary>
    public DateTime? ProcessedAt { get; set; }
}
