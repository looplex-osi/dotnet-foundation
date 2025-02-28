using System.Threading;
using System.Threading.Tasks;

namespace Looplex.Foundation.Ports
{
    public interface IAuthorizationService
    {
        Task<string> CreateAccessToken(string credentials, CancellationToken cancellationToken);
    }
}