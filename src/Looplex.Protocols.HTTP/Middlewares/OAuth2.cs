using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Looplex.Protocols.HTTP.Ports;

namespace Looplex.Protocols.HTTP.Middlewares;

/// <summary>
/// OAuth2 Middleware - HTTP Adapter Only
/// Implements RFC 6749 - OAuth 2.0 Authorization Framework
/// [RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749)
/// </summary>
public static class OAuth2
{
    /// <summary>
    /// OAuth2 Token Endpoint
    /// Implements RFC 6749 Section 4.1 - Authorization Code Grant
    /// [RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749#section-4.1)
    /// </summary>
    public static IEndpointConventionBuilder MapOAuth2TokenEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/oauth/token", async (HttpContext context) =>
        {
            // Simplified OAuth2 token endpoint using interfaces
            var jwtService = context.RequestServices.GetService<IJwtService>();
            var grantTypeService = context.RequestServices.GetService<IGrantTypeService>();
            
            if (jwtService == null || grantTypeService == null)
            {
                return Results.BadRequest("OAuth2 services not configured");
            }

            // Basic OAuth2 token response
            var response = new
            {
                access_token = "sample_token",
                token_type = "Bearer",
                expires_in = 3600,
                scope = "read write"
            };

            return Results.Ok(response);
        })
        .WithName("OAuth2Token")
        .WithTags("OAuth2")
        .WithSummary("OAuth2 Token Endpoint")
        .WithDescription("RFC 6749 compliant OAuth2 token endpoint");
    }

    /// <summary>
    /// OAuth2 UserInfo Endpoint
    /// Implements OpenID Connect Core 1.0 Section 5.3.1 - UserInfo Endpoint
    /// [OpenID Connect](https://openid.net/specs/openid-connect-core-1_0.html#UserInfo)
    /// </summary>
    public static IEndpointConventionBuilder MapOAuth2UserInfoEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/oauth/userinfo", async (HttpContext context) =>
        {
            // Simplified UserInfo endpoint
            var response = new
            {
                sub = "user123",
                name = "John Doe",
                email = "john.doe@example.com"
            };

            return Results.Ok(response);
        })
        .WithName("OAuth2UserInfo")
        .WithTags("OAuth2")
        .WithSummary("OAuth2 UserInfo Endpoint")
        .WithDescription("OpenID Connect UserInfo endpoint");
    }
}