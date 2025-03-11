using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

using Looplex.Foundation.Ports;

using Microsoft.IdentityModel.Tokens;

namespace Looplex.Foundation.WebApp.Adapters;

public sealed class JwtService : IJwtService
{
  public string GenerateToken(
    string privateKey,
    string issuer,
    string audience,
    ClaimsIdentity claimsIdentity,
    TimeSpan expiration)
  {
    using RSA privateKeyRsa = RSA.Create();
    privateKeyRsa.ImportFromPem(privateKey);

    JwtSecurityTokenHandler tokenHandler = new();

    SigningCredentials creds = new(new RsaSecurityKey(privateKeyRsa), SecurityAlgorithms.RsaSha256)
    {
      CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
    };

    SecurityTokenDescriptor tokenDescriptor = new()
    {
      Issuer = issuer,
      Audience = audience,
      Subject = claimsIdentity,
      Expires = DateTime.UtcNow.Add(expiration),
      SigningCredentials = creds
    };

    SecurityToken? token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
  }
}