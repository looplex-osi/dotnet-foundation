using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.WebApp.Adapters;

using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.WebApp;

public static class ExtensionMethods
{
  public static IServiceCollection AddWebApp(this IServiceCollection services)
  {
    services.AddHttpContextAccessor();
    services.AddSingleton<IJwtService, JwtService>();
    services.AddSingleton<Users>();
    return services;
  }
}