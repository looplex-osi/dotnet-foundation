using System.Security.Claims;
using System.Text;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

using NSubstitute;

namespace Looplex.Foundation.UnitTests.Entities;

/// <summary>
/// RFC 8693 token exchange: the optional <c>resource</c> parameter names the tenant the token is for and becomes the
/// <c>tenant</c> claim, so the token cannot be replayed against another tenant.
/// </summary>
[TestClass]
public class TokenExchangeTenantClaimTests
{
  private HttpClient _httpClient = null!;
  private IConfiguration _configuration = null!;
  private IJwtService _jwtService = null!;

  [TestInitialize]
  public void Setup()
  {
    _configuration = Substitute.For<IConfiguration>();
    _jwtService = Substitute.For<IJwtService>();
    _httpClient = new HttpClient(new TokenExchangeAuthenticationsTests.SuccessHttpMessageHandlerMock());

    _configuration["TokenExpirationTimeInMinutes"].Returns("20");
    _configuration["Audience"].Returns("audience");
    _configuration["Issuer"].Returns("issuer");
    _configuration["PublicKey"].Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes(RsaKeys.PublicKey)));
    _configuration["PrivateKey"].Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes(RsaKeys.PrivateKey)));
    _configuration["OicdUserInfoEndpoint"].Returns("https://graph.microsoft.com/oidc/userinfo");
    _jwtService.GenerateToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ClaimsIdentity>(), Arg.Any<TimeSpan>()).Returns("jwt");
  }

  [TestMethod]
  public async Task CreateAccessToken_WithResource_AddsTenantClaim()
  {
    string credentials = JsonConvert.SerializeObject(new
    {
      grant_type = "urn:ietf:params:oauth:grant-type:token-exchange",
      subject_token = "validToken",
      subject_token_type = "urn:ietf:params:oauth:token-type:access_token",
      resource = "dev.looplex.com.br",
    });
    TokenExchangeAuthentications service = new(new List<IPlugin>(), _configuration, _jwtService, _httpClient);

    await service.CreateAccessToken(credentials, "", CancellationToken.None);

    _jwtService.Received(1).GenerateToken(Arg.Any<string>(), "issuer", "audience",
      Arg.Is<ClaimsIdentity>(c => c.HasClaim(TokenExchangeAuthentications.TenantClaim, "dev.looplex.com.br") && c.FindFirst("email") != null),
      Arg.Any<TimeSpan>());
  }

  [TestMethod]
  public async Task CreateAccessToken_WithoutResource_HasNoTenantClaim()
  {
    string credentials = JsonConvert.SerializeObject(new
    {
      grant_type = "urn:ietf:params:oauth:grant-type:token-exchange",
      subject_token = "validToken",
      subject_token_type = "urn:ietf:params:oauth:token-type:access_token",
    });
    TokenExchangeAuthentications service = new(new List<IPlugin>(), _configuration, _jwtService, _httpClient);

    await service.CreateAccessToken(credentials, "", CancellationToken.None);

    _jwtService.Received(1).GenerateToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
      Arg.Is<ClaimsIdentity>(c => c.FindFirst(TokenExchangeAuthentications.TenantClaim) == null),
      Arg.Any<TimeSpan>());
  }

  [TestMethod]
  public async Task CreateAccessToken_BlankResource_IsRejected()
  {
    string credentials = JsonConvert.SerializeObject(new
    {
      grant_type = "urn:ietf:params:oauth:grant-type:token-exchange",
      subject_token = "validToken",
      subject_token_type = "urn:ietf:params:oauth:token-type:access_token",
      resource = "   ",
    });
    TokenExchangeAuthentications service = new(new List<IPlugin>(), _configuration, _jwtService, _httpClient);

    Exception exception = await Assert.ThrowsExceptionAsync<Exception>(() => service.CreateAccessToken(credentials, "", CancellationToken.None));
    Assert.AreEqual("resource is invalid.", exception.Message);
  }
}
