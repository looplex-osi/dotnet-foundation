using System;

using Newtonsoft.Json;

using PropertyChanged;

namespace Looplex.OAuth2.Entities;

[AddINotifyPropertyChangedInterface]
public class ClientService
{
  #region Reflectivity

  // ReSharper disable once EmptyConstructor
  public ClientService() : base() { }

  #endregion

  public string? ClientName { get; set; }
  public string? UserName { get; set; }
  public string Id { get; set; } = Guid.NewGuid().ToString();

  [JsonProperty] public string? ClientSecret { get; set; }
  
  [JsonIgnore] public string? Digest { get; set; }

  [JsonIgnore] public int? UserId { get; set; }

  public DateTimeOffset ExpirationTime { get; set; }

  public DateTimeOffset NotBefore { get; set; }
}
