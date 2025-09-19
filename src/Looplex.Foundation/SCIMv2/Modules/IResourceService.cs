using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2.Entities;

namespace Looplex.Foundation.SCIMv2.Modules;

/// <summary>
/// Base interface for resource services
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Collection name for this service
    /// </summary>
    string CollectionName { get; }
}

/// <summary>
/// Interface for resource-specific services
/// Provides CRUD operations for specific resource types
/// </summary>
/// <typeparam name="T">Resource type implementing IResource</typeparam>
public interface IResourceService<T> : IResourceService where T : IResource
{
    /// <summary>
    /// Query resources with pagination and filtering
    /// </summary>
    /// <param name="startIndex">Starting index for pagination</param>
    /// <param name="count">Number of items to return</param>
    /// <param name="filter">SCIM filter expression</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortOrder">Sort order (ascending/descending)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of resources and total count</returns>
    Task<(IList<T> Resources, int TotalCount)> QueryAsync(int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new resource
    /// </summary>
    /// <param name="resource">Resource to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID of the created resource</returns>
    Task<Guid> CreateAsync(T resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a specific resource by ID
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resource or null if not found</returns>
    Task<T?> RetrieveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replace a resource completely
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <param name="resource">New resource data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> ReplaceAsync(Guid id, T resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a resource using PATCH operations
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <param name="resource">Resource data</param>
    /// <param name="patches">JSON Patch operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateAsync(Guid id, T resource, PatchOperation[] patches, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a resource
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
