using System;
using System.Collections.Generic;
using Looplex.Foundation.Configuration;
using Looplex.Foundation.Ports;

namespace Looplex.Foundation.Adapters.Telemetry;

/// <summary>
/// Application Insights implementation of the ITelemetryService interface.
/// This adapter provides telemetry capabilities using Microsoft Application Insights,
/// including custom events, exceptions, metrics, and traces.
/// </summary>
public class ApplicationInsightsAdapter : ITelemetryService, IDisposable
{
    private readonly TelemetryOptions _options;
    private readonly Microsoft.ApplicationInsights.TelemetryClient? _telemetryClient;

    /// <summary>
    /// Initializes a new instance of the ApplicationInsightsAdapter class.
    /// </summary>
    /// <param name="options">The telemetry configuration options.</param>
    public ApplicationInsightsAdapter(TelemetryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        // Initialize Application Insights telemetry client if connection string is provided
        if (!string.IsNullOrEmpty(_options.ApplicationInsights.ConnectionString))
        {
            var config = Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.CreateDefault();
            config.ConnectionString = _options.ApplicationInsights.ConnectionString;
            _telemetryClient = new Microsoft.ApplicationInsights.TelemetryClient(config);
        }
        else if (!string.IsNullOrEmpty(_options.ApplicationInsights.InstrumentationKey))
        {
            // Legacy support for instrumentation key
            var config = Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.CreateDefault();
            config.InstrumentationKey = _options.ApplicationInsights.InstrumentationKey;
            _telemetryClient = new Microsoft.ApplicationInsights.TelemetryClient(config);
        }
    }

    /// <summary>
    /// Tracks a custom event with optional properties.
    /// </summary>
    /// <param name="eventName">The name of the event to track.</param>
    /// <param name="properties">Optional properties to include with the event.</param>
    public void TrackEvent(string eventName, IDictionary<string, object>? properties = null)
    {
        if (string.IsNullOrWhiteSpace(eventName) || _telemetryClient == null)
            return;

        try
        {
            var eventTelemetry = new Microsoft.ApplicationInsights.DataContracts.EventTelemetry(eventName);
            
            // Add properties
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    eventTelemetry.Properties[property.Key] = property.Value?.ToString() ?? string.Empty;
                }
            }
            
            // Add global attributes
            AddGlobalAttributes(eventTelemetry.Properties);
            
            _telemetryClient.TrackEvent(eventTelemetry);
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
        if (exception == null || _telemetryClient == null)
            return;

        try
        {
            var exceptionTelemetry = new Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry(exception);
            
            // Add properties
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    exceptionTelemetry.Properties[property.Key] = property.Value?.ToString() ?? string.Empty;
                }
            }
            
            // Add global attributes
            AddGlobalAttributes(exceptionTelemetry.Properties);
            
            _telemetryClient.TrackException(exceptionTelemetry);
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
        if (string.IsNullOrWhiteSpace(metricName) || _telemetryClient == null)
            return;

        try
        {
            var metricTelemetry = new Microsoft.ApplicationInsights.DataContracts.MetricTelemetry(metricName, value);
            
            // Add properties
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    metricTelemetry.Properties[property.Key] = property.Value?.ToString() ?? string.Empty;
                }
            }
            
            // Add global attributes
            AddGlobalAttributes(metricTelemetry.Properties);
            
            _telemetryClient.TrackMetric(metricTelemetry);
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
        if (string.IsNullOrWhiteSpace(message) || _telemetryClient == null)
            return;

        try
        {
            var traceTelemetry = new Microsoft.ApplicationInsights.DataContracts.TraceTelemetry(message);
            
            // Add properties
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    traceTelemetry.Properties[property.Key] = property.Value?.ToString() ?? string.Empty;
                }
            }
            
            // Add global attributes
            AddGlobalAttributes(traceTelemetry.Properties);
            
            _telemetryClient.TrackTrace(traceTelemetry);
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent telemetry failures from breaking the application
            System.Diagnostics.Debug.WriteLine($"Failed to track trace '{message}': {ex.Message}");
        }
    }

    /// <summary>
    /// Adds global attributes to telemetry properties.
    /// </summary>
    /// <param name="properties">The properties dictionary to add attributes to.</param>
    private void AddGlobalAttributes(IDictionary<string, string> properties)
    {
        if (_options.GlobalAttributes == null)
            return;

        foreach (var attribute in _options.GlobalAttributes)
        {
            properties[attribute.Key] = attribute.Value?.ToString() ?? string.Empty;
        }
        
        // Add service information
        properties["service.name"] = _options.ServiceName;
        properties["service.version"] = _options.ServiceVersion;
        properties["service.environment"] = _options.Environment;
    }

    /// <summary>
    /// Disposes the Application Insights telemetry client and releases resources.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _telemetryClient?.Flush();
            // Application Insights TelemetryClient doesn't implement IDisposable
            // It's managed by the DI container
        }
        catch (Exception ex)
        {
            // Log the exception but don't throw to avoid disrupting the application
            System.Diagnostics.Debug.WriteLine($"Error disposing ApplicationInsightsAdapter: {ex.Message}");
        }
    }
}
