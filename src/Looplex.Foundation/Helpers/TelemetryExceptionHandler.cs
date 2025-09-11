using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Looplex.Foundation.Helpers;

/// <summary>
/// Helper class for handling telemetry exceptions safely.
/// This ensures that telemetry failures don't disrupt the main application flow.
/// </summary>
public static class TelemetryExceptionHandler
{
    /// <summary>
    /// Safely executes a telemetry operation with exception handling.
    /// </summary>
    /// <param name="operation">The telemetry operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging purposes.</param>
    /// <param name="fallbackAction">Optional fallback action to execute if the operation fails.</param>
    public static void SafeExecute(Action operation, string operationName, Action? fallbackAction = null)
    {
        try
        {
            operation();
        }
        catch (Exception ex)
        {
            LogTelemetryError(operationName, ex);
            fallbackAction?.Invoke();
        }
    }

    /// <summary>
    /// Safely executes a telemetry operation with exception handling and returns a result.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The telemetry operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging purposes.</param>
    /// <param name="fallbackValue">The fallback value to return if the operation fails.</param>
    /// <returns>The result of the operation or the fallback value if it fails.</returns>
    public static T SafeExecute<T>(Func<T> operation, string operationName, T fallbackValue = default!)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            LogTelemetryError(operationName, ex);
            return fallbackValue;
        }
    }

    /// <summary>
    /// Safely executes an async telemetry operation with exception handling.
    /// </summary>
    /// <param name="operation">The async telemetry operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging purposes.</param>
    /// <param name="fallbackAction">Optional fallback action to execute if the operation fails.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async System.Threading.Tasks.Task SafeExecuteAsync(
        Func<System.Threading.Tasks.Task> operation, 
        string operationName, 
        Func<System.Threading.Tasks.Task>? fallbackAction = null)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            LogTelemetryError(operationName, ex);
            if (fallbackAction != null)
            {
                try
                {
                    await fallbackAction();
                }
                catch (Exception fallbackEx)
                {
                    LogTelemetryError($"{operationName}_Fallback", fallbackEx);
                }
            }
        }
    }

    /// <summary>
    /// Safely executes an async telemetry operation with exception handling and returns a result.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The async telemetry operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging purposes.</param>
    /// <param name="fallbackValue">The fallback value to return if the operation fails.</param>
    /// <returns>A task containing the result of the operation or the fallback value if it fails.</returns>
    public static async System.Threading.Tasks.Task<T> SafeExecuteAsync<T>(
        Func<System.Threading.Tasks.Task<T>> operation, 
        string operationName, 
        T fallbackValue = default!)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            LogTelemetryError(operationName, ex);
            return fallbackValue;
        }
    }

    /// <summary>
    /// Logs telemetry errors in a consistent format.
    /// </summary>
    /// <param name="operationName">The name of the operation that failed.</param>
    /// <param name="exception">The exception that occurred.</param>
    private static void LogTelemetryError(string operationName, Exception exception)
    {
        var errorMessage = $"Telemetry operation '{operationName}' failed: {exception.Message}";
        var fullError = $"{errorMessage}\nStack Trace: {exception.StackTrace}";

        // Log to debug output
        Debug.WriteLine(fullError);

        // Log to trace output (can be captured by logging frameworks)
        Trace.WriteLine(fullError);

        // Log to console in development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            Console.WriteLine($"[TELEMETRY ERROR] {errorMessage}");
        }
    }

    /// <summary>
    /// Determines if an exception should be retried based on its type and message.
    /// </summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns>True if the operation should be retried, false otherwise.</returns>
    public static bool ShouldRetry(Exception exception)
    {
        return exception switch
        {
            System.Net.Http.HttpRequestException => true,
            System.TimeoutException => true,
            System.Net.Sockets.SocketException => true,
            System.IO.IOException => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a user-friendly error message for telemetry failures.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A user-friendly error message.</returns>
    public static string GetUserFriendlyErrorMessage(Exception exception)
    {
        return exception switch
        {
            System.Net.Http.HttpRequestException => "Network connectivity issue detected",
            System.TimeoutException => "Operation timed out",
            System.Net.Sockets.SocketException => "Network connection failed",
            System.IO.IOException => "I/O operation failed",
            System.UnauthorizedAccessException => "Access denied",
            System.ArgumentException => "Invalid configuration detected",
            _ => "An unexpected error occurred"
        };
    }
}
