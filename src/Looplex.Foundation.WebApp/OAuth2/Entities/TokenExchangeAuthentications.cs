using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

using Looplex.Foundation.Entities;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.Serialization;
using Looplex.Foundation.WebApp.OAuth2.Dtos;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

namespace Looplex.Foundation.WebApp.OAuth2.Entities;

public class TokenExchangeAuthentications : Service, IAuthentications
{
  private readonly IConfiguration? _configuration;
  private readonly HttpClient? _httpClient;
  private readonly IJwtService? _jwtService;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public TokenExchangeAuthentications()
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public TokenExchangeAuthentications(
    IConfiguration configuration,
    IJwtService jwtService,
    HttpClient httpClient)
  {
    _configuration = configuration;
    _jwtService = jwtService;
    _httpClient = httpClient;
  }

  public async Task<string> CreateAccessToken(string json, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    ClientCredentialsGrantDto clientCredentialsDto = json.JsonDeserialize<ClientCredentialsGrantDto>();
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    ArgumentNullException.ThrowIfNull(clientCredentialsDto, "body");

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
      ctx.Result = new AccessTokenDto { AccessToken = accessToken }.JsonSerialize();
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (string)ctx.Result;
  }

  private static void ValidateGrantType(string grantType)
  {
    if (!grantType
          .Equals(Constants.TokenExchangeGrantType, StringComparison.InvariantCultureIgnoreCase))
    {
      throw new HttpRequestException($"{Constants.GrantType} is invalid.", null, HttpStatusCode.Unauthorized);
    }
  }

  private void ValidateTokenType(string? subjectTokenType)
  {
    if (string.IsNullOrWhiteSpace(subjectTokenType)
        || !subjectTokenType
          .Equals(Constants.AccessTokenType, StringComparison.InvariantCultureIgnoreCase))
    {
      throw new HttpRequestException($"{Constants.SubjectTokenType} is invalid.", null, HttpStatusCode.Unauthorized);
    }
  }

  private void ValidateAccessToken(string? accessToken)
  {
    if (string.IsNullOrWhiteSpace(accessToken))
    {
      throw new HttpRequestException("Token is invalid.", null, HttpStatusCode.Unauthorized);
    }
  }

  private async Task<UserInfo> GetUserInfoAsync(string accessToken)
  {
    string? userInfoEndpoint = _configuration!["OicdUserInfoEndpoint"];
    _httpClient!.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.Bearer, accessToken);
    HttpResponseMessage response = await _httpClient.GetAsync(userInfoEndpoint);
    response.EnsureSuccessStatusCode();
    string content = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<UserInfo>(content)!;
  }

  private string CreateAccessToken(UserInfo userInfo)
  {
    ClaimsIdentity claims = new ClaimsIdentity([
      new Claim("name", $"{userInfo.GivenName} {userInfo.FamilyName}"),
      new Claim("email", userInfo.Email),
      new Claim("photo", userInfo.Picture)
      // TODO add preferredLanguage
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