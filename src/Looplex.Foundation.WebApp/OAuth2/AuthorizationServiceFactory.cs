using Looplex.Foundation.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.WebApp.OAuth2
{
    public class AuthorizationServiceFactory : IAuthorizationServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AuthorizationServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAuthorizationService GetService(GrantType grantType)
        {
            switch (grantType)
            {
                case GrantType.TokenExchange:
                    return _serviceProvider.GetRequiredService<TokenExchangeAuthorizationService>();
                case GrantType.ClientCredentials:
                    return _serviceProvider.GetRequiredService<ClientCredentialsAuthorizationService>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(grantType), grantType, null);
            }
        }
    }
}