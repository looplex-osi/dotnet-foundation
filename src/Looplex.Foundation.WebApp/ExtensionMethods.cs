using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.Adapters;
using Looplex.Foundation.WebApp.OAuth2.Adapters;
using Microsoft.AspNetCore.Builder;
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
        services.AddScoped<IJwtService, JwtService>(); 
        services.AddScoped<IAuthorizationsFactory, AuthorizationsFactory>(); 
    }
}