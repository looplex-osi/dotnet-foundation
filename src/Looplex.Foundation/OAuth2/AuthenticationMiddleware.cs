using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Looplex.Foundation.Ports;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Looplex.Foundation.OAuth2;

public class AuthenticationMiddleware
{
  private readonly RequestDelegate _next;

  private readonly HashSet<string> _publicEndpoints = new() { "/token", "/health" };

  public AuthenticationMiddleware(RequestDelegate next)
  {
    _next = next ?? throw new ArgumentNullException(nameof(next));
  }

  public async Task Invoke(HttpContext context, IConfiguration configuration, IJwtService jwtService)
  {
    string requestPath = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

    // Skip authentication for public endpoints
    if (_publicEndpoints.Contains(requestPath))
    {
      await _next(context);
      return;
    }

    string audience = configuration["Audience"] ??
                      throw new InvalidOperationException("Audience configuration is missing");
    string issuer = configuration["Issuer"] ??
                    throw new InvalidOperationException("Issuer configuration is missing");

    string accessToken = string.Empty;

    string? authorization = context.Request.Headers["Authorization"];

    if (authorization != null && authorization.StartsWith("Bearer ", StringComparison.Ordinal))
    {
      accessToken = authorization.Substring("Bearer ".Length).Trim();
    }

    string publicKey = StringUtils.Base64Decode(configuration["PublicKey"] ??
                                                throw new InvalidOperationException(
                                                  "PublicKey configuration is missing"));
    bool authenticated = jwtService.ValidateToken(publicKey, issuer, audience, accessToken);

    if (!authenticated)
    {
      throw new Exception("AccessToken is invalid.");
    }

    JwtSecurityTokenHandler handler = new();
    JwtSecurityToken? token = handler.ReadJwtToken(accessToken);

    string? tenant = context.Request.Headers["X-looplex-tenant"];
    if (string.IsNullOrWhiteSpace(tenant))
    {
      throw new InvalidOperationException(
        "X-looplex-tenant header is missing");
    }

    List<Claim> claims = token.Claims.ToList();

    if (claims.Any(c => c.Type == "name" || c.Type == "email"))
    {
      string? name = claims.FirstOrDefault(c => c.Type == "name")?.Value;
      string? email = claims.FirstOrDefault(c => c.Type == "email")?.Value;

      if (string.IsNullOrWhiteSpace(name) ||
          string.IsNullOrWhiteSpace(email) ||
          string.IsNullOrWhiteSpace(tenant))
        throw new InvalidOperationException(
          "User claims are missing");
      
      context.Items["UserContext"] = new UserContext { Name = name!, Email = email!, Tenant = tenant! };
    }

    await _next(context);
  }
}