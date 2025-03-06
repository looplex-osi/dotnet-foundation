using System.Text;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

using NSubstitute;

namespace Looplex.Foundation.UnitTests.Entities;

[TestClass]
public class ClientCredentialsAuthenticationsTests
{
  private IClientCredentials _mockClientCredentials = null!;
  private IConfiguration _mockConfiguration = null!;
  private IJwtService _mockJwtService = null!;

  [TestInitialize]
  public void Setup()
  {
    _mockConfiguration = Substitute.For<IConfiguration>();
    _mockClientCredentials = Substitute.For<IClientCredentials>();
    _mockJwtService = Substitute.For<IJwtService>();

    _mockConfiguration["TokenExpirationTimeInMinutes"] = "20";
  }

  [TestMethod]
  public async Task CreateAccessToken_InvalidAuthorization_ThrowsUnauthorized()
  {
    // Arrange
    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = Constants.ClientCredentialsGrantType });

    ClientCredentialsAuthentications service = new(_mockConfiguration,
      _mockClientCredentials, _mockJwtService);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, "", CancellationToken.None));

    Assert.AreEqual("Invalid authorization.", exception.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_InvalidGrantType_ThrowsUnauthorized()
  {
    // Arrange
    string authorization = "Basic xxxxxx";

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "invalid" });

    ClientCredentialsAuthentications service = new(_mockConfiguration,
      _mockClientCredentials, _mockJwtService);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, authorization, CancellationToken.None));

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

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = Constants.ClientCredentialsGrantType });

    ClientCredential clientCredential = new()
    {
      ClientId = clientId,
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(-1),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(1)
    };

    _mockClientCredentials.RetrieveAsync(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(JsonConvert.SerializeObject(clientCredential));

    ClientCredentialsAuthentications service = new(_mockConfiguration,
      _mockClientCredentials, _mockJwtService);

    // Act
    string result = await service.CreateAccessToken(clientCredentials, authorization, CancellationToken.None);

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

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = Constants.ClientCredentialsGrantType });

    _mockClientCredentials.RetrieveAsync(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(string.Empty));

    ClientCredentialsAuthentications service = new(_mockConfiguration,
      _mockClientCredentials, _mockJwtService);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, authorization, CancellationToken.None));

    Assert.AreEqual("Invalid clientId or clientSecret.", exception.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_ClientNotBeforeError_ThrowsException()
  {
    // Arrange
    Guid clientId = Guid.NewGuid();
    string clientSecret = "clientSecret";

    string authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:clientSecret"));

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = Constants.ClientCredentialsGrantType });

    ClientCredential clientCredential = new()
    {
      ClientId = clientId,
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(10),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(20)
    };

    _mockClientCredentials.RetrieveAsync(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(JsonConvert.SerializeObject(clientCredential));

    ClientCredentialsAuthentications service = new(_mockConfiguration,
      _mockClientCredentials, _mockJwtService);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, authorization, CancellationToken.None));

    Assert.AreEqual("Client access not allowed.", exception.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_ClientExpired_ThrowsException()
  {
    // Arrange
    Guid clientId = Guid.NewGuid();
    string clientSecret = "clientSecret";

    string authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:clientSecret"));

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = Constants.ClientCredentialsGrantType });

    ClientCredential clientCredential = new()
    {
      ClientId = clientId,
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(-10),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(-5)
    };

    _mockClientCredentials.RetrieveAsync(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(JsonConvert.SerializeObject(clientCredential));

    ClientCredentialsAuthentications service = new(_mockConfiguration,
      _mockClientCredentials, _mockJwtService);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, authorization, CancellationToken.None));

    Assert.AreEqual("Client access is expired.", exception.Message);
  }
}