using System.Text;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

using NSubstitute;

namespace Looplex.Foundation.UnitTests.Entities;

[TestClass]
public class ClientCredentialsAuthenticationsTests
{
  private ClientServices _mockClientServices = null!;
  private IConfiguration _mockConfiguration = null!;
  private IJwtService _mockJwtService = null!;

  [TestInitialize]
  public void Setup()
  {
    _mockConfiguration = Substitute.For<IConfiguration>();
    _mockClientServices = Substitute.For<ClientServices>();
    _mockJwtService = Substitute.For<IJwtService>();

    _mockConfiguration["TokenExpirationTimeInMinutes"] = "20";
  }

  [TestMethod]
  public async Task CreateAccessToken_InvalidAuthorization_ThrowsUnauthorized()
  {
    // Arrange
    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "client_credentials" });

    ClientCredentialsAuthentications service = new(new List<IPlugin>(), _mockConfiguration,
      _mockClientServices, _mockJwtService);

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

    ClientCredentialsAuthentications service = new(new List<IPlugin>(), _mockConfiguration,
      _mockClientServices, _mockJwtService);

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
    string clientSecret = "secret";

    _mockConfiguration["Audience"].Returns("audience");
    _mockConfiguration["Issuer"].Returns("issuer");
    _mockConfiguration["PublicKey"].Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes(RsaKeys.PublicKey)));
    _mockConfiguration["PrivateKey"].Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes(RsaKeys.PrivateKey)));

    string authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "client_credentials" });

    ClientService clientService = new()
    {
      Id = Guid.NewGuid().ToString(),
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(-1),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(1)
    };

    _mockClientServices.Retrieve(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(clientService);

    ClientCredentialsAuthentications service = new(new List<IPlugin>(), _mockConfiguration,
      _mockClientServices, _mockJwtService);

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

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "client_credentials" });

    _mockClientServices.Retrieve(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns((ClientService?)null);

    ClientCredentialsAuthentications service = new(new List<IPlugin>(), _mockConfiguration,
      _mockClientServices, _mockJwtService);

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

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "client_credentials" });

    ClientService clientService = new()
    {
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(10),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(20)
    };

    _mockClientServices.Retrieve(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(clientService);

    ClientCredentialsAuthentications service = new(new List<IPlugin>(), _mockConfiguration,
      _mockClientServices, _mockJwtService);

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

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "client_credentials" });

    ClientService clientService = new()
    {
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(-10),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(-5)
    };

    _mockClientServices.Retrieve(clientId, clientSecret, Arg.Any<CancellationToken>())
      .Returns(clientService);

    ClientCredentialsAuthentications service = new(new List<IPlugin>(), _mockConfiguration,
      _mockClientServices, _mockJwtService);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, authorization, CancellationToken.None));

    Assert.AreEqual("Client access is expired.", exception.Message);
  }
}