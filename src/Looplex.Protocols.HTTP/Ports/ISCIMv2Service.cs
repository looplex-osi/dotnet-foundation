using System;
using System.Threading;
using System.Threading.Tasks;
using Looplex.SCIMv2.Entities;

namespace Looplex.Protocols.HTTP.Ports;

/// <summary>
/// Unified SCIMv2 service interface for HTTP endpoints
/// Eliminates reflection by providing direct method access to ISCIMv2
/// </summary>
public interface ISCIMv2Service
{
    /// <summary>
    /// Query resources from a collection (GET /collection)
    /// </summary>
    Task<object> QueryAsync(string collection, int startIndex, int count, string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieve a specific resource (GET /collection/:id)
    /// </summary>
    Task<object> RetrieveAsync(string collection, string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create a new resource from JSON (POST /collection)
    /// </summary>
    Task<object> CreateAsync(string collection, string json, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Replace a resource completely from JSON (PUT /collection/:id)
    /// </summary>
    Task<object> ReplaceAsync(string collection, string id, string json, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Modify a resource using PATCH (PATCH /collection/:id)
    /// </summary>
    Task<object> ModifyAsync(string collection, string id, PatchOperation[] patches, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a resource (DELETE /collection/:id)
    /// </summary>
    Task<object> DeleteAsync(string collection, string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the service provider configuration
    /// </summary>
    Task<object> GetServiceProviderConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk operations
    /// </summary>
    Task<object> BulkAsync(string json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific schema by ID
    /// </summary>
    Task<object> GetSchemaAsync(string schemaId, CancellationToken cancellationToken = default);
}

