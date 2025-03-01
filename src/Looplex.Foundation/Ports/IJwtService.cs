using System;
using System.Security.Claims;

namespace Looplex.Foundation.Ports;

public interface IJwtService
{
    string GenerateToken(string privateKey, string issuer, string audience, ClaimsIdentity claimsIdentity,
        TimeSpan expiration);

    string GetUserIdFromToken(string accessToken);
        
    bool ValidateToken(string publicKey, string issuer, string audience, string token);
}