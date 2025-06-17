using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Looplex.Foundation.OAuth2.Entities
{
  public class FlowRouter
  {
    private readonly ClientCredentialsAuthentications _clientCredentials;
    private readonly TokenExchangeAuthentications _tokenExchange;

    public FlowRouter(
        ClientCredentialsAuthentications clientCredentials,
        TokenExchangeAuthentications tokenExchange)
    {
      _clientCredentials = clientCredentials;
      _tokenExchange = tokenExchange;
    }

    public async Task<string> Route(string grantType, string json, string authentication, CancellationToken cancellationToken)
    {
      return grantType.ToLowerInvariant() switch
      {
        "client_credentials" =>
            await _clientCredentials.CreateAccessToken(json, authentication, cancellationToken),

        "urn:ietf:params:oauth:grant-type:token-exchange" =>
            await _tokenExchange.CreateAccessToken(json, authentication, cancellationToken),

        _ => throw new NotSupportedException($"Unsupported grant_type: {grantType}")
      };
    }
  }
}
