using System;
using System.Linq;
using Looplex.Foundation.Configuration;
using Looplex.Foundation.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Looplex.Foundation.UnitTests.Helpers;

/// <summary>
/// Unit tests for TelemetryConfigurationValidator to ensure proper validation of telemetry configurations.
/// </summary>
[TestClass]
public class TelemetryConfigurationValidatorTests
{
    [TestMethod]
    public void Validate_ShouldReturnError_WhenOptionsIsNull()
    {
        // Act
        var result = TelemetryConfigurationValidator.Validate(null);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(1, result.Errors.Count);
        Assert.AreEqual("TelemetryOptions cannot be null", result.Errors[0]);
    }


    [TestMethod]
    public void Validate_ShouldReturnError_WhenProviderIsInvalid()
    {
        // Arrange
        var options = new TelemetryOptions { Provider = "invalid" };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Invalid provider")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenServiceNameIsNull()
    {
        // Arrange
        var options = new TelemetryOptions { Provider = "NoOp", ServiceName = null };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("ServiceName cannot be null or empty")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenServiceNameIsTooLong()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "NoOp", 
            ServiceName = new string('a', 101) // 101 characters
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("ServiceName cannot exceed 100 characters")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenServiceVersionIsNull()
    {
        // Arrange
        var options = new TelemetryOptions { Provider = "NoOp", ServiceVersion = null };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("ServiceVersion cannot be null or empty")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenEnvironmentIsNull()
    {
        // Arrange
        var options = new TelemetryOptions { Provider = "NoOp", Environment = null };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Environment cannot be null or empty")));
    }

    [TestMethod]
    public void Validate_ShouldReturnWarning_WhenEnvironmentIsNonStandard()
    {
        // Arrange
        var options = new TelemetryOptions { Provider = "NoOp", Environment = "custom" };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.HasWarnings);
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("not a standard environment")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenOpenTelemetryOptionsIsNull()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "OpenTelemetry", 
            OpenTelemetry = null 
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("OpenTelemetry options cannot be null")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenOpenTelemetryEndpointIsInvalid()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "OpenTelemetry",
            OpenTelemetry = new OpenTelemetryOptions 
            { 
                Endpoint = "invalid-uri" 
            }
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("not a valid URI")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenOpenTelemetrySampleRateIsInvalid()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "OpenTelemetry",
            OpenTelemetry = new OpenTelemetryOptions 
            { 
                SampleRate = 1.5 // Invalid: > 1.0
            }
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("SampleRate must be between 0.0 and 1.0")));
    }

    [TestMethod]
    public void Validate_ShouldReturnWarning_WhenOpenTelemetrySampleRateIsHighForProduction()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "OpenTelemetry",
            Environment = "production",
            OpenTelemetry = new OpenTelemetryOptions 
            { 
                SampleRate = 0.5 // High for production
            }
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.HasWarnings);
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("SampleRate is high for production")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenApplicationInsightsOptionsIsNull()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "ApplicationInsights", 
            ApplicationInsights = null 
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("ApplicationInsights options cannot be null")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenApplicationInsightsConnectionStringIsNull()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "ApplicationInsights",
            ApplicationInsights = new ApplicationInsightsOptions 
            { 
                ConnectionString = null 
            }
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("ConnectionString cannot be null or empty")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenApplicationInsightsInstrumentationKeyIsInvalid()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "ApplicationInsights",
            ApplicationInsights = new ApplicationInsightsOptions 
            { 
                ConnectionString = "InstrumentationKey=test",
                InstrumentationKey = "invalid-guid-that-is-36-chars-long" 
            }
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("must be 36 characters long")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenDataDogOptionsIsNull()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "DataDog", 
            DataDog = null 
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("DataDog options cannot be null")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenDataDogApiKeyIsNull()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "DataDog",
            DataDog = new DataDogOptions 
            { 
                ApiKey = null 
            }
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("ApiKey cannot be null or empty")));
    }

    [TestMethod]
    public void Validate_ShouldReturnError_WhenDataDogApiKeyIsTooShort()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "DataDog",
            DataDog = new DataDogOptions 
            { 
                ApiKey = "short" // Too short
            }
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("appears to be too short")));
    }

    [TestMethod]
    public void Validate_ShouldReturnWarning_WhenDataDogSiteIsNonStandard()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "DataDog",
            DataDog = new DataDogOptions 
            { 
                ApiKey = "a".PadRight(32, 'a'), // Valid length
                Site = "custom.datadog.com", // Non-standard site
                Service = "TestService"
            }
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.HasWarnings);
        Assert.IsTrue(result.Warnings.Any(w => w.Contains("not a standard site")));
    }

    [TestMethod]
    public void Validate_ShouldReturnValid_WhenNoOpProviderIsUsed()
    {
        // Arrange
        var options = new TelemetryOptions 
        { 
            Provider = "NoOp",
            ServiceName = "TestService",
            ServiceVersion = "1.0.0",
            Environment = "development"
        };

        // Act
        var result = TelemetryConfigurationValidator.Validate(options);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsFalse(result.HasWarnings);
    }

    [TestMethod]
    public void GetSummary_ShouldReturnFormattedSummary()
    {
        // Arrange
        var result = new TelemetryValidationResult();
        result.AddError("Test error 1");
        result.AddError("Test error 2");
        result.AddWarning("Test warning 1");

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.IsTrue(summary.Contains("FAILED"));
        Assert.IsTrue(summary.Contains("Errors (2)"));
        Assert.IsTrue(summary.Contains("Test error 1"));
        Assert.IsTrue(summary.Contains("Test error 2"));
        Assert.IsTrue(summary.Contains("Warnings (1)"));
        Assert.IsTrue(summary.Contains("Test warning 1"));
    }
}
