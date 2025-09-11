using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Looplex.Foundation.Configuration;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace Looplex.Foundation.Adapters.Telemetry;

/// <summary>
/// OpenTelemetry implementation of the ITelemetryService interface.
/// This adapter provides comprehensive telemetry capabilities using OpenTelemetry,
/// including distributed tracing, metrics, and logging.
/// </summary>
public class OpenTelemetryAdapter : ITelemetryService, IDisposable
{
    private readonly TelemetryOptions _options;
    private readonly Tracer _tracer;
    private readonly Meter _meter;
    private readonly ActivitySource _activitySource;
    private readonly Counter<long> _eventCounter;
    private readonly Counter<long> _exceptionCounter;
    private readonly Counter<long> _metricCounter;
    private readonly Counter<long> _traceCounter;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryAdapter class.
    /// </summary>
    /// <param name="options">The telemetry configuration options.</param>
    public OpenTelemetryAdapter(TelemetryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        // Create activity source for distributed tracing
        _activitySource = new ActivitySource(_options.ServiceName, _options.ServiceVersion);
        
        // Create tracer
        _tracer = TracerProvider.Default.GetTracer(_options.ServiceName, _options.ServiceVersion);
        
        // Create meter for metrics
        _meter = new Meter(_options.ServiceName, _options.ServiceVersion);
        
        // Create counters for tracking telemetry operations
        _eventCounter = _meter.CreateCounter<long>("telemetry_events_total", "Total number of events tracked");
        _exceptionCounter = _meter.CreateCounter<long>("telemetry_exceptions_total", "Total number of exceptions tracked");
        _metricCounter = _meter.CreateCounter<long>("telemetry_metrics_total", "Total number of metrics tracked");
        _traceCounter = _meter.CreateCounter<long>("telemetry_traces_total", "Total number of traces tracked");
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

        TelemetryExceptionHandler.SafeExecute(() =>
        {
            // Create activity for the event
            using var activity = _activitySource.StartActivity($"Event: {eventName}");
            
            // Add properties as tags
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    activity?.SetTag(property.Key, property.Value?.ToString());
                }
            }
            
            // Add global attributes
            AddGlobalAttributes(activity);
            
            // Increment event counter
            _eventCounter.Add(1, new KeyValuePair<string, object?>("event_name", eventName));
        }, $"TrackEvent_{eventName}");
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

        TelemetryExceptionHandler.SafeExecute(() =>
        {
            // Create activity for the exception
            using var activity = _activitySource.StartActivity($"Exception: {exception.GetType().Name}");
            
            // Set exception details as tags
            activity?.SetTag("exception.type", exception.GetType().FullName);
            activity?.SetTag("exception.message", exception.Message);
            activity?.SetTag("exception.stacktrace", exception.StackTrace);
            
            // Add properties as tags
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    activity?.SetTag(property.Key, property.Value?.ToString());
                }
            }
            
            // Add global attributes
            AddGlobalAttributes(activity);
            
            // Record exception in activity
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            
            // Increment exception counter
            _exceptionCounter.Add(1, new KeyValuePair<string, object?>("exception_type", exception.GetType().Name));
        }, $"TrackException_{exception.GetType().Name}");
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

        TelemetryExceptionHandler.SafeExecute(() =>
        {
            // Create activity for the metric
            using var activity = _activitySource.StartActivity($"Metric: {metricName}");
            
            // Set metric details as tags
            activity?.SetTag("metric.name", metricName);
            activity?.SetTag("metric.value", value);
            
            // Add properties as tags
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    activity?.SetTag(property.Key, property.Value?.ToString());
                }
            }
            
            // Add global attributes
            AddGlobalAttributes(activity);
            
            // Increment metric counter
            _metricCounter.Add(1, new KeyValuePair<string, object?>("metric_name", metricName));
        }, $"TrackMetric_{metricName}");
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

        TelemetryExceptionHandler.SafeExecute(() =>
        {
            // Create activity for the trace
            using var activity = _activitySource.StartActivity($"Trace: {message}");
            
            // Set trace details as tags
            activity?.SetTag("trace.message", message);
            
            // Add properties as tags
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    activity?.SetTag(property.Key, property.Value?.ToString());
                }
            }
            
            // Add global attributes
            AddGlobalAttributes(activity);
            
            // Increment trace counter
            _traceCounter.Add(1, new KeyValuePair<string, object?>("trace_message", message));
        }, $"TrackTrace_{message}");
    }

    /// <summary>
    /// Adds global attributes to an activity.
    /// </summary>
    /// <param name="activity">The activity to add attributes to.</param>
    private void AddGlobalAttributes(Activity? activity)
    {
        if (activity == null || _options.GlobalAttributes == null)
            return;

        foreach (var attribute in _options.GlobalAttributes)
        {
            activity.SetTag(attribute.Key, attribute.Value?.ToString());
        }
        
        // Add service information
        activity.SetTag("service.name", _options.ServiceName);
        activity.SetTag("service.version", _options.ServiceVersion);
        activity.SetTag("service.environment", _options.Environment);
    }

    /// <summary>
    /// Disposes the OpenTelemetry adapter and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _activitySource?.Dispose();
            _meter?.Dispose();
            _disposed = true;
        }
    }
}
