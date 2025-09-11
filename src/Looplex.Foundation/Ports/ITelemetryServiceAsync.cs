using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Looplex.Foundation.Ports;

/// <summary>
/// Asynchronous interface for telemetry services.
/// This interface provides async methods for telemetry operations to improve performance.
/// </summary>
public interface ITelemetryServiceAsync
{
    /// <summary>
    /// Tracks a custom event asynchronously with optional properties.
    /// </summary>
    /// <param name="eventName">The name of the event to track.</param>
    /// <param name="properties">Optional properties to include with the event.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TrackEventAsync(string eventName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks an exception asynchronously with optional properties.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="properties">Optional properties to include with the exception.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TrackExceptionAsync(Exception exception, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a custom metric asynchronously with a numeric value and optional properties.
    /// </summary>
    /// <param name="metricName">The name of the metric to track.</param>
    /// <param name="value">The numeric value of the metric.</param>
    /// <param name="properties">Optional properties to include with the metric.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TrackMetricAsync(string metricName, double value, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a custom trace message asynchronously with optional properties.
    /// </summary>
    /// <param name="message">The trace message to track.</param>
    /// <param name="properties">Optional properties to include with the trace.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TrackTraceAsync(string message, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
}
