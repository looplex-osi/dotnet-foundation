using System;

namespace Looplex.Foundation.Ports;

/// <summary>
/// Interface for environment variable access to enable dependency injection and testing.
/// </summary>
public interface IEnvironmentProvider
{
    /// <summary>
    /// Gets the value of an environment variable.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <returns>The value of the environment variable, or null if not found.</returns>
    string? GetEnvironmentVariable(string variable);

    /// <summary>
    /// Gets the value of an environment variable with a default fallback.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <param name="defaultValue">The default value to return if the variable is not found.</param>
    /// <returns>The value of the environment variable, or the default value if not found.</returns>
    string GetEnvironmentVariable(string variable, string defaultValue);
}
