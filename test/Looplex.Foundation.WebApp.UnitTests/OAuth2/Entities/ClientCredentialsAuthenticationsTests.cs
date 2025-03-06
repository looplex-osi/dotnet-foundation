using System.Net;
using System.Text;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.OAuth2.Entities;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

using NSubstitute;

namespace Looplex.Foundation.WebApp.UnitTests.OAuth2.Entities;

[TestClass]
public class ClientCredentialsAuthenticationsTests
{
  private IClientCredentials _mockClientCredentials = null!;
  private IConfiguration _mockConfiguration = null!;
  private IHttpContextAccessor _mockHttpContextAccessor = null!;
  private IJwtService _mockJwtService = null!;

  [TestInitialize]
  public void Setup()
  {
    _mockConfiguration = Substitute.For<IConfiguration>();
    _mockClientCredentials = Substitute.For<IClientCredentials>();
    _mockJwtService = Substitute.For<IJwtService>();
    _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();

    IConfigurationSection? configurationSection = Substitute.For<IConfigurationSection>();
    configurationSection.Value.Returns("20");
    _mockConfiguration.GetSection("TokenExpirationTimeInMinutes").Returns(configurationSection);
  }

  [TestMethod]
  public async Task CreateAccessToken_InvalidAuthorization_ThrowsUnauthorized()
  {
    // Arrange
    _mockHttpContextAccessor.HttpContext.Returns(new DefaultHttpContext());

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = Constants.ClientCredentialsGrantType });

    ClientCredentialsAuthentications service = new ClientCredentialsAuthentications(_mockConfiguration,
      _mockClientCredentials, _mockJwtService, _mockHttpContextAccessor);

    // Act & Assert
    HttpRequestException exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(
      () => service.CreateAccessToken(clientCredentials, CancellationToken.None));

    Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
    Assert.AreEqual("Invalid authorization.", exception.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_InvalidGrantType_ThrowsUnauthorized()
  {
    // Arrange
    string authorization = "Basic xxxxxx";
    _mockHttpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
    _mockHttpContextAccessor.HttpContext!.Request.Headers["Authorization"] = authorization;

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "invalid" });

    ClientCredentialsAuthentications service = new ClientCredentialsAuthentications(_mockConfiguration,
      _mockClientCredentials, _mockJwtService, _mockHttpContextAccessor);

    // Act & Assert
    HttpRequestException exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(
      () => service.CreateAccessToken(clientCredentials, CancellationToken.None));

    Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
    Assert.AreEqual("grant_type is invalid.", exception.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_ValidBasicAuth_ReturnsAccessToken()
  {
    // Arrange
    Guid clientId = Guid.NewGuid();
    string clientSecret = "clientSecret";

    _mockConfiguration["Audience"].Returns("audience");
    _mockConfiguration["Issuer"].Returns("issuer");
    _mockConfiguration["PublicKey"].Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes(RsaKeys.PublicKey)));
    _mockConfiguration["PrivateKey"].Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes(RsaKeys.PrivateKey)));

    string authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:clientSecret"));
    _mockHttpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
    _mockHttpContextAccessor.HttpContext!.Request.Headers["Authorization"] = authorization;

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = Constants.ClientCredentialsGrantType });

    ClientCredential clientCredential = new ClientCredential
    {
      ClientId = clientId,
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(-1),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(1)
    };

    _mockClientCredentials.RetrieveAsync(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(JsonConvert.SerializeObject(clientCredential));

    ClientCredentialsAuthentications service = new ClientCredentialsAuthentications(_mockConfiguration,
      _mockClientCredentials, _mockJwtService, _mockHttpContextAccessor);

    // Act
    string result = await service.CreateAccessToken(clientCredentials, CancellationToken.None);

    // Assert
    Assert.IsNotNull(result);
    Assert.IsInstanceOfType(result, typeof(string));
  }

  [TestMethod]
  public async Task CreateAccessToken_ApiKeyNotFound_ThrowsUnauthorized()
  {
    // Arrange
    Guid clientId = Guid.NewGuid();
    string clientSecret = "clientSecret";

    string authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:clientSecret"));
    _mockHttpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
    _mockHttpContextAccessor.HttpContext!.Request.Headers["Authorization"] = authorization;

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = Constants.ClientCredentialsGrantType });

    _mockClientCredentials.RetrieveAsync(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(string.Empty));

    ClientCredentialsAuthentications service = new ClientCredentialsAuthentications(_mockConfiguration,
      _mockClientCredentials, _mockJwtService, _mockHttpContextAccessor);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, CancellationToken.None));

    Assert.AreEqual("Invalid clientId or clientSecret.", exception.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_ClientNotBeforeError_ThrowsException()
  {
    // Arrange
    Guid clientId = Guid.NewGuid();
    string clientSecret = "clientSecret";

    string authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:clientSecret"));
    _mockHttpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
    _mockHttpContextAccessor.HttpContext!.Request.Headers["Authorization"] = authorization;

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = Constants.ClientCredentialsGrantType });

    ClientCredential clientCredential = new ClientCredential
    {
      ClientId = clientId,
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(10),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(20)
    };

    _mockClientCredentials.RetrieveAsync(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(JsonConvert.SerializeObject(clientCredential));

    ClientCredentialsAuthentications service = new ClientCredentialsAuthentications(_mockConfiguration,
      _mockClientCredentials, _mockJwtService, _mockHttpContextAccessor);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, CancellationToken.None));

    Assert.AreEqual("Client access not allowed.", exception.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_ClientExpired_ThrowsException()
  {
    // Arrange
    Guid clientId = Guid.NewGuid();
    string clientSecret = "clientSecret";

    string authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:clientSecret"));
    _mockHttpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
    _mockHttpContextAccessor.HttpContext!.Request.Headers["Authorization"] = authorization;

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = Constants.ClientCredentialsGrantType });

    ClientCredential clientCredential = new ClientCredential
    {
      ClientId = clientId,
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(-10),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(-5)
    };

    _mockClientCredentials.RetrieveAsync(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(JsonConvert.SerializeObject(clientCredential));

    ClientCredentialsAuthentications service = new ClientCredentialsAuthentications(_mockConfiguration,
      _mockClientCredentials, _mockJwtService, _mockHttpContextAccessor);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, CancellationToken.None));

    Assert.AreEqual("Client access is expired.", exception.Message);
  }
}