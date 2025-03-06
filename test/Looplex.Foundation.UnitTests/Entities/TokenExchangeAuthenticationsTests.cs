using System.Net;
using System.Text;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

using NSubstitute;

namespace Looplex.Foundation.UnitTests.Entities;

[TestClass]
public class TokenExchangeAuthenticationsTests
{
  private HttpClient _httpClient = null!;
  private IConfiguration _mockConfiguration = null!;
  private IJwtService _mockJwtService = null!;

  [TestInitialize]
  public void Setup()
  {
    _mockConfiguration = Substitute.For<IConfiguration>();
    _mockJwtService = Substitute.For<IJwtService>();

    SuccessHttpMessageHandlerMock handlerMock = new();
    _httpClient = new HttpClient(handlerMock);

    _mockConfiguration["TokenExpirationTimeInMinutes"] = "20";
  }

  [TestMethod]
  public async Task CreateAccessToken_InvalidGrantType_ThrowsUnauthorized()
  {
    // Arrange
    string clientCredentials = JsonConvert.SerializeObject(new
    {
      grant_type = "invalid", subject_token = "invalid", subject_token_type = "invalid"
    });

    TokenExchangeAuthentications service = new(_mockConfiguration, _mockJwtService, _httpClient);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, "", CancellationToken.None));

    Assert.AreEqual("grant_type is invalid.", exception.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_InvalidSubjectTokenType_ThrowsUnauthorized()
  {
    // Arrange
    string clientCredentials = JsonConvert.SerializeObject(new
    {
      grant_type = "urn:ietf:params:oauth:grant-type:token-exchange", subject_token = "invalid", subject_token_type = "invalid"
    });

    TokenExchangeAuthentications service = new(_mockConfiguration, _mockJwtService, _httpClient);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, "", CancellationToken.None));

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

    string clientCredentials = JsonConvert.SerializeObject(new
    {
      grant_type = "urn:ietf:params:oauth:grant-type:token-exchange",
      subject_token = "validToken",
      subject_token_type = "urn:ietf:params:oauth:token-type:access_token"
    });

    TokenExchangeAuthentications service = new(_mockConfiguration, _mockJwtService, _httpClient);

    // Act
    string result = await service.CreateAccessToken(clientCredentials, "", CancellationToken.None);

    // Assert
    Assert.IsNotNull(result);
    Assert.IsInstanceOfType(result, typeof(string));
  }

  [TestMethod]
  public async Task CreateAccessToken_TokenIsEmpty_ThrowsUnauthorized()
  {
    // Arrange
    string clientCredentials = JsonConvert.SerializeObject(new
    {
      grant_type = "urn:ietf:params:oauth:grant-type:token-exchange",
      subject_token = "",
      subject_token_type = "urn:ietf:params:oauth:token-type:access_token"
    });

    TokenExchangeAuthentications service = new(_mockConfiguration, _mockJwtService, _httpClient);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, "", CancellationToken.None));

    Assert.AreEqual("Token is invalid.", exception.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_InvalidToken_ThrowsUnauthorized()
  {
    // Arrange
    _mockConfiguration["OicdUserInfoEndpoint"].Returns("https://graph.microsoft.com/oidc/userinfo");
    string clientCredentials = JsonConvert.SerializeObject(new
    {
      grant_type = "urn:ietf:params:oauth:grant-type:token-exchange",
      subject_token = "invalid",
      subject_token_type = "urn:ietf:params:oauth:token-type:access_token"
    });

    ErrorHttpMessageHandlerMock handlerMock = new();
    HttpClient httpClient = new(handlerMock);

    TokenExchangeAuthentications service = new(_mockConfiguration, _mockJwtService, httpClient);

    // Act & Assert
    HttpRequestException exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(
      () => service.CreateAccessToken(clientCredentials, "", CancellationToken.None));

    Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
  }

  private class SuccessHttpMessageHandlerMock : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      // Ensure the Authorization header contains the Bearer token
      Assert.AreEqual("validToken", request.Headers.Authorization!.Parameter);

      UserInfo userInfo = new()
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
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      var userInfo = new { Error = "Invalid access token" };
      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
      {
        Content = new StringContent(JsonConvert.SerializeObject(userInfo), Encoding.UTF8, "application/json")
      });
    }
  }
}