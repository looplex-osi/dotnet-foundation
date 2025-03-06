using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.OAuth2.Entities;

using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.WebApp.OAuth2.Adapters;

public class AuthenticationsFactory : IAuthenticationsFactory
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