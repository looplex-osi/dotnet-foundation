using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.Foundation.Helpers;

namespace Looplex.Foundation.UnitTests.Helpers;

/// <summary>
/// Unit tests for TelemetryServiceFactory validation functionality.
/// </summary>
[TestClass]
public class TelemetryServiceFactoryValidationTests
{
    /// <summary>
    /// Test configuration validation with null configuration.
    /// </summary>
    [TestMethod]
    public void ValidateConfiguration_WithNullConfig_ShouldReturnInvalid()
    {
        // Act
        var (isValid, errors) = TelemetryServiceFactory.ValidateConfiguration(null);

        // Assert
        Assert.IsFalse(isValid);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("Configuration object cannot be null", errors[0]);
    }

    /// <summary>
    /// Test configuration validation with valid NoOp configuration.
    /// </summary>
    [TestMethod]
    public void ValidateConfiguration_WithValidNoOpConfig_ShouldReturnValid()
    {
        // Arrange
        var config = new
        {
            Provider = "NoOp",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test"
        };

        // Act
        var (isValid, errors) = TelemetryServiceFactory.ValidateConfiguration(config);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual(0, errors.Count);
    }

    /// <summary>
    /// Test configuration validation with missing required fields.
    /// </summary>
    [TestMethod]
    public void ValidateConfiguration_WithMissingRequiredFields_ShouldReturnInvalid()
    {
        // Arrange
        var config = new
        {
            Provider = "NoOp"
            // Missing ServiceName, ServiceVersion, Environment
        };

        // Act
        var (isValid, errors) = TelemetryServiceFactory.ValidateConfiguration(config);

        // Assert
        Assert.IsFalse(isValid);
        Assert.AreEqual(3, errors.Count);
        Assert.IsTrue(errors.Contains("ServiceName is required"));
        Assert.IsTrue(errors.Contains("ServiceVersion is required"));
        Assert.IsTrue(errors.Contains("Environment is required"));
    }

    /// <summary>
    /// Test configuration validation with ApplicationInsights provider missing connection string.
    /// </summary>
    [TestMethod]
    public void ValidateConfiguration_WithApplicationInsightsMissingConnectionString_ShouldReturnInvalid()
    {
        // Arrange
        var config = new
        {
            Provider = "ApplicationInsights",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test"
            // Missing ApplicationInsightsConnectionString
        };

        // Act
        var (isValid, errors) = TelemetryServiceFactory.ValidateConfiguration(config);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(errors.Contains("ApplicationInsightsConnectionString is required for ApplicationInsights provider"));
    }

    /// <summary>
    /// Test configuration validation with DataDog provider missing API key.
    /// </summary>
    [TestMethod]
    public void ValidateConfiguration_WithDataDogMissingApiKey_ShouldReturnInvalid()
    {
        // Arrange
        var config = new
        {
            Provider = "DataDog",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test"
            // Missing DataDogApiKey
        };

        // Act
        var (isValid, errors) = TelemetryServiceFactory.ValidateConfiguration(config);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(errors.Contains("DataDogApiKey is required for DataDog provider"));
    }

    /// <summary>
    /// Test configuration validation with OpenTelemetry provider missing endpoint.
    /// </summary>
    [TestMethod]
    public void ValidateConfiguration_WithOpenTelemetryMissingEndpoint_ShouldReturnInvalid()
    {
        // Arrange
        var config = new
        {
            Provider = "OpenTelemetry",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test"
            // Missing OpenTelemetryEndpoint
        };

        // Act
        var (isValid, errors) = TelemetryServiceFactory.ValidateConfiguration(config);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(errors.Contains("OpenTelemetryEndpoint is required for OpenTelemetry provider"));
    }

    /// <summary>
    /// Test configuration validation with valid ApplicationInsights configuration.
    /// </summary>
    [TestMethod]
    public void ValidateConfiguration_WithValidApplicationInsightsConfig_ShouldReturnValid()
    {
        // Arrange
        var config = new
        {
            Provider = "ApplicationInsights",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            ApplicationInsightsConnectionString = "InstrumentationKey=test-key"
        };

        // Act
        var (isValid, errors) = TelemetryServiceFactory.ValidateConfiguration(config);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual(0, errors.Count);
    }

    /// <summary>
    /// Test configuration validation with valid DataDog configuration.
    /// </summary>
    [TestMethod]
    public void ValidateConfiguration_WithValidDataDogConfig_ShouldReturnValid()
    {
        // Arrange
        var config = new
        {
            Provider = "DataDog",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            DataDogApiKey = "test-api-key"
        };

        // Act
        var (isValid, errors) = TelemetryServiceFactory.ValidateConfiguration(config);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual(0, errors.Count);
    }

    /// <summary>
    /// Test configuration validation with valid OpenTelemetry configuration.
    /// </summary>
    [TestMethod]
    public void ValidateConfiguration_WithValidOpenTelemetryConfig_ShouldReturnValid()
    {
        // Arrange
        var config = new
        {
            Provider = "OpenTelemetry",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            OpenTelemetryEndpoint = "http://localhost:4317"
        };

        // Act
        var (isValid, errors) = TelemetryServiceFactory.ValidateConfiguration(config);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual(0, errors.Count);
    }
}
