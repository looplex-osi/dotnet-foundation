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
using Looplex.OAuth2.Entities;
using Looplex.Foundation.Ports;

namespace Looplex.OAuth2.IntegrationTests.Authentication;

[TestClass]
public class AuthenticationFlowTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private IJwtService _jwtService = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Use real ClientServices
                    services.AddScoped<ClientServices>();

                    // Mock JWT Service
                    _jwtService = Substitute.For<IJwtService>();
                    _jwtService.GenerateToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Security.Claims.ClaimsIdentity>(), Arg.Any<TimeSpan>())
                        .Returns("mock_jwt_token_12345");
                    services.AddSingleton(_jwtService);
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
    public async Task Authentication_ValidToken_ReturnsUserInfo()
    {
        // Arrange
        var validToken = "valid_jwt_token_12345";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

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
        Assert.IsTrue(userInfo.TryGetProperty("preferred_username", out var preferredUsername));

        Assert.AreEqual("test_user_id", sub.GetString());
        Assert.AreEqual("Test User", name.GetString());
        Assert.AreEqual("test@example.com", email.GetString());
        Assert.AreEqual("testuser", preferredUsername.GetString());
    }

    [TestMethod]
    public async Task Authentication_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var invalidToken = "invalid_jwt_token";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", invalidToken);

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Authentication_MissingToken_ReturnsUnauthorized()
    {
        // Act - No Authorization header
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Authentication_ExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var expiredToken = "expired_jwt_token";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Authentication_MalformedToken_ReturnsUnauthorized()
    {
        // Arrange
        var malformedToken = "not.a.valid.jwt";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", malformedToken);

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Authentication_EmptyToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "");

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Authentication_InvalidScheme_ReturnsUnauthorized()
    {
        // Arrange
        var token = "valid_token_but_wrong_scheme";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Authentication_UserInfoResponse_ContainsRequiredFields()
    {
        // Arrange
        var validToken = "valid_jwt_token_12345";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Check required OAuth2 UserInfo fields
        Assert.IsTrue(userInfo.TryGetProperty("sub", out _), "Missing 'sub' field");
        Assert.IsTrue(userInfo.TryGetProperty("name", out _), "Missing 'name' field");
        Assert.IsTrue(userInfo.TryGetProperty("email", out _), "Missing 'email' field");
        Assert.IsTrue(userInfo.TryGetProperty("preferred_username", out _), "Missing 'preferred_username' field");
    }

    [TestMethod]
    public async Task Authentication_UserInfoResponse_ContainsValidDataTypes()
    {
        // Arrange
        var validToken = "valid_jwt_token_12345";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Validate data types
        Assert.AreEqual(JsonValueKind.String, userInfo.GetProperty("sub").ValueKind);
        Assert.AreEqual(JsonValueKind.String, userInfo.GetProperty("name").ValueKind);
        Assert.AreEqual(JsonValueKind.String, userInfo.GetProperty("email").ValueKind);
        Assert.AreEqual(JsonValueKind.String, userInfo.GetProperty("preferred_username").ValueKind);
    }

    [TestMethod]
    public async Task Authentication_Performance_Under500ms()
    {
        // Arrange
        var validToken = "valid_jwt_token_12345";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }

    [TestMethod]
    public async Task Authentication_ConcurrentRequests_HandlesCorrectly()
    {
        // Arrange
        var validToken = "valid_jwt_token_12345";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

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
    public async Task Authentication_ResponseHeaders_AreCorrect()
    {
        // Arrange
        var validToken = "valid_jwt_token_12345";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.IsTrue(response.Headers.Contains("Cache-Control"));
        var cacheControl = response.Headers.GetValues("Cache-Control").First();
        Assert.IsTrue(cacheControl.Contains("no-cache") && cacheControl.Contains("no-store") && cacheControl.Contains("must-revalidate"));
    }

    [TestMethod]
    public async Task Authentication_InvalidHttpMethod_ReturnsMethodNotAllowed()
    {
        // Arrange
        var validToken = "valid_jwt_token_12345";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

        // Act - POST instead of GET
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/oauth/userinfo", content);

        // Assert
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Authentication_UserInfoResponse_IsValidJson()
    {
        // Arrange
        var validToken = "valid_jwt_token_12345";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var response = await _client.GetAsync("/oauth/userinfo");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        
        // Should be valid JSON
        try
        {
            JsonSerializer.Deserialize<JsonElement>(responseContent);
        }
        catch (JsonException)
        {
            Assert.Fail("Response is not valid JSON");
        }
        
        // Should not be empty
        Assert.IsFalse(string.IsNullOrEmpty(responseContent));
    }
}
