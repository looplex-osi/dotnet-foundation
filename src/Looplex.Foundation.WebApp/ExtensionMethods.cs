using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.Adapters;
using Looplex.Foundation.WebApp.OAuth2;
using Looplex.Foundation.WebApp.OAuth2.Adapters;
using Looplex.Foundation.WebApp.OAuth2.Entities;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.WebApp;

public static class ExtensionMethods
{
  public static void UseWebApp(this IApplicationBuilder app)
  {
  }

  public static void AddWebAppServices(this IServiceCollection services)
  {
    services.AddHttpContextAccessor();
    services.AddSingleton<IJwtService, JwtService>();
    services.AddSingleton<IAuthenticationsFactory, AuthenticationsFactory>();
    services.AddSingleton<TokenExchangeAuthentications>();
    services.AddSingleton<ClientCredentialsAuthentications>();
  }

  public static void UseWebAppRoutes(this IEndpointRouteBuilder app)
  {
    app.UseTokenRoute();
  }
}