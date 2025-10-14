using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Looplex.Protocols.HTTP.Ports;

namespace Looplex.Protocols.HTTP.IntegrationTests.OAuth2;

[TestClass]
public class OAuth2UserInfoEndpointTests
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
    public async Task UserInfoEndpoint_ValidRequest_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);

        var responseContent = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.IsTrue(userInfo.TryGetProperty("sub", out var sub));
        Assert.IsTrue(userInfo.TryGetProperty("name", out var name));
        Assert.IsTrue(userInfo.TryGetProperty("email", out var email));

        Assert.AreEqual("user123", sub.GetString());
        Assert.AreEqual("John Doe", name.GetString());
        Assert.AreEqual("john.doe@example.com", email.GetString());
    }

    [TestMethod]
    public async Task UserInfoEndpoint_WithAuthorizationHeader_ReturnsOk()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer mock_token_12345");

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task UserInfoEndpoint_WithQueryParameters_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/oauth/userinfo?format=json");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task UserInfoEndpoint_PostMethod_ReturnsMethodNotAllowed()
    {
        // Arrange
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/oauth/userinfo", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task UserInfoEndpoint_PutMethod_ReturnsMethodNotAllowed()
    {
        // Arrange
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/oauth/userinfo", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task UserInfoEndpoint_DeleteMethod_ReturnsMethodNotAllowed()
    {
        // Act
        var response = await _client.DeleteAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task UserInfoEndpoint_ResponseHeaders_AreCorrect()
    {
        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task UserInfoEndpoint_Performance_Under500Milliseconds()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }

    [TestMethod]
    public async Task UserInfoEndpoint_ConcurrentRequests_HandlesCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Make 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync("/oauth/userinfo"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should complete successfully
        Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.OK));
    }

    [TestMethod]
    public async Task UserInfoEndpoint_ResponseContent_IsValidJson()
    {
        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(string.IsNullOrEmpty(responseContent));
        
        // Verify it's valid JSON
        var userInfo = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.IsTrue(userInfo.ValueKind == JsonValueKind.Object);
    }

    [TestMethod]
    public async Task UserInfoEndpoint_WithInvalidPath_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/oauth/userinfo/invalid");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
