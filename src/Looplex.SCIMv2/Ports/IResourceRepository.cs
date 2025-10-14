using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Looplex.SCIMv2.Ports;

/// <summary>
/// Generic repository interface for SCIM resource data persistence operations.
/// Implements Repository Pattern for abstracting data access layer.
/// Provides CRUD operations with pagination and filtering support per RFC 7644.
/// [RFC 7644 Section 3.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4) - SCIM Protocol Operations
/// </summary>
/// <typeparam name="T">Resource type implementing SCIM resource contract</typeparam>
public interface IResourceRepository<T> where T : class
{
    /// <summary>
    /// Retrieves a specific resource by its unique identifier.
    /// Implements RFC 7644 Section 3.4.3 - Retrieve Resource
    /// [RFC 7644 Section 3.4.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.3)
    /// </summary>
    /// <param name="id">Unique identifier of the resource to retrieve</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Resource instance or null if not found</returns>
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new resource in the data store.
    /// Implements RFC 7644 Section 3.4.1 - Create Resource
    /// [RFC 7644 Section 3.4.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.1)
    /// </summary>
    /// <param name="resource">Resource instance to create</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created resource with generated identifier and metadata</returns>
    Task<T> CreateAsync(T resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing resource in the data store.
    /// Implements RFC 7644 Section 3.4.4 - Update Resource (PUT semantics)
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// </summary>
    /// <param name="id">Unique identifier of the resource to update</param>
    /// <param name="resource">Updated resource instance</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated resource instance</returns>
    Task<T> UpdateAsync(string id, T resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a resource from the data store.
    /// Implements RFC 7644 Section 3.4.5 - Delete Resource
    /// [RFC 7644 Section 3.4.5](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.5)
    /// </summary>
    /// <param name="id">Unique identifier of the resource to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if resource was successfully deleted, false otherwise</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries resources with pagination and filtering support.
    /// Implements RFC 7644 Section 3.4.2 - Query Resources
    /// [RFC 7644 Section 3.4.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2)
    /// Supports SCIM filter expressions per RFC 7644 Section 3.4.2.2
    /// [RFC 7644 Section 3.4.2.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2)
    /// </summary>
    /// <param name="startIndex">Starting index for pagination (1-based)</param>
    /// <param name="count">Maximum number of resources to return</param>
    /// <param name="filter">SCIM filter expression (optional)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Tuple containing list of resources and total count for pagination</returns>
    Task<(IList<T> Resources, int TotalCount)> QueryAsync(
        int startIndex, int count, string? filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries resources with pagination, filtering, sorting, and attribute filtering support.
    /// Implements RFC 7644 Section 3.4.2 - Query Resources with Sorting
    /// [RFC 7644 Section 3.4.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2)
    /// Supports SCIM filter expressions and sorting per RFC 7644 Section 3.4.2.3
    /// [RFC 7644 Section 3.4.2.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.3)
    /// Supports attribute filtering per RFC 7644 Section 3.4.2.3
    /// [RFC 7644 Section 3.4.2.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.3)
    /// </summary>
    /// <param name="startIndex">Starting index for pagination (1-based)</param>
    /// <param name="count">Maximum number of resources to return</param>
    /// <param name="filter">SCIM filter expression (optional)</param>
    /// <param name="sortBy">Field name for sorting (optional)</param>
    /// <param name="sortOrder">Sort order: "ascending" or "descending" (optional)</param>
    /// <param name="attributes">Comma-separated list of attributes to return (optional)</param>
    /// <param name="excludedAttributes">Comma-separated list of attributes to exclude (optional)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Tuple containing list of resources and total count for pagination</returns>
    Task<(IList<T> Resources, int TotalCount)> QueryAsync(
        int startIndex, int count, string? filter, string? sortBy, string? sortOrder, string? attributes, string? excludedAttributes, CancellationToken cancellationToken = default);
}
