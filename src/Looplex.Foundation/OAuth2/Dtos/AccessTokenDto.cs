using Looplex.Foundation.Entities;

using Newtonsoft.Json;

namespace Looplex.Foundation.OAuth2.Dtos;

public class AccessTokenDto : Actor
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass

  #endregion

  [JsonProperty("access_token")] public string? AccessToken { get; set; }
}