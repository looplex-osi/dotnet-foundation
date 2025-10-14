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
public class SCIMv2UsersEndpointTests
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
    public async Task UsersEndpoint_ValidRequest_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/Users");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);

        var responseContent = await response.Content.ReadAsStringAsync();
        var scimResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.IsTrue(scimResponse.TryGetProperty("schemas", out var schemas));
        Assert.IsTrue(scimResponse.TryGetProperty("totalResults", out var totalResults));
        Assert.IsTrue(scimResponse.TryGetProperty("itemsPerPage", out var itemsPerPage));
        Assert.IsTrue(scimResponse.TryGetProperty("startIndex", out var startIndex));
        Assert.IsTrue(scimResponse.TryGetProperty("resources", out var resources));

        Assert.IsTrue(schemas.ValueKind == JsonValueKind.Array);
        Assert.AreEqual(0, totalResults.GetInt32());
        Assert.AreEqual(0, itemsPerPage.GetInt32());
        Assert.AreEqual(1, startIndex.GetInt32());
        Assert.IsTrue(resources.ValueKind == JsonValueKind.Array);
    }

    [TestMethod]
    public async Task UsersEndpoint_WithQueryParameters_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/Users?startIndex=1&count=10");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task UsersEndpoint_WithFilterParameter_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/Users?filter=userName eq \"test\"");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task UsersEndpoint_WithSortParameter_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/Users?sortBy=userName&sortOrder=ascending");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task UsersEndpoint_PostMethod_ReturnsMethodNotAllowed()
    {
        // Arrange
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/scim+json");

        // Act
        var response = await _client.PostAsync("/scim/v2/Users", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task UsersEndpoint_PutMethod_ReturnsMethodNotAllowed()
    {
        // Arrange
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/scim+json");

        // Act
        var response = await _client.PutAsync("/scim/v2/Users", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task UsersEndpoint_DeleteMethod_ReturnsMethodNotAllowed()
    {
        // Act
        var response = await _client.DeleteAsync("/scim/v2/Users");

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task UsersEndpoint_ResponseHeaders_AreCorrect()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/Users");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task UsersEndpoint_Performance_Under500Milliseconds()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/scim/v2/Users");
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }

    [TestMethod]
    public async Task UsersEndpoint_ConcurrentRequests_HandlesCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Make 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync("/scim/v2/Users"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should complete successfully
        Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.OK));
    }

    [TestMethod]
    public async Task UsersEndpoint_ResponseContent_IsValidJson()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/Users");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(string.IsNullOrEmpty(responseContent));
        
        // Verify it's valid JSON
        var scimResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.IsTrue(scimResponse.ValueKind == JsonValueKind.Object);
    }

    [TestMethod]
    public async Task UsersEndpoint_WithInvalidPath_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/Users/invalid");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task UsersEndpoint_SCIMv2Compliance_ReturnsCorrectSchema()
    {
        // Act
        var response = await _client.GetAsync("/scim/v2/Users");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var scimResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        // Verify SCIMv2 compliance
        Assert.IsTrue(scimResponse.TryGetProperty("schemas", out var schemas));
        Assert.IsTrue(schemas.ValueKind == JsonValueKind.Array);
        
        var schemasArray = schemas.EnumerateArray().ToArray();
        Assert.IsTrue(schemasArray.Any(s => s.GetString() == "urn:ietf:params:scim:api:messages:2.0:ListResponse"));
    }
}
