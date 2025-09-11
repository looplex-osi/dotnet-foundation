using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Looplex.Foundation.Adapters;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;

namespace Looplex.Samples.WebApp.Helpers;

/// <summary>
/// Configuration class for telemetry settings.
/// </summary>
public class TelemetryConfiguration
{
    public string Provider { get; set; } = "NoOp";
    public string ServiceName { get; set; } = "notejam";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "development";
    public Dictionary<string, object>? GlobalAttributes { get; set; }
    
    // OpenTelemetry specific settings
    public string OpenTelemetryEndpoint { get; set; } = "http://localhost:4317";
    public string OpenTelemetryExportProtocol { get; set; } = "otlp";
    public bool OpenTelemetryEnableConsoleExporter { get; set; } = true;
    public double OpenTelemetrySampleRate { get; set; } = 1.0;
    public bool OpenTelemetryEnableHttpInstrumentation { get; set; } = true;
    public bool OpenTelemetryEnableSqlInstrumentation { get; set; } = true;
    public bool OpenTelemetryEnableRedisInstrumentation { get; set; } = false;
    public bool OpenTelemetryEnablePluginsInstrumentation { get; set; } = true;
    
    // Application Insights specific settings
    public string ApplicationInsightsConnectionString { get; set; } = string.Empty;
    public string ApplicationInsightsInstrumentationKey { get; set; } = string.Empty;
    
    // DataDog specific settings
    public string DataDogApiKey { get; set; } = string.Empty;
    public string DataDogSite { get; set; } = "datadoghq.com";
    public string DataDogService { get; set; } = string.Empty;
}

/// <summary>
/// Helper class for loading telemetry configuration from environment variables.
/// This helper provides a centralized way to load telemetry configuration
/// following the same pattern used for other configuration in the Notejam application.
/// </summary>
public static class TelemetryConfigurationHelper
{
    /// <summary>
    /// Loads telemetry configuration from environment variables.
    /// </summary>
    /// <returns>TelemetryConfiguration configured from environment variables.</returns>
    public static TelemetryConfiguration LoadTelemetryConfiguration()
    {
        return LoadTelemetryConfiguration(new EnvironmentProvider());
    }

    /// <summary>
    /// Loads telemetry configuration from environment variables using the provided environment provider.
    /// </summary>
    /// <param name="environmentProvider">The environment provider to use for accessing environment variables.</param>
    /// <returns>TelemetryConfiguration configured from environment variables.</returns>
    public static TelemetryConfiguration LoadTelemetryConfiguration(IEnvironmentProvider environmentProvider)
    {
        if (environmentProvider == null)
            throw new ArgumentNullException(nameof(environmentProvider));

        var config = new TelemetryConfiguration
        {
            Provider = environmentProvider.GetEnvironmentVariable("TELEMETRY_PROVIDER", "NoOp"),
            ServiceName = environmentProvider.GetEnvironmentVariable("TELEMETRY_SERVICE_NAME", "notejam"),
            ServiceVersion = environmentProvider.GetEnvironmentVariable("TELEMETRY_SERVICE_VERSION", "1.0.0"),
            Environment = environmentProvider.GetEnvironmentVariable("TELEMETRY_ENVIRONMENT", "development"),
            
            // OpenTelemetry configuration
            OpenTelemetryEndpoint = environmentProvider.GetEnvironmentVariable("OTEL_ENDPOINT", "http://localhost:4317"),
            OpenTelemetryExportProtocol = environmentProvider.GetEnvironmentVariable("OTEL_EXPORT_PROTOCOL", "otlp"),
            OpenTelemetryEnableConsoleExporter = bool.Parse(environmentProvider.GetEnvironmentVariable("OTEL_ENABLE_CONSOLE_EXPORTER", "true")),
            OpenTelemetrySampleRate = double.Parse(environmentProvider.GetEnvironmentVariable("OTEL_SAMPLE_RATE", "1.0"), CultureInfo.InvariantCulture),
            OpenTelemetryEnableHttpInstrumentation = bool.Parse(environmentProvider.GetEnvironmentVariable("OTEL_ENABLE_HTTP_INSTRUMENTATION", "true")),
            OpenTelemetryEnableSqlInstrumentation = bool.Parse(environmentProvider.GetEnvironmentVariable("OTEL_ENABLE_SQL_INSTRUMENTATION", "true")),
            OpenTelemetryEnableRedisInstrumentation = bool.Parse(environmentProvider.GetEnvironmentVariable("OTEL_ENABLE_REDIS_INSTRUMENTATION", "false")),
            OpenTelemetryEnablePluginsInstrumentation = bool.Parse(environmentProvider.GetEnvironmentVariable("OTEL_ENABLE_PLUGINS_INSTRUMENTATION", "true")),
            
            // Application Insights configuration
            ApplicationInsightsConnectionString = environmentProvider.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTIONSTRING", string.Empty),
            ApplicationInsightsInstrumentationKey = environmentProvider.GetEnvironmentVariable("APPLICATIONINSIGHTS_INSTRUMENTATION_KEY", string.Empty),
            
            // DataDog configuration
            DataDogApiKey = environmentProvider.GetEnvironmentVariable("DATADOG_API_KEY", string.Empty),
            DataDogSite = environmentProvider.GetEnvironmentVariable("DATADOG_SITE", "datadoghq.com"),
            DataDogService = environmentProvider.GetEnvironmentVariable("DATADOG_SERVICE", "notejam")
        };

        // Load global attributes
        var globalAttributesStr = environmentProvider.GetEnvironmentVariable("TELEMETRY_GLOBAL_ATTRIBUTES");
        if (!string.IsNullOrEmpty(globalAttributesStr))
        {
            config.GlobalAttributes = ParseGlobalAttributes(globalAttributesStr);
        }

        return config;
    }

    /// <summary>
    /// Parses global attributes from a comma-separated string of key=value pairs.
    /// </summary>
    /// <param name="attributesStr">Comma-separated string of key=value pairs.</param>
    /// <returns>Dictionary of global attributes.</returns>
    private static Dictionary<string, object> ParseGlobalAttributes(string attributesStr)
    {
        var attributes = new Dictionary<string, object>();
        
        if (string.IsNullOrWhiteSpace(attributesStr))
            return attributes;

        var pairs = attributesStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                attributes[keyValue[0].Trim()] = keyValue[1].Trim();
            }
        }

        return attributes;
    }
}
