using Looplex.Foundation.Core.Entities;

using Newtonsoft.Json;

namespace Looplex.Foundation.Core.OAuth2.Entities;

public sealed class UserInfo : Actor
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass

  #endregion

  [JsonProperty("sub")] public string Sub { get; set; }

  [JsonProperty("name")] public string Name { get; set; }

  [JsonProperty("family_name")] public string FamilyName { get; set; }

  [JsonProperty("given_name")] public string GivenName { get; set; }

  [JsonProperty("picture")] public string Picture { get; set; }

  [JsonProperty("email")] public string Email { get; set; }
}
