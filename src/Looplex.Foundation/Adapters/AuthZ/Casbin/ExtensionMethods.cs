using Casbin;

using Looplex.Foundation.Ports;

using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.Adapters.AuthZ.Casbin;

public static class ExtensionMethods
{
  public static void AddAuthZ(this IServiceCollection services, IEnforcer enforcer)
  {
    services.AddSingleton<IRbacService, RbacService>();
    services.AddSingleton(enforcer);
  }
}