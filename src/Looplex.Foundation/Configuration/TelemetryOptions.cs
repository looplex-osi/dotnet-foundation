using System.Collections.Generic;

namespace Looplex.Foundation.Configuration;

/// <summary>
/// Configuration options for telemetry services.
/// This class provides a flexible way to configure different telemetry providers
/// and their specific settings through environment variables or configuration files.
/// </summary>
public class TelemetryOptions
{
    /// <summary>
    /// The telemetry provider to use (e.g., "OpenTelemetry", "ApplicationInsights", "DataDog", "NoOp").
    /// </summary>
    public string Provider { get; set; } = "NoOp";

    /// <summary>
    /// The service name to use in telemetry data.
    /// </summary>
    public string ServiceName { get; set; } = "looplex-foundation";

    /// <summary>
    /// The service version to use in telemetry data.
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// The environment name (e.g., "development", "staging", "production").
    /// </summary>
    public string Environment { get; set; } = "development";

    /// <summary>
    /// OpenTelemetry specific configuration.
    /// </summary>
    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();

    /// <summary>
    /// Application Insights specific configuration.
    /// </summary>
    public ApplicationInsightsOptions ApplicationInsights { get; set; } = new();

    /// <summary>
    /// DataDog specific configuration.
    /// </summary>
    public DataDogOptions DataDog { get; set; } = new();

    /// <summary>
    /// Global attributes to include with all telemetry data.
    /// </summary>
    public Dictionary<string, object> GlobalAttributes { get; set; } = new();
}

/// <summary>
/// OpenTelemetry specific configuration options.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// The OpenTelemetry collector endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// The export protocol to use (e.g., "otlp", "jaeger", "zipkin").
    /// </summary>
    public string ExportProtocol { get; set; } = "otlp";

    /// <summary>
    /// Whether to enable console exporter for debugging.
    /// </summary>
    public bool EnableConsoleExporter { get; set; } = false;

    /// <summary>
    /// The sampling rate (0.0 to 1.0).
    /// </summary>
    public double SampleRate { get; set; } = 1.0;

    /// <summary>
    /// Whether to enable HTTP instrumentation.
    /// </summary>
    public bool EnableHttpInstrumentation { get; set; } = true;

    /// <summary>
    /// Whether to enable SQL instrumentation.
    /// </summary>
    public bool EnableSqlInstrumentation { get; set; } = true;

    /// <summary>
    /// Whether to enable Redis instrumentation.
    /// </summary>
    public bool EnableRedisInstrumentation { get; set; } = false;

    /// <summary>
    /// Whether to enable plugins instrumentation.
    /// </summary>
    public bool EnablePluginsInstrumentation { get; set; } = true;
}

/// <summary>
/// Application Insights specific configuration options.
/// </summary>
public class ApplicationInsightsOptions
{
    /// <summary>
    /// The Application Insights connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The instrumentation key (legacy).
    /// </summary>
    public string InstrumentationKey { get; set; } = string.Empty;
}

/// <summary>
/// DataDog specific configuration options.
/// </summary>
public class DataDogOptions
{
    /// <summary>
    /// The DataDog API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The DataDog site (e.g., "datadoghq.com", "datadoghq.eu").
    /// </summary>
    public string Site { get; set; } = "datadoghq.com";

    /// <summary>
    /// The DataDog service name.
    /// </summary>
    public string Service { get; set; } = string.Empty;
}
