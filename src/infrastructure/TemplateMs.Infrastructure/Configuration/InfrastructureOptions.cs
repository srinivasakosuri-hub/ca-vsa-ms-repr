namespace TemplateMs.Infrastructure.Configuration;

/// <summary>Strongly typed configuration options for the Infrastructure layer.</summary>
public sealed class InfrastructureOptions
{
    /// <summary>Configuration section key used in <c>appsettings.json</c>.</summary>
    public const string SectionName = "Infrastructure";

    /// <summary>Gets or sets the connection string for the command (write) database.</summary>
    public string CommandConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection string for the query (read) database.</summary>
    public string QueryConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of outbox messages processed per batch cycle.</summary>
    public int OutboxBatchSize { get; set; } = 50;

    /// <summary>Gets or sets the outbox processor polling interval in milliseconds.</summary>
    public int OutboxPollingIntervalMs { get; set; } = 5000;
}
