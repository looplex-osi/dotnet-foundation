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
                sanitized[header.Key] = "[REDACTED]";
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
                    sanitizedParams.Add($"{key}=[REDACTED]");
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
                sanitized[property.Key] = "[REDACTED]";
            }
            else if (property.Value is string stringValue)
            {
                // Check if the value itself contains sensitive patterns
                sanitized[property.Key] = SanitizeStringValue(stringValue);
            }
            else
            {
                sanitized[property.Key] = property.Value;
            }
        }

        return sanitized;
    }

    /// <summary>
    /// Pre-compiled regex pattern for email address detection with timeout protection.
    /// </summary>
    private static readonly Regex EmailRegex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
    
    /// <summary>
    /// Pre-compiled regex pattern for phone number detection with timeout protection.
    /// </summary>
    private static readonly Regex PhoneRegex = new Regex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", 
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    
    /// <summary>
    /// Pre-compiled regex pattern for credit card number detection with timeout protection.
    /// </summary>
    private static readonly Regex CreditCardRegex = new Regex(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", 
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    
    /// <summary>
    /// Pre-compiled regex pattern for SSN detection with timeout protection.
    /// </summary>
    private static readonly Regex SsnRegex = new Regex(@"\b\d{3}-\d{2}-\d{4}\b", 
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Sanitizes a string value by redacting sensitive patterns.
    /// Uses pre-compiled regex patterns with timeout to prevent regex injection attacks.
    /// </summary>
    /// <param name="value">The string value to sanitize.</param>
    /// <returns>A sanitized string with sensitive patterns redacted.</returns>
    public static string SanitizeStringValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        try
        {
            // Redact email addresses
            value = EmailRegex.Replace(value, "[REDACTED]");

            // Redact phone numbers
            value = PhoneRegex.Replace(value, "[REDACTED]");

            // Redact credit card numbers
            value = CreditCardRegex.Replace(value, "[REDACTED]");

            // Redact SSN patterns
            value = SsnRegex.Replace(value, "[REDACTED]");
        }
        catch (RegexMatchTimeoutException)
        {
            // If regex times out, return a safe fallback
            return "[PATTERN_TIMEOUT]";
        }

        return value;
    }

    /// <summary>
    /// Sanitizes a URL by redacting sensitive query parameters and user IDs.
    /// </summary>
    /// <param name="url">The URL to sanitize.</param>
    /// <returns>A sanitized URL with sensitive parameters and user IDs redacted.</returns>
    public static string SanitizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;

        try
        {
            var uri = new Uri(url);
            var sanitizedQuery = SanitizeQueryString(uri.Query.TrimStart('?'));
            
            // Sanitize path to remove user IDs and other sensitive path segments
            var sanitizedPath = SanitizePath(uri.AbsolutePath);
            
            var builder = new UriBuilder(uri)
            {
                Path = sanitizedPath,
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
    /// Sanitizes a path by redacting user IDs and other sensitive path segments.
    /// </summary>
    /// <param name="path">The path to sanitize.</param>
    /// <returns>A sanitized path with user IDs redacted.</returns>
    public static string SanitizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        // Common patterns for user IDs and tenant IDs in paths
        var patterns = new[]
        {
            // User ID patterns
            @"/users/\d+",           // /users/12345
            @"/user/\d+",            // /user/12345
            @"/profiles/\d+",        // /profiles/12345
            @"/accounts/\d+",        // /accounts/12345
            @"/customers/\d+",       // /customers/12345
            @"/members/\d+",         // /members/12345
            @"/clients/\d+",         // /clients/12345
            @"/orders/user/\d+",     // /orders/user/12345
            @"/api/users/\d+",       // /api/users/12345
            @"/api/user/\d+",        // /api/user/12345
            @"/api/profiles/\d+",    // /api/profiles/12345
            @"/api/accounts/\d+",    // /api/accounts/12345
            @"/api/customers/\d+",   // /api/customers/12345
            @"/api/members/\d+",     // /api/members/12345
            @"/api/clients/\d+",     // /api/clients/12345
            @"/api/orders/user/\d+", // /api/orders/user/12345
            
            // Tenant ID patterns
            @"/tenant/\d+",          // /tenant/12345
            @"/tenants/\d+",         // /tenants/12345
            @"/org/\d+",             // /org/12345
            @"/organization/\d+",    // /organization/12345
            @"/company/\d+",         // /company/12345
            @"/client/\d+",          // /client/12345
            @"/customer/\d+",        // /customer/12345
            @"/api/tenant/\d+",      // /api/tenant/12345
            @"/api/tenants/\d+",     // /api/tenants/12345
            @"/api/org/\d+",         // /api/org/12345
            @"/api/organization/\d+", // /api/organization/12345
            @"/api/company/\d+",     // /api/company/12345
            @"/api/client/\d+",      // /api/client/12345
            @"/api/customer/\d+"     // /api/customer/12345
        };

        var sanitizedPath = path;
        foreach (var pattern in patterns)
        {
            sanitizedPath = Regex.Replace(sanitizedPath, pattern, match =>
            {
                var parts = match.Value.Split('/');
                var lastPart = parts[parts.Length - 1];
                if (int.TryParse(lastPart, out _))
                {
                    // Determine if it's a tenant ID or user ID based on the path segment
                    var pathSegment = parts[parts.Length - 2].ToLowerInvariant();
                    if (pathSegment.Contains("tenant") || 
                        pathSegment.Contains("org") || 
                        pathSegment.Contains("organization") || 
                        pathSegment.Contains("company") ||
                        pathSegment.Contains("client") || 
                        pathSegment.Contains("customer"))
                    {
                        parts[parts.Length - 1] = "[TENANT_ID]";
                    }
                    else
                    {
                        parts[parts.Length - 1] = "[USER_ID]";
                    }
                    return string.Join("/", parts);
                }
                return match.Value;
            }, RegexOptions.IgnoreCase);
        }

        return sanitizedPath;
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

    /// <summary>
    /// Sanitizes an IP address by redacting the last octet for privacy.
    /// Uses proper IP address validation to prevent bypassing.
    /// </summary>
    /// <param name="ipAddress">The IP address to sanitize.</param>
    /// <returns>A sanitized IP address with the last octet redacted.</returns>
    public static string SanitizeIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return ipAddress;

        // Try to parse as IPv4 address for proper validation
        if (IPAddress.TryParse(ipAddress, out var parsedIp) && parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var parts = ipAddress.Split('.');
            if (parts.Length == 4 && parts.All(part => int.TryParse(part, out var octet) && octet >= 0 && octet <= 255))
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}.xxx";
            }
        }

        // If not a valid IPv4, return as-is (might be IPv6 or hostname)
        return ipAddress;
    }

    /// <summary>
    /// Completely redacts an IP address.
    /// Uses proper IP address validation to prevent bypassing.
    /// </summary>
    /// <param name="ipAddress">The IP address to redact.</param>
    /// <returns>A redacted IP address.</returns>
    public static string RedactIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return ipAddress;

        // Try to parse as IP address for proper validation
        if (IPAddress.TryParse(ipAddress, out _))
        {
            return "[IP_REDACTED]";
        }

        // If not a valid IP, return as-is (might be hostname)
        return ipAddress;
    }
}
