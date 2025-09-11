using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Looplex.Foundation.Adapters;
using Looplex.Foundation.Configuration;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;

namespace Looplex.Samples.WebApp.Helpers;

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
    /// <returns>TelemetryOptions configured from environment variables.</returns>
    public static TelemetryOptions LoadTelemetryOptions()
    {
        return LoadTelemetryOptions(new EnvironmentProvider());
    }

    /// <summary>
    /// Loads telemetry configuration from environment variables using the provided environment provider.
    /// </summary>
    /// <param name="environmentProvider">The environment provider to use for accessing environment variables.</param>
    /// <returns>TelemetryOptions configured from environment variables.</returns>
    public static TelemetryOptions LoadTelemetryOptions(IEnvironmentProvider environmentProvider)
    {
        if (environmentProvider == null)
            throw new ArgumentNullException(nameof(environmentProvider));

        var options = new TelemetryOptions
        {
            Provider = environmentProvider.GetEnvironmentVariable("TELEMETRY_PROVIDER", "NoOp"),
            ServiceName = environmentProvider.GetEnvironmentVariable("TELEMETRY_SERVICE_NAME", "notejam"),
            ServiceVersion = environmentProvider.GetEnvironmentVariable("TELEMETRY_SERVICE_VERSION", "1.0.0"),
            Environment = environmentProvider.GetEnvironmentVariable("TELEMETRY_ENVIRONMENT", "development")
        };

        // Load OpenTelemetry configuration
        options.OpenTelemetry = new OpenTelemetryOptions
        {
            Endpoint = environmentProvider.GetEnvironmentVariable("OTEL_ENDPOINT", "http://localhost:4317"),
            ExportProtocol = environmentProvider.GetEnvironmentVariable("OTEL_EXPORT_PROTOCOL", "otlp"),
            EnableConsoleExporter = bool.Parse(environmentProvider.GetEnvironmentVariable("OTEL_ENABLE_CONSOLE_EXPORTER", "true")),
            SampleRate = double.Parse(environmentProvider.GetEnvironmentVariable("OTEL_SAMPLE_RATE", "1.0"), CultureInfo.InvariantCulture),
            EnableHttpInstrumentation = bool.Parse(environmentProvider.GetEnvironmentVariable("OTEL_ENABLE_HTTP_INSTRUMENTATION", "true")),
            EnableSqlInstrumentation = bool.Parse(environmentProvider.GetEnvironmentVariable("OTEL_ENABLE_SQL_INSTRUMENTATION", "true")),
            EnableRedisInstrumentation = bool.Parse(environmentProvider.GetEnvironmentVariable("OTEL_ENABLE_REDIS_INSTRUMENTATION", "false")),
            EnablePluginsInstrumentation = bool.Parse(environmentProvider.GetEnvironmentVariable("OTEL_ENABLE_PLUGINS_INSTRUMENTATION", "true"))
        };

        // Load Application Insights configuration
        options.ApplicationInsights = new ApplicationInsightsOptions
        {
            ConnectionString = environmentProvider.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTIONSTRING", string.Empty),
            InstrumentationKey = environmentProvider.GetEnvironmentVariable("APPLICATIONINSIGHTS_INSTRUMENTATION_KEY", string.Empty)
        };

        // Load DataDog configuration
        options.DataDog = new DataDogOptions
        {
            ApiKey = environmentProvider.GetEnvironmentVariable("DATADOG_API_KEY", string.Empty),
            Site = environmentProvider.GetEnvironmentVariable("DATADOG_SITE", "datadoghq.com"),
            Service = environmentProvider.GetEnvironmentVariable("DATADOG_SERVICE", options.ServiceName)
        };

        // Load global attributes
        var globalAttributesStr = environmentProvider.GetEnvironmentVariable("TELEMETRY_GLOBAL_ATTRIBUTES");
        if (!string.IsNullOrEmpty(globalAttributesStr))
        {
            options.GlobalAttributes = ParseGlobalAttributes(globalAttributesStr);
        }

        return options;
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
