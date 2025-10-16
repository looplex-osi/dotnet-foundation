using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.Serialization;
using Microsoft.Extensions.Logging;

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
    ILogger<Service> logger,
    IConfiguration configuration,
    ClientServices clientServices,
    IJwtService jwtService) : base(plugins, logger)
  {
    _configuration = configuration;
    _clientServices = clientServices;
    _jwtService = jwtService;
  }

  public async Task<string> CreateAccessToken(string json, string authorization, CancellationToken cancellationToken)
  {
    // RFC 6749 Section 4.4 - Client Credentials Grant
    // https://tools.ietf.org/html/rfc6749#section-4.4
    Logger.LogInformation("Starting client credentials authentication process");
    
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    ClientCredentialsGrantDto? clientCredentialsDto = System.Text.Json.JsonSerializer.Deserialize<ClientCredentialsGrantDto>(json, FoundationJsonSerializer.DefaultOptions);
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    if (clientCredentialsDto == null)
    {
      // RFC 6749 Section 5.1 - Error Response
      // https://tools.ietf.org/html/rfc6749#section-5.1
      Logger.LogError("Invalid client credentials request: JSON deserialization failed");
      throw new ArgumentNullException(nameof(json));
    }

    // RFC 6749 Section 5.2 - Error Codes
    // https://tools.ietf.org/html/rfc6749#section-5.2
    Logger.LogDebug("Validating authorization header and grant type");
    ValidateAuthorizationHeader(authorization);
    ValidateGrantType(clientCredentialsDto.GrantType);
    (Guid clientId, string clientSecret) = TryGetClientCredentials(authorization);
    
    // RFC 6749 Section 10.1 - Security Considerations for Authorization Servers
    // https://tools.ietf.org/html/rfc6749#section-10.1
    Logger.LogInformation("Authenticating client with ID: {ClientId}", clientId);
    ClientService clientService =
      await GetClientCredentialByIdAndSecretOrDefaultAsync(clientId, clientSecret, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    ctx.Roles["ClientService"] = clientService;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      // RFC 7519 Section 7.2 - Security Considerations
      // https://tools.ietf.org/html/rfc7519#section-7.2
      Logger.LogDebug("Creating access token for authenticated client");
      string accessToken = CreateAccessToken((ClientService)ctx.Roles["ClientService"]);
      ctx.Result = System.Text.Json.JsonSerializer.Serialize(new AccessTokenDto { AccessToken = accessToken }, FoundationJsonSerializer.DefaultOptions);
      Logger.LogInformation("Access token created successfully for client: {ClientId}", clientId);
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (string)ctx.Result;
  }

  private void ValidateAuthorizationHeader(string? authorization)
  {
    if (authorization == null || !authorization.StartsWith("Basic "))
    {
      // RFC 6750 Section 2.1 - Authorization Request Header Field
      // https://tools.ietf.org/html/rfc6750#section-2.1
      Logger.LogWarning("Invalid authorization header format");
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

  private void ValidateGrantType(string? grantType)
  {
    if (grantType != null && !grantType
          .Equals("client_credentials", StringComparison.InvariantCultureIgnoreCase))
    {
      // RFC 6749 Section 5.2 - Error Codes
      // https://tools.ietf.org/html/rfc6749#section-5.2
      Logger.LogWarning("Invalid grant type: {GrantType}", grantType);
      throw new Exception("grant_type is invalid.");
    }
  }

  private async Task<ClientService> GetClientCredentialByIdAndSecretOrDefaultAsync(Guid clientId,
    string clientSecret, CancellationToken cancellationToken)
  {
    // RFC 6749 Section 10.1 - Security Considerations for Authorization Servers
    // https://tools.ietf.org/html/rfc6749#section-10.1
    Logger.LogDebug("Retrieving client credentials for ID: {ClientId}", clientId);
    var result = await _clientServices!.RetrieveAsync(clientId, cancellationToken);
    
    if (result == null)
    {
      // RFC 6749 Section 5.1 - Error Response
      // https://tools.ietf.org/html/rfc6749#section-5.1
      Logger.LogWarning("Client not found: {ClientId}", clientId);
      throw new InvalidCredentialsException($"Client with ID {clientId} not found");
    }

    if (result is not ClientService clientService)
    {
      // RFC 6749 Section 5.2 - Error Codes
      // https://tools.ietf.org/html/rfc6749#section-5.2
      Logger.LogError("Invalid client type returned: {ClientType}", result.GetType().Name);
      throw new InvalidCredentialsException($"Expected ClientService, got {result.GetType().Name}");
    }

    if (clientService.NotBefore > DateTimeOffset.UtcNow)
    {
      // SOC 2 CC6.1 - Logical Access Controls
      // https://www.aicpa.org/interestareas/frc/assuranceadvisoryservices/aicpasoc2report
      Logger.LogWarning("Client access not yet allowed: {ClientId}, NotBefore: {NotBefore}", clientId, clientService.NotBefore);
      throw new InvalidCredentialsException("Client access not allowed. Access time has not been reached.");
    }

    if (clientService.ExpirationTime <= DateTimeOffset.UtcNow)
    {
      // SOC 2 CC6.1 - Logical Access Controls
      // https://www.aicpa.org/interestareas/frc/assuranceadvisoryservices/aicpasoc2report
      Logger.LogWarning("Client access expired: {ClientId}, ExpirationTime: {ExpirationTime}", clientId, clientService.ExpirationTime);
      throw new Exception("Client access is expired.");
    }

    // RFC 6749 Section 10.1 - Security Considerations for Authorization Servers
    // https://tools.ietf.org/html/rfc6749#section-10.1
    Logger.LogDebug("Client credentials validated successfully: {ClientId}", clientId);
    return clientService;
  }

  private string CreateAccessToken(ClientService clientService)
  {
    // RFC 7519 Section 7.2 - Security Considerations
    // https://tools.ietf.org/html/rfc7519#section-7.2
    Logger.LogDebug("Creating JWT access token for client: {ClientId}", clientService.Id);
    
    ClaimsIdentity claims = new([
      new Claim("ClientId", clientService.Id)
    ]);

    string audience = _configuration!["Audience"]!;
    string issuer = _configuration["Issuer"]!;
    var tokenExpirationTimeInMinutes = int.Parse(_configuration["TokenExpirationTimeInMinutes"]!);

    // RFC 7519 Section 4.1 - Registered Claim Names
    // https://tools.ietf.org/html/rfc7519#section-4.1
    Logger.LogDebug("JWT configuration - Audience: {Audience}, Issuer: {Issuer}, Expiration: {ExpirationMinutes} minutes", 
      audience, issuer, tokenExpirationTimeInMinutes);

    string privateKey = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(_configuration["PrivateKey"]!));

    string accessToken = _jwtService!.GenerateToken(privateKey, issuer, audience, claims,
      TimeSpan.FromMinutes(tokenExpirationTimeInMinutes));
    
    // RFC 7519 Section 7.2 - Security Considerations
    // https://tools.ietf.org/html/rfc7519#section-7.2
    Logger.LogInformation("JWT access token generated successfully for client: {ClientId}", clientService.Id);
    return accessToken;
  }
}
