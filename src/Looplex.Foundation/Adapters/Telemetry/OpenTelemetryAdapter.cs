using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
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
    private readonly string _serviceName;
    private readonly string _serviceVersion;
    private readonly string _environment;
    private readonly Dictionary<string, object>? _globalAttributes;
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
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="serviceVersion">The version of the service.</param>
    /// <param name="environment">The environment (e.g., Development, Production).</param>
    /// <param name="globalAttributes">Optional global attributes to include with all telemetry.</param>
    public OpenTelemetryAdapter(string serviceName, string serviceVersion = "1.0.0", string environment = "Development", Dictionary<string, object>? globalAttributes = null)
    {
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _serviceVersion = serviceVersion ?? "1.0.0";
        _environment = environment ?? "Development";
        _globalAttributes = globalAttributes;
        
        // Create activity source for distributed tracing
        _activitySource = new ActivitySource(_serviceName, _serviceVersion);
        
        // Create tracer
        _tracer = TracerProvider.Default.GetTracer(_serviceName, _serviceVersion);
        
        // Create meter for metrics
        _meter = new Meter(_serviceName, _serviceVersion);
        
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
        if (activity == null)
            return;

        // Add global attributes if provided
        if (_globalAttributes != null)
        {
            foreach (var attribute in _globalAttributes)
            {
                activity.SetTag(attribute.Key, attribute.Value?.ToString());
            }
        }
        
        // Add service information
        activity.SetTag("service.name", _serviceName);
        activity.SetTag("service.version", _serviceVersion);
        activity.SetTag("service.environment", _environment);
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
