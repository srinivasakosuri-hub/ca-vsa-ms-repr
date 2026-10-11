namespace TemplateMs.Infrastructure.Messaging.Outbox;

/// <summary>Represents a single outbox record persisted in the <c>OutboxMessages</c> table.</summary>
public sealed class OutboxRecord
{
    /// <summary>Gets or sets the unique identifier for this outbox message.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the fully qualified type name of the integration event.</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON-serialized payload of the integration event.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the outbox record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the message was successfully processed, or null if pending.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Gets or sets the error message if processing failed, otherwise null.</summary>
    public string? Error { get; set; }
}
