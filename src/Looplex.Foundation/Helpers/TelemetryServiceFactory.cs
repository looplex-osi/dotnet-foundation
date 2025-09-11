using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Looplex.Foundation.Adapters.Telemetry;
using Looplex.Foundation.Ports;

namespace Looplex.Foundation.Helpers;

/// <summary>
/// Factory for creating telemetry service instances.
/// This factory provides a centralized way to create telemetry services based on provider type,
/// following the factory pattern used throughout the Foundation project.
/// </summary>
public static class TelemetryServiceFactory
{
    /// <summary>
    /// Creates an OpenTelemetry telemetry service instance.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="serviceVersion">The version of the service.</param>
    /// <param name="environment">The environment (e.g., Development, Production).</param>
    /// <param name="globalAttributes">Optional global attributes to include with all telemetry.</param>
    /// <returns>An OpenTelemetry telemetry service instance.</returns>
    public static ITelemetryService CreateOpenTelemetry(string serviceName, string serviceVersion = "1.0.0", string environment = "Development", Dictionary<string, object>? globalAttributes = null)
    {
        return new OpenTelemetryAdapter(serviceName, serviceVersion, environment, globalAttributes);
    }

    /// <summary>
    /// Creates an Application Insights telemetry service instance.
    /// </summary>
    /// <param name="connectionString">The Application Insights connection string.</param>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="serviceVersion">The version of the service.</param>
    /// <param name="environment">The environment (e.g., Development, Production).</param>
    /// <param name="globalAttributes">Optional global attributes to include with all telemetry.</param>
    /// <returns>An Application Insights telemetry service instance.</returns>
    public static ITelemetryService CreateApplicationInsights(string connectionString, string serviceName, string serviceVersion = "1.0.0", string environment = "Development", Dictionary<string, object>? globalAttributes = null)
    {
        return new ApplicationInsightsAdapter(connectionString, serviceName, serviceVersion, environment, globalAttributes);
    }

    /// <summary>
    /// Creates a DataDog telemetry service instance.
    /// </summary>
    /// <param name="apiKey">The DataDog API key.</param>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="serviceVersion">The version of the service.</param>
    /// <param name="environment">The environment (e.g., Development, Production).</param>
    /// <param name="site">The DataDog site (default: datadoghq.com).</param>
    /// <param name="globalAttributes">Optional global attributes to include with all telemetry.</param>
    /// <returns>A DataDog telemetry service instance.</returns>
    public static ITelemetryService CreateDataDog(string apiKey, string serviceName, string serviceVersion = "1.0.0", string environment = "Development", string site = "datadoghq.com", Dictionary<string, object>? globalAttributes = null)
    {
        return new DataDogAdapter(apiKey, serviceName, serviceVersion, environment, site, globalAttributes);
    }

    /// <summary>
    /// Creates a NoOp telemetry service instance.
    /// </summary>
    /// <returns>A NoOp telemetry service instance.</returns>
    public static ITelemetryService CreateNoOp()
    {
        return new NoOpTelemetryAdapter();
    }

    /// <summary>
    /// Creates a telemetry service instance with default configuration.
    /// </summary>
    /// <returns>A NoOp telemetry service instance.</returns>
    public static ITelemetryService CreateDefault()
    {
        return new NoOpTelemetryAdapter();
    }

    /// <summary>
    /// Creates a telemetry service instance based on configuration.
    /// This method is used by applications like Notejam that have their own configuration classes.
    /// </summary>
    /// <param name="config">The telemetry configuration object.</param>
    /// <returns>A telemetry service instance.</returns>
    public static ITelemetryService CreateTelemetryService(object config)
    {
        // Validate configuration first
        var (isValid, errors) = ValidateConfiguration(config);
        if (!isValid)
        {
            throw new ArgumentException($"Invalid telemetry configuration: {string.Join(", ", errors)}");
        }

        // Use reflection to access configuration properties
        var provider = GetPropertyValue<string>(config, "Provider") ?? "NoOp";
        var serviceName = GetPropertyValue<string>(config, "ServiceName") ?? "unknown";
        var serviceVersion = GetPropertyValue<string>(config, "ServiceVersion") ?? "1.0.0";
        var environment = GetPropertyValue<string>(config, "Environment") ?? "development";
        var globalAttributes = GetPropertyValue<Dictionary<string, object>?>(config, "GlobalAttributes");

        return provider.ToLowerInvariant() switch
        {
            "opentelemetry" => CreateOpenTelemetry(serviceName, serviceVersion, environment, globalAttributes),
            "applicationinsights" => CreateApplicationInsights(
                GetPropertyValue<string>(config, "ApplicationInsightsConnectionString") ?? string.Empty,
                serviceName, serviceVersion, environment, globalAttributes),
            "datadog" => CreateDataDog(
                GetPropertyValue<string>(config, "DataDogApiKey") ?? string.Empty,
                serviceName, serviceVersion, environment,
                GetPropertyValue<string>(config, "DataDogSite") ?? "datadoghq.com", globalAttributes),
            "noop" => CreateNoOp(),
            _ => CreateDefault()
        };
    }

