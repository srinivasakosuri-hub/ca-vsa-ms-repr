namespace TemplateMs.Infrastructure.Telemetry;

/// <summary>
/// Placeholder for OpenTelemetry configuration.
/// Extend this class to add tracing, metrics, and log exporters (e.g., OTLP, Jaeger, Prometheus).
/// </summary>
public static class TelemetryConfiguration
{
    /// <summary>The default service name used in telemetry spans and metrics.</summary>
    public const string ServiceName = "TemplateMs";
}
