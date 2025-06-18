using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Looplex.Foundation.OAuth2.Entities
{
  public class FlowRouter
  {
    private readonly IClientCredentialsAuthentications _clientCredentials;
    private readonly ITokenExchangeAuthentications _tokenExchange;

    public FlowRouter(
        IClientCredentialsAuthentications clientCredentials,
        ITokenExchangeAuthentications tokenExchange)
    {
      _clientCredentials = clientCredentials;
      _tokenExchange = tokenExchange;
    }

    public async Task<string> Route(string grantType, string json, string authentication, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(grantType))
        throw new ArgumentException("grantType cannot be null or empty", nameof(grantType));

      var normalized = grantType.Trim();

      return normalized switch
      {
        var g when g.Equals("client_credentials", StringComparison.OrdinalIgnoreCase) =>
            await _clientCredentials.CreateAccessToken(json, authentication, cancellationToken),

        var g when g.Equals("urn:ietf:params:oauth:grant-type:token-exchange", StringComparison.OrdinalIgnoreCase) =>
            await _tokenExchange.CreateAccessToken(json, authentication, cancellationToken),

        _ => throw new NotSupportedException($"Unsupported grant_type: {grantType}")
      };
    }
  }
}
