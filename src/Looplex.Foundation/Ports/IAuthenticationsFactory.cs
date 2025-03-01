using Looplex.Foundation.OAuth2.Entities;

namespace Looplex.Foundation.Ports;

public interface IAuthenticationsFactory
{
    IAuthentications GetService(GrantType grantType);
}