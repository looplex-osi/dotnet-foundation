using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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

/// <summary>
/// Implements OAuth2 token exchange authentication flow following RFC 8693 specification.
/// This service handles the exchange of external tokens for internal JWT access tokens,
/// supporting various token types and authentication mechanisms for secure token exchange.
/// </summary>
public class TokenExchangeAuthentications : Service, IAuthentications
{
  private readonly IConfiguration? _configuration;
  private readonly HttpClient? _httpClient;
  private readonly IJwtService? _jwtService;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public TokenExchangeAuthentications() : base()
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public TokenExchangeAuthentications(
    IList<IPlugin> plugins,
    ILogger<Service> logger,
    IConfiguration configuration,
    IJwtService jwtService,
    HttpClient httpClient) : base(plugins, logger)
  {
    _configuration = configuration;
    _jwtService = jwtService;
    _httpClient = httpClient;
  }

  /// <summary>
  /// Creates an access token by exchanging external authentication credentials.
  /// This method processes client credentials and external tokens to generate a new JWT access token
  /// following OAuth2 token exchange specification (RFC 8693).
  /// </summary>
  /// <param name="json">JSON string containing client credentials and token exchange parameters</param>
  /// <param name="authentication">Authentication method identifier for the token exchange</param>
  /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
  /// <returns>A JWT access token string that can be used for API authentication</returns>
  /// <exception cref="ArgumentNullException">Thrown when the JSON parameter is null or invalid</exception>
  /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or credentials are invalid</exception>
  public async Task<string> CreateAccessToken(string json, string authentication, CancellationToken cancellationToken)
  {
    // RFC 8693 Section 2.1 - Token Exchange Request
    // https://tools.ietf.org/html/rfc8693#section-2.1
    Logger.LogInformation("Starting OAuth2 token exchange process");
    
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    ClientCredentialsGrantDto? clientCredentialsDto = System.Text.Json.JsonSerializer.Deserialize<ClientCredentialsGrantDto>(json, FoundationJsonSerializer.DefaultOptions);
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    if (clientCredentialsDto == null)
    {
      // RFC 8693 Section 4.1 - Error Handling
      // https://tools.ietf.org/html/rfc8693#section-4.1
      Logger.LogError("Invalid token exchange request: JSON deserialization failed");
      throw new ArgumentNullException(nameof(json));
    }

    // RFC 8693 Section 2.1 - Token Exchange Request
    // https://tools.ietf.org/html/rfc8693#section-2.1
    Logger.LogDebug("Validating token exchange parameters");
    ValidateGrantType(clientCredentialsDto.GrantType);
    ValidateTokenType(clientCredentialsDto.SubjectTokenType);
    ValidateAccessToken(clientCredentialsDto.SubjectToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    ctx.Roles["ClientServices"] = clientCredentialsDto;
    // RFC 6750 Section 2.1 - Authorization Request Header Field
    // https://tools.ietf.org/html/rfc6750#section-2.1
    Logger.LogDebug("Retrieving user information from external token");
    ctx.Roles["UserInfo"] = await GetUserInfoAsync(clientCredentialsDto.SubjectToken!);
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      // RFC 7519 Section 7.2 - Security Considerations
      // https://tools.ietf.org/html/rfc7519#section-7.2
      Logger.LogDebug("Creating access token from user information");
      string accessToken = CreateAccessToken((UserInfo)ctx.Roles["UserInfo"]);
      ctx.Result = System.Text.Json.JsonSerializer.Serialize(new AccessTokenDto { AccessToken = accessToken }, FoundationJsonSerializer.DefaultOptions);
      Logger.LogInformation("Token exchange completed successfully");
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (string)ctx.Result;
  }

  private void ValidateGrantType(string? grantType)
  {
    if (grantType == null || !grantType
          .Equals("urn:ietf:params:oauth:grant-type:token-exchange", StringComparison.InvariantCultureIgnoreCase))
    {
      // RFC 8693 Section 4.1 - Error Handling
      // https://tools.ietf.org/html/rfc8693#section-4.1
      Logger.LogWarning("Invalid grant type for token exchange: {GrantType}", grantType);
      throw new Exception($"grant_type is invalid.");
    }
  }

  private void ValidateTokenType(string? subjectTokenType)
  {
    if (subjectTokenType == null
        || !subjectTokenType
          .Equals("urn:ietf:params:oauth:token-type:access_token", StringComparison.InvariantCultureIgnoreCase))
    {
      // RFC 8693 Section 2.1 - Token Exchange Request
      // https://tools.ietf.org/html/rfc8693#section-2.1
      Logger.LogWarning("Invalid subject token type: {SubjectTokenType}", subjectTokenType);
      throw new Exception("subject_token_type is invalid.");
    }
  }

  private void ValidateAccessToken(string? accessToken)
  {
    if (string.IsNullOrWhiteSpace(accessToken))
    {
      // RFC 6750 Section 2.1 - Authorization Request Header Field
      // https://tools.ietf.org/html/rfc6750#section-2.1
      Logger.LogWarning("Invalid or empty access token provided");
      throw new Exception("Token is invalid.");
    }
  }

  private async Task<UserInfo> GetUserInfoAsync(string accessToken)
  {
    string? userInfoEndpoint = _configuration!["OicdUserInfoEndpoint"];
    // RFC 6750 Section 2.1 - Authorization Request Header Field
    // https://tools.ietf.org/html/rfc6750#section-2.1
    Logger.LogDebug("Retrieving user information from endpoint: {UserInfoEndpoint}", userInfoEndpoint);
    
    _httpClient!.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    HttpResponseMessage response = await _httpClient.GetAsync(userInfoEndpoint);
    
    if (!response.IsSuccessStatusCode)
    {
      // RFC 7231 Section 6.5 - Client Error 4xx
      // https://tools.ietf.org/html/rfc7231#section-6.5
      Logger.LogError("Failed to retrieve user information. Status: {StatusCode}, Reason: {ReasonPhrase}", 
        response.StatusCode, response.ReasonPhrase);
    }
    
    response.EnsureSuccessStatusCode();
    string content = await response.Content.ReadAsStringAsync();
    
    // RFC 6750 Section 2.1 - Authorization Request Header Field
    // https://tools.ietf.org/html/rfc6750#section-2.1
    Logger.LogDebug("User information retrieved successfully");
    return JsonSerializer.Deserialize<UserInfo>(content)!;
  }

  private string CreateAccessToken(UserInfo userInfo)
  {
    // RFC 7519 Section 7.2 - Security Considerations
    // https://tools.ietf.org/html/rfc7519#section-7.2
    Logger.LogDebug("Creating JWT access token for user: {Email}", userInfo.Email);
    
    ClaimsIdentity claims = new([
      new Claim("name", $"{userInfo.GivenName ?? ""} {userInfo.FamilyName ?? ""}"),
      new Claim("email", userInfo.Email ?? ""),
      new Claim("photo", userInfo.Picture ?? "")
      // TODO add preferredLanguage
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
    Logger.LogInformation("JWT access token generated successfully for user: {Email}", userInfo.Email);
    return accessToken;
  }
}
