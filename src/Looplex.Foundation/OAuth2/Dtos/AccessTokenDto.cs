using Looplex.Foundation.Entities;

using Newtonsoft.Json;

namespace Looplex.Foundation.OAuth2.Dtos;

/// <summary>
/// Represents the access token response returned by the token endpoint
/// as part of the Client Credentials Grant flow (RFC 6749 §4.4).
/// The response format follows the recommendations of RFC 6749 §5.1.
/// </summary>
public class AccessTokenDto : Actor
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass

  #endregion

  /// <summary>
  /// The access token issued by the authorization server.
  /// </summary>
  [JsonProperty("access_token")] public string? AccessToken { get; set; }


  /// <summary>
  /// The type of the token issued. Typically "Bearer".
  /// </summary>
  [JsonProperty("token_type")] public string TokenType { get; set; } = "Bearer";

  /// <summary>
  /// The lifetime in seconds of the access token.
  /// </summary>
  [JsonProperty("expires_in")] public int ExpiresIn { get; set; }

  /// <summary>
  /// A space-separated list of scopes associated with the access token.
  /// </summary>
  [JsonProperty("scope")] public string? Scope { get; set; }

}