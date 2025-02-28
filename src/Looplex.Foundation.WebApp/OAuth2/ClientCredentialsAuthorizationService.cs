using System.Net;
using System.Security.Claims;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Looplex.Foundation.WebApp.OAuth2
{
    public class ClientCredentialsAuthorizationService(
        IConfiguration configuration,
        ClientCredentialService clientCredentialService,
        IJwtService jwtService) : IAuthorizationService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ClientCredentialService _clientCredentialService = clientCredentialService;
        private readonly IJwtService _jwtService = jwtService;

        public async Task CreateAccessToken(IContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string authorization = context.GetRequiredValue<string>("Authorization");
            var json = context.GetRequiredValue<string>("Resource");
            var clientCredentialsDto = JsonConvert.DeserializeObject<ClientCredentialsDto>(json)!;
            await context.Plugins.ExecuteAsync<IHandleInput>(context, cancellationToken);

            ArgumentNullException.ThrowIfNull(clientCredentialsDto, "body");

            ValidateAuthorizationHeader(authorization);
            ValidateGrantType(clientCredentialsDto.GrantType);
            var (clientId, clientSecret) = TryGetClientCredentials(authorization);
            var clientCredential =
                await GetClientCredentialByIdAndSecretOrDefaultAsync(clientId.ToString(), clientSecret, context, cancellationToken);
            await context.Plugins.ExecuteAsync<IValidateInput>(context, cancellationToken);

            context.Roles["ClientCredential"] = clientCredential;
            await context.Plugins.ExecuteAsync<IDefineRoles>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBind>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IBeforeAction>(context, cancellationToken);

            if (!context.SkipDefaultAction)
            {
                var accessToken = CreateAccessToken((ClientCredential)context.Roles["ClientCredential"]);
                context.Result = new AccessTokenDto
                {
                    AccessToken = accessToken
                };
            }

            await context.Plugins.ExecuteAsync<IAfterAction>(context, cancellationToken);

            await context.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(context, cancellationToken);
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
                && authorization.IsBasicAuthentication(out var base64Credentials)
                && base64Credentials != default)
            {
                return DecodeCredentials(base64Credentials);
            }

            throw new HttpRequestException("Invalid authorization.", null, HttpStatusCode.Unauthorized);
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
            var contextFactory = parentContext.Services.GetRequiredService<IContextFactory>();
            var context = contextFactory.Create([]);

            context.State.ParentContext = parentContext;
            context.State.ClientId = clientId;
            context.State.ClientSecret = clientSecret;
            context.State.CancellationToken = parentContext.GetRequiredValue<CancellationToken>("CancellationToken");
            var result = await GetClientCredentialByIdAndSecretOrDefaultAsync(context);
            context.DisposeIfPossible();
            return result;
        }

        private async Task<ClientCredential> GetClientCredentialByIdAndSecretOrDefaultAsync(IContext context)
        {
            await _clientCredentialService.GetByIdAndSecretOrDefaultAsync(context);
            ClientCredential? clientCredential = default;
            if (context.Roles.TryGetValue("ClientCredential", out var role))
            {
                clientCredential = (ClientCredential)role;
            }

            if (clientCredential == default)
            {
                throw new EntityInvalidException(["Invalid clientId or clientSecret."]);
            }

            if (clientCredential.NotBefore > DateTimeOffset.UtcNow)
            {
                throw new EntityInvalidException(["Client access not allowed."]);
            }

            if (clientCredential.ExpirationTime <= DateTimeOffset.UtcNow)
            {
                throw new EntityInvalidException(["Client access is expired."]);
            }

            return clientCredential;
        }

        private string CreateAccessToken(ClientCredential clientCredential)
        {
            var claims = new ClaimsIdentity([
                new Claim(Constants.ClientId, clientCredential.ClientId.ToString()!),
            ]);

            var audience = _configuration["Audience"]!;
            var issuer = _configuration["Issuer"]!;
            var tokenExpirationTimeInMinutes = _configuration.GetValue<int>("TokenExpirationTimeInMinutes");

            var privateKey = StringUtils.Base64Decode(_configuration["PrivateKey"]!);

            var accessToken = _jwtService.GenerateToken(privateKey, issuer, audience, claims,
                TimeSpan.FromMinutes(tokenExpirationTimeInMinutes));
            return accessToken;
        }
    }
}