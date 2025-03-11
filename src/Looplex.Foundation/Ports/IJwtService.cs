using System;
using System.Security.Claims;

namespace Looplex.Foundation.Ports;

public interface IJwtService
{
  string GenerateToken(string privateKey, string issuer, string audience, ClaimsIdentity claimsIdentity,
    TimeSpan expiration);
}