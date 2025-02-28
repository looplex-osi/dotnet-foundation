using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Looplex.Foundation.WebApp.OAuth2;

public class TokenExchangeAuthorizationService(
    IConfiguration configuration,
    IJwtService jwtService,
    IHttpClientFactory httpClientFactory) : IAuthorizationService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task CreateAccessToken(IContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var json = context.GetRequiredValue<string>("Resource");
        var clientCredentialsDto = JsonConvert.DeserializeObject<ClientCredentialsDto>(json)!;
        await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);
        
        ArgumentNullException.ThrowIfNull(clientCredentialsDto, "body");
        
        ValidateGrantType(clientCredentialsDto.GrantType);
        ValidateTokenType(clientCredentialsDto.SubjectTokenType);
        ValidateAccessToken(clientCredentialsDto.SubjectToken);
        await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);

        context.Roles["ClientCredentials"] = clientCredentialsDto;
        context.Roles["UserInfo"] = await GetUserInfoAsync(clientCredentialsDto.SubjectToken!);
        await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

        await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

        await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

        if (!context.SkipDefaultAction)
        {
            var accessToken = CreateAccessToken((UserInfo)context.Roles["UserInfo"]);
            context.Result = new AccessTokenDto
            {
                AccessToken = accessToken
            };
        }

        await context.Plugins.ExecuteAsync<IAfterAction>(context, cancellationToken);

        await context.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(context, cancellationToken);
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
        using var client = _httpClientFactory.CreateClient();
        var userInfoEndpoint = _configuration["OicdUserInfoEndpoint"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.Bearer, accessToken);
        var response = await client.GetAsync(userInfoEndpoint);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<UserInfo>(content)!;
    }

    private string CreateAccessToken(UserInfo userInfo)
    {
        var claims = new ClaimsIdentity([
            new Claim("name", $"{userInfo.GivenName} {userInfo.FamilyName}"),
            new Claim("email", userInfo.Email),
            new Claim("photo", userInfo.Picture),
            // TODO add preferredLanguage
        ]);
        
        var audience = _configuration["Audience"]!;
        var issuer = _configuration["Issuer"]!;
        var tokenExpirationTimeInMinutes = _configuration.GetValue<int>("TokenExpirationTimeInMinutes");

        var privateKey = StringUtils.Base64Decode(_configuration["PrivateKey"]!);
        
        var accessToken = _jwtService.GenerateToken(privateKey, issuer, audience, claims, TimeSpan.FromMinutes(tokenExpirationTimeInMinutes));
        return accessToken;
    }
}