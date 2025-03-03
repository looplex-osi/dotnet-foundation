using Looplex.Foundation.Middlewares;
using Looplex.Foundation.OAuth2;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation;

public static class ExtensionMethods
{
    public static void UseLooplexFoundation(this IApplicationBuilder app)
    {
        app.UseMiddleware<JsonResponseMiddleware>();
        app.UseMiddleware<AuthenticationMiddleware>();
    }
        
    public static void AddLooplexFoundationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserContext, UserContextAccessor>(); 
    }
}