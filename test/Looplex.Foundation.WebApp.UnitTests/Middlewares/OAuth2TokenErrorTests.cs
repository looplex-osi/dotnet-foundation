using System.Text;

using Looplex.Foundation.OAuth2.Entities;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json.Linq;

// A classe OAuth2 colide com o namespace Looplex.Foundation.OAuth2 dentro deste namespace de testes
using OAuth2Middleware = Looplex.Foundation.WebApp.Middlewares.OAuth2;

namespace Looplex.Foundation.WebApp.UnitTests.Middlewares;

/// <summary>The token endpoint answers expected failures per RFC 6749 §5.2 instead of a 500.</summary>
[TestClass]
public class OAuth2TokenErrorTests
{
  [TestMethod]
  public async Task UnknownGrantType_Returns400_UnsupportedGrantType()
  {
    DefaultHttpContext context = Context("grant_type=nope");

    await OAuth2Middleware.TokenMiddleware(context);

    Assert.AreEqual(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    Assert.AreEqual("no-store", context.Response.Headers.CacheControl.ToString());
    Assert.AreEqual("unsupported_grant_type", Body(context)["error"]!.Value<string>());
    Assert.IsFalse(context.Response.Headers.ContainsKey("WWW-Authenticate"));
  }

  [TestMethod]
  public async Task InvalidClient_Returns401_WithBasicChallenge()
  {
    // client_credentials without an Authorization header
    DefaultHttpContext context = Context("grant_type=client_credentials");

    await OAuth2Middleware.TokenMiddleware(context);

    Assert.AreEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    Assert.AreEqual("Basic", context.Response.Headers.WWWAuthenticate.ToString());
    Assert.AreEqual("invalid_client", Body(context)["error"]!.Value<string>());
  }

  private static DefaultHttpContext Context(string form)
  {
    ServiceCollection services = new();
    services.AddScoped<AuthenticationsFactory>();
    services.AddScoped<ClientCredentialsAuthentications>(_ => new ClientCredentialsAuthentications(
      [], new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), null!, null!));

    DefaultHttpContext context = new() { RequestServices = services.BuildServiceProvider() };
    context.Request.ContentType = "application/x-www-form-urlencoded";
    context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(form));
    context.Response.Body = new MemoryStream();
    return context;
  }

  private static JObject Body(DefaultHttpContext context)
  {
    context.Response.Body.Position = 0;
    return JObject.Parse(new StreamReader(context.Response.Body).ReadToEnd());
  }
}
