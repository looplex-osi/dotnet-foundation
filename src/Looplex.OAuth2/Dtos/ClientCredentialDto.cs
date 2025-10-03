using System;

using Looplex.Foundation.Entities;

namespace Looplex.OAuth2.Dtos;

public class ClientCredentialDto : Actor
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass

  #endregion

  public Guid ClientId { get; set; }
  public string ClientSecret { get; set; }
}
