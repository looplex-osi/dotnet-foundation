using Looplex.Foundation.Entities;

using Newtonsoft.Json;

namespace Looplex.Foundation.OAuth2.Dtos;

/// <summary>
/// Represents the input payload for the OAuth 2.0 Client Credentials Grant request.
/// Used to deserialize parameters such as grant_type and optional scope from the request body.
/// </summary>
public class ClientCredentialsGrantDto : Actor
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass

  #endregion

  /// <summary>
  /// The OAuth 2.0 grant type (e.g., "client_credentials").
  /// </summary>
  [JsonProperty("grant_type")] public string? GrantType { get; set; }

  /// <summary>
  /// Optional scope parameter to request specific permissions (used in client_credentials).
  /// </summary>
  [JsonProperty("scope")]  public string? Scope { get; set; }

  /// <summary>
  /// Used in Token Exchange (RFC 8693): the token representing the subject.
  /// </summary>
  [JsonProperty("subject_token")]  public string? SubjectToken { get; set; }

  /// <summary>
  /// Used in Token Exchange (RFC 8693): the type of the subject token.
  /// </summary>
  [JsonProperty("subject_token_type")] public string? SubjectTokenType { get; set; }
}