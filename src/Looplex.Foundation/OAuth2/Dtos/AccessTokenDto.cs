using Looplex.Foundation.Entities;

using System.Text.Json.Serialization;

namespace Looplex.Foundation.OAuth2.Dtos;

public class AccessTokenDto : Actor
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass

  #endregion

  [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
}