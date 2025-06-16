using Looplex.Foundation.Entities;

using Newtonsoft.Json;

namespace Looplex.Foundation.OAuth2.Dtos;

public class ClientCredentialsGrantDto : Actor
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass

  #endregion

  [JsonProperty("grant_type")] public string? GrantType { get; set; }

  [JsonProperty("subject_token")] public string? SubjectToken { get; set; }

  [JsonProperty("subject_token_type")] public string? SubjectTokenType { get; set; }

  [JsonProperty("client_id")] public string? ClientId { get; set; }

  [JsonProperty("client_secret")] public string? ClientSecret { get; set; }
}