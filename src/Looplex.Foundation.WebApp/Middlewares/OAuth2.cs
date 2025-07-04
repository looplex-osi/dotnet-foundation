using System.Security.Claims;
using System.Security.Cryptography;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.Adapters;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Loader;

using MediatR;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json;

namespace Looplex.Foundation.WebApp.Middlewares;

public static class OAuth2
{
  public static IServiceCollection AddOAuth2(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme =
          JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme =
          JwtBearerDefaults.AuthenticationScheme;
      })
      .AddJwtBearer(options => JwtBearerMiddleware(options, configuration));
    services.AddAuthorization();

    services.AddSingleton<IJwtService, JwtService>();
    services.AddScoped<AuthenticationsFactory>();
    services.AddScoped<ClientServices>(sp =>
    {
      PluginLoader loader = new();
      IEnumerable<string> dlls = Directory.Exists("plugins")
        ? Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"))
        : [];
      IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();
      var rbacService = sp.GetRequiredService<IRbacService>();
      var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
      var mediator = sp.GetRequiredService<IMediator>();
      return new ClientServices(plugins, rbacService, httpContextAccessor, mediator, configuration);
    });
    services.AddScoped<ClientCredentialsAuthentications>(sp =>
    {
      PluginLoader loader = new();
      IEnumerable<string> dlls = Directory.Exists("plugins")
        ? Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"))
        : [];
      IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();

      var clientCredentials = sp.GetRequiredService<ClientServices>();
      var jwtService = sp.GetRequiredService<IJwtService>();
      var logger = sp.GetRequiredService<ILogger<ClientCredentialsAuthentications>>();

      return new ClientCredentialsAuthentications(plugins, configuration, clientCredentials, jwtService, logger);
    });
    services.AddScoped<TokenExchangeAuthentications>(sp =>
    {
      PluginLoader loader = new();
      IEnumerable<string> dlls = Directory.Exists("plugins")
        ? Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"))
        : [];
      IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();
      var jwtService = sp.GetRequiredService<IJwtService>();
      var httpClient = sp.GetRequiredService<HttpClient>();
      return new TokenExchangeAuthentications(plugins, configuration, jwtService, httpClient);
    });

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

  /// <summary>
  /// Handles the OAuth2 token endpoint POST request, processing different grant types,
  /// including client_credentials. Validates input, delegates to the appropriate handler,
  /// and writes a JSON response. Standardized error responses follow RFC 6749 section 5.2.
  /// </summary>
  public static readonly RequestDelegate TokenMiddleware = async context =>
  {
    try
    {
      var factory = context.RequestServices.GetRequiredService<AuthenticationsFactory>();
      var cancellationToken = context.RequestAborted;

      var form = await context.Request.ReadFormAsync();
      var formDict = form.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

      var credentials = JsonConvert.SerializeObject(formDict);
      var grantType = form["grant_type"].ToString();
      var parsedGrantType = grantType.ToGrantType();

      var service = factory.GetService(parsedGrantType);

      string json = parsedGrantType switch
      {
        GrantType.ClientCredentials => await ((IClientCredentialsAuthentications)service)
          .CreateAccessToken(credentials, ExtractClientCredentials(context.Request, form), cancellationToken),

        GrantType.TokenExchange => await ((ITokenExchangeAuthentications)service)
          .CreateAccessToken(credentials, context.Request.Headers.Authorization.ToString(), cancellationToken),

        _ => throw new NotSupportedException($"Unsupported grant type: {grantType}")
      };

      context.Response.ContentType = "application/json; charset=utf-8";
      await context.Response.WriteAsync(json, cancellationToken);
    }
    catch (ArgumentNullException ex)
    {
      await WriteOAuthError(context, 400, "invalid_request", ex.Message);
    }
    catch (UnauthorizedAccessException ex)
    {
      await WriteOAuthError(context, 401, "invalid_client", ex.Message);
    }
    catch (NotSupportedException ex)
    {
      await WriteOAuthError(context, 400, "unsupported_grant_type", ex.Message);
    }
    catch (Exception ex)
    {
      await WriteOAuthError(context, 400, "invalid_request", ex.Message); // fallback
    }
  };
  /// <summary>
  /// Writes a JSON-formatted error response in compliance with RFC 6749 section 5.2.
  /// This method standardizes error output for OAuth 2.0 token endpoint failures.
  /// </summary>
  private static async Task WriteOAuthError(HttpContext context, int statusCode, string error, string description)
  {
    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json; charset=utf-8";

    var errorPayload = new
    {
      error,
      error_description = description
    };

    var json = JsonConvert.SerializeObject(errorPayload);
    await context.Response.WriteAsync(json);
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
  /// <summary>
  /// Extracts client credentials from either the Authorization header (Basic)
  /// or the request body, as allowed by RFC 6749 §2.3.1.
  /// </summary>
  private static (Guid, string) ExtractClientCredentials(HttpRequest request, IFormCollection form)
  {
    string? auth = request.Headers["Authorization"].ToString();

    if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
    {
      return TryGetClientCredentials(auth); // já existe em ClientCredentialsAuthentications
    }

    if (form.TryGetValue("client_id", out var clientId) &&
        form.TryGetValue("client_secret", out var clientSecret))
    {
      return (Guid.Parse(clientId), clientSecret.ToString());
    }

    throw new UnauthorizedAccessException("Missing client credentials.");
  }
  private static (Guid, string) TryGetClientCredentials(string authorization)
  {
    if (IsBasicAuthentication(authorization, out string? base64Credentials) && base64Credentials != null)
    {
      return DecodeCredentials(base64Credentials);
    }

    throw new UnauthorizedAccessException("Invalid Basic authorization format.");
  }
  private static bool IsBasicAuthentication(string value, out string? token)
  {
    token = null;
    if (value.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
    {
      token = value["Basic".Length..].Trim();
      return true;
    }
    return false;
  }

  private static (Guid, string) DecodeCredentials(string credentials)
  {
    string decoded = Strings.Base64Decode(credentials);
    string[] parts = decoded.Split(':');
    if (parts.Length != 2)
      throw new UnauthorizedAccessException("Invalid credentials format.");

    return (Guid.Parse(parts[0]), parts[1]);
  }
  public static readonly Action<JwtBearerOptions, IConfiguration> JwtBearerMiddleware = (options, configuration) =>
  {
    string issuer = configuration["Issuer"]!;
    string audience = configuration["Audience"]!;
    string publicKeyBase64 = configuration["PublicKey"]!;
    string publicKey = Strings.Base64Decode(publicKeyBase64);

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
      }
    };
  };
}