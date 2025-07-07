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

  public object GetService(GrantType grantType)
  {
    return grantType switch
    {
      GrantType.ClientCredentials => _serviceProvider!.GetRequiredService<IClientCredentialsAuthentications>(),
      GrantType.TokenExchange => _serviceProvider!.GetRequiredService<ITokenExchangeAuthentications>(),
      _ => throw new NotSupportedException($"Grant type {grantType} is not supported.")
    };
  }
}