using System.Net;
using System.Text;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.OAuth2.Entities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NSubstitute;

namespace Looplex.Foundation.WebApp.UnitTests.OAuth2.Entities;

[TestClass]
public class TokenExchangeAuthenticationsTests
{
    private IConfiguration _mockConfiguration = null!;
    private IJwtService _mockJwtService = null!;
    private HttpClient _httpClient = null!;
        
    [TestInitialize]
    public void Setup()
    {
        _mockConfiguration = Substitute.For<IConfiguration>();
        _mockJwtService = Substitute.For<IJwtService>();
        
        var handlerMock = new SuccessHttpMessageHandlerMock();
        _httpClient = new HttpClient(handlerMock);
            
        var configurationSection = Substitute.For<IConfigurationSection>();
        configurationSection.Value.Returns("20");
        _mockConfiguration.GetSection("TokenExpirationTimeInMinutes").Returns(configurationSection);
    }

    [TestMethod]
    public async Task CreateAccessToken_InvalidGrantType_ThrowsUnauthorized()
    {
        // Arrange
        var clientCredentials = JsonConvert.SerializeObject(new
        {
            grant_type = "invalid",
            subject_token = "invalid",
            subject_token_type = "invalid"
        });

        var service = new TokenExchangeAuthentications(_mockConfiguration, _mockJwtService, _httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(
            () => service.CreateAccessToken(clientCredentials, CancellationToken.None));

        Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.AreEqual("grant_type is invalid.", exception.Message);
    }

    [TestMethod]
    public async Task CreateAccessToken_InvalidSubjectTokenType_ThrowsUnauthorized()
    {
        // Arrange
        var clientCredentials = JsonConvert.SerializeObject(new
        {
            grant_type = Constants.TokenExchangeGrantType,
            subject_token = "invalid",
            subject_token_type = "invalid"
        });

        var service = new TokenExchangeAuthentications(_mockConfiguration, _mockJwtService, _httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(
            () => service.CreateAccessToken(clientCredentials, CancellationToken.None));

        Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.AreEqual("subject_token_type is invalid.", exception.Message);
    }

    [TestMethod]
    public async Task CreateAccessToken_ValidToken_ReturnsAccessToken()
    {
        // Arrange
        _mockConfiguration["Audience"].Returns("audience");
        _mockConfiguration["Issuer"].Returns("issuer");
        _mockConfiguration["PublicKey"].Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes(RsaKeys.PublicKey)));
        _mockConfiguration["PrivateKey"].Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes(RsaKeys.PrivateKey)));
        _mockConfiguration["OicdUserInfoEndpoint"].Returns("https://graph.microsoft.com/oidc/userinfo");

        var clientCredentials = JsonConvert.SerializeObject(new
        {
            grant_type = Constants.TokenExchangeGrantType,
            subject_token = "validToken",
            subject_token_type = Constants.AccessTokenType
        });

        var service = new TokenExchangeAuthentications(_mockConfiguration, _mockJwtService, _httpClient);

        // Act
        var result = await service.CreateAccessToken(clientCredentials, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(string));
    }

    [TestMethod]
    public async Task CreateAccessToken_TokenIsEmpty_ThrowsUnauthorized()
    {
        // Arrange
        var clientCredentials = JsonConvert.SerializeObject(new
        {
            grant_type = Constants.TokenExchangeGrantType,
            subject_token = "",
            subject_token_type = Constants.AccessTokenType
        });

        var service = new TokenExchangeAuthentications(_mockConfiguration, _mockJwtService, _httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(
            () => service.CreateAccessToken(clientCredentials, CancellationToken.None));

        Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.AreEqual("Token is invalid.", exception.Message);
    }

    [TestMethod]
    public async Task CreateAccessToken_InvalidToken_ThrowsUnauthorized()
    {
        // Arrange
        _mockConfiguration["OicdUserInfoEndpoint"].Returns("https://graph.microsoft.com/oidc/userinfo");
        var clientCredentials = JsonConvert.SerializeObject(new
        {
            grant_type = Constants.TokenExchangeGrantType,
            subject_token = "invalid",
            subject_token_type = Constants.AccessTokenType
        });
        
        var handlerMock = new ErrorHttpMessageHandlerMock();
        var httpClient = new HttpClient(handlerMock);
        
        var service = new TokenExchangeAuthentications(_mockConfiguration, _mockJwtService, httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(
            () => service.CreateAccessToken(clientCredentials, CancellationToken.None));

        Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    private class SuccessHttpMessageHandlerMock : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Ensure the Authorization header contains the Bearer token
            Assert.AreEqual("validToken", request.Headers.Authorization!.Parameter);

            var userInfo = new UserInfo
            {
                Sub = Guid.NewGuid().ToString(),
                Email = "foo@bar",
                FamilyName = "Bar",
                GivenName = "Foo",
                Name = "Bar",
                Picture = "fb"
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(userInfo), Encoding.UTF8, "application/json")
            });
        }
    }

    private class ErrorHttpMessageHandlerMock : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var userInfo = new { Error = "Invalid access token" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(JsonConvert.SerializeObject(userInfo), Encoding.UTF8, "application/json")
            });
        }
    }
}