using Looplex.Foundation.Entities;

using Newtonsoft.Json;

namespace Looplex.Foundation.WebApp.OAuth2.Dtos;

public class AccessTokenDto : Actor
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass

  #endregion

  [JsonProperty("access_token")] public required string AccessToken { get; set; }
}