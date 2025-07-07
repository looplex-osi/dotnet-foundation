using System;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Looplex.Foundation.UnitTests.Entities;

[TestClass]
public class AuthenticationsFactoryTests
{
  private IClientCredentialsAuthentications _clientCredentialsAuth = null!;
  private ITokenExchangeAuthentications _tokenExchangeAuth = null!;
  private IServiceProvider _serviceProvider = null!;
  private AuthenticationsFactory _factory = null!;

  [TestInitialize]
  public void SetUp()
  {
    _serviceProvider = Substitute.For<IServiceProvider>();

    _clientCredentialsAuth = Substitute.For<IClientCredentialsAuthentications>();
    _tokenExchangeAuth = Substitute.For<ITokenExchangeAuthentications>();

    _serviceProvider.GetService(typeof(IClientCredentialsAuthentications)).Returns(_clientCredentialsAuth);
    _serviceProvider.GetService(typeof(ITokenExchangeAuthentications)).Returns(_tokenExchangeAuth);

    _factory = new AuthenticationsFactory(_serviceProvider);
  }

  [TestMethod]
  public void GetService_TokenExchange_ReturnsTokenExchangeAuthentications()
  {
    // Act
    object result = _factory.GetService(GrantType.TokenExchange);

    // Assert
    Assert.IsNotNull(result);
    Assert.IsInstanceOfType(result, typeof(ITokenExchangeAuthentications));
    Assert.AreSame(_tokenExchangeAuth, result, "Expected TokenExchangeAuthentications instance.");
  }

  [TestMethod]
  public void GetService_ClientCredentials_ReturnsClientCredentialsAuthentications()
  {
    // Act
    object result = _factory.GetService(GrantType.ClientCredentials);

    // Assert
    Assert.IsNotNull(result);
    Assert.IsInstanceOfType(result, typeof(IClientCredentialsAuthentications));
    Assert.AreSame(_clientCredentialsAuth, result, "Expected ClientCredentialsAuthentications instance.");
  }

  [TestMethod]
  public void GetService_UnsupportedGrantType_ThrowsNotSupportedException()
  {
    // Act & Assert
    var ex = Assert.ThrowsException<NotSupportedException>(() =>
        _factory.GetService((GrantType)999));

    Assert.IsTrue(ex.Message.Contains("not supported"), "Expected exception to indicate unsupported grant type.");

  }
}
