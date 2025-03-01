using Looplex.Foundation.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Looplex.Foundation.WebApp.OAuth2;

public static class TokenRoutes
{
    private const string Resource = "/token";

    internal static readonly RequestDelegate TokenMiddleware = async (context) =>
    {
        var factory = context.RequestServices.GetRequiredService<IAuthorizationsFactory>();
        var cancellationToken = context.RequestAborted;
            
        var authorization = context.Request.Headers.Authorization.ToString();
            
        var form = await context.Request.ReadFormAsync();
        var formDict = form.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        var credentials = JsonConvert.SerializeObject(formDict);

        var grantType = form[Constants.GrantType].ToString().ToGrantType();
        var service = factory.GetService(grantType);
        
        var result = await service.CreateAccessToken(credentials, cancellationToken);

        await context.Response.WriteAsJsonAsync(result, cancellationToken);
    };

    public static void UseTokenRoute(this IEndpointRouteBuilder app, string[] services)
    {
        app.MapPost(
            Resource,
            TokenMiddleware);
    }
        
    private static GrantType ToGrantType(this string grantType)
    {
        return grantType switch
        {
            Constants.TokenExchangeGrantType => GrantType.TokenExchange,
            Constants.ClientCredentialsGrantType => GrantType.ClientCredentials,
            _ => throw new ArgumentException("Invalid value", nameof(grantType))
        };
    }
}