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

  /// <summary>
  /// RFC 8693 §2.1 <c>resource</c>: the tenant the issued token is intended for. Optional; when present it becomes the
  /// <c>tenant</c> claim, so the token cannot be replayed against another tenant.
  /// </summary>
  [JsonProperty("resource")] public string? Resource { get; set; }
}