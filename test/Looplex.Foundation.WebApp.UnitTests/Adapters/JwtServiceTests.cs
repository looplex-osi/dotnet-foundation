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
  private string _publicKeyPem = null!;

  [TestInitialize]
  public void SetUp()
  {
    _jwtService = new JwtService();

    // Generate RSA key pair
    using RSA rsa = RSA.Create(2048);
    byte[] privateKey = rsa.ExportPkcs8PrivateKey();
    byte[] publicKey = rsa.ExportSubjectPublicKeyInfo();

    _privateKeyPem = ConvertToPem(privateKey, "PRIVATE KEY");
    _publicKeyPem = ConvertToPem(publicKey, "PUBLIC KEY");
  }

  [TestMethod]
  public void GenerateToken_ValidInputs_ReturnsJwtString()
  {
    // Arrange
    ClaimsIdentity claimsIdentity = new(new[]
    {
      new Claim("email", "user@example.com"), new Claim("role", "admin")
    });

    TimeSpan expiration = TimeSpan.FromMinutes(30);

    // Act
    string token = _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, expiration);

    // Assert
    Assert.IsNotNull(token);
    Assert.IsFalse(string.IsNullOrWhiteSpace(token));
  }

  [TestMethod]
  public void ValidateToken_ValidToken_ReturnsTrue()
  {
    // Arrange
    ClaimsIdentity claimsIdentity = new(new[] { new Claim("email", "user@example.com") });

    string token =
      _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, TimeSpan.FromMinutes(30));

    // Act
    bool isValid = _jwtService.ValidateToken(_publicKeyPem, _issuer, _audience, token);

    // Assert
    Assert.IsTrue(isValid);
  }

  [TestMethod]
  public void ValidateToken_InvalidToken_ReturnsFalse()
  {
    // Arrange
    string invalidToken = "invalid.jwt.token";

    // Act
    bool isValid = _jwtService.ValidateToken(_publicKeyPem, _issuer, _audience, invalidToken);

    // Assert
    Assert.IsFalse(isValid);
  }

  [TestMethod]
  public void ValidateToken_TamperedToken_ReturnsFalse()
  {
    // Arrange
    ClaimsIdentity claimsIdentity = new(new[] { new Claim("email", "user@example.com") });

    string token =
      _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, TimeSpan.FromMinutes(30));
    string tamperedToken = token.Substring(0, token.Length - 1) + "a"; // Modify last character

    // Act
    bool isValid = _jwtService.ValidateToken(_publicKeyPem, _issuer, _audience, tamperedToken);

    // Assert
    Assert.IsFalse(isValid);
  }

  [TestMethod]
  public void ValidateToken_ExpiredToken_ReturnsFalse()
  {
    // Arrange
    ClaimsIdentity claimsIdentity = new(new[] { new Claim("email", "user@example.com") });

    string expiredToken =
      _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, TimeSpan.FromMilliseconds(1));

    // Act
    Thread.Sleep(2);
    bool isValid = _jwtService.ValidateToken(_publicKeyPem, _issuer, _audience, expiredToken);

    // Assert
    Assert.IsFalse(isValid);
  }

  [TestMethod]
  public void ValidateToken_InvalidPublicKey_ReturnsFalse()
  {
    // Arrange
    ClaimsIdentity claimsIdentity = new(new[] { new Claim("email", "user@example.com") });

    string token =
      _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, TimeSpan.FromMinutes(30));

    // Use a different RSA key pair
    using RSA rsa = RSA.Create(2048);
    string invalidPublicKey = ConvertToPem(rsa.ExportRSAPublicKey(), "PUBLIC KEY");

    // Act
    bool isValid = _jwtService.ValidateToken(invalidPublicKey, _issuer, _audience, token);

    // Assert
    Assert.IsFalse(isValid);
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