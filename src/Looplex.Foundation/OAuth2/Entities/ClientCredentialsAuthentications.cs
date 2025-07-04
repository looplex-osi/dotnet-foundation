using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Entities;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.OAuth2.Dtos;
using Looplex.Foundation.Ports;
using Looplex.Foundation.Serialization.Json;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;


namespace Looplex.Foundation.OAuth2.Entities;

/// <summary>
/// Handles the OAuth 2.0 Client Credentials Grant flow (RFC 6749 §4.4),
/// including validation of client credentials, plugin execution pipeline,
/// and secure generation of access tokens using JWT.
/// </summary>
public class ClientCredentialsAuthentications : Service, IClientCredentialsAuthentications
{
  private readonly ClientServices? _clientServices;
  private readonly IConfiguration? _configuration;
  private readonly IJwtService? _jwtService;
  private readonly ILogger<ClientCredentialsAuthentications> _logger;

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
    IJwtService jwtService,
    ILogger<ClientCredentialsAuthentications> logger) // inject logger 
    : base(plugins)
  {
    _configuration = configuration;
    _clientServices = clientServices;
    _jwtService = jwtService;
    _logger = logger;
  }

  /// <summary>
  /// Generates a JSON-formatted access token using the OAuth 2.0 Client Credentials Grant flow (RFC 6749 §4.4).
  /// Validates the client ID and secret, checks the client's validity period, and issues a JWT if allowed.
  /// </summary>
  /// <param name="json">A JSON string containing the grant_type and optional scope.</param>
  /// <param name="credentials">A tuple with the client_id (Guid) and client_secret (string).</param>
  /// <param name="cancellationToken">Token used to cancel the request.</param>
  /// <returns>A JSON string representing the AccessTokenDto.</returns>
  /// <exception cref="ArgumentNullException">Thrown when JSON is null or invalid.</exception>
  /// <exception cref="Exception">Thrown when grant_type is invalid or client validation fails.</exception>
  public async Task<string> CreateAccessToken(string json, (Guid clientId, string clientSecret) credentials, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    var clientCredentialsDto = json.Deserialize<ClientCredentialsGrantDto>()
        ?? throw new ArgumentNullException(nameof(json));

    ValidateGrantType(clientCredentialsDto.GrantType);

    // Extracted from either Authorization header or body
    (Guid clientId, string clientSecret) = credentials;

    _logger.LogDebug("Attempting to authenticate client_id: {ClientId}", clientId);
    ClientService clientService;
    try
    {
      clientService = await GetClientCredentialByIdAndSecretOrDefaultAsync(
        clientId, clientSecret, cancellationToken
      );
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to retrieve client credentials for client_id: {ClientId}", clientId);
      throw;
    }
    _logger.LogDebug("ClientService retrieved for client_id: {ClientId}", clientId);

    ctx.Roles["ClientService"] = clientService;

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      string token = GenerateJwtToken(clientService, clientCredentialsDto.Scope);

      _logger.LogInformation("Access token generated successfully for client_id: {ClientId}", clientId);

      ctx.Result = new AccessTokenDto
      {
        AccessToken = token,
        TokenType = "Bearer",
        ExpiresIn = (int)GetTokenExpiration().TotalSeconds,
        Scope = clientCredentialsDto.Scope
      }.Serialize();
    }
    else
    {
      _logger.LogDebug("Default action skipped for client_id: {ClientId}", clientId);
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (string)ctx.Result;
  }

  /// <summary>
  /// Generates a signed JWT access token with standard claims for the authenticated client.
  /// </summary>
  private string GenerateJwtToken(ClientService client, string? scope)
  {
    var claims = new List<Claim>
        {
            new("client_id", client.Id),
            new(JwtRegisteredClaimNames.Sub, client.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

    if (!string.IsNullOrWhiteSpace(scope))
    {
      claims.Add(new Claim("scope", scope));
    }

    var identity = new ClaimsIdentity(claims);

    return _jwtService!.GenerateToken(
        GetPrivateKey(),
        _configuration!["Issuer"]!,
        _configuration["Audience"]!,
        identity,
        GetTokenExpiration()
    );
  }
  /// <summary>
  /// Reads and parses the expiration duration for the JWT from configuration.
  /// </summary>
  private TimeSpan GetTokenExpiration()
  {
    int minutes = int.Parse(_configuration!["TokenExpirationTimeInMinutes"]!);
    return TimeSpan.FromMinutes(minutes);
  }

  /// <summary>
  /// Retrieves and decodes the Base64-encoded private signing key.
  /// </summary>
  private string GetPrivateKey()
  {
    return Strings.Base64Decode(_configuration!["PrivateKey"]!);
  }

  private static void ValidateAuthorizationHeader(string? authorization)
  {
    if (authorization == null || !authorization.StartsWith("Basic "))
    {
      throw new Exception("Invalid authorization.");
    }
  }

  private static void ValidateGrantType(string? grantType)
  {
    if (!string.Equals(grantType, "client_credentials", StringComparison.InvariantCultureIgnoreCase))
    {
      throw new Exception("grant_type is invalid.");
    }
  }

  private static (Guid, string) TryGetClientCredentials(string? authorization)
  {
    if (authorization != null &&
        IsBasicAuthentication(authorization, out string? base64Credentials) &&
        base64Credentials != null)
    {
      return DecodeCredentials(base64Credentials);
    }

    throw new Exception("Invalid authorization.");
  }

  private static bool IsBasicAuthentication(string value, out string? token)
  {
    token = null;
    if (value.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
    {
      token = value["Basic".Length..];
      return true;
    }
    return false;
  }

  private static (Guid, string) DecodeCredentials(string credentials)
  {
    string[] parts = Strings.Base64Decode(credentials).Split(':');

    if (parts.Length != 2)
    {
      throw new Exception("Invalid credentials format.");
    }

    return (Guid.Parse(parts[0]), parts[1]);
  }

  private async Task<ClientService> GetClientCredentialByIdAndSecretOrDefaultAsync(
      Guid clientId, string clientSecret, CancellationToken cancellationToken)
  {
    var clientService = await _clientServices!
        .Retrieve(clientId, clientSecret, cancellationToken)
        ?? throw new Exception("Invalid clientId or clientSecret.");

    if (clientService.NotBefore > DateTimeOffset.UtcNow)
    {
      throw new Exception("Client access not allowed.");
    }

    if (clientService.ExpirationTime <= DateTimeOffset.UtcNow)
    {
      throw new Exception("Client access is expired.");
    }

    return clientService;
  }
}