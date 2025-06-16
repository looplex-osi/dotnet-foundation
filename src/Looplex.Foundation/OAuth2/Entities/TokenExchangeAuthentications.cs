using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
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

    ClientCredentialsGrantDto? clientCredentialsDto = json.Deserialize<ClientCredentialsGrantDto>();
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    if (clientCredentialsDto == null)
      throw new ArgumentNullException(nameof(json));

    ctx.Roles["ClientServices"] = clientCredentialsDto;
    if (clientCredentialsDto.GrantType.Equals("client_credentials", StringComparison.OrdinalIgnoreCase))
    {
      if (!Guid.TryParse(clientCredentialsDto.ClientId, out var clientId))
        throw new Exception("Invalid ClientId");

      var clientSecret = clientCredentialsDto.ClientSecret ?? throw new Exception("ClientSecret missing");

      var client = await _clientServices.Retrieve(clientId, clientSecret, cancellationToken);

      if (client == null)
        throw new Exception("Client authentication failed.");

      var accessToken = CreateAccessTokenForClient(client);
      ctx.Result = new AccessTokenDto { AccessToken = accessToken }.Serialize();
    }

    else if (clientCredentialsDto.GrantType.Equals("urn:ietf:params:oauth:grant-type:token-exchange", StringComparison.OrdinalIgnoreCase))
    {
      // restante do código
    }
    else
    {
      throw new Exception("Unsupported grant_type.");
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