using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Looplex.Foundation.OAuth2;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

namespace Looplex.Foundation.WebApp.Middlewares;

public static class OAuth2
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

    GrantType grantType = form["grant_type"].ToString().ToGrantType();
    IAuthentications service = factory.GetService(grantType);

    string result = await service.CreateAccessToken(credentials, authorization, cancellationToken);

    await context.Response.WriteAsJsonAsync(result, cancellationToken);
  };

  public static IEndpointRouteBuilder UseTokenRoute(this IEndpointRouteBuilder app)
  {
    app.MapPost(
      Resource,
      TokenMiddleware);
    return app;
  }

  private static GrantType ToGrantType(this string grantType)
  {
    return grantType switch
    {
      "urn:ietf:params:oauth:grant-type:token-exchange" => GrantType.TokenExchange,
      "client_credentials" => GrantType.ClientCredentials,
      _ => throw new ArgumentException("Invalid value", nameof(grantType))
    };
  }
  
  public class Middleware(RequestDelegate next)
  {
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly HashSet<string> _publicEndpoints = new() { "/token", "/health" };

    public async Task Invoke(HttpContext context, IConfiguration configuration, IJwtService jwtService)
    {
      string requestPath = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

      // Skip authentication for public endpoints
      if (_publicEndpoints.Contains(requestPath))
      {
        await _next(context);
        return;
      }

      string audience = configuration["Audience"] ??
                        throw new InvalidOperationException("Audience configuration is missing");
      string issuer = configuration["Issuer"] ??
                      throw new InvalidOperationException("Issuer configuration is missing");

      string accessToken = string.Empty;

      string? authorization = context.Request.Headers["Authorization"];

      if (authorization != null && authorization.StartsWith("Bearer ", StringComparison.Ordinal))
      {
        accessToken = authorization.Substring("Bearer ".Length).Trim();
      }

      string publicKey = StringUtils.Base64Decode(configuration["PublicKey"] ??
                                                  throw new InvalidOperationException(
                                                    "PublicKey configuration is missing"));
      bool authenticated = jwtService.ValidateToken(publicKey, issuer, audience, accessToken);

      if (!authenticated)
      {
        throw new Exception("AccessToken is invalid.");
      }

      JwtSecurityTokenHandler handler = new();
      JwtSecurityToken? token = handler.ReadJwtToken(accessToken);

      string? tenant = context.Request.Headers["X-looplex-tenant"];
      if (string.IsNullOrWhiteSpace(tenant))
      {
        throw new InvalidOperationException(
          "X-looplex-tenant header is missing");
      }

      List<Claim> claims = token.Claims.ToList();

      if (claims.Any(c => c.Type == "name" || c.Type == "email"))
      {
        string? name = claims.FirstOrDefault(c => c.Type == "name")?.Value;
        string? email = claims.FirstOrDefault(c => c.Type == "email")?.Value;

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(tenant))
          throw new InvalidOperationException(
            "User claims are missing");
        
        context.Items["UserContext"] = new UserContext { Name = name!, Email = email!, Tenant = tenant! };
      }

      await _next(context);
    }
  }
}