using System.Collections.Generic;
using System.Net.Http;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation;

public static class ExtensionMethods
{
  public static IServiceCollection AddLooplexFoundationServices(this IServiceCollection services)
  {
    services.AddSingleton<AuthenticationsFactory>();
    services.AddSingleton<ClientCredentialsAuthentications>(s =>
    {
      var plugins = new List<IPlugin>();
      var configuration = s.GetRequiredService<IConfiguration>();
      var clientCredentials = s.GetRequiredService<IClientCredentials>();
      var jwtService = s.GetRequiredService<IJwtService>();
      return new ClientCredentialsAuthentications(plugins, configuration, clientCredentials, jwtService);
    });
    services.AddSingleton<TokenExchangeAuthentications>(s =>
    {
      var plugins = new List<IPlugin>();
      var configuration = s.GetRequiredService<IConfiguration>();
      var jwtService = s.GetRequiredService<IJwtService>();
      var httpClient = s.GetRequiredService<HttpClient>();
      return new TokenExchangeAuthentications(plugins, configuration, jwtService, httpClient);
    });
    services.AddSingleton<SCIMv2Factory>();
    services.AddSingleton<Users>();
    services.AddSingleton<Groups>();
    return services;
  }
}