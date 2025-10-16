using System.Threading;
using System.Threading.Tasks;

namespace Looplex.OAuth2.Entities;

public interface IAuthentications
{
  Task<string> CreateAccessToken(string json, string authentication, CancellationToken cancellationToken);
}
