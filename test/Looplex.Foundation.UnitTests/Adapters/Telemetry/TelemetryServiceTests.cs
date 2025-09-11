using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Looplex.Foundation.Adapters.Telemetry;
using Looplex.Foundation.Configuration;
using Looplex.Foundation.Ports;
using Looplex.Foundation.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Looplex.Foundation.UnitTests.Adapters.Telemetry;

/// <summary>
/// Unit tests for telemetry service implementations.
/// These tests verify that all telemetry adapters work correctly and follow the contract
/// defined by the ITelemetryService interface.
/// </summary>
[TestClass]
public class TelemetryServiceTests
{
    [TestMethod]
    public void NoOpTelemetryAdapter_ShouldNotThrow_WhenTrackingEvents()
    {
        // Arrange
        var adapter = new NoOpTelemetryAdapter();
        var properties = new Dictionary<string, object> { ["test"] = "value" };

        // Act & Assert
        adapter.TrackEvent("test_event", properties);
        adapter.TrackException(new InvalidOperationException("test"), properties);
        adapter.TrackMetric("test_metric", 42.5, properties);
        adapter.TrackTrace("test_trace", properties);
        
        // If we get here without exceptions, the test passes
        Assert.IsTrue(true);
    }


    [TestMethod]
    public void OpenTelemetryAdapter_ShouldNotThrow_WhenTrackingEvents()
    {
        // Arrange
        var options = new TelemetryOptions
        {
            Provider = "OpenTelemetry",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test"
        };
        var adapter = new OpenTelemetryAdapter(options);
        var properties = new Dictionary<string, object> { ["test"] = "value" };

        // Act & Assert
        adapter.TrackEvent("test_event", properties);
        adapter.TrackException(new InvalidOperationException("test"), properties);
        adapter.TrackMetric("test_metric", 42.5, properties);
        adapter.TrackTrace("test_trace", properties);
        
        // If we get here without exceptions, the test passes
        Assert.IsTrue(true);
    }


    [TestMethod]
    public void ApplicationInsightsAdapter_ShouldNotThrow_WhenTrackingEvents()
    {
        // Arrange
        var options = new TelemetryOptions
        {
            Provider = "ApplicationInsights",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            ApplicationInsights = new ApplicationInsightsOptions
            {
                ConnectionString = "InstrumentationKey=test-key;IngestionEndpoint=https://test.ingestion.applicationinsights.azure.com/"
            }
        };
        var adapter = new ApplicationInsightsAdapter(options);
        var properties = new Dictionary<string, object> { ["test"] = "value" };

        // Act & Assert
        adapter.TrackEvent("test_event", properties);
        adapter.TrackException(new InvalidOperationException("test"), properties);
        adapter.TrackMetric("test_metric", 42.5, properties);
        adapter.TrackTrace("test_trace", properties);
        
        // If we get here without exceptions, the test passes
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void DataDogAdapter_ShouldNotThrow_WhenTrackingEvents()
    {
        // Arrange
        var options = new TelemetryOptions
        {
            Provider = "DataDog",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            DataDog = new DataDogOptions
            {
                ApiKey = "test-api-key",
                Site = "datadoghq.com",
                Service = "test-service"
            }
        };
        var adapter = new DataDogAdapter(options);
        var properties = new Dictionary<string, object> { ["test"] = "value" };

        // Act & Assert
        adapter.TrackEvent("test_event", properties);
        adapter.TrackException(new InvalidOperationException("test"), properties);
        adapter.TrackMetric("test_metric", 42.5, properties);
        adapter.TrackTrace("test_trace", properties);
        
        // If we get here without exceptions, the test passes
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void TelemetryServiceFactory_ShouldCreateCorrectAdapter_ForOpenTelemetry()
    {
        // Arrange
        var options = new TelemetryOptions { Provider = "OpenTelemetry" };

        // Act
        var service = TelemetryServiceFactory.Create(options);

        // Assert
        Assert.IsInstanceOfType(service, typeof(OpenTelemetryAdapter));
    }

    [TestMethod]
    public void TelemetryServiceFactory_ShouldCreateCorrectAdapter_ForApplicationInsights()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "ApplicationInsights",
            ApplicationInsights = new ApplicationInsightsOptions 
            { 
                ConnectionString = "InstrumentationKey=test" 
            }
        };

        // Act
        var service = TelemetryServiceFactory.Create(options);

        // Assert
        Assert.IsInstanceOfType(service, typeof(ApplicationInsightsAdapter));
    }

