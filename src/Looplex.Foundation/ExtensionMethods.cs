using Looplex.Foundation.OAuth2;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;

using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation;

public static class ExtensionMethods
{
  public static void AddLooplexFoundationServices(this IServiceCollection services)
  {
    services.AddScoped<IUserContext, UserContextAccessor>();
    services.AddSingleton<IAuthenticationsFactory, AuthenticationsFactory>();
    services.AddSingleton<TokenExchangeAuthentications>();
    services.AddSingleton<ClientCredentialsAuthentications>();
  }
}