using System.Threading;
using System.Threading.Tasks;

namespace Looplex.Foundation.OAuth2.Entities;

public interface IAuthentications
{
  Task<string> CreateAccessToken(string json, CancellationToken cancellationToken);
}