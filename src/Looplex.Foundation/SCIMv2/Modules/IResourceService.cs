using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2.Entities;

namespace Looplex.Foundation.SCIMv2.Modules;

/// <summary>
/// Base interface for SCIM resource services providing collection identification.
/// Defines the contract for service layer components in SCIM v2.0 architecture.
/// Implements Service Layer Pattern for business logic abstraction.
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Gets the collection name for this service instance.
    /// Used for SCIM endpoint routing and resource identification.
    /// </summary>
    /// <value>Collection name string (e.g., "Users", "Groups", "Notes")</value>
    string CollectionName { get; }
}

/// <summary>
/// Generic interface for SCIM resource service operations.
/// Provides complete CRUD operations for specific resource types with SCIM v2.0 compliance.
/// Implements RFC 7644 protocol operations for resource management.
/// [RFC 7644 Section 3.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4) - SCIM Protocol Operations
/// </summary>
/// <typeparam name="T">Resource type implementing IResource interface</typeparam>
public interface IResourceService<T> : IResourceService where T : IResource
{
    /// <summary>
    /// Queries resources with pagination, filtering, and sorting support.
    /// Implements RFC 7644 Section 3.4.2 - Query Resources
    /// [RFC 7644 Section 3.4.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2)
    /// Supports SCIM filter expressions per RFC 7644 Section 3.4.2.2
    /// [RFC 7644 Section 3.4.2.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2)
    /// </summary>
    /// <param name="startIndex">Starting index for pagination (1-based)</param>
    /// <param name="count">Maximum number of resources to return</param>
    /// <param name="filter">SCIM filter expression (optional)</param>
    /// <param name="sortBy">Field name for sorting (optional)</param>
    /// <param name="sortOrder">Sort order: "ascending" or "descending" (optional)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Tuple containing list of resources and total count for pagination</returns>
    Task<(IList<T> Resources, int TotalCount)> QueryAsync(int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new resource in the specified collection.
    /// Implements RFC 7644 Section 3.4.1 - Create Resource
    /// [RFC 7644 Section 3.4.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.1)
    /// </summary>
    /// <param name="resource">Resource instance to create</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Unique identifier of the created resource</returns>
    Task<Guid> CreateAsync(T resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific resource by its unique identifier.
    /// Implements RFC 7644 Section 3.4.3 - Retrieve Resource
    /// [RFC 7644 Section 3.4.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.3)
    /// </summary>
    /// <param name="id">Unique identifier of the resource to retrieve</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Resource instance or null if not found</returns>
    Task<T?> RetrieveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a resource completely using PUT semantics.
    /// Implements RFC 7644 Section 3.4.4 - Update Resource (PUT)
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// Performs complete resource replacement
    /// </summary>
    /// <param name="id">Unique identifier of the resource to replace</param>
    /// <param name="resource">Complete resource instance for replacement</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if resource was successfully replaced, false otherwise</returns>
    Task<bool> ReplaceAsync(Guid id, T resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a resource completely using non-generic interface (polymorphic version).
    /// Implements RFC 7644 Section 3.4.4 - Update Resource (PUT)
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// Provides polymorphic resource replacement capability
    /// </summary>
    /// <param name="id">Unique identifier of the resource to replace</param>
    /// <param name="resource">Complete resource instance for replacement</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if resource was successfully replaced, false otherwise</returns>
    Task<bool> ReplaceAsync(Guid id, IResource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a resource using JSON Patch operations for partial updates.
    /// Implements RFC 7644 Section 3.4.4 - Update Resource (PATCH)
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// Uses RFC 6902 (JSON Patch) Section 4 - Operations
    /// [RFC 6902 Section 4](https://datatracker.ietf.org/doc/html/rfc6902#section-4)
    /// </summary>
    /// <param name="id">Unique identifier of the resource to update</param>
    /// <param name="resource">Current resource instance</param>
    /// <param name="patches">Array of JSON Patch operations to apply</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if patches were successfully applied, false otherwise</returns>
    Task<bool> UpdateAsync(Guid id, T resource, PatchOperation[] patches, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a resource from the specified collection.
    /// Implements RFC 7644 Section 3.4.5 - Delete Resource
    /// [RFC 7644 Section 3.4.5](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.5)
    /// </summary>
    /// <param name="id">Unique identifier of the resource to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if resource was successfully deleted, false otherwise</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}


