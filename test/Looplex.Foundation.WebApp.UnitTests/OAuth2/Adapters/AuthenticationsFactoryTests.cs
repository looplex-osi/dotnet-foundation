using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.OAuth2.Adapters;
using Looplex.Foundation.WebApp.OAuth2.Entities;

using NSubstitute;

namespace Looplex.Foundation.WebApp.UnitTests.OAuth2.Adapters;

[TestClass]
public class AuthenticationsFactoryTests
{
  private ClientCredentialsAuthentications _clientCredentialsAuth = null!;
  private AuthenticationsFactory _factory = null!;
  private IServiceProvider _serviceProvider = null!;

  private TokenExchangeAuthentications _tokenExchangeAuth = null!;

  [TestInitialize]
  public void SetUp()
  {
    // Mock IServiceProvider
    _serviceProvider = Substitute.For<IServiceProvider>();

    // Mock authentications implementations
    _tokenExchangeAuth = Substitute.For<TokenExchangeAuthentications>();
    _clientCredentialsAuth = Substitute.For<ClientCredentialsAuthentications>();

    // Setup service provider to return correct instances
    _serviceProvider.GetService(typeof(TokenExchangeAuthentications)).Returns(_tokenExchangeAuth);
    _serviceProvider.GetService(typeof(ClientCredentialsAuthentications)).Returns(_clientCredentialsAuth);

    // Initialize the factory with the mocked service provider
    _factory = new AuthenticationsFactory(_serviceProvider);
  }

  [TestMethod]
  public void GetService_TokenExchange_ReturnsTokenExchangeAuthentications()
  {
    // Act
    IAuthentications result = _factory.GetService(GrantType.TokenExchange);

    // Assert
    Assert.IsNotNull(result);
    Assert.AreSame(_tokenExchangeAuth, result, "Expected TokenExchangeAuthentications instance.");
  }

  [TestMethod]
  public void GetService_ClientCredentials_ReturnsClientCredentialsAuthentications()
  {
    // Act
    IAuthentications result = _factory.GetService(GrantType.ClientCredentials);

    // Assert
    Assert.IsNotNull(result);
    Assert.AreSame(_clientCredentialsAuth, result, "Expected ClientCredentialsAuthentications instance.");
  }

  [TestMethod]
  public void GetService_UnsupportedGrantType_ThrowsArgumentOutOfRangeException()
  {
    // Act & Assert
    ArgumentOutOfRangeException ex =
      Assert.ThrowsException<ArgumentOutOfRangeException>(() => _factory.GetService((GrantType)999));

    Assert.IsTrue(ex.Message.Contains("999"), "Expected exception to mention the invalid GrantType value.");
  }
}