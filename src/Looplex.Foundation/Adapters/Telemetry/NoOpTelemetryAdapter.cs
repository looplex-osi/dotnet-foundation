using System;
using System.Collections.Generic;
using Looplex.Foundation.Configuration;
using Looplex.Foundation.Ports;

namespace Looplex.Foundation.Adapters.Telemetry;

/// <summary>
/// A no-operation telemetry adapter that discards all telemetry data.
/// This adapter is useful for development, testing, or when telemetry is disabled.
/// It implements the ITelemetryService interface but performs no actual telemetry operations.
/// </summary>
public class NoOpTelemetryAdapter : ITelemetryService
{
    /// <summary>
    /// Initializes a new instance of the NoOpTelemetryAdapter class.
    /// </summary>
    public NoOpTelemetryAdapter()
    {
        // No initialization required for no-op adapter
    }

    /// <summary>
    /// Tracks a custom event (no-op implementation).
    /// </summary>
    /// <param name="eventName">The name of the event to track.</param>
    /// <param name="properties">Optional properties to include with the event.</param>
    public void TrackEvent(string eventName, IDictionary<string, object>? properties = null)
    {
        // No-op: discard the event
    }

    /// <summary>
    /// Tracks an exception (no-op implementation).
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="properties">Optional properties to include with the exception.</param>
    public void TrackException(Exception exception, IDictionary<string, object>? properties = null)
    {
        // No-op: discard the exception
    }

    /// <summary>
    /// Tracks a custom metric (no-op implementation).
    /// </summary>
    /// <param name="metricName">The name of the metric to track.</param>
    /// <param name="value">The numeric value of the metric.</param>
    /// <param name="properties">Optional properties to include with the metric.</param>
    public void TrackMetric(string metricName, double value, IDictionary<string, object>? properties = null)
    {
        // No-op: discard the metric
    }

    /// <summary>
    /// Tracks a custom trace message (no-op implementation).
    /// </summary>
    /// <param name="message">The trace message to track.</param>
    /// <param name="properties">Optional properties to include with the trace.</param>
    public void TrackTrace(string message, IDictionary<string, object>? properties = null)
    {
        // No-op: discard the trace
    }
}
