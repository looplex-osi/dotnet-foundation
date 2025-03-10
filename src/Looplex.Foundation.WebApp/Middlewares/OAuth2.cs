using System.Security.Claims;
using System.Security.Cryptography;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.Adapters;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json;

namespace Looplex.Foundation.WebApp.Middlewares;

public static class OAuth2
{
  public static IServiceCollection AddOAuth2(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    services.AddSingleton<IJwtService, JwtService>();
    
    services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme =
          JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme =
          JwtBearerDefaults.AuthenticationScheme;
      })
      .AddJwtBearer(options => JwtBearerMiddleware(options, configuration));
    services.AddAuthorization();
    
    return services;
  }
  
  public static WebApplication UseOAuth2(this WebApplication app, string prefix = "/token")
  {
    app.MapPost(
      prefix,
      TokenMiddleware);
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    return app;
  }
  
  public static readonly RequestDelegate TokenMiddleware = async context =>
  {
    var factory = context.RequestServices.GetRequiredService<AuthenticationsFactory>();
    CancellationToken cancellationToken = context.RequestAborted;

    string authorization = context.Request.Headers.Authorization.ToString();

    IFormCollection form = await context.Request.ReadFormAsync();
    Dictionary<string, string> formDict = form.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

    string credentials = JsonConvert.SerializeObject(formDict);

    GrantType grantType = form["grant_type"].ToString().ToGrantType();
    IAuthentications service = factory.GetService(grantType);

    string json = await service.CreateAccessToken(credentials, authorization, cancellationToken);

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(json, cancellationToken);
  };

  private static GrantType ToGrantType(this string grantType)
  {
    return grantType switch
    {
      "urn:ietf:params:oauth:grant-type:token-exchange" => GrantType.TokenExchange,
      "client_credentials" => GrantType.ClientCredentials,
      _ => throw new ArgumentException("Invalid value", nameof(grantType))
    };
  }

  public static readonly Action<JwtBearerOptions, IConfiguration> JwtBearerMiddleware = (options, configuration) =>
  {
    string issuer = configuration["Issuer"]!;
    string audience = configuration["Audience"]!;
    string publicKeyBase64 = configuration["PublicKey"]!;
    string publicKey = StringUtils.Base64Decode(publicKeyBase64);

    RSA rsa = RSA.Create();
    rsa.ImportFromPem(publicKey);

    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuerSigningKey = true,
      IssuerSigningKey =
        new RsaSecurityKey(rsa)
        {
          CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        },
      ValidateIssuer = true,
      ValidIssuer = issuer,
      ValidateAudience = true,
      ValidAudience = audience,
      ValidateLifetime = true,
      ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
      OnTokenValidated = context =>
      {
        var principal = context.Principal;
        var httpContext = context.HttpContext;

        string? tenant = httpContext.Request.Headers["X-looplex-tenant"];
        if (string.IsNullOrWhiteSpace(tenant))
        {
          context.Fail("X-looplex-tenant header is missing");
          return Task.CompletedTask;
        }

        var identity = principal!.Identity as ClaimsIdentity;
        identity!.AddClaim(new Claim("tenant", tenant));

        return Task.CompletedTask;
      },
      OnAuthenticationFailed = context =>
      {
        return Task.CompletedTask;
      },

      // Called when the user is not authenticated and a challenge is about to be sent 
      OnChallenge = context =>
      {
        // The error is set by the JWT validation
        Console.WriteLine("JWT Challenge triggered");
        Console.WriteLine($"Error: {context.Error}");
        Console.WriteLine($"Description: {context.ErrorDescription}");
                
        return Task.CompletedTask;
      }
    };
  };
}