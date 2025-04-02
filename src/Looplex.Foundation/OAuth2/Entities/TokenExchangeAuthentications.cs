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
    IConfiguration configuration,
    IJwtService jwtService,
    HttpClient httpClient) : base(plugins)
  {
    _configuration = configuration;
    _jwtService = jwtService;
    _httpClient = httpClient;
  }

  public async Task<string> CreateAccessToken(string json, string authentication, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    ClientCredentialsGrantDto? clientCredentialsDto = json.Deserialize<ClientCredentialsGrantDto>();
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    if (clientCredentialsDto == null)
      throw new ArgumentNullException(nameof(json));

    ValidateGrantType(clientCredentialsDto.GrantType);
    ValidateTokenType(clientCredentialsDto.SubjectTokenType);
    ValidateAccessToken(clientCredentialsDto.SubjectToken);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    ctx.Roles["ClientCredentials"] = clientCredentialsDto;
    ctx.Roles["UserInfo"] = await GetUserInfoAsync(clientCredentialsDto.SubjectToken!);
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      string accessToken = CreateAccessToken((UserInfo)ctx.Roles["UserInfo"]);
      ctx.Result = new AccessTokenDto { AccessToken = accessToken }.Serialize();
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (string)ctx.Result;
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