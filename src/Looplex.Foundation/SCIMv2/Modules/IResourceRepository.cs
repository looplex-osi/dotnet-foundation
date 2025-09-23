using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Looplex.Foundation.SCIMv2.Modules
{
    /// <summary>
    /// Repository interface for SCIMv2 resources
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    public interface IResourceRepository<T> where T : class
    {
        /// <summary>
        /// Query resources with filtering and pagination
        /// </summary>
        Task<(IList<T> Resources, int TotalCount)> QueryAsync(int startIndex, int count, string? filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a resource by ID
        /// </summary>
        Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a new resource
        /// </summary>
        Task<T> CreateAsync(T resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing resource
        /// </summary>
        Task<T> UpdateAsync(string id, T resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a resource
        /// </summary>
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}

