using System.Threading;
using System.Threading.Tasks;

namespace Looplex.Foundation.OAuth2.Entities;

public interface IAuthorizations
{
    Task<string> CreateAccessToken(string json, CancellationToken cancellationToken);
}