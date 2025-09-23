using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Modules;

namespace Looplex.Foundation.SCIMv2;

/// <summary>
/// Core SCIMv2 Interface - Provides SCIMv2 protocol operations
/// Implements RFC 7644 (SCIM Protocol) for resource management
/// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644) - SCIM Protocol
/// https://datatracker.ietf.org/doc/html/rfc7644
/// </summary>
public interface ISCIMv2
{
    /// <summary>
    /// Registers a resource service for a specific collection
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="service">Resource service implementation</param>
    /// <param name="collectionName">Collection name (e.g., "Users", "Groups")</param>
    void Register<T>(IResourceService<T> service, string collectionName) where T : IResource;

    /// <summary>
    /// Gets all registered collections
    /// </summary>
    /// <returns>List of registered collection names</returns>
    IEnumerable<string> GetRegisteredCollections();

    /// <summary>
    /// Checks if a collection is registered
    /// </summary>
    /// <param name="collection">Collection name to check</param>
    /// <returns>True if collection is registered</returns>
    bool IsCollectionRegistered(string collection);

    /// <summary>
    /// Deregisters a resource service for a specific collection
    /// </summary>
    /// <param name="collectionName">Collection name to deregister</param>
    /// <returns>True if collection was deregistered, false if not found</returns>
    bool Deregister(string collectionName);

    /// <summary>
    /// Deregisters all resource services
    /// </summary>
    /// <returns>Number of collections deregistered</returns>
    int DeregisterAll();

    /// <summary>
    /// Gets the current service name
    /// </summary>
    /// <returns>Service name or null if not configured</returns>
    string? GetServiceName();

    /// <summary>
    /// Gets the current service name or default
    /// </summary>
    /// <param name="defaultName">Default service name if not configured</param>
    /// <returns>Service name</returns>
    string GetServiceNameOrDefault(string defaultName = "looplex");

    /// <summary>
    /// Query resources from a collection (GET /collection)
    /// </summary>
    /// <param name="collection">Collection name</param>
    /// <param name="startIndex">Starting index for pagination</param>
    /// <param name="count">Number of items to return</param>
    /// <param name="filter">SCIM filter expression</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortOrder">Sort order (ascending/descending)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 ListResponse</returns>
    Task<SCIMv2Response> QueryAsync(string collection, int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new resource from JSON (POST /collection)
    /// Generic method that works with any registered collection
    /// </summary>
    /// <param name="collection">Collection name</param>
    /// <param name="json">JSON representation of the resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response with created resource</returns>
    Task<SCIMv2Response> CreateAsync(string collection, string json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new resource (POST /collection)
    /// </summary>
    /// <param name="collection">Collection name</param>
    /// <param name="resource">Resource to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response with created resource</returns>
    Task<SCIMv2Response> CreateAsync(string collection, IResource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a specific resource (GET /collection/:id)
    /// </summary>
    /// <param name="collection">Collection name</param>
    /// <param name="id">Resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response with resource</returns>
    Task<SCIMv2Response> RetrieveAsync(string collection, string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Modify a resource using PATCH (PATCH /collection/:id)
    /// </summary>
    /// <param name="collection">Collection name</param>
    /// <param name="id">Resource ID</param>
    /// <param name="patches">JSON Patch operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response with modified resource</returns>
    Task<SCIMv2Response> ModifyAsync(string collection, string id, PatchOperation[] patches, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replace a resource completely from JSON (PUT /collection/:id)
    /// Generic method that works with any registered collection
    /// </summary>
    /// <param name="collection">Collection name</param>
    /// <param name="id">Resource ID</param>
    /// <param name="json">JSON representation of the resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response with updated resource</returns>
    Task<SCIMv2Response> ReplaceAsync(string collection, string id, string json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replace a resource completely (PUT /collection/:id)
    /// </summary>
    /// <param name="collection">Collection name</param>
    /// <param name="id">Resource ID</param>
    /// <param name="resource">New resource data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response with replaced resource</returns>
    Task<SCIMv2Response> ReplaceAsync(string collection, string id, IResource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a resource (DELETE /collection/:id)
    /// </summary>
    /// <param name="collection">Collection name</param>
    /// <param name="id">Resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response indicating success</returns>
    Task<SCIMv2Response> DeleteAsync(string collection, string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all available schemas (GET /Schemas)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response with list of schemas</returns>
    Task<SCIMv2Response> GetSchemasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific schema by ID (GET /Schemas/:id)
    /// </summary>
    /// <param name="schemaId">Schema ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response with schema definition</returns>
    Task<SCIMv2Response> GetSchemaAsync(string schemaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get service provider configuration (GET /ServiceProviderConfig)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response with service provider configuration</returns>
    Task<SCIMv2Response> GetServiceProviderConfigAsync(CancellationToken cancellationToken = default);

}
