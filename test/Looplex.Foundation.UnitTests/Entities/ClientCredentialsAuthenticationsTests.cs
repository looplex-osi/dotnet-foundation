using System.Security.Claims;
using System.Text;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using NSubstitute;


namespace Looplex.Foundation.UnitTests.Entities;

[TestClass]
public class ClientCredentialsAuthenticationsTests
{
  private ClientServices _mockClientServices = null!;
  private IConfiguration _mockConfiguration = null!;
  private IJwtService _mockJwtService = null!;
  private ILogger<ClientCredentialsAuthentications> _mockLogger = null!;

  [TestInitialize]
  public void Setup()
  {
    _mockConfiguration = Substitute.For<IConfiguration>();
    _mockClientServices = Substitute.For<ClientServices>();
    _mockJwtService = Substitute.For<IJwtService>();

    _mockConfiguration["TokenExpirationTimeInMinutes"] = "20";
    _mockConfiguration["Audience"].Returns("audience");
    _mockConfiguration["Issuer"].Returns("issuer");
    _mockConfiguration["PrivateKey"].Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes("test-private-key")));
    _mockLogger = Substitute.For<ILogger<ClientCredentialsAuthentications>>();
  }

  [TestMethod]
  public async Task CreateAccessToken_InvalidGrantType_ThrowsUnauthorized()
  {
    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "invalid" });

    var handler = new ClientCredentialsAuthentications(new List<IPlugin>(), _mockConfiguration, _mockClientServices, _mockJwtService, _mockLogger);

    var ex = await Assert.ThrowsExceptionAsync<Exception>(() =>
        handler.CreateAccessToken(clientCredentials, (Guid.NewGuid(), "secret"), CancellationToken.None));

    Assert.AreEqual("grant_type is invalid.", ex.Message);
  }
  /// <summary>
  /// Verifies that the ClientCredentialsAuthentications handler successfully generates a valid access token when provided 
  /// with valid client_id and client_secret credentials and a correctly formatted client_credentials grant type request.
  ///
  /// This test ensures correct behavior of the Client Credentials Grant flow as defined in RFC 6749 §4.4.
  /// </summary>
  [TestMethod]
  public async Task CreateAccessToken_ValidCredentials_ReturnsAccessToken()
  {
    Guid clientId = Guid.NewGuid();
    string clientSecret = "secret";

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "client_credentials" });

    ClientService clientService = new()
    {
      Id = clientId.ToString(),
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(-1),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(1)
    };

    _mockClientServices.Retrieve(clientId, clientSecret, Arg.Any<CancellationToken>())
        .Returns(clientService);

    _mockJwtService.GenerateToken(
        Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
        Arg.Any<ClaimsIdentity>(), Arg.Any<TimeSpan>())
        .Returns("mock.jwt.token");

    var handler = new ClientCredentialsAuthentications(new List<IPlugin>(), _mockConfiguration, _mockClientServices, _mockJwtService, _mockLogger);

    string result = await handler.CreateAccessToken(clientCredentials, (clientId, clientSecret), CancellationToken.None);

    Assert.IsNotNull(result);
    Assert.IsTrue(result.Contains("access_token"));
  }

  [TestMethod]
  public async Task CreateAccessToken_ApiKeyNotFound_ThrowsUnauthorized()
  {
    Guid clientId = Guid.NewGuid();
    string clientSecret = "wrong-secret";

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "client_credentials" });

    _mockClientServices.Retrieve(clientId, clientSecret, Arg.Any<CancellationToken>())
        .Returns((ClientService?)null);

    var handler = new ClientCredentialsAuthentications(new List<IPlugin>(), _mockConfiguration, _mockClientServices, _mockJwtService, _mockLogger);

    var ex = await Assert.ThrowsExceptionAsync<Exception>(() =>
        handler.CreateAccessToken(clientCredentials, (clientId, clientSecret), CancellationToken.None));

    Assert.AreEqual("Invalid clientId or clientSecret.", ex.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_ClientNotBeforeError_ThrowsException()
  {
    Guid clientId = Guid.NewGuid();
    string clientSecret = "secret";

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "client_credentials" });

    ClientService clientService = new()
    {
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(10),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(20)
    };

    _mockClientServices.Retrieve(clientId, clientSecret, Arg.Any<CancellationToken>())
        .Returns(clientService);

    var handler = new ClientCredentialsAuthentications(new List<IPlugin>(), _mockConfiguration, _mockClientServices, _mockJwtService, _mockLogger);

    var ex = await Assert.ThrowsExceptionAsync<Exception>(() =>
        handler.CreateAccessToken(clientCredentials, (clientId, clientSecret), CancellationToken.None));

    Assert.AreEqual("Client access not allowed.", ex.Message);
  }

  [TestMethod]
  public async Task CreateAccessToken_ClientExpired_ThrowsException()
  {
    Guid clientId = Guid.NewGuid();
    string clientSecret = "secret";

    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "client_credentials" });

    ClientService clientService = new()
    {
      NotBefore = DateTimeOffset.UtcNow.AddMinutes(-10),
      ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(-5)
    };

    _mockClientServices.Retrieve(clientId, clientSecret, Arg.Any<CancellationToken>())
        .Returns(clientService);

    var handler = new ClientCredentialsAuthentications(new List<IPlugin>(), _mockConfiguration, _mockClientServices, _mockJwtService, _mockLogger);

    var ex = await Assert.ThrowsExceptionAsync<Exception>(() =>
        handler.CreateAccessToken(clientCredentials, (clientId, clientSecret), CancellationToken.None));

    Assert.AreEqual("Client access is expired.", ex.Message);
  }
}
