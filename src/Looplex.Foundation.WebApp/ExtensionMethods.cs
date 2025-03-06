using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.WebApp.Adapters;
using Looplex.Foundation.WebApp.Middlewares;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.WebApp;

public static class ExtensionMethods
{
  public static IServiceCollection AddWebAppServices(this IServiceCollection services)
  {
    services.AddHttpContextAccessor();
    services.AddSingleton<IJwtService, JwtService>();
    services.AddSingleton<Users>();
    return services;
  }

  public static IEndpointRouteBuilder UseWebAppRoutes(this IEndpointRouteBuilder app)
  {
    app.UseTokenRoute();
    return app;
  }
}