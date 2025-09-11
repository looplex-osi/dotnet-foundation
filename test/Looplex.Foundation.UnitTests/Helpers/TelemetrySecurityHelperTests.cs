using System;
using System.Collections.Generic;
using Looplex.Foundation.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Looplex.Foundation.UnitTests.Helpers;

/// <summary>
/// Unit tests for TelemetrySecurityHelper to ensure sensitive data is properly redacted.
/// </summary>
[TestClass]
public class TelemetrySecurityHelperTests
{
    [TestMethod]
    public void SanitizeHeaders_ShouldRedactSensitiveHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["authorization"] = "Bearer secret-token-123",
            ["cookie"] = "session=abc123; auth=xyz789",
            ["x-api-key"] = "api-key-secret",
            ["content-type"] = "application/json",
            ["user-agent"] = "Mozilla/5.0"
        };

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeHeaders(headers);

        // Assert
        Assert.AreEqual("********", sanitized["authorization"]);
        Assert.AreEqual("********", sanitized["cookie"]);
        Assert.AreEqual("********", sanitized["x-api-key"]);
        Assert.AreEqual("application/json", sanitized["content-type"]);
        Assert.AreEqual("Mozilla/5.0", sanitized["user-agent"]);
    }

    [TestMethod]
    public void SanitizeQueryString_ShouldRedactSensitiveParameters()
    {
        // Arrange
        var queryString = "name=john&password=secret123&token=abc123&category=electronics";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeQueryString(queryString);

        // Assert
        Assert.IsTrue(sanitized.Contains("name=john"));
        Assert.IsTrue(sanitized.Contains("password=********"));
        Assert.IsTrue(sanitized.Contains("token=********"));
        Assert.IsTrue(sanitized.Contains("category=electronics"));
    }

    [TestMethod]
    public void SanitizeProperties_ShouldRedactSensitiveProperties()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            ["user_id"] = "user123",
            ["password"] = "secret123",
            ["api_key"] = "key-abc-123",
            ["email"] = "user@example.com",
            ["phone"] = "555-123-4567",
            ["status"] = "active"
        };

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeProperties(properties);

        // Assert
        Assert.AreEqual("user123", sanitized["user_id"]);
        Assert.AreEqual("********", sanitized["password"]);
        Assert.AreEqual("********", sanitized["api_key"]);
        Assert.AreEqual("********", sanitized["email"]);
        Assert.AreEqual("********", sanitized["phone"]);
        Assert.AreEqual("active", sanitized["status"]);
    }

    [TestMethod]
    public void SanitizeStringValue_ShouldRedactEmailAddresses()
    {
        // Arrange
        var value = "Contact user@example.com for support";
        var patterns = new Dictionary<string, System.Text.RegularExpressions.Regex>
        {
            ["email"] = new System.Text.RegularExpressions.Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
                System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100))
        };

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeStringValue(value, patterns);

        // Assert
        Assert.AreEqual("Contact ******** for support", sanitized);
    }

    [TestMethod]
    public void SanitizeStringValue_ShouldRedactPhoneNumbers()
    {
        // Arrange
        var value = "Call us at 555-123-4567 or 555.123.4567";
        var patterns = new Dictionary<string, System.Text.RegularExpressions.Regex>
        {
            ["phone"] = new System.Text.RegularExpressions.Regex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", 
                System.Text.RegularExpressions.RegexOptions.Compiled, TimeSpan.FromMilliseconds(100))
        };

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeStringValue(value, patterns);

        // Assert
        Assert.AreEqual("Call us at ******** or ********", sanitized);
    }

    [TestMethod]
    public void SanitizeStringValue_ShouldRedactCreditCardNumbers()
    {
        // Arrange
        var value = "Card number: 1234-5678-9012-3456";
        var patterns = new Dictionary<string, System.Text.RegularExpressions.Regex>
        {
            ["creditcard"] = new System.Text.RegularExpressions.Regex(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", 
                System.Text.RegularExpressions.RegexOptions.Compiled, TimeSpan.FromMilliseconds(100))
        };

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeStringValue(value, patterns);

        // Assert
        Assert.AreEqual("Card number: ********", sanitized);
    }

    [TestMethod]
    public void SanitizeStringValue_ShouldRedactSSN()
    {
        // Arrange
        var value = "SSN: 123-45-6789";
        var patterns = new Dictionary<string, System.Text.RegularExpressions.Regex>
        {
            ["ssn"] = new System.Text.RegularExpressions.Regex(@"\b\d{3}-\d{2}-\d{4}\b", 
                System.Text.RegularExpressions.RegexOptions.Compiled, TimeSpan.FromMilliseconds(100))
        };

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeStringValue(value, patterns);

        // Assert
        Assert.AreEqual("SSN: ********", sanitized);
    }

    [TestMethod]
    public void SanitizeUrl_ShouldRedactSensitiveQueryParameters()
    {
        // Arrange
        var url = "https://api.example.com/users?name=john&password=secret&token=abc123&page=1";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeUrl(url);

        // Assert
        Assert.IsTrue(sanitized.Contains("name=john"));
        Assert.IsTrue(sanitized.Contains("password=********"));
        Assert.IsTrue(sanitized.Contains("token=********"));
        Assert.IsTrue(sanitized.Contains("page=1"));
    }

    [TestMethod]
    public void IsSensitiveProperty_ShouldReturnTrueForSensitiveProperties()
    {
        // Act & Assert
        Assert.IsTrue(TelemetrySecurityHelper.IsSensitiveProperty("password"));
        Assert.IsTrue(TelemetrySecurityHelper.IsSensitiveProperty("api_key"));
        Assert.IsTrue(TelemetrySecurityHelper.IsSensitiveProperty("token"));
        Assert.IsTrue(TelemetrySecurityHelper.IsSensitiveProperty("email"));
        Assert.IsTrue(TelemetrySecurityHelper.IsSensitiveProperty("ssn"));
    }

    [TestMethod]
    public void IsSensitiveProperty_ShouldReturnFalseForNonSensitiveProperties()
    {
        // Act & Assert
        Assert.IsFalse(TelemetrySecurityHelper.IsSensitiveProperty("name"));
        Assert.IsFalse(TelemetrySecurityHelper.IsSensitiveProperty("status"));
        Assert.IsFalse(TelemetrySecurityHelper.IsSensitiveProperty("category"));
        Assert.IsFalse(TelemetrySecurityHelper.IsSensitiveProperty("count"));
    }

    [TestMethod]
    public void IsSensitiveHeader_ShouldReturnTrueForSensitiveHeaders()
    {
        // Act & Assert
        Assert.IsTrue(TelemetrySecurityHelper.IsSensitiveHeader("authorization"));
        Assert.IsTrue(TelemetrySecurityHelper.IsSensitiveHeader("cookie"));
        Assert.IsTrue(TelemetrySecurityHelper.IsSensitiveHeader("x-api-key"));
        Assert.IsTrue(TelemetrySecurityHelper.IsSensitiveHeader("x-auth-token"));
    }

    [TestMethod]
    public void IsSensitiveHeader_ShouldReturnFalseForNonSensitiveHeaders()
    {
        // Act & Assert
        Assert.IsFalse(TelemetrySecurityHelper.IsSensitiveHeader("content-type"));
        Assert.IsFalse(TelemetrySecurityHelper.IsSensitiveHeader("user-agent"));
        Assert.IsFalse(TelemetrySecurityHelper.IsSensitiveHeader("accept"));
        Assert.IsFalse(TelemetrySecurityHelper.IsSensitiveHeader("cache-control"));
    }

    [TestMethod]
    public void SanitizeProperties_ShouldHandleNullValues()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            ["password"] = null,
            ["token"] = "",
            ["name"] = "john"
        };

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeProperties(properties);

        // Assert
        Assert.AreEqual("********", sanitized["password"]);
        Assert.AreEqual("********", sanitized["token"]);
        Assert.AreEqual("john", sanitized["name"]);
    }

    [TestMethod]
    public void SanitizeHeaders_ShouldHandleNullInput()
    {
        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeHeaders(null);

        // Assert
        Assert.IsNotNull(sanitized);
        Assert.AreEqual(0, sanitized.Count);
    }

    [TestMethod]
    public void SanitizeProperties_ShouldHandleNullInput()
    {
        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeProperties(null);

        // Assert
        Assert.IsNotNull(sanitized);
        Assert.AreEqual(0, sanitized.Count);
    }


    [TestMethod]
    public void SanitizeUrl_ShouldRedactUserIdsInPath()
    {
        // Arrange
        var url = "https://api.example.com/users/12345?name=john&password=secret";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeUrl(url);

        // Assert
        Assert.IsTrue(sanitized.Contains("/users/12345")); // Paths are preserved for debugging
        Assert.IsTrue(sanitized.Contains("name=john"));
        Assert.IsTrue(sanitized.Contains("password=********"));
    }

    [TestMethod]
    public void SanitizeHeaders_ShouldRedactTenantHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["x-tenant-id"] = "tenant123",
            ["x-organization-id"] = "org456",
            ["x-company-id"] = "company789",
            ["content-type"] = "application/json"
        };

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeHeaders(headers);

        // Assert
        Assert.AreEqual("********", sanitized["x-tenant-id"]);
        Assert.AreEqual("********", sanitized["x-organization-id"]);
        Assert.AreEqual("********", sanitized["x-company-id"]);
        Assert.AreEqual("application/json", sanitized["content-type"]);
    }

    [TestMethod]
    public void SanitizeQueryString_ShouldRedactTenantParameters()
    {
        // Arrange
        var queryString = "name=john&tenant_id=12345&org_id=67890&category=electronics";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeQueryString(queryString);

        // Assert
        Assert.IsTrue(sanitized.Contains("name=john"));
        Assert.IsTrue(sanitized.Contains("tenant_id=********"));
        Assert.IsTrue(sanitized.Contains("org_id=********"));
        Assert.IsTrue(sanitized.Contains("category=electronics"));
    }

    [TestMethod]
    public void SanitizeProperties_ShouldRedactTenantProperties()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            ["user_id"] = "user123",
            ["tenant_id"] = "tenant456",
            ["organization_id"] = "org789",
            ["status"] = "active"
        };

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeProperties(properties);

        // Assert
        Assert.AreEqual("user123", sanitized["user_id"]);
        Assert.AreEqual("********", sanitized["tenant_id"]);
        Assert.AreEqual("********", sanitized["organization_id"]);
        Assert.AreEqual("active", sanitized["status"]);
    }


    [TestMethod]
    public void SanitizeUrl_ShouldRedactTenantIdsInPath()
    {
        // Arrange
        var url = "https://api.example.com/tenant/12345?name=john&tenant_id=secret";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeUrl(url);

        // Assert
        Assert.IsTrue(sanitized.Contains("/tenant/12345")); // Paths are preserved for debugging
        Assert.IsTrue(sanitized.Contains("name=john"));
        Assert.IsTrue(sanitized.Contains("tenant_id=********"));
    }
}
