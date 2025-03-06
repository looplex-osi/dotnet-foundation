using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Looplex.Foundation.WebApp.Adapters;

namespace Looplex.Foundation.WebApp.UnitTests.Adapters;

[TestClass]
public class JwtServiceTests
{
    private JwtService _jwtService = null!;
    private string _privateKeyPem = null!;
    private string _publicKeyPem = null!;
    private readonly string _issuer = "test-issuer";
    private readonly string _audience = "test-audience";

    [TestInitialize]
    public void SetUp()
    {
        _jwtService = new JwtService();

        // Generate RSA key pair
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKey(); 
        var publicKey = rsa.ExportSubjectPublicKeyInfo();

        _privateKeyPem = ConvertToPem(privateKey, "PRIVATE KEY");
        _publicKeyPem = ConvertToPem(publicKey, "PUBLIC KEY");
    }

    [TestMethod]
    public void GenerateToken_ValidInputs_ReturnsJwtString()
    {
        // Arrange
        var claimsIdentity = new ClaimsIdentity(new[]
        {
            new Claim("email", "user@example.com"),
            new Claim("role", "admin")
        });

        var expiration = TimeSpan.FromMinutes(30);

        // Act
        var token = _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, expiration);

        // Assert
        Assert.IsNotNull(token);
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
    }

    [TestMethod]
    public void ValidateToken_ValidToken_ReturnsTrue()
    {
        // Arrange
        var claimsIdentity = new ClaimsIdentity(new[]
        {
            new Claim("email", "user@example.com")
        });

        var token = _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, TimeSpan.FromMinutes(30));

        // Act
        var isValid = _jwtService.ValidateToken(_publicKeyPem, _issuer, _audience, token);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void ValidateToken_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var isValid = _jwtService.ValidateToken(_publicKeyPem, _issuer, _audience, invalidToken);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void ValidateToken_TamperedToken_ReturnsFalse()
    {
        // Arrange
        var claimsIdentity = new ClaimsIdentity(new[]
        {
            new Claim("email", "user@example.com")
        });

        var token = _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, TimeSpan.FromMinutes(30));
        var tamperedToken = token.Substring(0, token.Length - 1) + "a"; // Modify last character

        // Act
        var isValid = _jwtService.ValidateToken(_publicKeyPem, _issuer, _audience, tamperedToken);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void ValidateToken_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        var claimsIdentity = new ClaimsIdentity(new[]
        {
            new Claim("email", "user@example.com")
        });

        var expiredToken = _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, TimeSpan.FromMilliseconds(1));

        // Act
        Thread.Sleep(2);
        var isValid = _jwtService.ValidateToken(_publicKeyPem, _issuer, _audience, expiredToken);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void ValidateToken_InvalidPublicKey_ReturnsFalse()
    {
        // Arrange
        var claimsIdentity = new ClaimsIdentity(new[]
        {
            new Claim("email", "user@example.com")
        });

        var token = _jwtService.GenerateToken(_privateKeyPem, _issuer, _audience, claimsIdentity, TimeSpan.FromMinutes(30));

        // Use a different RSA key pair
        using var rsa = RSA.Create(2048);
        var invalidPublicKey = ConvertToPem(rsa.ExportRSAPublicKey(), "PUBLIC KEY");

        // Act
        var isValid = _jwtService.ValidateToken(invalidPublicKey, _issuer, _audience, token);

        // Assert
        Assert.IsFalse(isValid);
    }

    private static string ConvertToPem(byte[] keyData, string label)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"-----BEGIN {label}-----");
        builder.AppendLine(Convert.ToBase64String(keyData, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine($"-----END {label}-----");
        return builder.ToString();
    }
}