    /// <summary>
    /// Validates telemetry configuration parameters.
    /// </summary>
    /// <param name="config">The configuration object to validate.</param>
    /// <returns>Validation result with errors if any.</returns>
    public static (bool IsValid, List<string> Errors) ValidateConfiguration(object config)
    {
        var errors = new List<string>();
        
        if (config == null)
        {
            errors.Add("Configuration object cannot be null");
            return (false, errors);
        }

        var provider = GetPropertyValue<string>(config, "Provider") ?? "NoOp";
        var serviceName = GetPropertyValue<string>(config, "ServiceName");
        var serviceVersion = GetPropertyValue<string>(config, "ServiceVersion");
        var environment = GetPropertyValue<string>(config, "Environment");

        // Validate required fields
        if (string.IsNullOrWhiteSpace(serviceName))
            errors.Add("ServiceName is required");

        if (string.IsNullOrWhiteSpace(serviceVersion))
            errors.Add("ServiceVersion is required");

        if (string.IsNullOrWhiteSpace(environment))
            errors.Add("Environment is required");

        // Validate provider-specific requirements
        switch (provider.ToLowerInvariant())
        {
            case "applicationinsights":
                var connectionString = GetPropertyValue<string>(config, "ApplicationInsightsConnectionString");
                if (string.IsNullOrWhiteSpace(connectionString))
                    errors.Add("ApplicationInsightsConnectionString is required for ApplicationInsights provider");
                break;

            case "datadog":
                var apiKey = GetPropertyValue<string>(config, "DataDogApiKey");
                if (string.IsNullOrWhiteSpace(apiKey))
                    errors.Add("DataDogApiKey is required for DataDog provider");
                break;

            case "opentelemetry":
                var endpoint = GetPropertyValue<string>(config, "OpenTelemetryEndpoint");
                if (string.IsNullOrWhiteSpace(endpoint))
                    errors.Add("OpenTelemetryEndpoint is required for OpenTelemetry provider");
                break;
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Performs health check on telemetry service.
    /// </summary>
    /// <param name="telemetryService">The telemetry service to check.</param>
    /// <returns>Health check result.</returns>
    public static async Task<(bool IsHealthy, string Message)> HealthCheckAsync(ITelemetryService telemetryService)
    {
        try
        {
            // Test basic functionality
            var testEvent = "health_check_test";
            var testProperties = new Dictionary<string, object> { ["timestamp"] = DateTime.UtcNow };
            
            // Try to track an event
            telemetryService.TrackEvent(testEvent, testProperties);
            
            // For async services, wait a bit to ensure processing
            if (telemetryService is ITelemetryServiceAsync asyncService)
            {
                await Task.Delay(100); // Give time for async processing
            }
            
            return (true, "Telemetry service is healthy");
        }
        catch (Exception ex)
        {
            return (false, $"Telemetry service health check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs health check on telemetry service (synchronous version).
    /// </summary>
    public static (bool IsHealthy, string Message) HealthCheck(ITelemetryService telemetryService)
    {
        try
        {
            var testEvent = "health_check_test";
            var testProperties = new Dictionary<string, object> { ["timestamp"] = DateTime.UtcNow };
            
            telemetryService.TrackEvent(testEvent, testProperties);
            
            return (true, "Telemetry service is healthy");
        }
        catch (Exception ex)
        {
            return (false, $"Telemetry service health check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to get property value using reflection.
    /// </summary>
    private static T? GetPropertyValue<T>(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        if (property != null && property.CanRead)
        {
            var value = property.GetValue(obj);
            if (value is T typedValue)
                return typedValue;
        }
        return default(T);
    }
}
