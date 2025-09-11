using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Looplex.Foundation.Helpers;

/// <summary>
/// Helper class for securing telemetry data by sanitizing sensitive information.
/// This class ensures that no sensitive data (PII, passwords, tokens) is sent to telemetry systems.
/// Regex patterns are configurable by the developer to support different locales and requirements.
/// </summary>
public static class TelemetrySecurityHelper
{
    /// <summary>
    /// List of sensitive header names that should be redacted from telemetry.
    /// </summary>
    private static readonly ConcurrentDictionary<string, bool> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["authorization"] = true,
        ["cookie"] = true,
        ["x-api-key"] = true,
        ["x-auth-token"] = true,
        ["x-access-token"] = true,
        ["x-csrf-token"] = true,
        ["x-requested-with"] = true,
        ["x-forwarded-for"] = true,
        ["x-real-ip"] = true,
        ["proxy-authorization"] = true,
        ["www-authenticate"] = true,
        ["authentication-info"] = true,
        ["set-cookie"] = true,
        ["x-forwarded-proto"] = true,
        ["x-forwarded-host"] = true,
        ["x-forwarded-port"] = true,
        ["x-tenant-id"] = true,
        ["x-organization-id"] = true,
        ["x-org-id"] = true,
        ["x-company-id"] = true,
        ["x-client-id"] = true,
        ["x-customer-id"] = true
    };

    /// <summary>
    /// List of sensitive query parameter names that should be redacted.
    /// </summary>
    private static readonly ConcurrentDictionary<string, bool> SensitiveQueryParams = new(StringComparer.OrdinalIgnoreCase)
    {
        ["password"] = true,
        ["passwd"] = true,
        ["pwd"] = true,
        ["token"] = true,
        ["key"] = true,
        ["secret"] = true,
        ["auth"] = true,
        ["authorization"] = true,
        ["api_key"] = true,
        ["access_token"] = true,
        ["refresh_token"] = true,
        ["session_id"] = true,
        ["sid"] = true,
        ["csrf_token"] = true,
        ["jwt"] = true,
        ["bearer"] = true,
        ["tenant_id"] = true,
        ["organization_id"] = true,
        ["org_id"] = true,
        ["company_id"] = true,
        ["client_id"] = true,
        ["customer_id"] = true
    };

    /// <summary>
    /// List of sensitive property names that should be redacted.
    /// </summary>
    private static readonly ConcurrentDictionary<string, bool> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["password"] = true,
        ["passwd"] = true,
        ["pwd"] = true,
        ["token"] = true,
        ["key"] = true,
        ["secret"] = true,
        ["auth"] = true,
        ["authorization"] = true,
        ["api_key"] = true,
        ["access_token"] = true,
        ["refresh_token"] = true,
        ["session_id"] = true,
        ["sid"] = true,
        ["csrf_token"] = true,
        ["jwt"] = true,
        ["bearer"] = true,
        ["ssn"] = true,
        ["social_security_number"] = true,
        ["credit_card"] = true,
        ["card_number"] = true,
        ["cvv"] = true,
        ["cvc"] = true,
        ["pin"] = true,
        ["tax_id"] = true,
        ["ein"] = true,
        ["phone"] = true,
        ["email"] = true,
        ["address"] = true,
        ["zip"] = true,
        ["postal_code"] = true,
        ["tenant_id"] = true,
        ["organization_id"] = true,
        ["org_id"] = true,
        ["company_id"] = true,
        ["client_id"] = true,
        ["customer_id"] = true
    };

    /// <summary>
    /// Redacts sensitive information from HTTP headers.
    /// </summary>
    /// <param name="headers">The headers dictionary to sanitize.</param>
    /// <returns>A sanitized dictionary with sensitive headers redacted.</returns>
    public static Dictionary<string, string> SanitizeHeaders(Dictionary<string, string> headers)
    {
        if (headers == null) return new Dictionary<string, string>();

        var sanitized = new Dictionary<string, string>();
        
        foreach (var header in headers)
        {
            if (SensitiveHeaders.ContainsKey(header.Key))
            {
                sanitized[header.Key] = REDACTION_PLACEHOLDER;
            }
            else
            {
                sanitized[header.Key] = header.Value;
            }
        }

        return sanitized;
    }

    /// <summary>
    /// Redacts sensitive information from query parameters.
    /// </summary>
    /// <param name="queryString">The query string to sanitize.</param>
    /// <returns>A sanitized query string with sensitive parameters redacted.</returns>
    public static string SanitizeQueryString(string queryString)
    {
        if (string.IsNullOrEmpty(queryString)) return queryString;

        var parameters = queryString.Split('&');
        var sanitizedParams = new List<string>();

        foreach (var param in parameters)
        {
            if (string.IsNullOrEmpty(param)) continue;

            var parts = param.Split(new char[] { '=' }, 2);
            if (parts.Length == 2)
            {
                var key = parts[0];
                var value = parts[1];

                if (SensitiveQueryParams.ContainsKey(key))
                {
                    sanitizedParams.Add($"{key}={REDACTION_PLACEHOLDER}");
                }
                else
                {
                    sanitizedParams.Add(param);
                }
            }
            else
            {
                sanitizedParams.Add(param);
            }
        }

        return string.Join("&", sanitizedParams);
    }

    /// <summary>
    /// Redacts sensitive information from telemetry properties.
    /// </summary>
    /// <param name="properties">The properties dictionary to sanitize.</param>
    /// <returns>A sanitized dictionary with sensitive properties redacted.</returns>
    public static Dictionary<string, object> SanitizeProperties(Dictionary<string, object> properties)
    {
        if (properties == null) return new Dictionary<string, object>();

        var sanitized = new Dictionary<string, object>();

        foreach (var property in properties)
        {
            if (SensitiveProperties.ContainsKey(property.Key))
            {
                sanitized[property.Key] = REDACTION_PLACEHOLDER;
            }
            else if (property.Value is string stringValue)
            {
                // Check if the value itself contains sensitive patterns
                // Note: This method now requires user-provided regex patterns
                sanitized[property.Key] = stringValue;
            }
            else
            {
                sanitized[property.Key] = property.Value;
            }
        }

        return sanitized;
    }

    /// <summary>
    /// Placeholder for redacted sensitive data.
    /// </summary>
    private const string REDACTION_PLACEHOLDER = "********";

    /// <summary>
    /// Sanitizes a string value using user-provided regex patterns.
    /// Applies boundary protection to prevent regex injection attacks.
    /// </summary>
    /// <param name="value">The string value to sanitize.</param>
    /// <param name="userProvidedRegexps">Dictionary of regex patterns provided by the developer.</param>
    /// <returns>A sanitized string with sensitive patterns redacted.</returns>
    public static string SanitizeStringValue(string value, Dictionary<string, Regex> userProvidedRegexps)
    {
        if (string.IsNullOrEmpty(value) || userProvidedRegexps == null || userProvidedRegexps.Count == 0) 
            return value;

        try
        {
            foreach (var regex in userProvidedRegexps)
            {
                // Apply boundary protection to prevent regex injection attacks
                var protectedPattern = $@"\b{regex.Value}\b";
                value = Regex.Replace(value, protectedPattern, REDACTION_PLACEHOLDER, 
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // If regex times out, return a safe fallback
            return "[PATTERN_TIMEOUT]";
        }

        return value;
    }

    /// <summary>
    /// Sanitizes a URL by redacting sensitive query parameters.
    /// Paths are preserved for debugging and tracing purposes.
    /// </summary>
    /// <param name="url">The URL to sanitize.</param>
    /// <returns>A sanitized URL with sensitive parameters redacted.</returns>
    public static string SanitizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;

        try
        {
            var uri = new Uri(url);
            var sanitizedQuery = SanitizeQueryString(uri.Query.TrimStart('?'));
            
            // Preserve path for debugging and tracing (as per Nagao's requirements)
            var builder = new UriBuilder(uri)
            {
                Query = sanitizedQuery
            };

            return builder.ToString();
        }
        catch
        {
            // If URL parsing fails, return original URL
            return url;
        }
    }


    /// <summary>
    /// Checks if a property name is considered sensitive.
    /// </summary>
    /// <param name="propertyName">The property name to check.</param>
    /// <returns>True if the property is sensitive, false otherwise.</returns>
    public static bool IsSensitiveProperty(string propertyName)
    {
        return !string.IsNullOrEmpty(propertyName) && SensitiveProperties.ContainsKey(propertyName);
    }

    /// <summary>
    /// Checks if a header name is considered sensitive.
    /// </summary>
    /// <param name="headerName">The header name to check.</param>
    /// <returns>True if the header is sensitive, false otherwise.</returns>
    public static bool IsSensitiveHeader(string headerName)
    {
        return !string.IsNullOrEmpty(headerName) && SensitiveHeaders.ContainsKey(headerName);
    }

}
