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

  public bool ValidateToken(
    string publicKey,
    string issuer,
    string audience,
    string token)
  {
    if (string.IsNullOrEmpty(token))
    {
      return false;
    }

    using RSA publicKeyRsa = RSA.Create();
    try
    {
      publicKeyRsa.ImportFromPem(publicKey);
    }
    catch (Exception)
    {
      return false;
    }

    JwtSecurityTokenHandler tokenHandler = new();
    TokenValidationParameters validationParameters = new()
    {
      ValidateIssuerSigningKey = true,
      IssuerSigningKey =
        new RsaSecurityKey(publicKeyRsa)
        {
          CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        },
      ValidateIssuer = true,
      ValidIssuer = issuer,
      ValidateAudience = true,
      ValidAudience = audience,
      ValidateLifetime = true,
      ClockSkew = TimeSpan.Zero
    };

    try
    {
      tokenHandler.ValidateToken(token, validationParameters, out _);
      return true;
    }
    catch
    {
      return false;
    }
  }
}