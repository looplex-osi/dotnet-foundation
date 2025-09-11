using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Microsoft.AspNetCore.Http;

namespace Looplex.Samples.WebApp.Middlewares;

/// <summary>
/// Middleware for telemetry demonstration in the Notejam application.
/// This middleware tracks HTTP requests, response times, and errors to demonstrate
/// the telemetry capabilities of the Foundation library.
/// </summary>
public class TelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the TelemetryMiddleware class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="telemetryService">The telemetry service for tracking events.</param>
    public TelemetryMiddleware(RequestDelegate next, ITelemetryService telemetryService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        
        // Track request start with sanitized data
        var requestProperties = new Dictionary<string, object>
        {
            ["request_id"] = requestId,
            ["method"] = context.Request.Method,
            ["path"] = TelemetrySecurityHelper.SanitizeUrl(context.Request.Path),
            ["user_agent"] = context.Request.Headers.UserAgent.ToString(),
            ["remote_ip"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        // Sanitize headers if needed
        var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        var sanitizedHeaders = TelemetrySecurityHelper.SanitizeHeaders(headers);
        requestProperties["headers"] = sanitizedHeaders;

        _telemetryService.TrackEvent("http_request_started", TelemetrySecurityHelper.SanitizeProperties(requestProperties));

        try
        {
            await _next(context);
            
            stopwatch.Stop();
            
            // Track successful request completion with sanitized data
            var completionProperties = new Dictionary<string, object>
            {
                ["request_id"] = requestId,
                ["method"] = context.Request.Method,
                ["path"] = TelemetrySecurityHelper.SanitizeUrl(context.Request.Path),
                ["status_code"] = context.Response.StatusCode,
                ["duration_ms"] = stopwatch.ElapsedMilliseconds,
                ["content_length"] = context.Response.ContentLength ?? 0
            };

            _telemetryService.TrackEvent("http_request_completed", TelemetrySecurityHelper.SanitizeProperties(completionProperties));

            // Track response time metric with sanitized data
            var metricProperties = new Dictionary<string, object>
            {
                ["method"] = context.Request.Method,
                ["path"] = TelemetrySecurityHelper.SanitizeUrl(context.Request.Path),
                ["status_code"] = context.Response.StatusCode.ToString()
            };

            _telemetryService.TrackMetric("http_response_time_ms", stopwatch.ElapsedMilliseconds, TelemetrySecurityHelper.SanitizeProperties(metricProperties));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Track request error with sanitized data
            var errorProperties = new Dictionary<string, object>
            {
                ["request_id"] = requestId,
                ["method"] = context.Request.Method,
                ["path"] = TelemetrySecurityHelper.SanitizeUrl(context.Request.Path),
                ["duration_ms"] = stopwatch.ElapsedMilliseconds,
                ["error_type"] = ex.GetType().Name
            };

            _telemetryService.TrackException(ex, TelemetrySecurityHelper.SanitizeProperties(errorProperties));

            // Track error metric with sanitized data
            var errorMetricProperties = new Dictionary<string, object>
            {
                ["method"] = context.Request.Method,
                ["path"] = TelemetrySecurityHelper.SanitizeUrl(context.Request.Path),
                ["error_type"] = ex.GetType().Name
            };

            _telemetryService.TrackMetric("http_errors_total", 1, TelemetrySecurityHelper.SanitizeProperties(errorMetricProperties));

            throw; // Re-throw to let other middleware handle the error
        }
    }
}

/// <summary>
/// Extension methods for adding telemetry middleware to the application pipeline.
/// </summary>
public static class TelemetryMiddlewareExtensions
{
    /// <summary>
    /// Adds telemetry middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseTelemetry(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TelemetryMiddleware>();
    }
}
