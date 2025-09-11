using System;
using System.Collections.Generic;
using System.Linq;
using Looplex.Foundation.Configuration;

namespace Looplex.Foundation.Helpers;

/// <summary>
/// Validator for telemetry configuration options.
/// This class ensures that telemetry configurations are valid before use.
/// </summary>
public static class TelemetryConfigurationValidator
{
    /// <summary>
    /// Validates telemetry options and returns validation results.
    /// </summary>
    /// <param name="options">The telemetry options to validate.</param>
    /// <returns>Validation result containing any errors or warnings.</returns>
    public static TelemetryValidationResult Validate(TelemetryOptions options)
    {
        var result = new TelemetryValidationResult();

        if (options == null)
        {
            result.AddError("TelemetryOptions cannot be null");
            return result;
        }

        // Validate provider
        ValidateProvider(options, result);

        // Validate service information
        ValidateServiceInformation(options, result);

        // Validate provider-specific configurations
        switch (options.Provider?.ToLowerInvariant())
        {
            case "opentelemetry":
                ValidateOpenTelemetryOptions(options.OpenTelemetry, result);
                break;
            case "applicationinsights":
                ValidateApplicationInsightsOptions(options.ApplicationInsights, result);
                break;
            case "datadog":
                ValidateDataDogOptions(options.DataDog, result);
                break;
            case "noop":
                // NoOp doesn't require additional validation
                break;
            default:
                result.AddError($"Unsupported telemetry provider: {options.Provider}");
                break;
        }

        return result;
    }

    /// <summary>
    /// Validates the telemetry provider configuration.
    /// </summary>
    /// <param name="options">The telemetry options.</param>
    /// <param name="result">The validation result to add errors to.</param>
    private static void ValidateProvider(TelemetryOptions options, TelemetryValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            result.AddError("Provider cannot be null or empty");
            return;
        }

