using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Looplex.Foundation.OAuth2;
using Looplex.Foundation.Ports;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using NSubstitute;

namespace Looplex.Foundation.UnitTests.OAuth2;

[TestClass]
public class AuthenticationMiddlewareTests
{
  private IConfiguration _configuration = null!;
  private HttpContext _context = null!;
  private IJwtService _jwtService = null!;
  private AuthenticationMiddleware _middleware = null!;
  private RequestDelegate _next = null!;
  private IServiceProvider _serviceProvider = null!;

  [TestInitialize]
  public void Setup()
  {
    _next = Substitute.For<RequestDelegate>();
    _serviceProvider = Substitute.For<IServiceProvider>();
    _configuration = Substitute.For<IConfiguration>();
    _jwtService = Substitute.For<IJwtService>();

    // Mock configuration values
    _configuration["Audience"].Returns("test-audience");
    _configuration["Issuer"].Returns("test-issuer");
    _configuration["PublicKey"]
      .Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes("test-public-key")));

    _middleware = new AuthenticationMiddleware(_next);

    // Create mock HTTP context
    _context = new DefaultHttpContext();
    _context.Request.Headers["Authorization"] = "Bearer test-token";
    _context.Items.Clear();
  }

  [TestMethod]
  public async Task Invoke_ValidToken_ShouldAuthenticateUser()
  {
    // Arrange
    List<Claim> claims = new List<Claim> { new("name", "John Doe"), new("email", "john.doe@example.com") };
    string expectedIssuer = "test-issuer";
    string expectedAudience = "test-audience";
    string expectedPublicKey = "test-public-key"; // Ensure this matches your actual test value
    string expectedToken = GenerateJwtToken(expectedIssuer, expectedAudience, claims);
    _configuration["Audience"].Returns(expectedAudience);
    _configuration["Issuer"].Returns(expectedIssuer);
    _configuration["PublicKey"]
      .Returns(Convert.ToBase64String(Encoding.UTF8.GetBytes(expectedPublicKey)));
    string expectedTenant = "TenantTest";

    _context.Request.Headers["Authorization"] = $"Bearer {expectedToken}";
    _context.Request.Headers["X-looplex-tenant"] = $"{expectedTenant}";

    _jwtService.ValidateToken(
      Arg.Is<string>(publicKey => publicKey == expectedPublicKey),
      Arg.Is<string>(issuer => issuer == expectedIssuer),
      Arg.Is<string>(audience => audience == expectedAudience),
      Arg.Is<string>(token => token == expectedToken)
    ).Returns(true); // Ensure the ValidateToken method is called with the correct values

    // Act
    await _middleware.Invoke(_context, _configuration, _jwtService);

    // Assert
    Assert.IsTrue(_context.Items.ContainsKey("UserContext"), "UserContext should be added to HttpContext.Items");

    UserContext? userContext = _context.Items["UserContext"] as UserContext;
    Assert.IsNotNull(userContext, "UserContext should not be null");
    Assert.AreEqual("John Doe", userContext.Name, "Name should be 'John Doe'");
    Assert.AreEqual("john.doe@example.com", userContext.Email, "Email should be 'john.doe@example.com'");
    Assert.AreEqual(expectedTenant, userContext.Tenant, $"Tenant should be '{expectedTenant}'");

    // Verify that ValidateToken was called exactly once with the correct parameters
    _jwtService.Received(1).ValidateToken(
      Arg.Is<string>(publicKey => publicKey == expectedPublicKey),
      Arg.Is<string>(issuer => issuer == expectedIssuer),
      Arg.Is<string>(audience => audience == expectedAudience),
      Arg.Is<string>(isToken => isToken == expectedToken)
    );

    await _next.Received(1).Invoke(_context); // Ensure next middleware is called
  }

  [TestMethod]
  public async Task Invoke_InvalidToken_ShouldThrowException()
  {
    // Arrange
    _jwtService.ValidateToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(false);

    // Act & Assert
    await Assert.ThrowsExceptionAsync<Exception>(
      async () => await _middleware.Invoke(_context, _configuration, _jwtService),
      "AccessToken is invalid.");
    await _next.DidNotReceive().Invoke(_context); // Ensure next middleware is NOT called
  }

  [TestMethod]
  public async Task Invoke_MissingAuthorizationHeader_ShouldThrowException()
  {
    // Arrange
    _context.Request.Headers.Remove("Authorization");

    // Act & Assert
    await Assert.ThrowsExceptionAsync<Exception>(
      async () => await _middleware.Invoke(_context, _configuration, _jwtService),
      "AccessToken is invalid.");
    await _next.DidNotReceive().Invoke(_context); // Ensure next middleware is NOT called
  }

  public static string GenerateJwtToken(string issuer, string audience, IEnumerable<Claim> claims)
  {
    byte[] skey = new byte[32];
    RandomNumberGenerator.Create().GetBytes(skey);
    SymmetricSecurityKey securityKey = new(skey) { KeyId = Guid.NewGuid().ToString() };
    SigningCredentials signingCredentials = new(securityKey, SecurityAlgorithms.HmacSha256);
    return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, audience, claims, null,
      DateTime.UtcNow.AddMinutes(20), signingCredentials));
  }
}