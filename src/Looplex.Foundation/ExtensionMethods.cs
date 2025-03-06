using Looplex.Foundation.OAuth2;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.SCIMv2.Entities;

using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation;

public static class ExtensionMethods
{
  public static IServiceCollection AddLooplexFoundationServices(this IServiceCollection services)
  {
    services.AddScoped<IUserContext, UserContextAccessor>();
    services.AddSingleton<AuthenticationsFactory>();
    services.AddSingleton<TokenExchangeAuthentications>();
    services.AddSingleton<ClientCredentialsAuthentications>();
    services.AddSingleton<SCIMv2Factory>();
    services.AddSingleton<Users>();
    services.AddSingleton<Groups>();
    return services;
  }
}