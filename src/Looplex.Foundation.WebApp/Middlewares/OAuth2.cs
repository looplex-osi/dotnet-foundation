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
      return new ClientCredentialsAuthentications(plugins, configuration, clientCredentials, jwtService);
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

  public static readonly RequestDelegate TokenMiddleware = async context =>
  {
    var factory = context.RequestServices.GetRequiredService<AuthenticationsFactory>();
    CancellationToken cancellationToken = context.RequestAborted;

    string authorization = context.Request.Headers.Authorization.ToString();

    IFormCollection form = await context.Request.ReadFormAsync();
    Dictionary<string, string> formDict = form.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

    string credentials = JsonConvert.SerializeObject(formDict);

    try
    {
      GrantType grantType = form["grant_type"].ToString().ToGrantType();
      IAuthentications service = factory.GetService(grantType);

      string json = await service.CreateAccessToken(credentials, authorization, cancellationToken);

      context.Response.ContentType = "application/json; charset=utf-8";
      await context.Response.WriteAsync(json, cancellationToken);
    }
    catch (OAuth2Exception ex)
    {
      await WriteOAuth2Error(context, ex);
    }
  };

  /// <summary>RFC 6749 §5.2 error response: 401 for <c>invalid_client</c>, 400 otherwise. Only fixed texts are echoed.</summary>
  private static async Task WriteOAuth2Error(HttpContext context, OAuth2Exception ex)
  {
    bool unauthorized = ex.Error == OAuth2Exception.InvalidClient;

    context.Response.StatusCode = unauthorized ? StatusCodes.Status401Unauthorized : StatusCodes.Status400BadRequest;
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.Pragma = "no-cache";
    if (unauthorized)
      context.Response.Headers.WWWAuthenticate = "Basic";

    context.Response.ContentType = "application/json; charset=utf-8";
    await context.Response.WriteAsync(
      JsonConvert.SerializeObject(new { error = ex.Error, error_description = ex.Message }),
      context.RequestAborted);
  }

  private static GrantType ToGrantType(this string grantType)
  {
    return grantType switch
    {
      "urn:ietf:params:oauth:grant-type:token-exchange" => GrantType.TokenExchange,
      "client_credentials" => GrantType.ClientCredentials,
      _ => throw new OAuth2Exception(OAuth2Exception.UnsupportedGrantType, "grant_type is invalid.")
    };
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

    options.Events = new JwtBearerEvents { OnTokenValidated = ResolveTenantOnTokenValidated };
  };

  /// <summary>Claim carrying the tenant of the request. Issued by the token exchange or added here.</summary>
  public const string TenantClaim = TokenExchangeAuthentications.TenantClaim;

  /// <summary>Header used by first-party clients to select the tenant.</summary>
  public const string TenantHeader = "X-looplex-tenant";

  /// <summary>Route value (e.g. <c>/mcp/{tenant}</c>) for hosts that cannot send custom headers.</summary>
  public const string TenantRouteValue = "tenant";

  /// <summary>
  /// Resolves the tenant of the request, in order of trust: the <c>tenant</c> claim already in the token (the caller
  /// cannot choose it), the <c>{tenant}</c> route value, then the <c>X-looplex-tenant</c> header. The resolved value is
  /// stored as the <c>tenant</c> claim for the rest of the request; authentication fails when none is present.
  /// <para>
  /// Security note: only tokens issued by the token exchange carry the <c>tenant</c> claim and are pinned to a tenant.
  /// Client-credentials tokens carry none, so the caller of such a token selects the tenant via route or header;
  /// tenant membership must be enforced by the RBAC (<c>g = user, role, domain</c>). A claim that conflicts with the
  /// route/header value is rejected (authentication fails).
  /// </para>
  /// <para>
  /// The <c>{tenant}</c> route value is read from <c>RouteValues</c>, which is only populated with endpoint routing;
  /// without it that source is silently skipped and the header (or the claim) is used instead.
  /// </para>
  /// </summary>
  public static Task ResolveTenantOnTokenValidated(TokenValidatedContext context)
  {
    var identity = context.Principal?.Identity as ClaimsIdentity;

    string? claimTenant = identity?.FindFirst(TenantClaim)?.Value;
    string? requested = context.HttpContext.Request.RouteValues[TenantRouteValue]?.ToString()
      ?? context.HttpContext.Request.Headers[TenantHeader].FirstOrDefault();

    // A token pinned to a tenant must not be used against another one (exact match, like the RBAC domain).
    if (!string.IsNullOrWhiteSpace(claimTenant) && !string.IsNullOrWhiteSpace(requested)
        && !string.Equals(claimTenant.Trim(), requested.Trim(), StringComparison.Ordinal))
    {
      context.Fail("tenant mismatch: the requested tenant differs from the token's tenant claim");
      return Task.CompletedTask;
    }

    string? tenant = claimTenant ?? requested;

    if (string.IsNullOrWhiteSpace(tenant))
    {
      context.Fail($"tenant is required: '{TenantClaim}' token claim, {{{TenantRouteValue}}} route value or {TenantHeader} header");
      return Task.CompletedTask;
    }

    if (identity is not null && identity.FindFirst(TenantClaim) is null)
      identity.AddClaim(new Claim(TenantClaim, tenant));

    return Task.CompletedTask;
  }
}