using System;

using Looplex.Foundation.SCIMv2.Entities;

using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.OAuth2.Entities;

public class SCIMv2Factory
{
  private readonly IServiceProvider? _serviceProvider;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public SCIMv2Factory() { }

  #endregion

  public SCIMv2Factory(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }

  public SCIMv2<T> GetService<T>() where T : Resource, new()
  {
    switch (typeof(T))
    {
      case var t when t == typeof(User):
        var service = _serviceProvider!.GetRequiredService<Users>();
        return service as SCIMv2<T> ?? throw new InvalidOperationException($"Cannot cast {typeof(Users)} to {typeof(SCIMv2<T>)}");
      default:
        throw new ArgumentOutOfRangeException(nameof(T), typeof(T).Name, null);
    }
  }
}