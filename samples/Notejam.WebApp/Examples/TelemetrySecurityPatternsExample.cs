using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Looplex.Samples.WebApp.Examples;

/// <summary>
/// Example configuration for telemetry security patterns.
/// This class demonstrates how developers should configure regex patterns for their specific locale and requirements.
/// </summary>
public static class TelemetryConfigurationExample
{
    /// <summary>
    /// Example regex patterns for Brazilian locale (CPF, CNPJ, etc.).
    /// Developers should customize these patterns based on their specific requirements.
    /// </summary>
    public static Dictionary<string, Regex> GetBrazilianPatterns()
    {
        return new Dictionary<string, Regex>
        {
            // Email patterns
            ["email"] = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
                RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
            
            // Brazilian CPF pattern
            ["cpf"] = new Regex(@"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b", 
                RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
            
            // Brazilian CNPJ pattern
            ["cnpj"] = new Regex(@"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b", 
                RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
            
            // Brazilian phone pattern
            ["phone"] = new Regex(@"\b\(?\d{2}\)?\s?\d{4,5}-?\d{4}\b", 
                RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
            
            // Credit card pattern
            ["creditcard"] = new Regex(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", 
                RegexOptions.Compiled, TimeSpan.FromMilliseconds(100))
        };
    }

    /// <summary>
    /// Example regex patterns for US locale (SSN, etc.).
    /// Developers should customize these patterns based on their specific requirements.
    /// </summary>
    public static Dictionary<string, Regex> GetUSPatterns()
    {
        return new Dictionary<string, Regex>
        {
            // Email patterns
            ["email"] = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
                RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
            
            // US SSN pattern
            ["ssn"] = new Regex(@"\b\d{3}-\d{2}-\d{4}\b", 
                RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
            
            // US phone pattern
            ["phone"] = new Regex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", 
                RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
            
            // Credit card pattern
            ["creditcard"] = new Regex(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", 
                RegexOptions.Compiled, TimeSpan.FromMilliseconds(100))
        };
    }

    /// <summary>
    /// Example of how to use the TelemetrySecurityHelper with custom patterns.
    /// </summary>
    public static void ExampleUsage()
    {
        // Get patterns for your locale
        var patterns = GetBrazilianPatterns();
        
        // Example string with sensitive data
        // var sensitiveData = "User email: joao@example.com, CPF: 123.456.789-00, Phone: (11) 99999-9999";
        
        // Sanitize using user-provided patterns
        // Note: TelemetrySecurityHelper.SanitizeStringValue requires the patterns to be configured
        // This is just an example of how the method would be called
        // var sanitized = TelemetrySecurityHelper.SanitizeStringValue(sensitiveData, patterns);
        
        // Result: "User email: ********, CPF: ********, Phone: ********"
    }
}
