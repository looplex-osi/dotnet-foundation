using System;
using Looplex.Foundation.Adapters.Telemetry;
using Looplex.Foundation.Configuration;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;

namespace Looplex.Foundation.Helpers;

/// <summary>
/// Factory for creating telemetry service instances.
/// This factory provides a centralized way to create telemetry services based on configuration,
/// following the factory pattern used throughout the Foundation project.
/// </summary>
public static class TelemetryServiceFactory
{
    // Telemetry provider constants (case-insensitive)
    private const string OpenTelemetryProvider = "OpenTelemetry";
    private const string ApplicationInsightsProvider = "ApplicationInsights";
    private const string DataDogProvider = "DataDog";
    private const string NoOpProvider = "NoOp";
    /// <summary>
    /// Creates a telemetry service instance based on the provided configuration.
    /// </summary>
    /// <param name="options">The telemetry configuration options.</param>
    /// <returns>A telemetry service instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an unsupported provider is specified.</exception>
    public static ITelemetryService Create(TelemetryOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        return CreateInternal(options);
    }

    /// <summary>
    /// Creates a telemetry service instance with validation.
    /// </summary>
    /// <param name="options">The telemetry configuration options.</param>
    /// <returns>A telemetry service instance.</returns>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid or unsupported provider is specified.</exception>
    public static ITelemetryService CreateWithValidation(TelemetryOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // Validate configuration
        var validationResult = TelemetryConfigurationValidator.Validate(options);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid telemetry configuration: {validationResult.GetSummary()}");
        }

        // Log warnings if any
        if (validationResult.HasWarnings)
        {
            System.Diagnostics.Debug.WriteLine($"Telemetry configuration warnings: {validationResult.GetSummary()}");
        }

        return CreateInternal(options);
    }

    /// <summary>
    /// Internal method to create telemetry service without validation.
    /// </summary>
    /// <param name="options">The telemetry configuration options.</param>
    /// <returns>A telemetry service instance.</returns>
    private static ITelemetryService CreateInternal(TelemetryOptions options)
    {
        return options.Provider?.ToLowerInvariant() switch
        {
            "opentelemetry" => new OpenTelemetryAdapter(options),
            "applicationinsights" => new ApplicationInsightsAdapter(options),
            "datadog" => new DataDogAdapter(options),
            "noop" => new NoOpTelemetryAdapter(),
            _ => throw new ArgumentException($"Unsupported telemetry provider: {options.Provider}", nameof(options))
        };
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
    /// Creates an async telemetry service instance based on the provided options.
    /// </summary>
    /// <param name="options">The telemetry configuration options.</param>
    /// <param name="disposeInnerService">Whether to dispose the inner service when the async adapter is disposed.</param>
    /// <returns>An async telemetry service instance.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported provider is specified or configuration is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public static ITelemetryServiceAsync CreateAsync(TelemetryOptions options, bool disposeInnerService = true)
    {
        var innerService = Create(options);
        return new AsyncTelemetryAdapter(innerService, disposeInnerService);
    }
}
