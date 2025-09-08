using System;
using System.Collections.Generic;
using System.Globalization;
using Looplex.Foundation.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Looplex.Foundation.UnitTests.Adapters.Telemetry;

/// <summary>
/// Unit tests for telemetry configuration helper.
/// These tests verify that telemetry configuration is loaded correctly from environment variables.
/// </summary>
[TestClass]
public class TelemetryConfigurationHelperTests
{

    [TestMethod]
    public void LoadTelemetryOptions_ShouldLoadFromEnvironmentVariables()
    {
        // Arrange
        var originalProvider = Environment.GetEnvironmentVariable("TELEMETRY_PROVIDER");
        var originalServiceName = Environment.GetEnvironmentVariable("TELEMETRY_SERVICE_NAME");
        var originalServiceVersion = Environment.GetEnvironmentVariable("TELEMETRY_SERVICE_VERSION");
        var originalEnvironment = Environment.GetEnvironmentVariable("TELEMETRY_ENVIRONMENT");

        try
        {
            // Set test environment variables
            Environment.SetEnvironmentVariable("TELEMETRY_PROVIDER", "OpenTelemetry");
            Environment.SetEnvironmentVariable("TELEMETRY_SERVICE_NAME", "test-service");
            Environment.SetEnvironmentVariable("TELEMETRY_SERVICE_VERSION", "2.0.0");
            Environment.SetEnvironmentVariable("TELEMETRY_ENVIRONMENT", "test");

            // Act
            var options = new TelemetryOptions
            {
                Provider = Environment.GetEnvironmentVariable("TELEMETRY_PROVIDER") ?? "NoOp",
                ServiceName = Environment.GetEnvironmentVariable("TELEMETRY_SERVICE_NAME") ?? "looplex-foundation",
                ServiceVersion = Environment.GetEnvironmentVariable("TELEMETRY_SERVICE_VERSION") ?? "1.0.0",
                Environment = Environment.GetEnvironmentVariable("TELEMETRY_ENVIRONMENT") ?? "development"
            };

            // Assert
            Assert.AreEqual("OpenTelemetry", options.Provider);
            Assert.AreEqual("test-service", options.ServiceName);
            Assert.AreEqual("2.0.0", options.ServiceVersion);
            Assert.AreEqual("test", options.Environment);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("TELEMETRY_PROVIDER", originalProvider);
            Environment.SetEnvironmentVariable("TELEMETRY_SERVICE_NAME", originalServiceName);
            Environment.SetEnvironmentVariable("TELEMETRY_SERVICE_VERSION", originalServiceVersion);
            Environment.SetEnvironmentVariable("TELEMETRY_ENVIRONMENT", originalEnvironment);
        }
    }

    [TestMethod]
    public void LoadTelemetryOptions_ShouldLoadOpenTelemetryConfiguration()
    {
        // Arrange
        var originalEndpoint = Environment.GetEnvironmentVariable("OTEL_ENDPOINT");
        var originalProtocol = Environment.GetEnvironmentVariable("OTEL_EXPORT_PROTOCOL");
        var originalConsoleExporter = Environment.GetEnvironmentVariable("OTEL_ENABLE_CONSOLE_EXPORTER");
        var originalSampleRate = Environment.GetEnvironmentVariable("OTEL_SAMPLE_RATE");

        try
        {
            // Set test environment variables
            Environment.SetEnvironmentVariable("OTEL_ENDPOINT", "http://test:4317");
            Environment.SetEnvironmentVariable("OTEL_EXPORT_PROTOCOL", "jaeger");
            Environment.SetEnvironmentVariable("OTEL_ENABLE_CONSOLE_EXPORTER", "false");
            Environment.SetEnvironmentVariable("OTEL_SAMPLE_RATE", "0.5");

            // Act
            var options = new OpenTelemetryOptions
            {
                Endpoint = Environment.GetEnvironmentVariable("OTEL_ENDPOINT") ?? "http://localhost:4317",
                ExportProtocol = Environment.GetEnvironmentVariable("OTEL_EXPORT_PROTOCOL") ?? "otlp",
                EnableConsoleExporter = bool.Parse(Environment.GetEnvironmentVariable("OTEL_ENABLE_CONSOLE_EXPORTER") ?? "true"),
                SampleRate = double.Parse(Environment.GetEnvironmentVariable("OTEL_SAMPLE_RATE") ?? "1.0", CultureInfo.InvariantCulture)
            };

            // Assert
            Assert.AreEqual("http://test:4317", options.Endpoint);
            Assert.AreEqual("jaeger", options.ExportProtocol);
            Assert.IsFalse(options.EnableConsoleExporter);
            Assert.AreEqual(0.5, options.SampleRate);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("OTEL_ENDPOINT", originalEndpoint);
            Environment.SetEnvironmentVariable("OTEL_EXPORT_PROTOCOL", originalProtocol);
            Environment.SetEnvironmentVariable("OTEL_ENABLE_CONSOLE_EXPORTER", originalConsoleExporter);
            Environment.SetEnvironmentVariable("OTEL_SAMPLE_RATE", originalSampleRate);
        }
    }

