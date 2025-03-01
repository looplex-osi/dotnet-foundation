using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.OAuth2.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.WebApp.OAuth2.Adapters;

public class AuthorizationsFactory : IAuthorizationsFactory
{
    private readonly IServiceProvider? _serviceProvider;

    #region Reflectivity
    // ReSharper disable once PublicConstructorInAbstractClass
    public AuthorizationsFactory() { }
    #endregion
    
    public AuthorizationsFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IAuthorizations GetService(GrantType grantType)
    {
        switch (grantType)
        {
            case GrantType.TokenExchange:
                return _serviceProvider!.GetRequiredService<TokenExchangeAuthorizations>();
            case GrantType.ClientCredentials:
                return _serviceProvider!.GetRequiredService<ClientCredentialsAuthorizations>();
            default:
                throw new ArgumentOutOfRangeException(nameof(grantType), grantType, null);
        }
    }
}