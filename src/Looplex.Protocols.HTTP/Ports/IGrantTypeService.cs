namespace Looplex.Protocols.HTTP.Ports;

public interface IGrantTypeService
{
    string ClientCredentials { get; }
    string AuthorizationCode { get; }
    string RefreshToken { get; }
    string TokenExchange { get; }
    string Password { get; }
}

