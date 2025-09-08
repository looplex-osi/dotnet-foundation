using System;
using System.Collections.Generic;

namespace Looplex.Foundation.Ports;

/// <summary>
/// Provides telemetry functionality for tracking events, exceptions, metrics, and traces.
/// This interface abstracts telemetry providers, allowing applications to use different
/// telemetry backends (OpenTelemetry, Application Insights, DataDog, etc.) without
/// coupling to specific implementations.
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Tracks a custom event with optional properties.
    /// </summary>
    /// <param name="eventName">The name of the event to track.</param>
    /// <param name="properties">Optional properties to include with the event.</param>
    void TrackEvent(string eventName, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Tracks an exception with optional properties.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="properties">Optional properties to include with the exception.</param>
    void TrackException(Exception exception, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Tracks a custom metric with a numeric value and optional properties.
    /// </summary>
    /// <param name="metricName">The name of the metric to track.</param>
    /// <param name="value">The numeric value of the metric.</param>
    /// <param name="properties">Optional properties to include with the metric.</param>
    void TrackMetric(string metricName, double value, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Tracks a custom trace message with optional properties.
    /// </summary>
    /// <param name="message">The trace message to track.</param>
    /// <param name="properties">Optional properties to include with the trace.</param>
    void TrackTrace(string message, IDictionary<string, object>? properties = null);
}
