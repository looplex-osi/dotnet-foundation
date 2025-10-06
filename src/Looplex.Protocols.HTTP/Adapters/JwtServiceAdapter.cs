using System;
using System.Security.Claims;
using Looplex.Foundation.Adapters;
using Looplex.Protocols.HTTP.Ports;

namespace Looplex.Protocols.HTTP.Adapters;

public class JwtServiceAdapter : IJwtService
{
    private readonly Looplex.Foundation.Adapters.JwtService _jwtService;

    public JwtServiceAdapter()
    {
        _jwtService = new Looplex.Foundation.Adapters.JwtService();
    }

    public string GenerateToken(string privateKey, string issuer, string audience, ClaimsIdentity claimsIdentity,
        TimeSpan expiration)
    {
        return _jwtService.GenerateToken(privateKey, issuer, audience, claimsIdentity, expiration);
    }
}

