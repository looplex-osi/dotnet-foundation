using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.SCIMv2.Entities;
using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.SCIMv2.Modules;

/// <summary>
/// Generic base class to eliminate duplication between Users.cs and Groups.cs
/// Production-ready implementation with real persistence
/// 
/// Implements RFC 7644 (SCIM Protocol) for resource management
/// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644) - SCIM Protocol
/// 
/// RFC Compliance:
/// - RFC 7644 Section 3.4.1 - Create Resource
/// - RFC 7644 Section 3.4.2 - Query Resources
/// - RFC 7644 Section 3.4.3 - Retrieve Resource
/// - RFC 7644 Section 3.4.4 - Update Resource
/// - RFC 7644 Section 3.4.5 - Delete Resource
/// </summary>
public abstract class BaseResourceService<T> : IResourceService<T> where T : Resource, new()
{
    #region Fields

    private readonly IResourceRepository<T> _repository;

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor with repository injection
    /// </summary>
    /// <param name="repository">Repository for data persistence</param>
    protected BaseResourceService(IResourceRepository<T> repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    #endregion

    #region Common CRUD Operations

    /// <summary>
    /// Query resources with common SCIMv2 logic
    /// Production implementation with real persistence
    /// </summary>
    public virtual async Task<ListResponse<T>> Query(int startIndex, int count,
        string? filter, string? sortBy, string? sortOrder,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return await _repository.QueryAsync(startIndex, count, filter, sortBy, sortOrder, cancellationToken);
    }

    /// <summary>
    /// Create resource with common SCIMv2 logic
    /// Production implementation with real persistence
    /// </summary>
    public virtual async Task<Guid> Create(T resource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return await _repository.CreateAsync(resource, cancellationToken);
    }

    /// <summary>
    /// Retrieve resource with common SCIMv2 logic
    /// Production implementation with real persistence
    /// </summary>
    public virtual async Task<T?> Retrieve(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return await _repository.RetrieveAsync(id, cancellationToken);
    }

    /// <summary>
    /// Replace resource with common SCIMv2 logic
    /// Production implementation with real persistence
    /// </summary>
    public virtual async Task<bool> Replace(Guid id, T resource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return await _repository.ReplaceAsync(id, resource, cancellationToken);
    }

    /// <summary>
    /// Update resource with common SCIMv2 logic
    /// Production implementation with real persistence
    /// </summary>
    public virtual async Task<bool> Update(Guid id, T resource, JArray patches, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Convert JArray to PatchOperation[]
        var patchOperations = new List<PatchOperation>();
        foreach (var patch in patches)
        {
            if (patch is JObject patchObj)
            {
                patchOperations.Add(new PatchOperation
                {
                    Op = patchObj["op"]?.ToString() ?? "replace",
                    Path = patchObj["path"]?.ToString() ?? "",
                    Value = patchObj["value"]
                });
            }
        }
        
        return await _repository.UpdateAsync(id, resource, patchOperations.ToArray(), cancellationToken);
    }

    /// <summary>
    /// Delete resource with common SCIMv2 logic
    /// Production implementation with real persistence
    /// </summary>
    public virtual async Task<bool> Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return await _repository.DeleteAsync(id, cancellationToken);
    }

    #endregion

    #region IResourceService<T> Implementation

    /// <summary>
    /// Collection name for this service
    /// Implements IResourceService.CollectionName
    /// </summary>
    public abstract string CollectionName { get; }

    /// <summary>
    /// Query resources with pagination and filtering
    /// Implements IResourceService<T>.QueryAsync
    /// </summary>
    public async Task<(IList<T> Resources, int TotalCount)> QueryAsync(int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default)
    {
        var result = await Query(startIndex, count, filter, sortBy, sortOrder, cancellationToken);
        return (result.Resources, (int)result.TotalResults);
    }

    /// <summary>
    /// Create a new resource
    /// Implements IResourceService<T>.CreateAsync
    /// </summary>
    public async Task<Guid> CreateAsync(T resource, CancellationToken cancellationToken = default)
    {
        return await Create(resource, cancellationToken);
    }

    /// <summary>
    /// Retrieve a specific resource by ID
    /// Implements IResourceService<T>.RetrieveAsync
    /// </summary>
    public async Task<T?> RetrieveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Retrieve(id, cancellationToken);
    }

    /// <summary>
    /// Replace a resource completely
    /// Implements IResourceService<T>.ReplaceAsync
    /// </summary>
    public async Task<bool> ReplaceAsync(Guid id, T resource, CancellationToken cancellationToken = default)
    {
        return await Replace(id, resource, cancellationToken);
    }

    /// <summary>
    /// Replace a resource completely (non-generic version)
    /// Implements IResourceService<T>.ReplaceAsync
    /// </summary>
    public async Task<bool> ReplaceAsync(Guid id, IResource resource, CancellationToken cancellationToken = default)
    {
        if (resource is T typedResource)
        {
            return await Replace(id, typedResource, cancellationToken);
        }
        throw new ArgumentException($"Resource must be of type {typeof(T).Name}", nameof(resource));
    }

    /// <summary>
    /// Update a resource using PATCH operations
    /// Implements IResourceService<T>.UpdateAsync
    /// </summary>
    public async Task<bool> UpdateAsync(Guid id, T resource, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        // Convert PatchOperation[] to JArray
        var jArray = new JArray();
        foreach (var patch in patches)
        {
            jArray.Add(JObject.FromObject(patch));
        }
        return await Update(id, resource, jArray, cancellationToken);
    }

    /// <summary>
    /// Delete a resource
    /// Implements IResourceService<T>.DeleteAsync
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Delete(id, cancellationToken);
    }

    #endregion
}