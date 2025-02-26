using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.OAuth2
{
    public static class ExtensionMethods
    {
        public static void UseOAuth2(this IApplicationBuilder app)
        {
            app.UseMiddleware<AuthenticationMiddleware>();
        }
        
        public static void AddOAuth2Services(this IServiceCollection services)
        {
            services.AddScoped<IUserContext, UserContextAccessor>(); 
        }
    }
}