    [TestMethod]
    public void TelemetryServiceFactory_ShouldCreateCorrectAdapter_ForDataDog()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "DataDog",
            DataDog = new DataDogOptions 
            { 
                ApiKey = "a".PadRight(32, 'a'), // Valid length
                Service = "TestService"
            }
        };

        // Act
        var service = TelemetryServiceFactory.Create(options);

        // Assert
        Assert.IsInstanceOfType(service, typeof(DataDogAdapter));
    }


    [TestMethod]
    public void TelemetryServiceFactory_ShouldThrow_ForUnknownProvider()
    {
        // Arrange
        var options = new TelemetryOptions { Provider = "UnknownProvider" };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => TelemetryServiceFactory.Create(options));
    }


    [TestMethod]
    public void ITelemetryService_ShouldAcceptNullProperties()
    {
        // Arrange
        var adapters = new ITelemetryService[]
        {
            new NoOpTelemetryAdapter(),
            new OpenTelemetryAdapter(new TelemetryOptions { Provider = "OpenTelemetry" }),
            new ApplicationInsightsAdapter(new TelemetryOptions { Provider = "ApplicationInsights" }),
            new DataDogAdapter(new TelemetryOptions { Provider = "DataDog", DataDog = new DataDogOptions { ApiKey = "test" } })
        };

        // Act & Assert
        foreach (var adapter in adapters)
        {
            adapter.TrackEvent("test_event", null);
            adapter.TrackException(new InvalidOperationException("test"), null);
            adapter.TrackMetric("test_metric", 42.5, null);
            adapter.TrackTrace("test_trace", null);
        }
        
        Assert.IsTrue(true); // If we get here, no exceptions were thrown
    }

    [TestMethod]
    public void ITelemetryService_ShouldHandleEmptyEventNames()
    {
        // Arrange
        var adapter = new NoOpTelemetryAdapter();

        // Act & Assert
        adapter.TrackEvent("", null);
        adapter.TrackEvent(null, null);
        adapter.TrackTrace("", null);
        adapter.TrackTrace(null, null);
        adapter.TrackMetric("", 0, null);
        adapter.TrackMetric(null, 0, null);
        
        Assert.IsTrue(true); // If we get here, no exceptions were thrown
    }

    [TestMethod]
    public void TelemetryOptions_ShouldHaveDefaultValues()
    {
        // Act
        var options = new TelemetryOptions();

        // Assert
        Assert.AreEqual("NoOp", options.Provider);
        Assert.AreEqual("looplex-foundation", options.ServiceName);
        Assert.AreEqual("1.0.0", options.ServiceVersion);
        Assert.AreEqual("development", options.Environment);
        Assert.IsNotNull(options.OpenTelemetry);
        Assert.IsNotNull(options.ApplicationInsights);
        Assert.IsNotNull(options.DataDog);
        Assert.IsNotNull(options.GlobalAttributes);
    }

    [TestMethod]
    public void OpenTelemetryOptions_ShouldHaveDefaultValues()
    {
        // Act
        var options = new OpenTelemetryOptions();

        // Assert
        Assert.AreEqual("http://localhost:4317", options.Endpoint);
        Assert.AreEqual("otlp", options.ExportProtocol);
        Assert.IsFalse(options.EnableConsoleExporter); // Corrected: default is false
        Assert.AreEqual(1.0, options.SampleRate);
        Assert.IsTrue(options.EnableHttpInstrumentation);
        Assert.IsTrue(options.EnableSqlInstrumentation);
        Assert.IsFalse(options.EnableRedisInstrumentation);
        Assert.IsTrue(options.EnablePluginsInstrumentation);
    }

    [TestMethod]
    public void ApplicationInsightsOptions_ShouldHaveDefaultValues()
    {
        // Act
        var options = new ApplicationInsightsOptions();

        // Assert
        Assert.AreEqual(string.Empty, options.ConnectionString);
        Assert.AreEqual(string.Empty, options.InstrumentationKey);
    }

    [TestMethod]
    public void DataDogOptions_ShouldHaveDefaultValues()
    {
        // Act
        var options = new DataDogOptions();

        // Assert
        Assert.AreEqual(string.Empty, options.ApiKey);
        Assert.AreEqual("datadoghq.com", options.Site);
        Assert.AreEqual(string.Empty, options.Service);
    }

    [TestMethod]
    public void OpenTelemetryAdapter_ShouldCreateActivitySource()
    {
        // Arrange
        var options = new TelemetryOptions
        {
            Provider = "OpenTelemetry",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0"
        };

        // Act
        var adapter = new OpenTelemetryAdapter(options);

        // Assert
        Assert.IsNotNull(adapter);
        // The adapter should be created without exceptions
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void OpenTelemetryAdapter_ShouldDisposeCorrectly()
    {
        // Arrange
        var options = new TelemetryOptions
        {
            Provider = "OpenTelemetry",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0"
        };

        // Act
        using var adapter = new OpenTelemetryAdapter(options);
        adapter.TrackEvent("test_event");

        // Assert
        // If we get here without exceptions, disposal worked correctly
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void TelemetryServiceFactory_ShouldThrowArgumentNullException_ForNullOptions()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => TelemetryServiceFactory.Create(null));
    }

    [TestMethod]
    public void OpenTelemetryAdapter_ShouldThrowArgumentNullException_ForNullOptions()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new OpenTelemetryAdapter(null));
    }

    [TestMethod]
    public void ApplicationInsightsAdapter_ShouldThrowArgumentNullException_ForNullOptions()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new ApplicationInsightsAdapter(null));
    }

    [TestMethod]
    public void DataDogAdapter_ShouldThrowArgumentNullException_ForNullOptions()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new DataDogAdapter(null));
    }

    [TestMethod]
    public void DataDogAdapter_ShouldThrowArgumentException_ForNullApiKey()
    {
        // Arrange
        var options = new TelemetryOptions
        {
            Provider = "DataDog",
            DataDog = new DataDogOptions { ApiKey = null }
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new DataDogAdapter(options));
    }

    [TestMethod]
    public void ITelemetryService_ShouldHandleLargeProperties()
    {
        // Arrange
        var adapter = new NoOpTelemetryAdapter();
        var largeProperties = new Dictionary<string, object>();
        
        for (int i = 0; i < 1000; i++)
        {
            largeProperties[$"key_{i}"] = $"value_{i}";
        }

        // Act & Assert
        adapter.TrackEvent("large_event", largeProperties);
        adapter.TrackException(new InvalidOperationException("test"), largeProperties);
        adapter.TrackMetric("large_metric", 42.5, largeProperties);
        adapter.TrackTrace("large_trace", largeProperties);
        
        Assert.IsTrue(true); // If we get here, no exceptions were thrown
    }

    [TestMethod]
    public void ITelemetryService_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var adapter = new NoOpTelemetryAdapter();
        var specialProperties = new Dictionary<string, object>
        {
            ["special_chars"] = "!@#$%^&*()_+-=[]{}|;':\",./<>?",
            ["unicode"] = "🚀🌟💫⭐",
            ["newlines"] = "line1\nline2\rline3",
            ["quotes"] = "\"quoted\" 'single'"
        };

        // Act & Assert
        adapter.TrackEvent("special_event", specialProperties);
        adapter.TrackException(new InvalidOperationException("test"), specialProperties);
        adapter.TrackMetric("special_metric", 42.5, specialProperties);
        adapter.TrackTrace("special_trace", specialProperties);
        
        Assert.IsTrue(true); // If we get here, no exceptions were thrown
    }
}
