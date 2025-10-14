using System;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Protocols.HTTP.Ports;
using Looplex.SCIMv2;
using Looplex.SCIMv2.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Looplex.Protocols.HTTP.Adapters;

/// <summary>
/// SCIMv2 Service Adapter that connects HTTP endpoints to the real ISCIMv2 service
/// Eliminates reflection by providing direct interface access
/// </summary>
public class SCIMv2ServiceAdapter : ISCIMv2Service
{
    private readonly ISCIMv2 _scimv2Service;
    private readonly ILogger<SCIMv2ServiceAdapter> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SCIMv2ServiceAdapter(ISCIMv2 scimv2Service, ILogger<SCIMv2ServiceAdapter> logger, IHttpContextAccessor httpContextAccessor)
    {
        _scimv2Service = scimv2Service ?? throw new ArgumentNullException(nameof(scimv2Service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Applies HTTP headers from SCIMv2 response to HTTP context
    /// Memory-optimized version that avoids JSON serialization
    /// </summary>
    private void ApplyHeaders(object response)
    {
        if (_httpContextAccessor.HttpContext?.Response == null) return;

        try
        {
            // Use reflection to avoid JSON serialization/deserialization
            var responseType = response.GetType();
            
            // Check for Location property
            var locationProperty = responseType.GetProperty("Location");
            if (locationProperty?.GetValue(response) is string location && !string.IsNullOrEmpty(location))
            {
                _httpContextAccessor.HttpContext.Response.Headers["Location"] = location;
            }
            
            // Check for ETag property
            var etagProperty = responseType.GetProperty("ETag");
            if (etagProperty?.GetValue(response) is string etag && !string.IsNullOrEmpty(etag))
            {
                _httpContextAccessor.HttpContext.Response.Headers["ETag"] = etag;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply headers from SCIMv2 response");
        }
    }

    public async Task<object> QueryAsync(string collection, int startIndex, int count, string? filter, string? sortBy, string? sortOrder, string? attributes, string? excludedAttributes, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Querying collection {Collection} with startIndex={StartIndex}, count={Count}, filter={Filter}, attributes={Attributes}, excludedAttributes={ExcludedAttributes}", 
                collection, startIndex, count, filter, attributes, excludedAttributes);

            var response = await _scimv2Service.QueryAsync(collection, startIndex, count, filter, sortBy, sortOrder, attributes, excludedAttributes, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in QueryAsync for collection {Collection}", collection);
            throw;
        }
    }

    public async Task<object> RetrieveAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving resource {Id} from collection {Collection}", id, collection);

            var response = await _scimv2Service.RetrieveAsync(collection, id, cancellationToken);
            ApplyHeaders(response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RetrieveAsync for collection {Collection}, id {Id}", collection, id);
            throw;
        }
    }

    public async Task<object> CreateAsync(string collection, string json, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating resource in collection {Collection}", collection);

            var response = await _scimv2Service.CreateAsync(collection, json, cancellationToken);
            ApplyHeaders(response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAsync for collection {Collection}", collection);
            throw;
        }
    }

    public async Task<object> ReplaceAsync(string collection, string id, string json, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Replacing resource {Id} in collection {Collection}", id, collection);

            var response = await _scimv2Service.ReplaceAsync(collection, id, json, cancellationToken);
            ApplyHeaders(response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReplaceAsync for collection {Collection}, id {Id}", collection, id);
            throw;
        }
    }

    public async Task<object> ModifyAsync(string collection, string id, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Modifying resource {Id} in collection {Collection} with {PatchCount} patches", id, collection, patches.Length);

            var response = await _scimv2Service.ModifyAsync(collection, id, patches, cancellationToken);
            ApplyHeaders(response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ModifyAsync for collection {Collection}, id {Id}", collection, id);
            throw;
        }
    }

    public async Task<object> DeleteAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting resource {Id} from collection {Collection}", id, collection);

            var response = await _scimv2Service.DeleteAsync(collection, id, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync for collection {Collection}, id {Id}", collection, id);
            throw;
        }
    }

    public async Task<object> GetServiceProviderConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting service provider configuration");

            var response = await _scimv2Service.GetServiceProviderConfigAsync(cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetServiceProviderConfigAsync");
            throw;
        }
    }

    public async Task<object> BulkAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Processing bulk operations");

            var response = await _scimv2Service.BulkAsync(json, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BulkAsync");
            throw;
        }
    }

    public async Task<object> GetSchemaAsync(string schemaId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting schema {SchemaId}", schemaId);

            var response = await _scimv2Service.GetSchemaAsync(schemaId, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSchemaAsync for schema {SchemaId}", schemaId);
            throw;
        }
    }
}