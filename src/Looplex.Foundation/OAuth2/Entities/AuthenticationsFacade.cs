using System;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.OAuth2.Dtos;
using Looplex.Foundation.OAuth2.Entities;
using Newtonsoft.Json.Linq;
using Looplex.Foundation.Ports;

namespace Looplex.Foundation.OAuth2.Entities
{
    public class AuthenticationsFacade : IAuthentications
    {
        private readonly AuthenticationsFactory _factory;

        public AuthenticationsFacade(AuthenticationsFactory factory)
        {
            _factory = factory;
        }

        public async Task<string> CreateAccessToken(string json, string authentication, CancellationToken cancellationToken)
        {
            var jObject = JObject.Parse(json);
            var grantTypeValue = jObject["grant_type"]?.ToString();

            if (string.IsNullOrWhiteSpace(grantTypeValue))
                throw new ArgumentException("grant_type is required");

            GrantType grantType = grantTypeValue switch
            {
                "client_credentials" => GrantType.ClientCredentials,
                "urn:ietf:params:oauth:grant-type:token-exchange" => GrantType.TokenExchange,
                _ => throw new NotSupportedException($"Unsupported grant_type: {grantTypeValue}")
            };

            var service = _factory.GetService(grantType);
            return await service.CreateAccessToken(json, authentication, cancellationToken);
        }
    }
}
