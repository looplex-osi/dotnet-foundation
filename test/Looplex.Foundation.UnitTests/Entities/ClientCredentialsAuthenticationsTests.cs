using System.Text;

using Looplex.OAuth2.Entities;
using Looplex.Foundation.Entities;
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
  private ILogger<Service> _mockLogger = null!;

  [TestInitialize]
  public void Setup()
  {
    _mockConfiguration = Substitute.For<IConfiguration>();
    _mockClientServices = Substitute.For<ClientServices>(null, null, null, null);
    _mockJwtService = Substitute.For<IJwtService>();
    _mockLogger = Substitute.For<ILogger<Service>>();

    _mockConfiguration["TokenExpirationTimeInMinutes"].Returns("20");
  }

  [TestMethod]
  public async Task CreateAccessToken_InvalidAuthorization_ThrowsUnauthorized()
  {
    // Arrange
    string clientCredentials = JsonConvert.SerializeObject(new { grant_type = "client_credentials" });

    ClientCredentialsAuthentications service = new(new List<IPlugin>(), _mockLogger, _mockConfiguration,
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

    ClientCredentialsAuthentications service = new(new List<IPlugin>(), _mockLogger, _mockConfiguration,
      _mockClientServices, _mockJwtService);

    // Act & Assert
    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(
      () => service.CreateAccessToken(clientCredentials, authorization, CancellationToken.None));

    Assert.AreEqual("grant_type is invalid.", exception.Message);
  }
}