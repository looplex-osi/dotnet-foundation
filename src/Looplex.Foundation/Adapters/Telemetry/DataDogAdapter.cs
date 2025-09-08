using System;
using System.Collections.Generic;
using Looplex.Foundation.Configuration;
using Looplex.Foundation.Ports;

namespace Looplex.Foundation.Adapters.Telemetry;

/// <summary>
/// DataDog implementation of the ITelemetryService interface.
/// This adapter provides telemetry capabilities using DataDog's telemetry APIs,
/// including custom events, exceptions, metrics, and traces.
/// </summary>
public class DataDogAdapter : ITelemetryService, IDisposable
{
    private readonly TelemetryOptions _options;
    private readonly string _apiKey;
    private readonly string _site;
    private readonly string _service;

    /// <summary>
    /// Initializes a new instance of the DataDogAdapter class.
    /// </summary>
    /// <param name="options">The telemetry configuration options.</param>
    public DataDogAdapter(TelemetryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        _apiKey = _options.DataDog.ApiKey ?? throw new ArgumentException("DataDog API key is required", nameof(options));
        _site = _options.DataDog.Site ?? "datadoghq.com";
        _service = _options.DataDog.Service ?? _options.ServiceName;
    }

    /// <summary>
    /// Tracks a custom event with optional properties.
    /// </summary>
    /// <param name="eventName">The name of the event to track.</param>
    /// <param name="properties">Optional properties to include with the event.</param>
    public void TrackEvent(string eventName, IDictionary<string, object>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return;

        try
        {
            // Create event data
            var eventData = new
            {
                title = eventName,
                text = $"Event: {eventName}",
                alert_type = "info",
                source_type_name = _service,
                tags = CreateTags(properties)
            };

            // Send to DataDog Events API
            SendToDataDog("events", eventData);
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent telemetry failures from breaking the application
            System.Diagnostics.Debug.WriteLine($"Failed to track event '{eventName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Tracks an exception with optional properties.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="properties">Optional properties to include with the exception.</param>
    public void TrackException(Exception exception, IDictionary<string, object>? properties = null)
    {
        if (exception == null)
            return;

        try
        {
            // Create exception data
            var exceptionData = new
            {
                title = $"Exception: {exception.GetType().Name}",
                text = $"Exception: {exception.Message}\nStack Trace: {exception.StackTrace}",
                alert_type = "error",
                source_type_name = _service,
                tags = CreateTags(properties, new Dictionary<string, object>
                {
                    ["exception.type"] = exception.GetType().FullName ?? string.Empty,
                    ["exception.message"] = exception.Message,
                    ["exception.stacktrace"] = exception.StackTrace ?? string.Empty
                })
            };

            // Send to DataDog Events API
            SendToDataDog("events", exceptionData);
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent telemetry failures from breaking the application
            System.Diagnostics.Debug.WriteLine($"Failed to track exception '{exception.GetType().Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Tracks a custom metric with a numeric value and optional properties.
    /// </summary>
    /// <param name="metricName">The name of the metric to track.</param>
    /// <param name="value">The numeric value of the metric.</param>
    /// <param name="properties">Optional properties to include with the metric.</param>
    public void TrackMetric(string metricName, double value, IDictionary<string, object>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            return;

        try
        {
            // Create metric data
            var metricData = new
            {
                series = new[]
                {
                    new
                    {
                        metric = metricName,
                        points = new[] { new[] { DateTimeOffset.UtcNow.ToUnixTimeSeconds(), value } },
                        type = "gauge",
                        tags = CreateTags(properties)
                    }
                }
            };

            // Send to DataDog Metrics API
            SendToDataDog("series", metricData);
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent telemetry failures from breaking the application
            System.Diagnostics.Debug.WriteLine($"Failed to track metric '{metricName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Tracks a custom trace message with optional properties.
    /// </summary>
    /// <param name="message">The trace message to track.</param>
    /// <param name="properties">Optional properties to include with the trace.</param>
    public void TrackTrace(string message, IDictionary<string, object>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        try
        {
            // Create trace data
            var traceData = new
            {
                title = "Trace",
                text = message,
                alert_type = "info",
                source_type_name = _service,
                tags = CreateTags(properties)
            };

            // Send to DataDog Events API
            SendToDataDog("events", traceData);
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent telemetry failures from breaking the application
            System.Diagnostics.Debug.WriteLine($"Failed to track trace '{message}': {ex.Message}");
        }
    }

    /// <summary>
    /// Creates tags from properties and global attributes.
    /// </summary>
    /// <param name="properties">Optional properties to include.</param>
    /// <param name="additionalTags">Additional tags to include.</param>
    /// <returns>Array of tag strings.</returns>
    private string[] CreateTags(IDictionary<string, object>? properties = null, IDictionary<string, object>? additionalTags = null)
    {
        var tags = new List<string>();

        // Add service information
        tags.Add($"service:{_service}");
        tags.Add($"version:{_options.ServiceVersion}");
        tags.Add($"environment:{_options.Environment}");

        // Add global attributes
        if (_options.GlobalAttributes != null)
        {
            foreach (var attribute in _options.GlobalAttributes)
            {
                tags.Add($"{attribute.Key}:{attribute.Value}");
            }
        }

        // Add properties
        if (properties != null)
        {
            foreach (var property in properties)
            {
                tags.Add($"{property.Key}:{property.Value}");
            }
        }

        // Add additional tags
        if (additionalTags != null)
        {
            foreach (var tag in additionalTags)
            {
                tags.Add($"{tag.Key}:{tag.Value}");
            }
        }

        return tags.ToArray();
    }

    /// <summary>
    /// Sends data to DataDog API.
    /// </summary>
    /// <param name="endpoint">The API endpoint to send data to.</param>
    /// <param name="data">The data to send.</param>
    private void SendToDataDog(string endpoint, object data)
    {
        // Note: This is a simplified implementation
        // In a real implementation, you would use HttpClient to send data to DataDog's API
        // For now, we'll just log the data to demonstrate the structure
        
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        System.Diagnostics.Debug.WriteLine($"DataDog {endpoint}: {json}");
        
        // TODO: Implement actual HTTP call to DataDog API
        // Example: POST https://api.datadoghq.com/api/v1/{endpoint}
        // Headers: DD-API-KEY: {_apiKey}, Content-Type: application/json
    }

    /// <summary>
    /// Disposes the DataDog adapter and releases resources.
    /// </summary>
    public void Dispose()
    {
        try
        {
            // DataDog adapter doesn't maintain persistent connections
            // but we can add cleanup logic here if needed in the future
        }
        catch (Exception ex)
        {
            // Log the exception but don't throw to avoid disrupting the application
            System.Diagnostics.Debug.WriteLine($"Error disposing DataDogAdapter: {ex.Message}");
        }
    }
}