        var validProviders = new[] { "opentelemetry", "applicationinsights", "datadog", "noop" };
        if (!validProviders.Contains(options.Provider.ToLowerInvariant()))
        {
            result.AddError($"Invalid provider '{options.Provider}'. Valid providers are: {string.Join(", ", validProviders)}");
        }
    }

    /// <summary>
    /// Validates service information configuration.
    /// </summary>
    /// <param name="options">The telemetry options.</param>
    /// <param name="result">The validation result to add errors to.</param>
    private static void ValidateServiceInformation(TelemetryOptions options, TelemetryValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            result.AddError("ServiceName cannot be null or empty");
        }
        else if (options.ServiceName.Length > 100)
        {
            result.AddError("ServiceName cannot exceed 100 characters");
        }

        if (string.IsNullOrWhiteSpace(options.ServiceVersion))
        {
            result.AddError("ServiceVersion cannot be null or empty");
        }
        else if (options.ServiceVersion.Length > 50)
        {
            result.AddError("ServiceVersion cannot exceed 50 characters");
        }

        if (string.IsNullOrWhiteSpace(options.Environment))
        {
            result.AddError("Environment cannot be null or empty");
        }
        else
        {
            var validEnvironments = new[] { "development", "staging", "production", "test" };
            if (!validEnvironments.Contains(options.Environment.ToLowerInvariant()))
            {
                result.AddWarning($"Environment '{options.Environment}' is not a standard environment. Consider using: {string.Join(", ", validEnvironments)}");
            }
        }
    }

    /// <summary>
    /// Validates OpenTelemetry-specific configuration.
    /// </summary>
    /// <param name="options">The OpenTelemetry options.</param>
    /// <param name="result">The validation result to add errors to.</param>
    private static void ValidateOpenTelemetryOptions(OpenTelemetryOptions options, TelemetryValidationResult result)
    {
        if (options == null)
        {
            result.AddError("OpenTelemetry options cannot be null when provider is 'opentelemetry'");
            return;
        }

        // Validate endpoint
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            result.AddError("OpenTelemetry Endpoint cannot be null or empty");
        }
        else if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpointUri))
        {
            result.AddError($"OpenTelemetry Endpoint '{options.Endpoint}' is not a valid URI");
        }
        else if (endpointUri.Scheme != "http" && endpointUri.Scheme != "https")
        {
            result.AddError($"OpenTelemetry Endpoint must use http or https scheme, got: {endpointUri.Scheme}");
        }

        // Validate export protocol
        if (string.IsNullOrWhiteSpace(options.ExportProtocol))
        {
            result.AddError("OpenTelemetry ExportProtocol cannot be null or empty");
        }
        else
        {
            var validProtocols = new[] { "otlp", "jaeger", "zipkin", "prometheus" };
            if (!validProtocols.Contains(options.ExportProtocol.ToLowerInvariant()))
            {
                result.AddWarning($"OpenTelemetry ExportProtocol '{options.ExportProtocol}' is not a standard protocol. Consider using: {string.Join(", ", validProtocols)}");
            }
        }

        // Validate sample rate
        if (options.SampleRate < 0.0 || options.SampleRate > 1.0)
        {
            result.AddError($"OpenTelemetry SampleRate must be between 0.0 and 1.0, got: {options.SampleRate}");
        }
        else if (options.SampleRate > 0.1)
        {
            result.AddWarning("OpenTelemetry SampleRate is high for production environment. Consider using a lower value (e.g., 0.1) to reduce overhead");
        }
    }

    /// <summary>
    /// Validates Application Insights-specific configuration.
    /// </summary>
    /// <param name="options">The Application Insights options.</param>
    /// <param name="result">The validation result to add errors to.</param>
    private static void ValidateApplicationInsightsOptions(ApplicationInsightsOptions options, TelemetryValidationResult result)
    {
        if (options == null)
        {
            result.AddError("ApplicationInsights options cannot be null when provider is 'applicationinsights'");
            return;
        }

        // Validate connection string
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            result.AddError("ApplicationInsights ConnectionString cannot be null or empty");
        }
        else if (!options.ConnectionString.Contains("InstrumentationKey=") && !options.ConnectionString.Contains("ConnectionString="))
        {
            result.AddError("ApplicationInsights ConnectionString must contain either 'InstrumentationKey=' or 'ConnectionString='");
        }

        // Validate instrumentation key (if provided)
        if (!string.IsNullOrWhiteSpace(options.InstrumentationKey))
        {
            if (options.InstrumentationKey.Length != 36)
            {
                result.AddError("ApplicationInsights InstrumentationKey must be 36 characters long (GUID format)");
            }
            else if (!Guid.TryParse(options.InstrumentationKey, out _))
            {
                result.AddError("ApplicationInsights InstrumentationKey must be a valid GUID");
            }
        }
    }

    /// <summary>
    /// Validates DataDog-specific configuration.
    /// </summary>
    /// <param name="options">The DataDog options.</param>
    /// <param name="result">The validation result to add errors to.</param>
    private static void ValidateDataDogOptions(DataDogOptions options, TelemetryValidationResult result)
    {
        if (options == null)
        {
            result.AddError("DataDog options cannot be null when provider is 'datadog'");
            return;
        }

        // Validate API key
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            result.AddError("DataDog ApiKey cannot be null or empty");
        }
        else if (options.ApiKey.Length < 32)
        {
            result.AddError("DataDog ApiKey appears to be too short. Valid API keys are typically 32+ characters");
        }

        // Validate site
        if (string.IsNullOrWhiteSpace(options.Site))
        {
            result.AddError("DataDog Site cannot be null or empty");
        }
        else
        {
            var validSites = new[] { "datadoghq.com", "datadoghq.eu", "us3.datadoghq.com", "us5.datadoghq.com", "ddog-gov.com" };
            if (!validSites.Contains(options.Site.ToLowerInvariant()))
            {
                result.AddWarning($"DataDog Site '{options.Site}' is not a standard site. Valid sites are: {string.Join(", ", validSites)}");
            }
        }

        // Validate service name
        if (string.IsNullOrWhiteSpace(options.Service))
        {
            result.AddError("DataDog Service cannot be null or empty");
        }
    }
}

/// <summary>
/// Result of telemetry configuration validation.
/// </summary>
public class TelemetryValidationResult
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Gets the validation warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Gets a value indicating whether there are any warnings.
    /// </summary>
    public bool HasWarnings => _warnings.Count > 0;

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    /// <param name="error">The error message.</param>
    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            _errors.Add(error);
        }
    }

    /// <summary>
    /// Adds a warning to the validation result.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    public void AddWarning(string warning)
    {
        if (!string.IsNullOrWhiteSpace(warning))
        {
            _warnings.Add(warning);
        }
    }

    /// <summary>
    /// Gets a summary of the validation result.
    /// </summary>
    /// <returns>A string summary of the validation result.</returns>
    public string GetSummary()
    {
        var summary = $"Validation {(IsValid ? "PASSED" : "FAILED")}";
        
        if (_errors.Count > 0)
        {
            summary += $"\nErrors ({_errors.Count}):";
            foreach (var error in _errors)
            {
                summary += $"\n  - {error}";
            }
        }

        if (_warnings.Count > 0)
        {
            summary += $"\nWarnings ({_warnings.Count}):";
            foreach (var warning in _warnings)
            {
                summary += $"\n  - {warning}";
            }
        }

        return summary;
    }
}
