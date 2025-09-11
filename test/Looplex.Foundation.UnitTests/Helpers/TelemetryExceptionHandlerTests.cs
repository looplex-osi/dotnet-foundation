using System;
using System.Threading.Tasks;
using Looplex.Foundation.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Looplex.Foundation.UnitTests.Helpers;

/// <summary>
/// Unit tests for TelemetryExceptionHandler to ensure safe execution of telemetry operations.
/// </summary>
[TestClass]
public class TelemetryExceptionHandlerTests
{
    [TestMethod]
    public void SafeExecute_ShouldExecuteOperation_WhenNoException()
    {
        // Arrange
        var executed = false;
        var operation = new Action(() => executed = true);

        // Act
        TelemetryExceptionHandler.SafeExecute(operation, "TestOperation");

        // Assert
        Assert.IsTrue(executed);
    }

    [TestMethod]
    public void SafeExecute_ShouldNotThrow_WhenExceptionOccurs()
    {
        // Arrange
        var operation = new Action(() => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        // Should not throw
        TelemetryExceptionHandler.SafeExecute(operation, "TestOperation");
    }

    [TestMethod]
    public void SafeExecute_ShouldExecuteFallback_WhenExceptionOccurs()
    {
        // Arrange
        var operation = new Action(() => throw new InvalidOperationException("Test exception"));
        var fallbackExecuted = false;
        var fallback = new Action(() => fallbackExecuted = true);

        // Act
        TelemetryExceptionHandler.SafeExecute(operation, "TestOperation", fallback);

        // Assert
        Assert.IsTrue(fallbackExecuted);
    }

    [TestMethod]
    public void SafeExecute_ShouldReturnValue_WhenNoException()
    {
        // Arrange
        var expectedValue = "test result";
        var operation = new Func<string>(() => expectedValue);

        // Act
        var result = TelemetryExceptionHandler.SafeExecute(operation, "TestOperation", "fallback");

        // Assert
        Assert.AreEqual(expectedValue, result);
    }

    [TestMethod]
    public void SafeExecute_ShouldReturnFallbackValue_WhenExceptionOccurs()
    {
        // Arrange
        var fallbackValue = "fallback result";
        var operation = new Func<string>(() => throw new InvalidOperationException("Test exception"));

        // Act
        var result = TelemetryExceptionHandler.SafeExecute(operation, "TestOperation", fallbackValue);

        // Assert
        Assert.AreEqual(fallbackValue, result);
    }

    [TestMethod]
    public async Task SafeExecuteAsync_ShouldExecuteOperation_WhenNoException()
    {
        // Arrange
        var executed = false;
        var operation = new Func<Task>(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Act
        await TelemetryExceptionHandler.SafeExecuteAsync(operation, "TestOperation");

        // Assert
        Assert.IsTrue(executed);
    }

    [TestMethod]
    public async Task SafeExecuteAsync_ShouldNotThrow_WhenExceptionOccurs()
    {
        // Arrange
        var operation = new Func<Task>(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Test exception");
        });

        // Act & Assert
        // Should not throw
        await TelemetryExceptionHandler.SafeExecuteAsync(operation, "TestOperation");
    }

    [TestMethod]
    public async Task SafeExecuteAsync_ShouldExecuteFallback_WhenExceptionOccurs()
    {
        // Arrange
        var operation = new Func<Task>(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Test exception");
        });
        var fallbackExecuted = false;
        var fallback = new Func<Task>(async () =>
        {
            await Task.Delay(1);
            fallbackExecuted = true;
        });

        // Act
        await TelemetryExceptionHandler.SafeExecuteAsync(operation, "TestOperation", fallback);

        // Assert
        Assert.IsTrue(fallbackExecuted);
    }

    [TestMethod]
    public async Task SafeExecuteAsync_ShouldReturnValue_WhenNoException()
    {
        // Arrange
        var expectedValue = "test result";
        var operation = new Func<Task<string>>(async () =>
        {
            await Task.Delay(1);
            return expectedValue;
        });

        // Act
        var result = await TelemetryExceptionHandler.SafeExecuteAsync(operation, "TestOperation", "fallback");

        // Assert
        Assert.AreEqual(expectedValue, result);
    }

    [TestMethod]
    public async Task SafeExecuteAsync_ShouldReturnFallbackValue_WhenExceptionOccurs()
    {
        // Arrange
        var fallbackValue = "fallback result";
        var operation = new Func<Task<string>>(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Test exception");
        });

        // Act
        var result = await TelemetryExceptionHandler.SafeExecuteAsync(operation, "TestOperation", fallbackValue);

        // Assert
        Assert.AreEqual(fallbackValue, result);
    }

    [TestMethod]
    public void ShouldRetry_ShouldReturnTrue_ForRetryableExceptions()
    {
        // Act & Assert
        Assert.IsTrue(TelemetryExceptionHandler.ShouldRetry(new System.Net.Http.HttpRequestException()));
        Assert.IsTrue(TelemetryExceptionHandler.ShouldRetry(new System.TimeoutException()));
        Assert.IsTrue(TelemetryExceptionHandler.ShouldRetry(new System.Net.Sockets.SocketException()));
        Assert.IsTrue(TelemetryExceptionHandler.ShouldRetry(new System.IO.IOException()));
    }

    [TestMethod]
    public void ShouldRetry_ShouldReturnFalse_ForNonRetryableExceptions()
    {
        // Act & Assert
        Assert.IsFalse(TelemetryExceptionHandler.ShouldRetry(new ArgumentException()));
        Assert.IsFalse(TelemetryExceptionHandler.ShouldRetry(new InvalidOperationException()));
        Assert.IsFalse(TelemetryExceptionHandler.ShouldRetry(new UnauthorizedAccessException()));
    }

    [TestMethod]
    public void GetUserFriendlyErrorMessage_ShouldReturnAppropriateMessage_ForDifferentExceptions()
    {
        // Act & Assert
        Assert.AreEqual("Network connectivity issue detected", 
            TelemetryExceptionHandler.GetUserFriendlyErrorMessage(new System.Net.Http.HttpRequestException()));
        Assert.AreEqual("Operation timed out", 
            TelemetryExceptionHandler.GetUserFriendlyErrorMessage(new System.TimeoutException()));
        Assert.AreEqual("Network connection failed", 
            TelemetryExceptionHandler.GetUserFriendlyErrorMessage(new System.Net.Sockets.SocketException()));
        Assert.AreEqual("I/O operation failed", 
            TelemetryExceptionHandler.GetUserFriendlyErrorMessage(new System.IO.IOException()));
        Assert.AreEqual("Access denied", 
            TelemetryExceptionHandler.GetUserFriendlyErrorMessage(new UnauthorizedAccessException()));
        Assert.AreEqual("Invalid configuration detected", 
            TelemetryExceptionHandler.GetUserFriendlyErrorMessage(new ArgumentException()));
        Assert.AreEqual("An unexpected error occurred", 
            TelemetryExceptionHandler.GetUserFriendlyErrorMessage(new InvalidOperationException()));
    }
}
