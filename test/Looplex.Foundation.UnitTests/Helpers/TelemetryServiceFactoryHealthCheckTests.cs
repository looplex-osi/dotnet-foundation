using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.Foundation.Adapters.Telemetry;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;

namespace Looplex.Foundation.UnitTests.Helpers;

/// <summary>
/// Unit tests for TelemetryServiceFactory health check functionality.
/// </summary>
[TestClass]
public class TelemetryServiceFactoryHealthCheckTests
{
    /// <summary>
    /// Test health check with NoOp adapter should return healthy.
    /// </summary>
    [TestMethod]
    public void HealthCheck_WithNoOpAdapter_ShouldReturnHealthy()
    {
        // Arrange
        var telemetryService = new NoOpTelemetryAdapter();

        // Act
        var (isHealthy, message) = TelemetryServiceFactory.HealthCheck(telemetryService);

        // Assert
        Assert.IsTrue(isHealthy);
        Assert.AreEqual("Telemetry service is healthy", message);
    }

    /// <summary>
    /// Test async health check with NoOp adapter should return healthy.
    /// </summary>
    [TestMethod]
    public async Task HealthCheckAsync_WithNoOpAdapter_ShouldReturnHealthy()
    {
        // Arrange
        var telemetryService = new NoOpTelemetryAdapter();

        // Act
        var (isHealthy, message) = await TelemetryServiceFactory.HealthCheckAsync(telemetryService);

        // Assert
        Assert.IsTrue(isHealthy);
        Assert.AreEqual("Telemetry service is healthy", message);
    }

    /// <summary>
    /// Test health check with failing adapter should return unhealthy.
    /// </summary>
    [TestMethod]
    public void HealthCheck_WithFailingAdapter_ShouldReturnUnhealthy()
    {
        // Arrange
        var telemetryService = new FailingTelemetryAdapter();

        // Act
        var (isHealthy, message) = TelemetryServiceFactory.HealthCheck(telemetryService);

        // Assert
        Assert.IsFalse(isHealthy);
        Assert.IsTrue(message.Contains("Telemetry service health check failed"));
    }

    /// <summary>
    /// Test async health check with failing adapter should return unhealthy.
    /// </summary>
    [TestMethod]
    public async Task HealthCheckAsync_WithFailingAdapter_ShouldReturnUnhealthy()
    {
        // Arrange
        var telemetryService = new FailingTelemetryAdapter();

        // Act
        var (isHealthy, message) = await TelemetryServiceFactory.HealthCheckAsync(telemetryService);

        // Assert
        Assert.IsFalse(isHealthy);
        Assert.IsTrue(message.Contains("Telemetry service health check failed"));
    }

    /// <summary>
    /// Test health check with async adapter should handle async operations.
    /// </summary>
    [TestMethod]
    public async Task HealthCheckAsync_WithAsyncAdapter_ShouldHandleAsyncOperations()
    {
        // Arrange
        var telemetryService = new AsyncTelemetryAdapter(new NoOpTelemetryAdapter());

        // Act
        var (isHealthy, message) = await TelemetryServiceFactory.HealthCheckAsync(telemetryService);

        // Assert
        Assert.IsTrue(isHealthy);
        Assert.AreEqual("Telemetry service is healthy", message);
    }
}

/// <summary>
/// Test telemetry adapter that always fails for testing purposes.
/// </summary>
public class FailingTelemetryAdapter : ITelemetryService
{
    public void TrackEvent(string eventName, IDictionary<string, object>? properties = null)
    {
        throw new InvalidOperationException("Test failure");
    }

    public void TrackException(Exception exception, IDictionary<string, object>? properties = null)
    {
        throw new InvalidOperationException("Test failure");
    }

    public void TrackMetric(string metricName, double value, IDictionary<string, object>? properties = null)
    {
        throw new InvalidOperationException("Test failure");
    }

    public void TrackTrace(string message, IDictionary<string, object>? properties = null)
    {
        throw new InvalidOperationException("Test failure");
    }
}
