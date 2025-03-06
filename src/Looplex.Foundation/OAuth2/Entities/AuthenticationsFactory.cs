using System;

using Looplex.Foundation.Ports;

using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.OAuth2.Entities;

public class AuthenticationsFactory
{
  private readonly IServiceProvider? _serviceProvider;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public AuthenticationsFactory() { }

  #endregion

  public AuthenticationsFactory(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }

  public IAuthentications GetService(GrantType grantType)
  {
    switch (grantType)
    {
      case GrantType.TokenExchange:
        return _serviceProvider!.GetRequiredService<TokenExchangeAuthentications>();
      case GrantType.ClientCredentials:
        return _serviceProvider!.GetRequiredService<ClientCredentialsAuthentications>();
      default:
        throw new ArgumentOutOfRangeException(nameof(grantType), grantType, null);
    }
  }
}