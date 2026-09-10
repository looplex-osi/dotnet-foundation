using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

// A classe OAuth2 colide com o namespace Looplex.Foundation.OAuth2 dentro deste namespace de testes
using OAuth2Middleware = Looplex.Foundation.WebApp.Middlewares.OAuth2;

namespace Looplex.Foundation.WebApp.UnitTests.Middlewares;

/// <summary>
/// The tenant of a request is resolved, in order, from the token claim, the <c>{tenant}</c> route value and the
/// <c>X-looplex-tenant</c> header. Hosts that cannot send custom headers (remote MCP connectors) use the route.
/// </summary>
[TestClass]
public class OAuth2TenantResolutionTests
{
  [TestMethod]
  public async Task TokenClaim_Wins_OverRouteAndHeader()
  {
    TokenValidatedContext context = Context(claimTenant: "claim.tenant", routeTenant: "route.tenant", headerTenant: "header.tenant");

    await OAuth2Middleware.ResolveTenantOnTokenValidated(context);

    Assert.IsNull(context.Result?.Failure);
    Assert.AreEqual("claim.tenant", context.Principal!.FindFirst(OAuth2Middleware.TenantClaim)!.Value);
    Assert.AreEqual(1, context.Principal.FindAll(OAuth2Middleware.TenantClaim).Count(), "claim must not be duplicated");
  }

  [TestMethod]
  public async Task Route_Wins_OverHeader_WhenNoClaim()
  {
    TokenValidatedContext context = Context(claimTenant: null, routeTenant: "route.tenant", headerTenant: "header.tenant");

    await OAuth2Middleware.ResolveTenantOnTokenValidated(context);

    Assert.AreEqual("route.tenant", context.Principal!.FindFirst(OAuth2Middleware.TenantClaim)!.Value);
  }

  [TestMethod]
  public async Task Header_IsUsed_WhenNoClaimOrRoute()
  {
    TokenValidatedContext context = Context(claimTenant: null, routeTenant: null, headerTenant: "header.tenant");

    await OAuth2Middleware.ResolveTenantOnTokenValidated(context);

    Assert.AreEqual("header.tenant", context.Principal!.FindFirst(OAuth2Middleware.TenantClaim)!.Value);
  }

  [TestMethod]
  public async Task Fails_WhenTenantIsMissingEverywhere()
  {
    TokenValidatedContext context = Context(claimTenant: null, routeTenant: null, headerTenant: null);

    await OAuth2Middleware.ResolveTenantOnTokenValidated(context);

    Assert.IsNotNull(context.Result?.Failure);
    StringAssert.Contains(context.Result!.Failure!.Message, "tenant is required");
  }

  private static TokenValidatedContext Context(string? claimTenant, string? routeTenant, string? headerTenant)
  {
    DefaultHttpContext http = new();
    if (routeTenant is not null) http.Request.RouteValues[OAuth2Middleware.TenantRouteValue] = routeTenant;
    if (headerTenant is not null) http.Request.Headers[OAuth2Middleware.TenantHeader] = headerTenant;

    List<Claim> claims = [new Claim("email", "user@tenant.test")];
    if (claimTenant is not null) claims.Add(new Claim(OAuth2Middleware.TenantClaim, claimTenant));

    AuthenticationScheme scheme = new(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
    return new TokenValidatedContext(http, scheme, new JwtBearerOptions())
    {
      Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")),
    };
  }
}
