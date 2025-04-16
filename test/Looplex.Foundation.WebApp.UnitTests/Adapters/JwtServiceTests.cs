using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Looplex.Foundation.WebApp.Adapters;

namespace Looplex.Foundation.WebApp.UnitTests.Adapters;

[TestClass]
public class JwtServiceTests
{
  private readonly string _audience = "test-audience";
  private readonly string _issuer = "test-issuer";
  private JwtService _jwtService = null!;
  private string _privateKeyPem = null!;

  [TestInitialize]
  public void SetUp()
  {
    _jwtService = new JwtService();

    // Generate RSA key pair
    using RSA rsa = RSA.Create(2048);
    byte[] privateKey = rsa.ExportPkcs8PrivateKey();
    byte[] publicKey = rsa.ExportSubjectPublicKeyInfo();

    _privateKeyPem = ConvertToPem(privateKey, "PRIVATE KEY");
  }

  [TestMethod]
  public void GenerateToken_ValidInputs_ReturnsJwtString()
  {
    // Arrange
    ClaimsIdentity claimsIdentity = new(new[] { new Claim("email", "user@example.com"), new Claim("role", "admin") });

    TimeSpan expiration = TimeSpan.FromMinutes(30);

    // Act
    string token = _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, expiration);

    // Assert
    Assert.IsNotNull(token);
    Assert.IsFalse(string.IsNullOrWhiteSpace(token));
  }

  private static string ConvertToPem(byte[] keyData, string label)
  {
    StringBuilder builder = new();
    builder.AppendLine($"-----BEGIN {label}-----");
    builder.AppendLine(Convert.ToBase64String(keyData, Base64FormattingOptions.InsertLineBreaks));
    builder.AppendLine($"-----END {label}-----");
    return builder.ToString();
  }
}