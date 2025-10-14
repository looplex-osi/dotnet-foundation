using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Looplex.Protocols.HTTP.Ports;

namespace Looplex.Protocols.HTTP.IntegrationTests.SCIMv2;

[TestClass]
public class SCIMv2ServiceProviderConfigTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Mock SCIMv2 Service
                    var scimService = Substitute.For<ISCIMv2Service>();
                    services.AddSingleton(scimService);

                    // Mock JWT Service
                    var jwtService = Substitute.For<IJwtService>();
                    services.AddSingleton(jwtService);

                    // Mock Grant Type Service
                    var grantTypeService = Substitute.For<IGrantTypeService>();
                    services.AddSingleton(grantTypeService);
                });
            });

        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task ServiceProviderConfig_ValidRequest_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/ServiceProviderConfig");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);

        var responseContent = await response.Content.ReadAsStringAsync();
        var config = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.IsTrue(config.TryGetProperty("schemas", out var schemas));
        Assert.IsTrue(config.TryGetProperty("patch", out var patch));
        Assert.IsTrue(config.TryGetProperty("bulk", out var bulk));
        Assert.IsTrue(config.TryGetProperty("filter", out var filter));
        Assert.IsTrue(config.TryGetProperty("changePassword", out var changePassword));
        Assert.IsTrue(config.TryGetProperty("sort", out var sort));
        Assert.IsTrue(config.TryGetProperty("etag", out var etag));
        Assert.IsTrue(config.TryGetProperty("authenticationSchemes", out var authSchemes));

        // Verify schemas
        Assert.IsTrue(schemas.ValueKind == JsonValueKind.Array);
        var schemasArray = schemas.EnumerateArray().ToArray();
        Assert.IsTrue(schemasArray.Any(s => s.GetString() == "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig"));

        // Verify patch configuration
        Assert.IsTrue(patch.TryGetProperty("supported", out var patchSupported));
        Assert.IsTrue(patchSupported.GetBoolean());

        // Verify bulk configuration
        Assert.IsTrue(bulk.TryGetProperty("supported", out var bulkSupported));
        Assert.IsFalse(bulkSupported.GetBoolean());
        Assert.IsTrue(bulk.TryGetProperty("maxOperations", out var maxOps));
        Assert.AreEqual(0, maxOps.GetInt32());
        Assert.IsTrue(bulk.TryGetProperty("maxPayloadSize", out var maxPayload));
        Assert.AreEqual(0, maxPayload.GetInt32());

        // Verify filter configuration
        Assert.IsTrue(filter.TryGetProperty("supported", out var filterSupported));
        Assert.IsTrue(filterSupported.GetBoolean());
        Assert.IsTrue(filter.TryGetProperty("maxResults", out var maxResults));
        Assert.AreEqual(200, maxResults.GetInt32());

        // Verify changePassword configuration
        Assert.IsTrue(changePassword.TryGetProperty("supported", out var changePasswordSupported));
        Assert.IsFalse(changePasswordSupported.GetBoolean());

        // Verify sort configuration
        Assert.IsTrue(sort.TryGetProperty("supported", out var sortSupported));
        Assert.IsFalse(sortSupported.GetBoolean());

        // Verify etag configuration
        Assert.IsTrue(etag.TryGetProperty("supported", out var etagSupported));
        Assert.IsFalse(etagSupported.GetBoolean());

        // Verify authentication schemes
        Assert.IsTrue(authSchemes.ValueKind == JsonValueKind.Array);
        var authSchemesArray = authSchemes.EnumerateArray().ToArray();
        Assert.AreEqual(1, authSchemesArray.Length);
        
        var oauth2Scheme = authSchemesArray[0];
        Assert.IsTrue(oauth2Scheme.TryGetProperty("type", out var type));
        Assert.IsTrue(oauth2Scheme.TryGetProperty("name", out var name));
        Assert.IsTrue(oauth2Scheme.TryGetProperty("description", out var description));
        Assert.IsTrue(oauth2Scheme.TryGetProperty("specUri", out var specUri));
        Assert.IsTrue(oauth2Scheme.TryGetProperty("primary", out var primary));

        Assert.AreEqual("oauth2", type.GetString());
        Assert.AreEqual("OAuth 2.0", name.GetString());
        Assert.AreEqual("OAuth 2.0 Bearer Token", description.GetString());
        Assert.AreEqual("https://tools.ietf.org/html/rfc6749", specUri.GetString());
        Assert.IsTrue(primary.GetBoolean());
    }

    [TestMethod]
    public async Task ServiceProviderConfig_WithQueryParameters_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/ServiceProviderConfig?format=json");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ServiceProviderConfig_PostMethod_ReturnsMethodNotAllowed()
    {
        // Arrange
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/scim+json");

        // Act
        var response = await _client.PostAsync("/scim/v2/ServiceProviderConfig", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task ServiceProviderConfig_PutMethod_ReturnsMethodNotAllowed()
    {
        // Arrange
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/scim+json");

        // Act
        var response = await _client.PutAsync("/scim/v2/ServiceProviderConfig", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task ServiceProviderConfig_DeleteMethod_ReturnsMethodNotAllowed()
    {
        // Act
        var response = await _client.DeleteAsync("/scim/v2/ServiceProviderConfig");

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task ServiceProviderConfig_ResponseHeaders_AreCorrect()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/ServiceProviderConfig");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task ServiceProviderConfig_Performance_Under500Milliseconds()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/scim/v2/ServiceProviderConfig");
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }

    [TestMethod]
    public async Task ServiceProviderConfig_ConcurrentRequests_HandlesCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Make 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync("/scim/v2/ServiceProviderConfig"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should complete successfully
        Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.OK));
    }

    [TestMethod]
    public async Task ServiceProviderConfig_ResponseContent_IsValidJson()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/ServiceProviderConfig");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(string.IsNullOrEmpty(responseContent));
        
        // Verify it's valid JSON
        var config = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.IsTrue(config.ValueKind == JsonValueKind.Object);
    }

    [TestMethod]
    public async Task ServiceProviderConfig_WithInvalidPath_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/ServiceProviderConfig/invalid");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task ServiceProviderConfig_RFC7644Compliance_ReturnsCorrectSchema()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/ServiceProviderConfig");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var config = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        // Verify RFC 7644 compliance
        Assert.IsTrue(config.TryGetProperty("schemas", out var schemas));
        Assert.IsTrue(schemas.ValueKind == JsonValueKind.Array);
        
        var schemasArray = schemas.EnumerateArray().ToArray();
        Assert.IsTrue(schemasArray.Any(s => s.GetString() == "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig"));
    }

    [TestMethod]
    public async Task ServiceProviderConfig_AuthenticationSchemes_AreValid()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/ServiceProviderConfig");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var config = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.IsTrue(config.TryGetProperty("authenticationSchemes", out var authSchemes));
        Assert.IsTrue(authSchemes.ValueKind == JsonValueKind.Array);
        
        var authSchemesArray = authSchemes.EnumerateArray().ToArray();
        Assert.AreEqual(1, authSchemesArray.Length);
        
        var oauth2Scheme = authSchemesArray[0];
        Assert.IsTrue(oauth2Scheme.TryGetProperty("type", out var type));
        Assert.IsTrue(oauth2Scheme.TryGetProperty("primary", out var primary));
        
        Assert.AreEqual("oauth2", type.GetString());
        Assert.IsTrue(primary.GetBoolean());
    }
}
