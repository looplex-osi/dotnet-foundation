using System.Net;
using System.Security.Claims;

using Looplex.Foundation.Entities;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.Serialization;
using Looplex.Foundation.WebApp.OAuth2.Dtos;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Looplex.Foundation.WebApp.OAuth2.Entities;

public class ClientCredentialsAuthentications : Service, IAuthentications
{
  private readonly IClientCredentials? _clientCredentials;
  private readonly IConfiguration? _configuration;
  private readonly IHttpContextAccessor? _httpContextAccessor;
  private readonly IJwtService? _jwtService;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public ClientCredentialsAuthentications()
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public ClientCredentialsAuthentications(
    IConfiguration configuration,
    IClientCredentials clientCredentials,
    IJwtService jwtService,
    IHttpContextAccessor httpContextAccessor)
  {
    _configuration = configuration;
    _clientCredentials = clientCredentials;
    _jwtService = jwtService;
    _httpContextAccessor = httpContextAccessor;
  }

  public async Task<string> CreateAccessToken(string json, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    StringValues? authorization = _httpContextAccessor?.HttpContext?.Request.Headers.Authorization;
    ClientCredentialsGrantDto clientCredentialsDto = json.JsonDeserialize<ClientCredentialsGrantDto>();
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    ArgumentNullException.ThrowIfNull(clientCredentialsDto, "body");

    ValidateAuthorizationHeader(authorization);
    ValidateGrantType(clientCredentialsDto.GrantType);
    (Guid clientId, string clientSecret) = TryGetClientCredentials(authorization);
    ClientCredential clientCredential =
      await GetClientCredentialByIdAndSecretOrDefaultAsync(clientId, clientSecret, ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    ctx.Roles["ClientCredential"] = clientCredential;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      string accessToken = CreateAccessToken((ClientCredential)ctx.Roles["ClientCredential"]);
      ctx.Result = new AccessTokenDto { AccessToken = accessToken }.JsonSerialize();
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (string)ctx.Result;
  }

  private static void ValidateAuthorizationHeader(string? authorization)
  {
    if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Basic "))
    {
      throw new HttpRequestException("Invalid authorization.", null, HttpStatusCode.Unauthorized);
    }
  }

  private static (Guid, string) TryGetClientCredentials(string? authorization)
  {
    if (authorization != default
        && IsBasicAuthentication(authorization, out string? base64Credentials)
        && base64Credentials != default)
    {
      return DecodeCredentials(base64Credentials);
    }

    throw new HttpRequestException("Invalid authorization.", null, HttpStatusCode.Unauthorized);
  }

  private static bool IsBasicAuthentication(string value, out string? token)
  {
    token = null;
    bool result = false;

    if (value.StartsWith(Constants.Basic, StringComparison.OrdinalIgnoreCase))
    {
      token = value[Constants.Basic.Length..];
      result = true;
    }

    return result;
  }

  private static (Guid, string) DecodeCredentials(string credentials)
  {
    string[] parts = StringUtils.Base64Decode(credentials).Split(':');

    if (parts.Length != 2)
    {
      throw new HttpRequestException("Invalid credentials format.", null, HttpStatusCode.Unauthorized);
    }

    return (Guid.Parse(parts[0]), parts[1]);
  }

  private static void ValidateGrantType(string grantType)
  {
    if (!grantType
          .Equals(Constants.ClientCredentialsGrantType, StringComparison.InvariantCultureIgnoreCase))
    {
      throw new HttpRequestException($"{Constants.GrantType} is invalid.", null, HttpStatusCode.Unauthorized);
    }
  }

  private async Task<ClientCredential> GetClientCredentialByIdAndSecretOrDefaultAsync(Guid clientId,
    string clientSecret,
    IContext parentContext, CancellationToken cancellationToken)
  {
    string json = await _clientCredentials!.RetrieveAsync(clientId, clientSecret, cancellationToken);
    if (string.IsNullOrWhiteSpace(json))
    {
      throw new Exception("Invalid clientId or clientSecret.");
    }

    ClientCredential? clientCredential = json.JsonDeserialize<ClientCredential>();

    if (clientCredential == default)
    {
      throw new Exception("Invalid clientId or clientSecret.");
    }

    if (clientCredential.NotBefore > DateTimeOffset.UtcNow)
    {
      throw new Exception("Client access not allowed.");
    }

    if (clientCredential.ExpirationTime <= DateTimeOffset.UtcNow)
    {
      throw new Exception("Client access is expired.");
    }

    return clientCredential;
  }

  private string CreateAccessToken(ClientCredential clientCredential)
  {
    ClaimsIdentity claims = new ClaimsIdentity([
      new Claim(Constants.ClientId, clientCredential.ClientId.ToString())
    ]);

    string audience = _configuration!["Audience"]!;
    string issuer = _configuration["Issuer"]!;
    int tokenExpirationTimeInMinutes = _configuration.GetValue<int>("TokenExpirationTimeInMinutes");

    string privateKey = StringUtils.Base64Decode(_configuration["PrivateKey"]!);

    string accessToken = _jwtService!.GenerateToken(privateKey, issuer, audience, claims,
      TimeSpan.FromMinutes(tokenExpirationTimeInMinutes));
    return accessToken;
  }
}