using System;

using Looplex.Foundation.SCIMv2.Entities;

using Newtonsoft.Json;

using PropertyChanged;

namespace Looplex.Foundation.OAuth2.Entities;

[AddINotifyPropertyChangedInterface]
public class ClientService : Resource
{
  #region Reflectivity

  // ReSharper disable once EmptyConstructor
  public ClientService() : base() { }

  #endregion

  public string? ClientName { get; set; }

  [JsonProperty] public string? ClientSecret { get; set; }
  
  [JsonIgnore] public string? Digest { get; set; }

  [JsonIgnore] public int? UserId { get; set; }

  public DateTimeOffset ExpirationTime { get; set; }

  public DateTimeOffset NotBefore { get; set; }
}