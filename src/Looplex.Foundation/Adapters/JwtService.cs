using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

using Looplex.Foundation.Ports;

using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Looplex.Foundation.Adapters;

public sealed class JwtService : IJwtService
{
  public string GenerateToken(
    string privateKey,
    string issuer,
    string audience,
    ClaimsIdentity claimsIdentity,
    TimeSpan expiration)
  {
    using var stringReader = new StringReader(privateKey);
    using var pemReader = new PemReader(stringReader);
    var keyPair = (RsaPrivateCrtKeyParameters)pemReader.ReadObject();
    var rsaParams = DotNetUtilities.ToRSAParameters(keyPair);
    
    using RSA privateKeyRsa = RSA.Create();
    privateKeyRsa.ImportParameters(rsaParams);

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
