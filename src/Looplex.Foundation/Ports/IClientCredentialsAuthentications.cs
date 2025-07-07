using System;
using System.Threading;
using System.Threading.Tasks;

namespace Looplex.Foundation.Ports
{
  /// <summary>
  /// Defines the contract for handling the OAuth 2.0 Client Credentials Grant flow,
  /// allowing a client to obtain an access token using only its credentials.
  /// </summary>
  public interface IClientCredentialsAuthentications
  {
    /// <summary>
    /// Generates an access token using the client_credentials grant type as defined in RFC 6749 §4.4.
    /// </summary>
    /// <param name="json">The input JSON containing the grant_type and optional scope.</param>
    /// <param name="credentials">Tuple containing client ID and client secret.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>A JSON string representing the access token response.</returns>
    Task<string> CreateAccessToken(string json, (Guid clientId, string clientSecret) credentials, CancellationToken cancellationToken);
  }
}
