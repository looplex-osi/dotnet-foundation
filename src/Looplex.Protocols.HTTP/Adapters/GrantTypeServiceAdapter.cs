using Looplex.Protocols.HTTP.Ports;

namespace Looplex.Protocols.HTTP.Adapters;

public class GrantTypeServiceAdapter : IGrantTypeService
{
    public string ClientCredentials => "client_credentials";
    public string AuthorizationCode => "authorization_code";
    public string RefreshToken => "refresh_token";
    public string TokenExchange => "urn:ietf:params:oauth:grant-type:token-exchange";
    public string Password => "password";
}
