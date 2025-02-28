namespace Looplex.Foundation.Ports
{
    public interface IAuthorizationServiceFactory
    {
        IAuthorizationService GetService(GrantType grantType);
    }
}