using System.Threading;
using System.Threading.Tasks;

namespace Looplex.Foundation.Ports;
/// <summary>
/// Defines the contract for handling the Token Exchange Grant (RFC 8693).
/// </summary>
public interface ITokenExchangeAuthentications
{
  /// <summary>
  /// Issues an access token using the Token Exchange Grant.
  /// </summary>
  Task<string> CreateAccessToken(string json, string authorization, CancellationToken cancellationToken);
}