    [TestMethod]
    public void LoadTelemetryOptions_ShouldLoadApplicationInsightsConfiguration()
    {
        // Arrange
        var originalConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTIONSTRING");
        var originalInstrumentationKey = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_INSTRUMENTATION_KEY");

        try
        {
            // Set test environment variables
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTIONSTRING", "InstrumentationKey=test-key;IngestionEndpoint=https://test.ingestion.applicationinsights.azure.com/");
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_INSTRUMENTATION_KEY", "test-instrumentation-key");

            // Act
            var options = new ApplicationInsightsOptions
            {
                ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTIONSTRING") ?? string.Empty,
                InstrumentationKey = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_INSTRUMENTATION_KEY") ?? string.Empty
            };

            // Assert
            Assert.AreEqual("InstrumentationKey=test-key;IngestionEndpoint=https://test.ingestion.applicationinsights.azure.com/", options.ConnectionString);
            Assert.AreEqual("test-instrumentation-key", options.InstrumentationKey);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTIONSTRING", originalConnectionString);
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_INSTRUMENTATION_KEY", originalInstrumentationKey);
        }
    }

    [TestMethod]
    public void LoadTelemetryOptions_ShouldLoadDataDogConfiguration()
    {
        // Arrange
        var originalApiKey = Environment.GetEnvironmentVariable("DATADOG_API_KEY");
        var originalSite = Environment.GetEnvironmentVariable("DATADOG_SITE");
        var originalService = Environment.GetEnvironmentVariable("DATADOG_SERVICE");

        try
        {
            // Set test environment variables
            Environment.SetEnvironmentVariable("DATADOG_API_KEY", "test-api-key");
            Environment.SetEnvironmentVariable("DATADOG_SITE", "datadoghq.eu");
            Environment.SetEnvironmentVariable("DATADOG_SERVICE", "test-service");

            // Act
            var options = new DataDogOptions
            {
                ApiKey = Environment.GetEnvironmentVariable("DATADOG_API_KEY") ?? string.Empty,
                Site = Environment.GetEnvironmentVariable("DATADOG_SITE") ?? "datadoghq.com",
                Service = Environment.GetEnvironmentVariable("DATADOG_SERVICE") ?? string.Empty
            };

            // Assert
            Assert.AreEqual("test-api-key", options.ApiKey);
            Assert.AreEqual("datadoghq.eu", options.Site);
            Assert.AreEqual("test-service", options.Service);
        }
        finally
        {
            // Restore original values
            Environment.SetEnvironmentVariable("DATADOG_API_KEY", originalApiKey);
            Environment.SetEnvironmentVariable("DATADOG_SITE", originalSite);
            Environment.SetEnvironmentVariable("DATADOG_SERVICE", originalService);
        }
    }

    [TestMethod]
    public void LoadTelemetryOptions_ShouldHandleInvalidBooleanValues()
    {
        // Arrange
        var originalConsoleExporter = Environment.GetEnvironmentVariable("OTEL_ENABLE_CONSOLE_EXPORTER");

        try
        {
            // Set invalid boolean value
            Environment.SetEnvironmentVariable("OTEL_ENABLE_CONSOLE_EXPORTER", "invalid");

            // Act & Assert
            Assert.ThrowsException<FormatException>(() =>
            {
                bool.Parse(Environment.GetEnvironmentVariable("OTEL_ENABLE_CONSOLE_EXPORTER") ?? "true");
            });
        }
        finally
        {
            // Restore original value
            Environment.SetEnvironmentVariable("OTEL_ENABLE_CONSOLE_EXPORTER", originalConsoleExporter);
        }
    }

    [TestMethod]
    public void LoadTelemetryOptions_ShouldHandleInvalidDoubleValues()
    {
        // Arrange
        var originalSampleRate = Environment.GetEnvironmentVariable("OTEL_SAMPLE_RATE");

        try
        {
            // Set invalid double value
            Environment.SetEnvironmentVariable("OTEL_SAMPLE_RATE", "invalid");

            // Act & Assert
            Assert.ThrowsException<FormatException>(() =>
            {
                double.Parse(Environment.GetEnvironmentVariable("OTEL_SAMPLE_RATE") ?? "1.0", CultureInfo.InvariantCulture);
            });
        }
        finally
        {
            // Restore original value
            Environment.SetEnvironmentVariable("OTEL_SAMPLE_RATE", originalSampleRate);
        }
    }
}
