using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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

using Newtonsoft.Json;

namespace Looplex.Foundation.OAuth2.Entities;

public class TokenExchangeAuthentications : Service, ITokenExchangeAuthentications
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
  // injete ClientServices
  private readonly ClientServices _clientServices;

  [ActivatorUtilitiesConstructor]
  public TokenExchangeAuthentications(
    IList<IPlugin> plugins,
    IConfiguration configuration,
    IJwtService jwtService,
    HttpClient httpClient,
    ClientServices clientServices) : base(plugins)
  {
    _configuration = configuration;
    _jwtService = jwtService;
    _httpClient = httpClient;
    _clientServices = clientServices;
  }

  public async Task<string> CreateAccessToken(string json, string authentication, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    var jObject = Newtonsoft.Json.Linq.JObject.Parse(json);
    var grantType = jObject["grant_type"]?.ToString();

    if (string.IsNullOrWhiteSpace(grantType))
      throw new ArgumentNullException("grant_type", "Grant type is required.");

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    switch (grantType.ToLowerInvariant())
    {
      case "client_credentials":
        {
          var dto = json.Deserialize<ClientCredentialDto>()
                    ?? throw new ArgumentNullException(nameof(json), "Invalid client credentials payload");

          if (dto.ClientId == Guid.Empty)
            throw new ArgumentException("ClientId is required.");

          if (string.IsNullOrWhiteSpace(dto.ClientSecret))
            throw new ArgumentException("ClientSecret is required.");

          var client = await _clientServices.Retrieve(dto.ClientId, dto.ClientSecret, cancellationToken);
          if (client == null)
            throw new UnauthorizedAccessException($"Client authentication failed for client ID: {dto.ClientId}");

          ctx.Result = CreateAccessTokenForClient(client);
          break;
        }

      case "urn:ietf:params:oauth:grant-type:token-exchange":
        {
          var dto = json.Deserialize<TokenExchangeDto>()
                    ?? throw new ArgumentNullException(nameof(json), "Invalid token exchange payload");

          if (!"urn:ietf:params:oauth:grant-type:token-exchange"
                .Equals(dto.GrantType, StringComparison.InvariantCultureIgnoreCase))
            throw new InvalidOperationException("Invalid grant_type for token exchange.");

          if (!"urn:ietf:params:oauth:token-type:access_token"
                .Equals(dto.SubjectTokenType, StringComparison.InvariantCultureIgnoreCase))
            throw new InvalidOperationException("Unsupported subject_token_type. Only access_token is allowed.");

          if (string.IsNullOrWhiteSpace(dto.SubjectToken))
            throw new InvalidOperationException("Missing subject_token.");

          UserInfo userInfo = await GetUserInfoAsync(dto.SubjectToken);

          string newAccessToken = CreateAccessToken(userInfo);

          ctx.Result = newAccessToken;
          break;
        }


      default:
        throw new Exception($"Unsupported grant_type: {grantType}");
    }
    return (string)ctx.Result!;
  }

  private string CreateAccessTokenForClient(ClientService client)
  {
    ClaimsIdentity claims = new(new[]
    {
        new Claim("client_id", client.Id.ToString()),
        new Claim("client_name", client.ClientName ?? "ConfidentialClient")
    });

    string audience = _configuration!["Audience"]!;
    string issuer = _configuration["Issuer"]!;
    var tokenExpirationTimeInMinutes = int.Parse(_configuration["TokenExpirationTimeInMinutes"]!);

    // Decodifique a chave privada em string, não em bytes
    var privateKeyBase64 = _configuration["PrivateKey"]!;
    var privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
    var privateKeyString = System.Text.Encoding.UTF8.GetString(privateKeyBytes);

    return _jwtService!.GenerateToken(privateKeyString, issuer, audience, claims,
        TimeSpan.FromMinutes(tokenExpirationTimeInMinutes));
  }


  private static void ValidateGrantType(string? grantType)
  {
    if (grantType == null || !grantType
          .Equals("urn:ietf:params:oauth:grant-type:token-exchange", StringComparison.InvariantCultureIgnoreCase))
    {
      throw new Exception($"grant_type is invalid.");
    }
  }

  private void ValidateTokenType(string? subjectTokenType)
  {
    if (subjectTokenType == null
        || !subjectTokenType
          .Equals("urn:ietf:params:oauth:token-type:access_token", StringComparison.InvariantCultureIgnoreCase))
    {
      throw new Exception("subject_token_type is invalid.");
    }
  }

  private void ValidateAccessToken(string? accessToken)
  {
    if (string.IsNullOrWhiteSpace(accessToken))
    {
      throw new Exception("Token is invalid.");
    }
  }

  private async Task<UserInfo> GetUserInfoAsync(string accessToken)
  {
    string? userInfoEndpoint = _configuration!["OicdUserInfoEndpoint"];
    _httpClient!.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    HttpResponseMessage response = await _httpClient.GetAsync(userInfoEndpoint);
    response.EnsureSuccessStatusCode();
    string content = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<UserInfo>(content)!;
  }

  private string CreateAccessToken(UserInfo userInfo)
  {
    ClaimsIdentity claims = new([
      new Claim("name", $"{userInfo.GivenName} {userInfo.FamilyName}"),
      new Claim("email", userInfo.Email),
      new Claim("photo", userInfo.Picture)
      // TODO add preferredLanguage
    ]);

    string audience = _configuration!["Audience"]!;
    string issuer = _configuration["Issuer"]!;
    var tokenExpirationTimeInMinutes = int.Parse(_configuration["TokenExpirationTimeInMinutes"]!);

    string privateKey = Strings.Base64Decode(_configuration["PrivateKey"]!);

    string accessToken = _jwtService!.GenerateToken(privateKey, issuer, audience, claims,
      TimeSpan.FromMinutes(tokenExpirationTimeInMinutes));
    return accessToken;
  }
}