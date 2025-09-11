using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;

namespace Looplex.Foundation.Helpers;

/// <summary>
/// Async wrapper adapter that provides asynchronous telemetry operations.
/// This adapter wraps any ITelemetryService to provide async capabilities.
/// </summary>
public class AsyncTelemetryAdapter : ITelemetryService, ITelemetryServiceAsync, IDisposable
{
    private readonly ITelemetryService _innerService;
    private readonly bool _disposeInnerService;

    /// <summary>
    /// Initializes a new instance of the AsyncTelemetryAdapter class.
    /// </summary>
    /// <param name="innerService">The inner telemetry service to wrap.</param>
    /// <param name="disposeInnerService">Whether to dispose the inner service when this adapter is disposed.</param>
    public AsyncTelemetryAdapter(ITelemetryService innerService, bool disposeInnerService = false)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _disposeInnerService = disposeInnerService;
    }

    /// <summary>
    /// Tracks a custom event with optional properties.
    /// </summary>
    /// <param name="eventName">The name of the event to track.</param>
    /// <param name="properties">Optional properties to include with the event.</param>
    public void TrackEvent(string eventName, IDictionary<string, object>? properties = null)
    {
        _innerService.TrackEvent(eventName, properties);
    }

    /// <summary>
    /// Tracks a custom event asynchronously with optional properties.
    /// </summary>
    /// <param name="eventName">The name of the event to track.</param>
    /// <param name="properties">Optional properties to include with the event.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task TrackEventAsync(string eventName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        await TelemetryExceptionHandler.SafeExecuteAsync(async () =>
        {
            await Task.Run(() => _innerService.TrackEvent(eventName, properties), cancellationToken);
        }, $"TrackEventAsync_{eventName}");
    }

    /// <summary>
    /// Tracks an exception with optional properties.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="properties">Optional properties to include with the exception.</param>
    public void TrackException(Exception exception, IDictionary<string, object>? properties = null)
    {
        _innerService.TrackException(exception, properties);
    }

    /// <summary>
    /// Tracks an exception asynchronously with optional properties.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="properties">Optional properties to include with the exception.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task TrackExceptionAsync(Exception exception, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        await TelemetryExceptionHandler.SafeExecuteAsync(async () =>
        {
            await Task.Run(() => _innerService.TrackException(exception, properties), cancellationToken);
        }, $"TrackExceptionAsync_{exception.GetType().Name}");
    }

    /// <summary>
    /// Tracks a custom metric with a numeric value and optional properties.
    /// </summary>
    /// <param name="metricName">The name of the metric to track.</param>
    /// <param name="value">The numeric value of the metric.</param>
    /// <param name="properties">Optional properties to include with the metric.</param>
    public void TrackMetric(string metricName, double value, IDictionary<string, object>? properties = null)
    {
        _innerService.TrackMetric(metricName, value, properties);
    }

    /// <summary>
    /// Tracks a custom metric asynchronously with a numeric value and optional properties.
    /// </summary>
    /// <param name="metricName">The name of the metric to track.</param>
    /// <param name="value">The numeric value of the metric.</param>
    /// <param name="properties">Optional properties to include with the metric.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task TrackMetricAsync(string metricName, double value, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        await TelemetryExceptionHandler.SafeExecuteAsync(async () =>
        {
            await Task.Run(() => _innerService.TrackMetric(metricName, value, properties), cancellationToken);
        }, $"TrackMetricAsync_{metricName}");
    }

    /// <summary>
    /// Tracks a custom trace message with optional properties.
    /// </summary>
    /// <param name="message">The trace message to track.</param>
    /// <param name="properties">Optional properties to include with the trace.</param>
    public void TrackTrace(string message, IDictionary<string, object>? properties = null)
    {
        _innerService.TrackTrace(message, properties);
    }

    /// <summary>
    /// Tracks a custom trace message asynchronously with optional properties.
    /// </summary>
    /// <param name="message">The trace message to track.</param>
    /// <param name="properties">Optional properties to include with the trace.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task TrackTraceAsync(string message, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        await TelemetryExceptionHandler.SafeExecuteAsync(async () =>
        {
            await Task.Run(() => _innerService.TrackTrace(message, properties), cancellationToken);
        }, $"TrackTraceAsync_{message}");
    }

    /// <summary>
    /// Disposes the adapter and optionally the inner service.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (_disposeInnerService && _innerService is IDisposable disposableService)
            {
                disposableService.Dispose();
            }
        }
        catch (Exception ex)
        {
            TelemetryExceptionHandler.SafeExecute(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing AsyncTelemetryAdapter: {ex.Message}");
            }, "Dispose");
        }
    }
}
