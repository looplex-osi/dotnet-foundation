using System;
using Looplex.Foundation.Ports;

namespace Looplex.Foundation.Adapters;

/// <summary>
/// Default implementation of IEnvironmentProvider that uses System.Environment.
/// </summary>
public class EnvironmentProvider : IEnvironmentProvider
{
    /// <summary>
    /// Gets the value of an environment variable.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <returns>The value of the environment variable, or null if not found.</returns>
    public string? GetEnvironmentVariable(string variable)
    {
        return Environment.GetEnvironmentVariable(variable);
    }

    /// <summary>
    /// Gets the value of an environment variable with a default fallback.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <param name="defaultValue">The default value to return if the variable is not found.</param>
    /// <returns>The value of the environment variable, or the default value if not found.</returns>
    public string GetEnvironmentVariable(string variable, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(variable) ?? defaultValue;
    }
}
