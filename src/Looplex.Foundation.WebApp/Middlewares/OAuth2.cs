using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

namespace Looplex.Foundation.WebApp.Middlewares;

public static class TokenRoutes
{
  private const string Resource = "/token";

  internal static readonly RequestDelegate TokenMiddleware = async context =>
  {
    IAuthenticationsFactory factory = context.RequestServices.GetRequiredService<IAuthenticationsFactory>();
    CancellationToken cancellationToken = context.RequestAborted;

    string authorization = context.Request.Headers.Authorization.ToString();

    IFormCollection form = await context.Request.ReadFormAsync();
    Dictionary<string, string> formDict = form.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

    string credentials = JsonConvert.SerializeObject(formDict);

    GrantType grantType = form[Constants.GrantType].ToString().ToGrantType();
    IAuthentications service = factory.GetService(grantType);

    string result = await service.CreateAccessToken(credentials, authorization, cancellationToken);

    await context.Response.WriteAsJsonAsync(result, cancellationToken);
  };

  public static void UseTokenRoute(this IEndpointRouteBuilder app)
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