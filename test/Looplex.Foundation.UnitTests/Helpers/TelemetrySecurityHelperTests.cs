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
        Assert.AreEqual("[REDACTED]", sanitized["authorization"]);
        Assert.AreEqual("[REDACTED]", sanitized["cookie"]);
        Assert.AreEqual("[REDACTED]", sanitized["x-api-key"]);
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
        Assert.IsTrue(sanitized.Contains("password=[REDACTED]"));
        Assert.IsTrue(sanitized.Contains("token=[REDACTED]"));
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
        Assert.AreEqual("[REDACTED]", sanitized["password"]);
        Assert.AreEqual("[REDACTED]", sanitized["api_key"]);
        Assert.AreEqual("[REDACTED]", sanitized["email"]);
        Assert.AreEqual("[REDACTED]", sanitized["phone"]);
        Assert.AreEqual("active", sanitized["status"]);
    }

    [TestMethod]
    public void SanitizeStringValue_ShouldRedactEmailAddresses()
    {
        // Arrange
        var value = "Contact user@example.com for support";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeStringValue(value);

        // Assert
        Assert.AreEqual("Contact [REDACTED] for support", sanitized);
    }

    [TestMethod]
    public void SanitizeStringValue_ShouldRedactPhoneNumbers()
    {
        // Arrange
        var value = "Call us at 555-123-4567 or 555.123.4567";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeStringValue(value);

        // Assert
        Assert.AreEqual("Call us at [REDACTED] or [REDACTED]", sanitized);
    }

    [TestMethod]
    public void SanitizeStringValue_ShouldRedactCreditCardNumbers()
    {
        // Arrange
        var value = "Card number: 1234-5678-9012-3456";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeStringValue(value);

        // Assert
        Assert.AreEqual("Card number: [REDACTED]", sanitized);
    }

    [TestMethod]
    public void SanitizeStringValue_ShouldRedactSSN()
    {
        // Arrange
        var value = "SSN: 123-45-6789";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeStringValue(value);

        // Assert
        Assert.AreEqual("SSN: [REDACTED]", sanitized);
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
        Assert.IsTrue(sanitized.Contains("password=[REDACTED]"));
        Assert.IsTrue(sanitized.Contains("token=[REDACTED]"));
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
        Assert.AreEqual("[REDACTED]", sanitized["password"]);
        Assert.AreEqual("[REDACTED]", sanitized["token"]);
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
    public void SanitizePath_ShouldRedactUserIds()
    {
        // Arrange
        var paths = new[]
        {
            "/users/12345",
            "/api/users/67890",
            "/profiles/11111",
            "/accounts/22222",
            "/orders/user/44444",
            "/api/orders/user/55555"
        };

        // Act & Assert
        foreach (var path in paths)
        {
            var sanitized = TelemetrySecurityHelper.SanitizePath(path);
            Assert.IsTrue(sanitized.Contains("[USER_ID]"), $"Path {path} should contain [USER_ID]");
            Assert.IsFalse(sanitized.Contains("12345"), $"Path {path} should not contain user ID");
        }
    }

    [TestMethod]
    public void SanitizePath_ShouldNotRedactNonUserPaths()
    {
        // Arrange
        var paths = new[]
        {
            "/api/health",
            "/api/status",
            "/api/version",
            "/docs/swagger",
            "/static/css/style.css"
        };

        // Act & Assert
        foreach (var path in paths)
        {
            var sanitized = TelemetrySecurityHelper.SanitizePath(path);
            Assert.AreEqual(path, sanitized, $"Path {path} should remain unchanged");
        }
    }

    [TestMethod]
    public void SanitizeIpAddress_ShouldRedactLastOctet()
    {
        // Arrange
        var ipAddress = "192.168.1.100";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeIpAddress(ipAddress);

        // Assert
        Assert.AreEqual("192.168.1.xxx", sanitized);
    }

    [TestMethod]
    public void RedactIpAddress_ShouldCompletelyRedact()
    {
        // Arrange
        var ipAddress = "192.168.1.100";

        // Act
        var redacted = TelemetrySecurityHelper.RedactIpAddress(ipAddress);

        // Assert
        Assert.AreEqual("[IP_REDACTED]", redacted);
    }

    [TestMethod]
    public void SanitizeUrl_ShouldRedactUserIdsInPath()
    {
        // Arrange
        var url = "https://api.example.com/users/12345?name=john&password=secret";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeUrl(url);

        // Assert
        Assert.IsTrue(sanitized.Contains("/users/[USER_ID]"));
        Assert.IsTrue(sanitized.Contains("name=john"));
        Assert.IsTrue(sanitized.Contains("password=[REDACTED]"));
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
        Assert.AreEqual("[REDACTED]", sanitized["x-tenant-id"]);
        Assert.AreEqual("[REDACTED]", sanitized["x-organization-id"]);
        Assert.AreEqual("[REDACTED]", sanitized["x-company-id"]);
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
        Assert.IsTrue(sanitized.Contains("tenant_id=[REDACTED]"));
        Assert.IsTrue(sanitized.Contains("org_id=[REDACTED]"));
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
        Assert.AreEqual("[REDACTED]", sanitized["tenant_id"]);
        Assert.AreEqual("[REDACTED]", sanitized["organization_id"]);
        Assert.AreEqual("active", sanitized["status"]);
    }

    [TestMethod]
    public void SanitizePath_ShouldRedactTenantIds()
    {
        // Arrange
        var paths = new[]
        {
            "/tenant/12345",
            "/api/tenant/67890",
            "/org/11111",
            "/api/organization/22222",
            "/company/33333",
            "/api/company/44444",
            "/client/55555",
            "/api/customer/66666"
        };

        // Act & Assert
        foreach (var path in paths)
        {
            var sanitized = TelemetrySecurityHelper.SanitizePath(path);
            Assert.IsTrue(sanitized.Contains("[TENANT_ID]"), $"Path {path} should contain [TENANT_ID]");
            Assert.IsFalse(sanitized.Contains("12345"), $"Path {path} should not contain tenant ID");
        }
    }

    [TestMethod]
    public void SanitizePath_ShouldDistinguishBetweenUserAndTenantIds()
    {
        // Arrange
        var userPaths = new[] { "/users/12345", "/api/profiles/67890" };
        var tenantPaths = new[] { "/tenant/12345", "/api/org/67890" };

        // Act & Assert for User IDs
        foreach (var path in userPaths)
        {
            var sanitized = TelemetrySecurityHelper.SanitizePath(path);
            Assert.IsTrue(sanitized.Contains("[USER_ID]"), $"Path {path} should contain [USER_ID]");
            Assert.IsFalse(sanitized.Contains("[TENANT_ID]"), $"Path {path} should not contain [TENANT_ID]");
        }

        // Act & Assert for Tenant IDs
        foreach (var path in tenantPaths)
        {
            var sanitized = TelemetrySecurityHelper.SanitizePath(path);
            Assert.IsTrue(sanitized.Contains("[TENANT_ID]"), $"Path {path} should contain [TENANT_ID]");
            Assert.IsFalse(sanitized.Contains("[USER_ID]"), $"Path {path} should not contain [USER_ID]");
        }
    }

    [TestMethod]
    public void SanitizeUrl_ShouldRedactTenantIdsInPath()
    {
        // Arrange
        var url = "https://api.example.com/tenant/12345?name=john&tenant_id=secret";

        // Act
        var sanitized = TelemetrySecurityHelper.SanitizeUrl(url);

        // Assert
        Assert.IsTrue(sanitized.Contains("/tenant/[TENANT_ID]"));
        Assert.IsTrue(sanitized.Contains("name=john"));
        Assert.IsTrue(sanitized.Contains("tenant_id=[REDACTED]"));
    }
}
