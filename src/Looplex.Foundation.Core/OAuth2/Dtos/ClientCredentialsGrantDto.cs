using Looplex.Foundation.Core.Entities;

using System.Text.Json.Serialization;

namespace Looplex.Foundation.Core.OAuth2.Dtos;

public class ClientCredentialsGrantDto : Actor
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass

  #endregion

  [JsonPropertyName("grant_type")] public string? GrantType { get; set; }

  [JsonPropertyName("subject_token")] public string? SubjectToken { get; set; }

  [JsonPropertyName("subject_token_type")] public string? SubjectTokenType { get; set; }
}
