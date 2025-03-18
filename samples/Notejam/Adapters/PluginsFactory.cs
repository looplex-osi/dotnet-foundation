using Looplex.Foundation.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.OpenForExtension.Abstractions.Plugins;

namespace Looplex.Samples.Adapters;

public class PluginsFactory : IPluginsFactory
{
  public IList<IPlugin> GetForService<T>() where T : Service
  {
    return GetForService(typeof(T));
  }
  
  public IList<IPlugin> GetForService(Type type)
  {
    if (!typeof(Service).IsAssignableFrom(type)) // Must inherit from Service
      throw new Exception($"Type {type.Name} must inherit from {nameof(Service)}.");

    switch (type)
    {
      case var t when t == typeof(Users):
        return new List<IPlugin>();
      case var t when t == typeof(Groups):
        return new List<IPlugin>();
      case var t when t == typeof(Notejam):
        return new List<IPlugin>();
      // TODO add plugin loading configuration for scim resources services
      default:
        return new List<IPlugin>();
    }
  }
}