using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.Serialization;

using Looplex.Foundation.Entities;
using Looplex.OAuth2.Dtos;
using Looplex.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.OAuth2.Entities;

public class ClientCredentialsAuthentications : Service, IAuthentications
{
  private readonly ClientServices? _clientServices;
  private readonly IConfiguration? _configuration;
  private readonly IJwtService? _jwtService;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public ClientCredentialsAuthentications() : base()
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public ClientCredentialsAuthentications(
    IList<IPlugin> plugins,
    IConfiguration configuration,
    ClientServices clientServices,
    IJwtService jwtService) : base(plugins)
  {
    _configuration = configuration;
    _clientServices = clientServices;
    _jwtService = jwtService;
  }

  public async Task<string> CreateAccessToken(string json, string authorization, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    ClientCredentialsGrantDto? clientCredentialsDto = System.Text.Json.JsonSerializer.Deserialize<ClientCredentialsGrantDto>(json, FoundationJsonSerializer.DefaultOptions);
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    if (clientCredentialsDto == null)
      throw new ArgumentNullException(nameof(json));

    ValidateAuthorizationHeader(authorization);
    ValidateGrantType(clientCredentialsDto.GrantType);
    (Guid clientId, string clientSecret) = TryGetClientCredentials(authorization);
    ClientService clientService =
      await GetClientCredentialByIdAndSecretOrDefaultAsync(clientId, clientSecret, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    ctx.Roles["ClientService"] = clientService;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      string accessToken = CreateAccessToken((ClientService)ctx.Roles["ClientService"]);
      ctx.Result = System.Text.Json.JsonSerializer.Serialize(new AccessTokenDto { AccessToken = accessToken }, FoundationJsonSerializer.DefaultOptions);
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
    if (string.IsNullOrWhiteSpace(credentials))
      throw new ArgumentException("Credentials cannot be null or empty", nameof(credentials));

    try
    {
      string decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(credentials));
      string[] parts = decoded.Split(':');

      if (parts.Length != 2)
      {
        throw new InvalidCredentialsException("Invalid credentials format. Expected 'clientId:clientSecret'");
      }

      if (string.IsNullOrWhiteSpace(parts[0]))
        throw new InvalidCredentialsException("Client ID cannot be empty");

      if (string.IsNullOrWhiteSpace(parts[1]))
        throw new InvalidCredentialsException("Client secret cannot be empty");

      return (Guid.Parse(parts[0]), parts[1]);
    }
    catch (FormatException ex)
    {
      throw new InvalidCredentialsException("Invalid Base64 format in credentials", ex);
    }
    catch (ArgumentException ex)
    {
      throw new InvalidCredentialsException("Invalid credentials format", ex);
    }
  }

  private static void ValidateGrantType(string? grantType)
  {
    if (grantType != null && !grantType
          .Equals("client_credentials", StringComparison.InvariantCultureIgnoreCase))
    {
      throw new Exception("grant_type is invalid.");
    }
  }

  private async Task<ClientService> GetClientCredentialByIdAndSecretOrDefaultAsync(Guid clientId,
    string clientSecret, CancellationToken cancellationToken)
  {
    var result = await _clientServices!.RetrieveAsync(clientId, cancellationToken);
    
    if (result == null)
      throw new InvalidCredentialsException($"Client with ID {clientId} not found");

    if (result is not ClientService clientService)
      throw new InvalidCredentialsException($"Expected ClientService, got {result.GetType().Name}");

    if (clientService.NotBefore > DateTimeOffset.UtcNow)
    {
      throw new InvalidCredentialsException("Client access not allowed. Access time has not been reached.");
    }

    if (clientService.ExpirationTime <= DateTimeOffset.UtcNow)
    {
      throw new Exception("Client access is expired.");
    }

    return clientService;
  }

  private string CreateAccessToken(ClientService clientService)
  {
    ClaimsIdentity claims = new([
      new Claim("ClientId", clientService.Id)
    ]);

    string audience = _configuration!["Audience"]!;
    string issuer = _configuration["Issuer"]!;
    var tokenExpirationTimeInMinutes = int.Parse(_configuration["TokenExpirationTimeInMinutes"]!);

    string privateKey = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(_configuration["PrivateKey"]!));

    string accessToken = _jwtService!.GenerateToken(privateKey, issuer, audience, claims,
      TimeSpan.FromMinutes(tokenExpirationTimeInMinutes));
    return accessToken;
  }
}
