using System;
using System.Threading;
using System.Threading.Tasks;

namespace Looplex.Foundation.OAuth2.Entities;

public interface IAuthentications
{
  Task<string> CreateAccessToken(string json, (Guid clientId, string clientSecret) credentials, CancellationToken cancellationToken);
}