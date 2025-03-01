using System.Net;
using System.Security.Claims;
using Looplex.Foundation.Entities;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.Serialization;
using Looplex.Foundation.WebApp.OAuth2.Dtos;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Looplex.Foundation.WebApp.OAuth2.Entities;

public class ClientCredentialsAuthorizations : Service, IAuthorizations
{
    private readonly IConfiguration? _configuration;
    private readonly IClientCredentials? _clientCredentials;
    private readonly IJwtService? _jwtService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    #region Reflectivity
    // ReSharper disable once PublicConstructorInAbstractClass
    public ClientCredentialsAuthorizations() : base() { }
    #endregion
    
    public ClientCredentialsAuthorizations(IList<IPlugin> plugins,
        IConfiguration configuration,
        IClientCredentials clientCredentials,
        IJwtService jwtService,
        IHttpContextAccessor httpContextAccessor) : base(plugins)
    {
        _configuration = configuration;
        _clientCredentials = clientCredentials;
        _jwtService = jwtService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> CreateAccessToken(string json, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ctx = NewContext();

        var authorization = _httpContextAccessor?.HttpContext?.Request.Headers.Authorization;
        var clientCredentialsDto = json.JsonDeserialize<ClientCredentialsDto>();
        await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

        ArgumentNullException.ThrowIfNull(clientCredentialsDto, "body");

        ValidateAuthorizationHeader(authorization);
        ValidateGrantType(clientCredentialsDto.GrantType);
        var (clientId, clientSecret) = TryGetClientCredentials(authorization);
        var clientCredential =
            await GetClientCredentialByIdAndSecretOrDefaultAsync(clientId.ToString(), clientSecret, ctx, cancellationToken);
        await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

        ctx.Roles["ClientCredential"] = clientCredential;
        await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

        if (!ctx.SkipDefaultAction)
        {
            var accessToken = CreateAccessToken((ClientCredential)ctx.Roles["ClientCredential"]);
            ctx.Result = new AccessTokenDto
            {
                AccessToken = accessToken
            }.JsonSerialize();
        }

        await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);
        
        return (string)ctx.Result;
    }

    private static void ValidateAuthorizationHeader(string? authorization)
    {
        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Basic "))
        {
            throw new HttpRequestException("Invalid authorization.", null, HttpStatusCode.Unauthorized);
        }
    }

    private static (Guid, string) TryGetClientCredentials(string? authorization)
    {
        if (authorization != default
            && IsBasicAuthentication(authorization, out var base64Credentials)
            && base64Credentials != default)
        {
            return DecodeCredentials(base64Credentials);
        }

        throw new HttpRequestException("Invalid authorization.", null, HttpStatusCode.Unauthorized);
    }
    
    private static bool IsBasicAuthentication(string value, out string? token)
    {
        token = null;
        var result = false;
        
        if (value.StartsWith(Constants.Basic, StringComparison.OrdinalIgnoreCase))
        {
            token = value[Constants.Basic.Length..];
            result = true;
        }

        return result;
    }

    private static (Guid, string) DecodeCredentials(string credentials)
    {
        var parts = StringUtils.Base64Decode(credentials).Split(':');

        if (parts.Length != 2)
        {
            throw new HttpRequestException("Invalid credentials format.", null, HttpStatusCode.Unauthorized);
        }

        return (Guid.Parse(parts[0]), parts[1]);
    }

    private static void ValidateGrantType(string grantType)
    {
        if (!grantType
                .Equals(Constants.ClientCredentialsGrantType, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new HttpRequestException($"{Constants.GrantType} is invalid.", null, HttpStatusCode.Unauthorized);
        }
    }

    private async Task<ClientCredential> GetClientCredentialByIdAndSecretOrDefaultAsync(string clientId, string clientSecret,
        IContext parentContext, CancellationToken cancellationToken)
    {
        var json = await _clientCredentials!.RetrieveAsync(clientId, clientSecret, cancellationToken);
        var clientCredential = json.JsonDeserialize<ClientCredential>();

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
        var claims = new ClaimsIdentity([
            new Claim(Constants.ClientId, clientCredential.ClientId.ToString()!),
        ]);

        var audience = _configuration!["Audience"]!;
        var issuer = _configuration["Issuer"]!;
        var tokenExpirationTimeInMinutes = _configuration.GetValue<int>("TokenExpirationTimeInMinutes");

        var privateKey = StringUtils.Base64Decode(_configuration["PrivateKey"]!);

        var accessToken = _jwtService!.GenerateToken(privateKey, issuer, audience, claims,
            TimeSpan.FromMinutes(tokenExpirationTimeInMinutes));
        return accessToken;
    }
}