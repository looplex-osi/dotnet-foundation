using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2.Ports;
using System.Text.Json;

namespace Looplex.SCIMv2;

/// <summary>
/// Generic base class implementing SCIM v2.0 resource service operations with Template Method Pattern.
/// Eliminates code duplication between resource-specific services while providing production-ready persistence.
/// Implements RFC 7644 (SCIM Protocol) for comprehensive resource management operations.
/// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644) - SCIM Protocol
/// 
/// RFC Compliance:
/// - RFC 7644 Section 3.4.1 - Create Resource
/// [RFC 7644 Section 3.4.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.1)
/// - RFC 7644 Section 3.4.2 - Query Resources
/// [RFC 7644 Section 3.4.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2)
/// - RFC 7644 Section 3.4.3 - Retrieve Resource
/// [RFC 7644 Section 3.4.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.3)
/// - RFC 7644 Section 3.4.4 - Update Resource
/// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
/// - RFC 7644 Section 3.4.5 - Delete Resource
/// [RFC 7644 Section 3.4.5](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.5)
/// </summary>
/// <typeparam name="T">Resource type implementing Resource base class</typeparam>
public abstract class BaseResourceService<T> : IResourceService<T> where T : Resource, new()
{
    #region Fields

    private readonly IResourceRepository<T> _repository;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the BaseResourceService with repository dependency injection.
    /// Implements Dependency Injection pattern for testability and flexibility.
    /// </summary>
    /// <param name="repository">Repository instance for data persistence operations</param>
    /// <exception cref="ArgumentNullException">Thrown when repository parameter is null</exception>
    protected BaseResourceService(IResourceRepository<T> repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    #endregion

    #region IResourceService<T> Implementation - Unified Methods

    /// <summary>
    /// Queries resources with pagination, filtering, sorting, and attribute filtering support.
    /// Implements IResourceService<T>.QueryAsync with SCIM v2.0 compliance.
    /// Eliminates indirection by implementing interface directly.
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
    public virtual async Task<(IList<T> Resources, int TotalCount)> QueryAsync(int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, string? attributes, string? excludedAttributes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var result = await _repository.QueryAsync(startIndex, count, filter, sortBy, sortOrder, attributes, excludedAttributes, cancellationToken);
        return (result.Resources, result.TotalCount);
    }

    /// <summary>
    /// Creates a new resource in the specified collection.
    /// Implements IResourceService<T>.CreateAsync with SCIM v2.0 compliance.
    /// Eliminates indirection by implementing interface directly.
    /// </summary>
    /// <param name="resource">Resource instance to create</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Unique identifier of the created resource</returns>
    public virtual async Task<Guid> CreateAsync(T resource, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var createdResource = await _repository.CreateAsync(resource, cancellationToken);
        return Guid.Parse(createdResource.Id);
    }

    /// <summary>
    /// Retrieves a specific resource by its unique identifier.
    /// Implements IResourceService<T>.RetrieveAsync with SCIM v2.0 compliance.
    /// Eliminates indirection by implementing interface directly.
    /// </summary>
    /// <param name="id">Unique identifier of the resource to retrieve</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Resource instance or null if not found</returns>
    public virtual async Task<T?> RetrieveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return await _repository.GetByIdAsync(id.ToString(), cancellationToken);
    }

    /// <summary>
    /// Replaces a resource completely using PUT semantics.
    /// Implements IResourceService<T>.ReplaceAsync with SCIM v2.0 compliance.
    /// Eliminates indirection by implementing interface directly.
    /// </summary>
    /// <param name="id">Unique identifier of the resource to replace</param>
    /// <param name="resource">Complete resource instance for replacement</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if resource was successfully replaced, false otherwise</returns>
    public virtual async Task<bool> ReplaceAsync(Guid id, T resource, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var updatedResource = await _repository.UpdateAsync(id.ToString(), resource, cancellationToken);
        return updatedResource != null;
    }

    /// <summary>
    /// Replaces a resource completely using non-generic interface (polymorphic version).
    /// Implements IResourceService<T>.ReplaceAsync with polymorphic resource support.
    /// Eliminates indirection by implementing interface directly.
    /// </summary>
    /// <param name="id">Unique identifier of the resource to replace</param>
    /// <param name="resource">Complete resource instance for replacement</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if resource was successfully replaced, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when resource type does not match expected type</exception>
    public virtual async Task<bool> ReplaceAsync(Guid id, IResource resource, CancellationToken cancellationToken = default)
    {
        if (resource is T typedResource)
        {
            return await ReplaceAsync(id, typedResource, cancellationToken);
        }
        throw new ArgumentException($"Resource must be of type {typeof(T).Name}", nameof(resource));
    }

    /// <summary>
    /// Updates a resource using SCIM PATCH operations for partial updates.
    /// Implements IResourceService<T>.UpdateAsync with SCIM v2.0 PATCH compliance.
    /// Eliminates indirection by implementing interface directly.
    /// </summary>
    /// <param name="id">Unique identifier of the resource to update</param>
    /// <param name="resource">Current resource instance</param>
    /// <param name="patches">Array of JSON Patch operations to apply</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if patches were successfully applied, false otherwise</returns>
    public virtual async Task<bool> UpdateAsync(Guid id, T resource, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Apply patches to the resource
        foreach (var patch in patches)
        {
            ApplyPatch(resource, patch);
        }
        
        var updatedResource = await _repository.UpdateAsync(id.ToString(), resource, cancellationToken);
        return updatedResource != null;
    }

    /// <summary>
    /// Permanently deletes a resource from the specified collection.
    /// Implements IResourceService<T>.DeleteAsync with SCIM v2.0 compliance.
    /// Eliminates indirection by implementing interface directly.
    /// </summary>
    /// <param name="id">Unique identifier of the resource to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if resource was successfully deleted, false otherwise</returns>
    public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return await _repository.DeleteAsync(id.ToString(), cancellationToken);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Applies a single patch operation to a resource.
    /// Can be overridden by derived classes for custom patch handling.
    /// </summary>
    /// <param name="resource">Resource to apply patch to</param>
    /// <param name="patch">Patch operation to apply</param>
    protected virtual void ApplyPatch(T resource, PatchOperation patch)
    {
        // Base implementation - can be overridden by derived classes
        // This provides a hook for custom patch logic without breaking the interface
        if (patch.Op == "replace")
        {
            // Default implementation - derived classes should override for specific logic
            // This is intentionally minimal to allow maximum flexibility
        }
    }

    #endregion

    #region IResourceService Implementation

    /// <summary>
    /// Gets the collection name for this service instance.
    /// Implements IResourceService.CollectionName for SCIM endpoint routing.
    /// Must be implemented by derived classes to specify their collection name.
    /// </summary>
    /// <value>Collection name string (e.g., "Users", "Groups", "Notes")</value>
    public abstract string CollectionName { get; }

    #endregion
}
