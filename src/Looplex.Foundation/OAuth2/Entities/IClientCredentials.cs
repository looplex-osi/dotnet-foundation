using System;
using System.Threading;
using System.Threading.Tasks;

namespace Looplex.Foundation.OAuth2.Entities;

public interface IClientCredentials
{
    Task<string> QueryAsync(int startIndex, int itemsPerPage, CancellationToken cancellationToken);
    Task<string> RetrieveAsync(string id, CancellationToken cancellationToken);
    Task<string> RetrieveAsync(Guid clientId, string clientSecret, CancellationToken cancellationToken);
    Task<string> CreateAsync(string json, CancellationToken cancellationToken);
    Task<string> UpdateAsync(string id, string json, CancellationToken cancellationToken);
    Task<string> PatchAsync(string id, string json, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);}