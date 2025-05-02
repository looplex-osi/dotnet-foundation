using System;

using Newtonsoft.Json;

using PropertyChanged;

namespace Looplex.Foundation.SCIMv2.Entities;

[AddINotifyPropertyChangedInterface]
public class ClientCredential : Resource
{
  #region Reflectivity

  // ReSharper disable once EmptyConstructor
  public ClientCredential() : base() { }

  #endregion

  public string? ClientName { get; set; }

  public Guid? ClientId { get; set; }

  [JsonIgnore] public string? Digest { get; set; }

  [JsonIgnore] public int? UserId { get; set; }

  public DateTimeOffset ExpirationTime { get; set; }

  public DateTimeOffset NotBefore { get; set; }
}