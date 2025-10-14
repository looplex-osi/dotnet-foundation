using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Looplex.Protocols.HTTP.Ports;

namespace Looplex.Protocols.HTTP.IntegrationTests.OAuth2;

[TestClass]
public class OAuth2TokenEndpointTests
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
                    jwtService.GenerateToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Security.Claims.ClaimsIdentity>(), Arg.Any<TimeSpan>())
                        .Returns("mock_jwt_token_12345");
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
    public async Task TokenEndpoint_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            grant_type = "client_credentials",
            scope = "read write"
        };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/oauth/token", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);

        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.IsTrue(tokenResponse.TryGetProperty("access_token", out var accessToken));
        Assert.IsTrue(tokenResponse.TryGetProperty("token_type", out var tokenType));
        Assert.IsTrue(tokenResponse.TryGetProperty("expires_in", out var expiresIn));
        Assert.IsTrue(tokenResponse.TryGetProperty("scope", out var scope));

        Assert.AreEqual("sample_token", accessToken.GetString());
        Assert.AreEqual("Bearer", tokenType.GetString());
        Assert.AreEqual(3600, expiresIn.GetInt32());
        Assert.AreEqual("read write", scope.GetString());
    }

    [TestMethod]
    public async Task TokenEndpoint_InvalidContentType_ReturnsOk()
    {
        // Arrange
        var request = "grant_type=client_credentials&scope=read write";
        var content = new StringContent(request, Encoding.UTF8, "application/x-www-form-urlencoded");

        // Act
        var response = await _client.PostAsync("/oauth/token", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task TokenEndpoint_EmptyBody_ReturnsOk()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/oauth/token", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task TokenEndpoint_InvalidJson_ReturnsOk()
    {
        // Arrange
        var content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/oauth/token", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task TokenEndpoint_GetMethod_ReturnsMethodNotAllowed()
    {
        // Act
        var response = await _client.GetAsync("/oauth/token");

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task TokenEndpoint_PutMethod_ReturnsMethodNotAllowed()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/oauth/token", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task TokenEndpoint_ResponseHeaders_AreCorrect()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/oauth/token", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task TokenEndpoint_Performance_Under1Second()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsync("/oauth/token", content);
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [TestMethod]
    public async Task TokenEndpoint_ConcurrentRequests_HandlesCorrectly()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Make 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.PostAsync("/oauth/token", content));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should complete successfully
        Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.OK));
    }
}
