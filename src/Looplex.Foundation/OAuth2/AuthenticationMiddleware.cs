using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Looplex.Foundation.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.OAuth2
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public AuthenticationMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task Invoke(HttpContext context)
        {
            var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
            var audience = configuration["Audience"] ??
                           throw new InvalidOperationException("Audience configuration is missing");
            var issuer = configuration["Issuer"] ??
                         throw new InvalidOperationException("Issuer configuration is missing");

            var accessToken = string.Empty;

            string authorization = context.Request.Headers["Authorization"];

            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                accessToken = authorization.Substring("Bearer ".Length).Trim();
            }

            var publicKey = StringUtils.Base64Decode(configuration["PublicKey"] ??
                                                     throw new InvalidOperationException(
                                                         "PublicKey configuration is missing"));
            var jwtService = _serviceProvider.GetRequiredService<IJwtService>();
            var authenticated = jwtService.ValidateToken(publicKey, issuer, audience, accessToken);

            if (!authenticated)
            {
                throw new Exception("AccessToken is invalid.");
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);

            string tenant = context.Request.Headers["X-looplex-tenant"];
            if (string.IsNullOrWhiteSpace(tenant))
                throw new InvalidOperationException(
                    "X-looplex-tenant header is missing");
            
            var claims = token.Claims.ToList();

            if (claims.Any(c => c.Type == "name" || c.Type == "email"))
            {
                var name = claims.FirstOrDefault(c => c.Type == "name")?.Value;
                var email = claims.FirstOrDefault(c => c.Type == "email")?.Value;

                context.Items["UserContext"] = new UserContext
                {
                    Name = name,
                    Email = email,
                    Tenant = tenant
                };
            }

            await _next(context);
        }
    }
}