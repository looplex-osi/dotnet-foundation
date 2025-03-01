using Looplex.Foundation.OAuth2.Entities;

namespace Looplex.Foundation.Ports;

public interface IAuthorizationsFactory
{
    IAuthorizations GetService(GrantType grantType);
}