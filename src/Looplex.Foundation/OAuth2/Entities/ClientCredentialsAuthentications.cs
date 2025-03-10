using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Entities;
using Looplex.Foundation.OAuth2.Dtos;
using Looplex.Foundation.Ports;
using Looplex.Foundation.Serialization.Json;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.OAuth2.Entities;

public class ClientCredentialsAuthentications : Service, IAuthentications
{
  private readonly IClientCredentials? _clientCredentials;
  private readonly IConfiguration? _configuration;
  private readonly IJwtService? _jwtService;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public ClientCredentialsAuthentications() : base(new List<IPlugin>())
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public ClientCredentialsAuthentications(
    IList<IPlugin> plugins,
    IConfiguration configuration,
    IClientCredentials clientCredentials,
    IJwtService jwtService) : base(plugins)
  {
    _configuration = configuration;
    _clientCredentials = clientCredentials;
    _jwtService = jwtService;
  }

  public async Task<string> CreateAccessToken(string json, string authorization, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    ClientCredentialsGrantDto? clientCredentialsDto = json.Deserialize<ClientCredentialsGrantDto>();
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    if (clientCredentialsDto == null)
      throw new ArgumentNullException(nameof(json));

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
      ctx.Result = new AccessTokenDto { AccessToken = accessToken }.Serialize();
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (string)ctx.Result;
  }

  private static void ValidateAuthorizationHeader(string? authorization)
  {
    if (authorization == null || !authorization.StartsWith("Basic "))
    {
      throw new Exception("Invalid authorization.");
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

    throw new Exception("Invalid authorization.");
  }

  private static bool IsBasicAuthentication(string value, out string? token)
  {
    token = null;
    bool result = false;

    if (value.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
    {
      token = value["Basic".Length..];
      result = true;
    }

    return result;
  }

  private static (Guid, string) DecodeCredentials(string credentials)
  {
    string[] parts = StringUtils.Base64Decode(credentials).Split(':');

    if (parts.Length != 2)
    {
      throw new Exception("Invalid credentials format.");
    }

    return (Guid.Parse(parts[0]), parts[1]);
  }

  private static void ValidateGrantType(string? grantType)
  {
    if (grantType != null && !grantType
          .Equals("client_credentials", StringComparison.InvariantCultureIgnoreCase))
    {
      throw new Exception($"grant_type is invalid.");
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

    ClientCredential? clientCredential = json.Deserialize<ClientCredential>();

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
    ClaimsIdentity claims = new([
      new Claim("ClientId", clientCredential.ClientId.ToString())
    ]);

    string audience = _configuration!["Audience"]!;
    string issuer = _configuration["Issuer"]!;
    var tokenExpirationTimeInMinutes = int.Parse(_configuration["TokenExpirationTimeInMinutes"]!);

    string privateKey = StringUtils.Base64Decode(_configuration["PrivateKey"]!);

    string accessToken = _jwtService!.GenerateToken(privateKey, issuer, audience, claims,
      TimeSpan.FromMinutes(tokenExpirationTimeInMinutes));
    return accessToken;
  }
}