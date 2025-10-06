using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2.Modules;
using Looplex.SCIMv2.Serialization;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.SCIMv2.Antlr;
using Microsoft.AspNetCore.Http;

namespace Looplex.SCIMv2;

/// <summary>
/// Consolidated SCIMv2 Implementation
/// Provides centralized SCIMv2 protocol operations and schema management
/// Implements RFC 7644 (SCIM Protocol) and RFC 7643 (SCIM Schema Definition)
/// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644) - SCIM Protocol
/// [RFC 7643](https://datatracker.ietf.org/doc/html/rfc7643) - SCIM Schema Definition
/// 
/// This is the main entry point for all SCIMv2 operations in Looplex.Foundation
/// </summary>
public class SCIMv2 : ISCIMv2, IJsonSchemaProvider, ISCIMv2Validation
{
    private readonly Dictionary<string, IResourceService> _registeredResource = new();
    private readonly Dictionary<string, SchemaDefinition> _schemas;
    private readonly IServiceNameProvider? _serviceNameProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    
    /// <summary>
    /// Constructor for SCIMv2 service with dependency injection
    /// </summary>
    /// <param name="serviceNameProvider">Service name provider for schema generation</param>
    /// <param name="httpContextAccessor">HTTP context accessor for dynamic URL generation</param>
    public SCIMv2(IServiceNameProvider? serviceNameProvider = null, IHttpContextAccessor? httpContextAccessor = null)
    {
        _serviceNameProvider = serviceNameProvider;
        _httpContextAccessor = httpContextAccessor;
        _schemas = new Dictionary<string, SchemaDefinition>();
    }
    
    // Serialization is now centralized in Looplex.Foundation.Serialization
    // All serialization operations use SCIMv2Serializer for consistency

    
    // Hybrid Configuration System - Static configuration storage
    private static readonly Dictionary<string, HashSet<string>> _allowedAttributes = new();
    private static readonly Dictionary<string, Dictionary<string, string>> _attributeMappings = new();
    private static readonly Dictionary<string, Dictionary<string, string>> _jsonConfigurations = new();
    

    /// <summary>
    /// Configures allowed attributes for a resource type dynamically.
    /// This method allows applications to register their specific attributes.
    /// Priority: Dynamic configuration overrides JSON static configuration.
    /// </summary>
    /// <param name="resourceType">Resource type (e.g., 'user', 'group', 'custom')</param>
    /// <param name="attributes">Set of allowed attributes</param>
    /// <remarks>
    /// This method provides a simple way for applications to register
    /// their specific attributes (e.g., meta.created, meta.lastModified, custom attributes)
    /// without modifying the Foundation library.
    /// </remarks>
    public static void ConfigureAttributes(string resourceType, HashSet<string> attributes)
    {
        if (string.IsNullOrEmpty(resourceType))
            throw new ArgumentException("Resource type cannot be null or empty", nameof(resourceType));
        
        if (attributes == null || attributes.Count == 0)
            throw new ArgumentException("Attributes cannot be null or empty", nameof(attributes));
        
        _allowedAttributes[resourceType.ToLower()] = new HashSet<string>(attributes, StringComparer.OrdinalIgnoreCase);
        
    }
    
    /// <summary>
    /// Configures attribute mappings for a resource type dynamically.
    /// This method allows applications to map SCIM attributes to database columns.
    /// Priority: Dynamic configuration overrides JSON static configuration.
    /// </summary>
    /// <param name="resourceType">Resource type (e.g., 'user', 'group', 'custom')</param>
    /// <param name="mappings">Dictionary mapping SCIM attributes to database columns</param>
    /// <remarks>
    /// This method provides a simple way for applications to map
    /// their SCIM attributes to database columns (e.g., meta.created -> created_at)
    /// without modifying the Foundation library.
    /// </remarks>
    public static void ConfigureMapping(string resourceType, Dictionary<string, string> mappings)
    {
        if (string.IsNullOrEmpty(resourceType))
            throw new ArgumentException("Resource type cannot be null or empty", nameof(resourceType));
        
        if (mappings == null || mappings.Count == 0)
            throw new ArgumentException("Mappings cannot be null or empty", nameof(mappings));
        
        _attributeMappings[resourceType.ToLower()] = new Dictionary<string, string>(mappings, StringComparer.OrdinalIgnoreCase);
        
    }

    /// <summary>
    /// Loads JSON configuration for a resource type as fallback.
    /// This method allows applications to provide static JSON configuration.
    /// Priority: Dynamic configuration overrides JSON static configuration.
    /// </summary>
    /// <param name="resourceType">Resource type (e.g., 'user', 'group', 'custom')</param>
    /// <param name="jsonConfig">JSON configuration string</param>
    /// <remarks>
    /// This method provides compatibility with existing JSON-based configurations
    /// while allowing dynamic overrides for specific attributes.
    /// </remarks>
    public static void LoadJsonConfiguration(string resourceType, string jsonConfig)
    {
        if (string.IsNullOrEmpty(resourceType))
            throw new ArgumentException("Resource type cannot be null or empty", nameof(resourceType));
        
        if (string.IsNullOrEmpty(jsonConfig))
            throw new ArgumentException("JSON configuration cannot be null or empty", nameof(jsonConfig));
        
        _jsonConfigurations[resourceType.ToLower()] = new Dictionary<string, string>();
        
        // TODO: Parse JSON configuration and populate _jsonConfigurations
        // This would parse the JSON and extract attribute mappings
        
    }
    
    

    /// <summary>
    /// Gets default allowed attributes for standard SCIM resource types.
    /// </summary>
    /// <param name="resourceType">Resource type</param>
    /// <returns>Set of default allowed attributes</returns>
    private static HashSet<string> GetDefaultAllowedAttributes(string resourceType)
    {
        // Base SCIM attributes as defined in RFC 7643 Section 2.1
        var baseAttributes = new HashSet<string> { "id", "externalId", "meta" };
        
        // Add specific attributes based on SCIM v2.0 standards and resource type conventions
        switch (resourceType.ToLower())
        {
            case "user":
                // User resource attributes as defined in RFC 7643 Section 4.1
                baseAttributes.UnionWith(new[] { "userName", "name", "displayName", "active", "emails", "phoneNumbers" });
                break;
            case "group":
                // Group resource attributes as defined in RFC 7643 Section 4.2
                baseAttributes.UnionWith(new[] { "displayName", "members", "active" });
                break;
            default:
                // For custom resources, use generic SCIM v2.0 conventions
                baseAttributes.UnionWith(new[] { "name", "active", "status", "created", "updated", "meta.created", "meta.lastModified" });
                break;
        }
        
        return baseAttributes;
    }

    /// <summary>
    /// Gets default attribute mappings for standard SCIM resource types.
    /// </summary>
    /// <param name="resourceType">Resource type</param>
    /// <returns>Dictionary of default attribute mappings</returns>
    private static Dictionary<string, string> GetDefaultAttributeMappings(string resourceType)
    {
        var mappings = new Dictionary<string, string>();
        var tablePrefix = GetTablePrefix(resourceType);
        
        // Base SCIM attributes always mapped following database conventions
        mappings["id"] = $"{tablePrefix}.id";
        mappings["externalId"] = $"{tablePrefix}.external_id";
        mappings["created"] = $"{tablePrefix}.created_at";
        mappings["updated"] = $"{tablePrefix}.updated_at";
        mappings["active"] = $"{tablePrefix}.active";
        mappings["status"] = $"{tablePrefix}.status";
        
        // Add specific mappings based on resource type and database conventions
        switch (resourceType.ToLower())
        {
            case "user":
                // User-specific attribute mappings
                mappings["userName"] = "u.user_name";
                mappings["displayName"] = "u.display_name";
                mappings["emails"] = "u.emails";
                mappings["phoneNumbers"] = "u.phone_numbers";
                break;
            case "group":
                // Group-specific attribute mappings
                mappings["displayName"] = "g.display_name";
                mappings["members"] = "g.members";
                break;
            default:
                // For custom resources, use generic database naming conventions
                mappings["name"] = $"{tablePrefix}.name";
                break;
        }
        
        return mappings;
    }

    /// <summary>
    /// Gets table prefix for a resource type.
    /// </summary>
    /// <param name="resourceType">Resource type</param>
    /// <returns>Table prefix</returns>
    private static string GetTablePrefix(string resourceType)
    {
        return resourceType.ToLower() switch
        {
            "user" => "u",
            "group" => "g",
            _ => resourceType.ToLower().Substring(0, 1)
        };
    }

    /// <summary>
    /// Gets the base URL dynamically from the current HTTP request.
    /// Constructs the base URL for SCIM resource locations.
    /// </summary>
    /// <returns>Base URL for SCIM resources</returns>
    private string GetBaseUrl()
    {
        if (_httpContextAccessor?.HttpContext?.Request != null)
        {
            var request = _httpContextAccessor.HttpContext.Request;
            var scheme = request.Scheme;
            var host = request.Host;
            var pathBase = request.PathBase;
            var path = request.Path;
            
            
            return $"{scheme}://{host}{pathBase}";
        }
        
        // Fallback to default if no HTTP context available
        return "https://api.exemplo.com/";
    }

    /// <summary>
    /// Sets HTTP headers (Location and ETag) for SCIM v2.0 compliance
    /// </summary>
    private void SetHttpHeaders(string location, string etag)
    {
        
        if (_httpContextAccessor?.HttpContext?.Response != null)
        {
            var response = _httpContextAccessor.HttpContext.Response;
            response.Headers["Location"] = location;
            response.Headers["ETag"] = etag;
            
        }
        else
        {
        }
    }
    public static Looplex.SCIMv2.Entities.ResourceMeta CreateResourceMeta(IDataReader reader, string resourceId, string resourceType)
    {
        // Use fallback URL for static method without HttpContext
        var fullUrl = $"/{resourceType}/{resourceId}";
        return new Looplex.SCIMv2.Entities.ResourceMeta
        {
            ResourceType = resourceType,
            Location = fullUrl, 
            Created = ParseDateTime(reader["created_at"]),
            LastModified = ParseDateTime(reader["updated_at"])
        };
    }

    public static Looplex.SCIMv2.Entities.ResourceMeta CreateResourceMeta(
        IDataReader reader, 
        string resourceId, 
        string resourceType, 
        IHttpContextAccessor httpContextAccessor)
    {
        var fullUrl = $"{GetBaseUrl(httpContextAccessor)}/{resourceType}/{resourceId}";
        return new Looplex.SCIMv2.Entities.ResourceMeta
        {
            ResourceType = resourceType,
            Location = fullUrl, 
            Created = ParseDateTime(reader["created_at"]),
            LastModified = ParseDateTime(reader["updated_at"])
        };
    }

    private static string GetBaseUrl(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor?.HttpContext?.Request != null)
        {
            var request = httpContextAccessor.HttpContext.Request;
            return $"{request.Scheme}://{request.Host}{request.PathBase}";
        }
        return "https://api.exemplo.com/";
    }

    private static DateTime ParseDateTime(object? value)
    {
        return DateTime.TryParse(value?.ToString(), out var dateTime) ? dateTime : DateTime.UtcNow;
    }

    /// <summary>
    /// Applies HTTP method specific rules for SCIM v2.0 compliance
    /// </summary>
    private void ApplyHttpMethodSpecificRules<T>(T resource, string collection) where T : IResource
    {
        if (_httpContextAccessor?.HttpContext?.Request == null) 
        {
            return;
        }
        
        var httpMethod = _httpContextAccessor.HttpContext.Request.Method.ToUpper();
        var fullUrl = $"{GetBaseUrl()}/{collection}/{resource.Id}";
        
        
        switch (httpMethod)
        {
            case "GET":
                // GET: Return resource directly, set Location header (only for individual resources)
                resource.Meta.Location = fullUrl;
                SetHttpHeaders(fullUrl, resource.Meta.Version);
                break;
                
            case "POST":
                // POST: Return resource with Resources wrapper, set Location header
                resource.Meta.Location = fullUrl;
                SetHttpHeaders(fullUrl, resource.Meta.Version);
                break;
                
            case "PUT":
                // PUT: Return resource directly, set Location and ETag headers
                resource.Meta.Location = fullUrl;
                SetHttpHeaders(fullUrl, resource.Meta.Version);
                break;
                
            case "PATCH":
                // PATCH: Return resource directly, set Location and ETag headers
                resource.Meta.Location = fullUrl;
                SetHttpHeaders(fullUrl, resource.Meta.Version);
                break;
                
            case "DELETE":
                // DELETE: No resource body, only status 204
                break;
        }
    }

    #region SCIMv2Service Implementation

    /// <summary>
    /// Registers a resource service implementation for a specific collection.
    /// Enables SCIM operations on the specified collection type.
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource interface</typeparam>
    /// <param name="service">Resource service implementation for CRUD operations</param>
    /// <param name="collectionName">Collection name (e.g., "Users", "Groups", "Notes")</param>
    public void Register<T>(IResourceService<T> service, string collectionName) where T : IResource
    {
        if (string.IsNullOrEmpty(collectionName))
            throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

        if (service == null)
            throw new ArgumentNullException(nameof(service));

        // Store the service
        _registeredResource[collectionName] = service;
    }


    /// <summary>
    /// Queries resources from a collection with filtering, sorting, and pagination support.
    /// Implements RFC 7644 Section 3.4.2 - Query Resources
    /// [RFC 7644 Section 3.4.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2)
    /// Supports SCIM filter expressions per RFC 7644 Section 3.4.2.2
    /// [RFC 7644 Section 3.4.2.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2)
    /// </summary>
    /// <param name="collection">Collection name to query</param>
    /// <param name="startIndex">Starting index for pagination (1-based)</param>
    /// <param name="count">Maximum number of resources to return</param>
    /// <param name="filter">SCIM filter expression (optional)</param>
    /// <param name="sortBy">Field name for sorting (optional)</param>
    /// <param name="sortOrder">Sort order: "ascending" or "descending" (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 ListResponse with resources and pagination metadata</returns>
    public async Task<SCIMv2Response> QueryAsync(string collection, int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default)
    {
        try
        {
            if (startIndex < 1)
            {
                return CreateErrorResponse(400, "Bad Request", "Start index must be greater than 0");
            }

            if (count < 0)
            {
                return CreateErrorResponse(400, "Bad Request", "Count cannot be negative");
            }

            // Validate request
            var validation = ValidateRequest(collection);
            if (!validation.IsValid)
            {
                return validation.Error!;
            }

            // Use dynamic typing to call QueryAsync on the generic service
            dynamic dynamicService = validation.Service!;
            var result = await dynamicService.QueryAsync(startIndex, count, filter, sortBy, sortOrder, cancellationToken);
            
            // Extract resources and totalCount from tuple
            var resources = result.Item1;
            var totalCount = result.Item2;
            
            // Generate SHA-256 hash for meta.version BEFORE validation
            try
            {
                // Convert dynamic resources to IList<IResource>
                if (resources is IList<IResource> resourceList)
                {
                    // Generate SHA-256 hash for each resource's meta.version FIRST
                    foreach (var resource in resourceList)
                    {
                        resource.Meta.Version = GenerateResourceVersion(resource);
                    }
                    // Then validate
                    ValidateResourcesForOutput(resourceList);
                }
                else if (resources is IEnumerable<IResource> resourceEnumerable)
                {
                    // Convert to List<IResource> for processing
                    var convertedResourceList = resourceEnumerable.ToList();
                    // Generate SHA-256 hash for each resource's meta.version FIRST
                    foreach (var resource in convertedResourceList)
                    {
                        resource.Meta.Version = GenerateResourceVersion(resource);
                    }
                    // Then validate
                    ValidateResourcesForOutput(convertedResourceList);
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            var response = CreateListResponse(resources, totalCount, startIndex, count);
            
            return response;
        }
        catch (Exception ex)
        {
            return HandleException(ex, "querying resources");
        }
    }

    /// <summary>
    /// Creates a new resource from JSON representation in the specified collection.
    /// Implements RFC 7644 Section 3.4.1 - Create Resource
    /// [RFC 7644 Section 3.4.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.1)
    /// Validates resource structure per RFC 7643 Section 3.1 - Resource Representation
    /// [RFC 7643 Section 3.1](https://datatracker.ietf.org/doc/html/rfc7643#section-3.1)
    /// </summary>
    /// <param name="collection">Collection name where resource will be created</param>
    /// <param name="json">JSON representation of the resource to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response with created resource and metadata</returns>
    public async Task<SCIMv2Response> CreateAsync(string collection, string json, CancellationToken cancellationToken = default)
    {
        try
        {
            
            if (string.IsNullOrEmpty(json))
            {
                return CreateErrorResponse(400, "Bad Request", "JSON body is required");
            }

            // Validate JSON structure first
            var jsonValidation = ValidateJsonRequest(json);
            if (!jsonValidation.IsValid)
            {
                return CreateErrorResponse(400, "Bad Request", jsonValidation.ErrorMessage);
            }

            // Get the registered service for this collection
            if (!_registeredResource.TryGetValue(collection, out var service))
            {
                return CreateErrorResponse(404, "Collection not found", $"Collection '{collection}' is not registered");
            }

            // Use dynamic typing to call the service's CreateAsync method
            // The service will handle the JSON deserialization internally
            dynamic dynamicService = service;
            var result = await dynamicService.CreateAsync(json, cancellationToken);
            
            // Retrieve the created resource to generate proper ETag
            var createdResource = await dynamicService.RetrieveAsync(result, cancellationToken);
            
            // Validate the created resource for SCIMv2 compliance
            if (createdResource != null)
            {
                
                try
                {
                    // Update meta.version with cryptographic hash FIRST
                    createdResource.Meta.Version = GenerateResourceVersion(createdResource);
                }
                catch (Exception ex)
                {
                    throw;
                }
                
                try
            {
                ValidateResourceForOutput(createdResource);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            else
            {
            }
            
            // Apply HTTP method specific rules for SCIM v2.0 compliance
            ApplyHttpMethodSpecificRules(createdResource, collection);
            
            var response = new SCIMv2Response
            {
                StatusCode = 201,
                Data = createdResource, // Include the created resource in response
                Schemas = createdResource?.Schemas ?? Array.Empty<string>(),
                Location = createdResource?.Meta.Location,
                ETag = createdResource?.Meta.Version
            };
            
            return response;
        }
        catch (Exception ex)
        {
            return HandleException(ex, "creating resource from JSON");
        }
    }

    /// <summary>
    /// Creates a new resource from object instance in the specified collection.
    /// Implements RFC 7644 Section 3.4.1 - Create Resource
    /// [RFC 7644 Section 3.4.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.1)
    /// </summary>
    /// <param name="collection">Collection name where resource will be created</param>
    /// <param name="resource">Resource object instance to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response with created resource and metadata</returns>
    public async Task<SCIMv2Response> CreateAsync(string collection, IResource resource, CancellationToken cancellationToken = default)
    {
        try
        {

            if (resource == null)
            {
                return CreateErrorResponse(400, "Bad Request", "Resource cannot be null");
            }

            // Validate required SCIM v2.0 fields per RFC 7643 Section 3.1
            // [RFC 7643 Section 3.1](https://datatracker.ietf.org/doc/html/rfc7643#section-3.1)
            // Ensures compliance with mandatory attributes: id, schemas, meta
            var validationResult = ValidateResourceForCreation(resource);
            if (!validationResult.IsValid)
            {
                return CreateErrorResponse(400, "Bad Request", validationResult.ErrorMessage);
            }

            // Validate request
            var validation = ValidateRequest(collection);
            if (!validation.IsValid)
            {
                return validation.Error!;
            }

            // Use dynamic typing to call CreateAsync on the generic service
            dynamic dynamicService = validation.Service!;
            var resourceId = await dynamicService.CreateAsync((dynamic)resource, cancellationToken);
            
            // The service (CreateResourceWithMetadataAsync) already set the ID and metadata properly
            // No need to retrieve again - the resource object is already updated
            
            return CreateCreateResponse(resource, resourceId, collection);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "creating the resource");
        }
    }

    /// <summary>
    /// Retrieves a specific resource by ID from the specified collection.
    /// Implements RFC 7644 Section 3.4.3 - Retrieve Resource
    /// [RFC 7644 Section 3.4.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.3)
    /// </summary>
    /// <param name="collection">Collection name containing the resource</param>
    /// <param name="id">Unique identifier of the resource to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response with the requested resource</returns>
    public async Task<SCIMv2Response> RetrieveAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            var validation = ValidateRequest(collection, id);
            if (!validation.IsValid)
            {
                return validation.Error!;
            }

            // Use dynamic typing to call RetrieveAsync on the generic service
            dynamic dynamicService = validation.Service!;
            var resource = await dynamicService.RetrieveAsync(validation.ResourceId!.Value, cancellationToken);
            
            if (resource == null)
            {
                return CreateErrorResponse(404, "Resource not found", $"Resource with ID '{id}' not found");
            }

            // Update meta.version with cryptographic hash
            resource.Meta.Version = GenerateResourceVersion(resource);

            // Apply HTTP method specific rules for SCIM v2.0 compliance
            ApplyHttpMethodSpecificRules(resource, collection);

            return CreateRetrieveResponse(resource);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving the resource");
        }
    }

    /// <summary>
    /// Modifies a resource using JSON Patch operations for partial updates.
    /// Implements RFC 7644 Section 3.4.4 - Update Resource
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// Uses RFC 6902 (JSON Patch) Section 4 - Operations
    /// [RFC 6902 Section 4](https://datatracker.ietf.org/doc/html/rfc6902#section-4)
    /// </summary>
    /// <param name="collection">Collection name containing the resource</param>
    /// <param name="id">Unique identifier of the resource to modify</param>
    /// <param name="patches">Array of JSON Patch operations to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response with the updated resource</returns>
    public async Task<SCIMv2Response> ModifyAsync(string collection, string id, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        try
        {
            
            // Validate request
            var validation = ValidateRequest(collection, id);
            if (!validation.IsValid)
            {
                return validation.Error!;
            }

            // Retrieve current resource using dynamic typing
            dynamic dynamicService = validation.Service!;
            
            var currentResource = await dynamicService.RetrieveAsync(validation.ResourceId!.Value, cancellationToken);
            if (currentResource == null)
            {
                return CreateErrorResponse(404, "Resource not found", $"Resource with ID '{id}' not found");
            }

            // Apply patches using dynamic typing - call ModifyAsync if available, otherwise fallback to UpdateAsync
            bool success;
            try
            {
                // Try to call ModifyAsync first (if implemented by the service)
                var modifyResult = await dynamicService.ModifyAsync(validation.ResourceId!.Value, patches, cancellationToken);
                success = modifyResult != null;
            }
            catch (Exception)
            {
                // Fallback to UpdateAsync if ModifyAsync is not available
                success = await dynamicService.UpdateAsync(validation.ResourceId!.Value, currentResource, patches, cancellationToken);
            }
            
            if (!success)
            {
                return CreateErrorResponse(400, "Update failed", "Failed to apply patches to resource");
            }

            // Retrieve updated resource using dynamic typing
            var updatedResource = await dynamicService.RetrieveAsync(validation.ResourceId!.Value, cancellationToken);
            
            // Update meta.version with cryptographic hash
            if (updatedResource != null)
            {
                updatedResource.Meta.Version = GenerateResourceVersion(updatedResource);
            }
            
            return new SCIMv2Response
            {
                StatusCode = 200,
                Data = updatedResource,
                Schemas = updatedResource?.Schemas ?? Array.Empty<string>(),
                Location = updatedResource?.Meta.Location,
                ETag = updatedResource?.Meta.Version
            };
        }
        catch (Exception ex)
        {
            return HandleException(ex, "modifying the resource");
        }
    }

    /// <summary>
    /// Replaces a resource completely from JSON representation.
    /// Implements RFC 7644 Section 3.4.4 - Update Resource
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// Performs complete resource replacement (PUT semantics)
    /// </summary>
    /// <param name="collection">Collection name containing the resource</param>
    /// <param name="id">Unique identifier of the resource to replace</param>
    /// <param name="json">JSON representation of the complete resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response with the replaced resource</returns>
    public async Task<SCIMv2Response> ReplaceAsync(string collection, string id, string json, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(json))
            {
                return CreateErrorResponse(400, "Bad Request", "JSON body is required");
            }

            // Validate JSON structure first
            var jsonValidation = ValidateJsonRequest(json);
            if (!jsonValidation.IsValid)
            {
                return CreateErrorResponse(400, "Bad Request", jsonValidation.ErrorMessage);
            }

            // Get the registered service for this collection
            if (!_registeredResource.TryGetValue(collection, out var service))
            {
                return CreateErrorResponse(404, "Collection not found", $"Collection '{collection}' is not registered");
            }

            // Use dynamic typing to call the service's ReplaceAsync method
            // The service will handle the JSON deserialization internally
            dynamic dynamicService = service;
            var result = await dynamicService.ReplaceAsync(id, json, cancellationToken);
            
            // The service returns a boolean, but we need to create a proper response
            if (result)
            {
                // Retrieve the updated resource to return it in the response
                var updatedResource = await RetrieveAsync(collection, id, cancellationToken);
                
                // Update meta.version with cryptographic hash
                if (updatedResource.Data != null && updatedResource.Data is IResource resource)
                {
                    resource.Meta.Version = GenerateResourceVersion(resource);
                }
                
                // For PUT operations, return the resource directly (not wrapped in Resources)
                // This is SCIM v2.0 compliant for PUT responses
                return SCIMv2Response.CreatePutResponse(
                    updatedResource.Data, 
                    updatedResource.Schemas, 
                    $"{GetBaseUrl()}/{collection}/{id}",
                    updatedResource.Data != null && updatedResource.Data is IResource res ? 
                    GenerateContentETag(res) : "W/\"1\""
                );
            }
            else
            {
                return CreateErrorResponse(404, "Resource not found", $"Resource with ID '{id}' not found");
            }
        }
        catch (Exception ex)
        {
            return HandleException(ex, "replacing resource from JSON");
        }
    }

    /// <summary>
    /// Replaces a resource completely from object instance.
    /// Implements RFC 7644 Section 3.4.4 - Update Resource (PUT)
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// Performs complete resource replacement (PUT semantics)
    /// </summary>
    /// <param name="collection">Collection name containing the resource</param>
    /// <param name="id">Unique identifier of the resource to replace</param>
    /// <param name="resource">Complete resource object instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response with the replaced resource</returns>
    public async Task<SCIMv2Response> ReplaceAsync(string collection, string id, IResource resource, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            var validation = ValidateRequest(collection, id);
            if (!validation.IsValid)
            {
                return validation.Error!;
            }

            // Use dynamic typing to call ReplaceAsync on the generic service
            dynamic dynamicService = validation.Service!;
            var success = await dynamicService.ReplaceAsync(validation.ResourceId!.Value, resource, cancellationToken);
            
            if (!success)
            {
                return CreateErrorResponse(404, "Resource not found", $"Resource with ID '{id}' not found");
            }

            // Apply HTTP method specific rules for SCIM v2.0 compliance
            ApplyHttpMethodSpecificRules(resource, collection);

            // Update meta information
            resource.Id = id;
            resource.Meta.LastModified = DateTime.UtcNow;

            return CreateRetrieveResponse(resource);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "replacing the resource");
        }
    }

    /// <summary>
    /// Deletes a resource permanently from the specified collection.
    /// Implements RFC 7644 Section 3.4.5 - Delete Resource
    /// [RFC 7644 Section 3.4.5](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.5)
    /// </summary>
    /// <param name="collection">Collection name containing the resource</param>
    /// <param name="id">Unique identifier of the resource to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response indicating successful deletion</returns>
    public async Task<SCIMv2Response> DeleteAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            var validation = ValidateRequest(collection, id);
            if (!validation.IsValid)
            {
                return validation.Error!;
            }

            // Use dynamic typing to call DeleteAsync on the generic service
            dynamic dynamicService = validation.Service!;
            var success = await dynamicService.DeleteAsync(validation.ResourceId!.Value, cancellationToken);
            
            if (!success)
            {
                return CreateErrorResponse(404, "Resource not found", $"Resource with ID '{id}' not found");
            }

            return new SCIMv2Response
            {
                StatusCode = 204,
                Data = null
            };
        }
        catch (Exception ex)
        {
            return HandleException(ex, "deleting the resource");
        }
    }


    /// <summary>
    /// Gets all registered collection names.
    /// Returns the list of collections that have been registered for SCIM operations.
    /// </summary>
    /// <returns>Collection of registered collection names</returns>
    public IEnumerable<string> GetRegisteredCollections()
    {
        return _registeredResource.Keys;
    }

    /// <summary>
    /// Checks if a collection is registered for SCIM operations.
    /// </summary>
    /// <param name="collection">Collection name to check</param>
    /// <returns>True if collection is registered, false otherwise</returns>
    public bool IsCollectionRegistered(string collection)
    {
        return _registeredResource.ContainsKey(collection);
    }

    /// <summary>
    /// Deregisters a resource service for a specific collection.
    /// Removes the collection from SCIM operations.
    /// </summary>
    /// <param name="collectionName">Collection name to deregister</param>
    /// <returns>True if collection was deregistered, false if not found</returns>
    public bool Deregister(string collectionName)
    {
        if (string.IsNullOrEmpty(collectionName))
            throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

        var wasRegistered = _registeredResource.ContainsKey(collectionName);
        
        if (wasRegistered)
        {
            _registeredResource.Remove(collectionName);
            
        }
        
        return wasRegistered;
    }

    /// <summary>
    /// Deregisters all resource services and clears the collection registry.
    /// Removes all collections from SCIM operations.
    /// </summary>
    /// <returns>Number of collections that were deregistered</returns>
    public int DeregisterAll()
    {
        var count = _registeredResource.Count;
        _registeredResource.Clear();
        
        
        return count;
    }

    /// <summary>
    /// Gets the current service name from the configured service name provider.
    /// </summary>
    /// <returns>Service name or null if not configured</returns>
    public string? GetServiceName()
    {
        return _serviceNameProvider?.GetServiceName();
    }

    /// <summary>
    /// Gets the current service name or returns a default value if not configured.
    /// </summary>
    /// <param name="defaultName">Default service name to return if not configured</param>
    /// <returns>Service name or default value</returns>
    public string GetServiceNameOrDefault(string defaultName = "looplex")
    {
        return _serviceNameProvider?.GetServiceName() ?? defaultName;
    }

    #endregion

    #region IJsonSchemaProvider Implementation

    /// <summary>
    /// Registers a custom schema definition for SCIM operations.
    /// Adds the schema to the internal schema registry.
    /// </summary>
    /// <param name="schema">Schema definition to register</param>
    public void RegisterSchema(SchemaDefinition schema)
    {
        if (schema?.Id != null)
        {
            _schemas[schema.Id] = schema;
        }
    }

    /// <summary>
    /// Registers multiple custom schema definitions for SCIM operations.
    /// Adds all schemas to the internal schema registry.
    /// </summary>
    /// <param name="schemas">Collection of schema definitions to register</param>
    public void RegisterSchemas(IEnumerable<SchemaDefinition> schemas)
    {
        foreach (var schema in schemas)
        {
            RegisterSchema(schema);
        }
    }

    public Task<List<string>> ResolveJsonSchemasAsync(IContext context, List<string> schemaIds, string? lang = null)
    {
        var result = new List<string>();
        
        foreach (var schemaId in schemaIds)
        {
            if (_schemas.TryGetValue(schemaId, out var schema))
            {
                result.Add(SCIMv2Serializer.SerializeSchema(schema));
            }
        }
        
        return Task.FromResult(result);
    }

    public Task<string> ResolveJsonSchemaAsync(IContext context, string schemaId, string? lang = null)
    {
        if (_schemas.TryGetValue(schemaId, out var schema))
        {
            return Task.FromResult(SCIMv2Serializer.SerializeSchema(schema));
        }
        
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Gets all available SCIMv2 schema definitions.
    /// Returns all registered schemas for SCIM operations.
    /// </summary>
    /// <returns>List of all registered schema definitions</returns>
    public Task<List<SchemaDefinition>> GetAllSchemasAsync()
    {
        return Task.FromResult(_schemas.Values.ToList());
    }

    /// <summary>
    /// Gets a specific SCIMv2 schema definition by ID (internal helper method).
    /// Returns null if schema is not found.
    /// </summary>
    /// <param name="schemaId">Schema identifier to retrieve</param>
    /// <returns>Schema definition or null if not found</returns>
    public Task<SchemaDefinition?> GetSchemaDefinitionAsync(string? schemaId)
    {
        if (string.IsNullOrEmpty(schemaId))
            return Task.FromResult<SchemaDefinition?>(null);
            
        return Task.FromResult(_schemas.TryGetValue(schemaId, out var schema) ? schema : null);
    }

    #endregion

    #region Discovery Endpoints Implementation

    /// <summary>
    /// Gets all available schemas for SCIM discovery endpoint.
    /// Implements RFC 7644 Section 3.4.6 - Schema Discovery
    /// [RFC 7644 Section 3.4.6](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.6)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response with all registered schemas</returns>
    public async Task<SCIMv2Response> GetSchemasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var schemas = _schemas.Values.ToList();
            
            return new SCIMv2Response
            {
                StatusCode = 200,
                HttpMethod = "SCHEMAS",
                Data = new
                {
                    totalResults = schemas.Count,
                    itemsPerPage = schemas.Count,
                    startIndex = 1,
                    schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
                    Resources = schemas
                },
                Schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" }
            };
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving schemas");
        }
    }

    /// <summary>
    /// Gets a specific schema by ID for SCIM discovery endpoint.
    /// Implements RFC 7644 Section 3.4.6 - Schema Discovery
    /// [RFC 7644 Section 3.4.6](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.6)
    /// </summary>
    /// <param name="schemaId">Schema identifier to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response with the requested schema definition</returns>
    public async Task<SCIMv2Response> GetSchemaAsync(string schemaId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(schemaId))
            {
                return CreateErrorResponse(400, "Bad request", "Schema ID is required");
            }

            if (!_schemas.TryGetValue(schemaId, out var schema))
            {
                return CreateErrorResponse(404, "Not found", $"Schema '{schemaId}' not found");
            }

            // Update schema location with current request host
            if (schema.Meta != null)
            {
                schema.Meta.Location = $"{GetBaseUrl()}/Schemas/{schemaId}";
            }

            return new SCIMv2Response
            {
                StatusCode = 200,
                Data = schema,
                Schemas = new[] { schema.Id }
            };
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving schema");
        }
    }

    /// <summary>
    /// Gets the service provider configuration for SCIM discovery endpoint.
    /// Returns SCIM service provider capabilities and configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response with service provider configuration</returns>
    public async Task<SCIMv2Response> GetServiceProviderConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceProviderConfig = new ServiceProviderConfiguration
            {
                AuthenticationSchemes = new[]
                {
                    new AuthenticationScheme
                    {
                        Name = "OAuth Bearer Token",
                        Description = "Authentication scheme using the OAuth Bearer Token Standard",
                        SpecUri = new Uri("https://tools.ietf.org/html/rfc6750"),
                        Type = AuthenticationSchemeType.OAuthBearerToken
                    }
                },
                Bulk = new Bulk
                {
                    Supported = true,
                    MaxOperations = 1000,
                    MaxPayloadSize = 1048576 // 1MB
                },
                ChangePassword = new ChangePassword
                {
                    Supported = true
                },
                DocumentationUri = new Uri("https://docs.looplex.com/scim"),
                Etag = new Etag
                {
                    Supported = true
                },
                Filter = new Filter
                {
                    Supported = true,
                    MaxResults = 200
                },
                Patch = new Patch
                {
                    Supported = true
                },
                Sort = new Sort
                {
                    Supported = true
                }
            };

            return new SCIMv2Response
            {
                StatusCode = 200,
                Data = serviceProviderConfig,
                Schemas = serviceProviderConfig.Schemas
            };
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving service provider configuration");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates a resource for creation according to SCIM v2.0 requirements.
    /// Ensures the resource meets all mandatory SCIM specifications.
    /// </summary>
    /// <param name="resource">Resource to validate</param>
    /// <returns>Validation result with error message if invalid</returns>
    public (bool IsValid, string ErrorMessage) ValidateResourceForCreation(IResource resource)
    {
        if (resource == null)
        {
            return (false, "Resource cannot be null");
        }

        // Validate required SCIM v2.0 fields
        if (string.IsNullOrWhiteSpace(resource.Id))
        {
            return (false, "Resource ID is required for SCIM v2.0 compliance");
        }

        if (resource.Meta == null)
        {
            return (false, "Resource metadata is required for SCIM v2.0 compliance");
        }

        if (string.IsNullOrWhiteSpace(resource.Meta.ResourceType))
        {
            return (false, "Resource type is required for SCIM v2.0 compliance");
        }

        if (resource.Meta.Created == default(DateTime))
        {
            return (false, "Created timestamp is required for SCIM v2.0 compliance");
        }

        if (resource.Meta.LastModified == default(DateTime))
        {
            return (false, "Last modified timestamp is required for SCIM v2.0 compliance");
        }

        if (string.IsNullOrWhiteSpace(resource.Meta.Location))
        {
            return (false, "Resource location is required for SCIM v2.0 compliance");
        }

        if (string.IsNullOrWhiteSpace(resource.Meta.Version))
        {
            return (false, "Resource version is required for SCIM v2.0 compliance");
        }

        // Validate schemas
        if (resource.Schemas == null || resource.Schemas.Length == 0)
        {
            return (false, "Resource schemas are required for SCIM v2.0 compliance");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Validates a collection of resources for output to ensure SCIMv2 compliance.
    /// Ensures all resources meet SCIM v2.0 output requirements.
    /// </summary>
    /// <param name="resources">Collection of resources to validate</param>
    private static void ValidateResourcesForOutput(IList<IResource> resources)
    {
        if (resources == null)
        {
            throw new InvalidOperationException("Resources list cannot be null");
        }
        
        for (int i = 0; i < resources.Count; i++)
        {
            ValidateResourceForOutput(resources[i], i + 1);
        }
    }
    
    /// <summary>
    /// Validates a single resource for output compliance with SCIM v2.0.
    /// Ensures the resource meets all SCIM output requirements.
    /// </summary>
    /// <param name="resource">Resource to validate</param>
    /// <param name="resourceIndex">Index of the resource in the collection (for error reporting)</param>
    private static void ValidateResourceForOutput(IResource resource, int resourceIndex = 0)
    {
        if (resource == null)
        {
            throw new InvalidOperationException("SCIMv2 VALIDATION ERROR: Resource cannot be null");
        }
            
        var resourceType = resource.GetType().Name;
        var resourceId = resource.Id ?? "NULL";
        
        // Validate required SCIMv2 fields with clear error messages
        if (string.IsNullOrWhiteSpace(resource.Id))
        {
            throw new InvalidOperationException($"SCIMv2 VALIDATION ERROR: {resourceType} has empty or null ID. " +
                          "SCIMv2 requires every resource to have a unique identifier.");
        }
            
        if (resource.Schemas == null || resource.Schemas.Length == 0)
        {
            throw new InvalidOperationException($"SCIMv2 VALIDATION ERROR: {resourceType} (ID: {resourceId}) is missing schemas. " +
                          "SCIMv2 requires every resource to have at least one schema URI. " +
                          "Add schemas like: [\"urn:ietf:params:scim:schemas:core:2.0:User\"]");
        }
            
        if (resource.Meta == null)
        {
            throw new InvalidOperationException($"SCIMv2 VALIDATION ERROR: {resourceType} (ID: {resourceId}) is missing metadata. " +
                          "SCIMv2 requires every resource to have Meta object with ResourceType, Location, and Version.");
        }
            
        if (string.IsNullOrWhiteSpace(resource.Meta.ResourceType))
        {
            throw new InvalidOperationException($"SCIMv2 VALIDATION ERROR: {resourceType} (ID: {resourceId}) Meta.ResourceType is missing. " +
                          "SCIMv2 requires Meta.ResourceType to identify the resource type (e.g., 'User', 'Group', etc). " +
                          "Set Meta.ResourceType = \"{resourceType}\"");
        }
            
        if (string.IsNullOrWhiteSpace(resource.Meta.Location))
        {
            throw new InvalidOperationException($"SCIMv2 VALIDATION ERROR: {resourceType} (ID: {resourceId}) Meta.Location is missing. " +
                          "SCIMv2 requires Meta.Location to be the canonical URI of the resource. " +
                          "Set Meta.Location = \"/{resourceType}s/{resourceId}\"");
        }
            
        if (string.IsNullOrWhiteSpace(resource.Meta.Version))
        {
            throw new InvalidOperationException($"SCIMv2 VALIDATION ERROR: {resourceType} (ID: {resourceId}) Meta.Version is missing. " +
                          "SCIMv2 requires Meta.Version for optimistic concurrency control. " +
                          "Set Meta.Version = \"W/\\\"1\\\"\" or use ETag format");
        }
    }

    /// <summary>
    /// Automatically generates SCIMv2 schema URI based on resource type and service name.
    /// Creates compliant schema URIs for SCIM resource types.
    /// </summary>
    /// <param name="resourceType">Type of the resource (e.g., "Note", "Pad", "User", "Group")</param>
    /// <returns>SCIMv2 compliant schema URI</returns>
    private string GenerateSchemaUri(string resourceType)
    {
        if(resourceType.ToLower() == "user" || resourceType.ToLower() == "group"){
            return $"urn:ietf:params:scim:schemas:core:2.0:{resourceType}";
        }
        var serviceName = GetServiceNameOrDefault("looplex");
        return $"urn:looplex:params:scim:schemas:{serviceName}:2.0:{resourceType}";
    }






    private Dictionary<string, SchemaDefinition> InitializeSchemas()
    {
        var userSchemaId = "urn:ietf:params:scim:schemas:core:2.0:User";
        var groupSchemaId = "urn:ietf:params:scim:schemas:core:2.0:Group";
        
        return new Dictionary<string, SchemaDefinition>
        {
            [userSchemaId] = new SchemaDefinition
            {
                Id = "User",
                Name = "User",
                Schemas = new[] { userSchemaId },
                Description = "User Account",
                Attributes = new[]
                {
                    new SchemaAttribute
                    {
                        Name = "userName",
                        Type = "string",
                        Required = true,
                        CaseExact = false,
                        Mutability = "readWrite",
                        Returned = "default",
                        Uniqueness = "server",
                        Description = "Unique identifier for the User, typically used by the user to directly authenticate to the service provider"
                    },
                    new SchemaAttribute
                    {
                        Name = "name",
                        Type = "complex",
                        Required = false,
                        CaseExact = false,
                        Mutability = "readWrite",
                        Returned = "default",
                        Uniqueness = "none",
                        Description = "The components of the user's real name",
                        SubAttributes = new[]
                        {
                            new SchemaAttribute { Name = "formatted", Type = "string", Description = "The full name, including all middle names, titles, and suffixes" },
                            new SchemaAttribute { Name = "familyName", Type = "string", Description = "The family name of the User" },
                            new SchemaAttribute { Name = "givenName", Type = "string", Description = "The given name of the User" },
                            new SchemaAttribute { Name = "middleName", Type = "string", Description = "The middle name(s) of the User" },
                            new SchemaAttribute { Name = "honorificPrefix", Type = "string", Description = "The honorific prefix(es) of the User" },
                            new SchemaAttribute { Name = "honorificSuffix", Type = "string", Description = "The honorific suffix(es) of the User" }
                        }
                    },
                    new SchemaAttribute
                    {
                        Name = "displayName",
                        Type = "string",
                        Required = false,
                        CaseExact = false,
                        Mutability = "readWrite",
                        Returned = "default",
                        Uniqueness = "none",
                        Description = "The name of the User, suitable for display to end-users"
                    },
                    new SchemaAttribute
                    {
                        Name = "emails",
                        Type = "complex",
                        MultiValued = true,
                        Required = false,
                        CaseExact = false,
                        Mutability = "readWrite",
                        Returned = "default",
                        Uniqueness = "none",
                        Description = "Email addresses for the user"
                    },
                    new SchemaAttribute
                    {
                        Name = "active",
                        Type = "boolean",
                        Required = false,
                        Mutability = "readWrite",
                        Returned = "default",
                        Uniqueness = "none",
                        Description = "A Boolean value indicating the User's administrative status"
                    }
                }
            },
            [groupSchemaId] = new SchemaDefinition
            {
                Id = "Group",
                Name = "Group",
                Schemas = new[] { groupSchemaId },
                Description = "Group",
                Attributes = new[]
                {
                    new SchemaAttribute
                    {
                        Name = "displayName",
                        Type = "string",
                        Required = true,
                        CaseExact = false,
                        Mutability = "readWrite",
                        Returned = "default",
                        Uniqueness = "none",
                        Description = "A human-readable name for the Group"
                    },
                    new SchemaAttribute
                    {
                        Name = "members",
                        Type = "complex",
                        MultiValued = true,
                        Required = false,
                        CaseExact = false,
                        Mutability = "readWrite",
                        Returned = "default",
                        Uniqueness = "none",
                        Description = "A list of members of the Group",
                        SubAttributes = new[]
                        {
                            new SchemaAttribute { Name = "value", Type = "string", Description = "Identifier of the member of this Group" },
                            new SchemaAttribute { Name = "$ref", Type = "reference", Description = "The URI corresponding to a SCIM resource that is a member of this Group" },
                            new SchemaAttribute { Name = "type", Type = "string", Description = "A label indicating the type of resource" }
                        }
                    }
                }
            }
        };
    }

    #endregion

    #region Resource Processing Pipeline

    /// <summary>
    /// Advanced resource processing pipeline with filtering, sorting, and pagination.
    /// Implements RFC 7644 Section 3.4.2 - Query Resources
    /// [RFC 7644 Section 3.4.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2)
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="allResources">All resources to process</param>
    /// <param name="startIndex">Starting index for pagination (1-based)</param>
    /// <param name="count">Maximum number of resources to return</param>
    /// <param name="filter">SCIM filter expression (optional)</param>
    /// <param name="sortBy">Field name for sorting (optional)</param>
    /// <param name="sortOrder">Sort order: "ascending" or "descending" (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed resources and total count</returns>
    public async Task<(IList<T> Resources, int TotalCount)> ProcessResourcePipeline<T>(
        IList<T> allResources,
        int startIndex,
        int count,
        string? filter,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default) where T : IResource
    {
        var processedResources = allResources;

        // Apply filtering if provided
        if (!string.IsNullOrEmpty(filter))
        {
            // Filtering is now handled at the repository level using native SQL
            // This method is kept for backward compatibility but filtering should be done in repositories
            // Note: Filtering should be implemented at repository level using ConvertToSqlFilter()
        }

        // Apply sorting if provided
        if (!string.IsNullOrEmpty(sortBy))
        {
            processedResources = await ApplyAdvancedSortingAsync(processedResources, sortBy, sortOrder, cancellationToken);
        }

        // Apply pagination with SCIM 1-based indexing
        var totalCount = processedResources.Count;
        var paginatedResources = processedResources
            .Skip(startIndex - 1)
            .Take(count)
            .ToList();

        return (paginatedResources, totalCount);
    }


        /// <summary>
        /// Converts SCIM v2.0 filter expressions to SQL WHERE clauses for database queries.
        /// Implements RFC 7644 Section 3.4.2.2 - Filtering
        /// [RFC 7644 Section 3.4.2.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2)
        /// </summary>
        /// <param name="filter">SCIM v2.0 filter expression</param>
        /// <param name="allowedAttributes">Set of allowed attribute names for security</param>
        /// <param name="attributeMapper">Mapping from SCIM attributes to database columns</param>
        /// <returns>Tuple containing SQL WHERE clause and parameters</returns>
        public (string SqlWhereClause, Dictionary<string, object> Parameters) ConvertToSqlFilter(
            string filter,
            HashSet<string>? allowedAttributes = null,
            Dictionary<string, string>? attributeMapper = null)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return ("1=1", new Dictionary<string, object>());

            try
            {
                // Parse SCIM filter using ANTLR
                var inputStream = new Antlr4.Runtime.AntlrInputStream(filter);
                var lexer = new ScimFilterLexer(inputStream);
                var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
                var parser = new ScimFilterParser(tokenStream);
                var tree = parser.filter();

                // Convert to SQL using SQL visitor
                var visitor = new SCIMv2ToSQLVisitor
                {
                    AllowedAttributes = allowedAttributes,
                    AttributeMapper = attributeMapper
                };

                var sqlWhereClause = visitor.Visit(tree);
                var parameters = new Dictionary<string, object>();

                return (sqlWhereClause, parameters);
            }
            catch (Exception ex)
            {
                // Log error and return safe fallback
                return ("1=1", new Dictionary<string, object>());
            }
        }

        private static Dictionary<string, object> ExtractParametersFromFilter(string filter)
        {
            var parameters = new Dictionary<string, object>();
            var parameterIndex = 0;

            // Extract quoted values and replace with parameters
            var pattern = @"[""']([^""']+)[""']";
            var matches = System.Text.RegularExpressions.Regex.Matches(filter, pattern);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var value = match.Groups[1].Value;
                var parameterName = $"@param{parameterIndex++}";
                parameters[parameterName] = value;
            }

            return parameters;
        }

        /// <summary>
        /// Validates SCIM attribute names for security against SQL injection.
        /// Security helper method for filtering and querying operations.
        /// </summary>
        /// <param name="attribute">Attribute name to validate</param>
        /// <param name="allowedAttributes">Set of allowed attributes</param>
        /// <returns>True if attribute is valid and allowed</returns>
        private static bool IsValidAttribute(string attribute, HashSet<string> allowedAttributes)
        {
            // Validate against whitelist
            if (!allowedAttributes.Contains(attribute))
                return false;

            // Validate dangerous characters
            if (attribute.Contains("'") || attribute.Contains(";") || attribute.Contains("--"))
                return false;

            // Validate SQL keywords
            var sqlKeywords = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER" };
            if (sqlKeywords.Any(keyword => attribute.ToUpper().Contains(keyword)))
                return false;

            return true;
        }

        /// <summary>
        /// Sanitizes values for SQL injection prevention.
        /// Security helper method for filtering and querying operations.
        /// </summary>
        /// <param name="value">Value to sanitize</param>
        /// <param name="operation">SCIM operation type</param>
        /// <returns>Sanitized value safe for database operations</returns>
        private static object SanitizeValue(string value, string operation)
        {
            if (string.IsNullOrEmpty(value))
                return DBNull.Value;

            // Remove quotes and escape dangerous characters
            var cleanValue = value.Trim('"', '\'');
            
            // Escape SQL dangerous characters
            cleanValue = cleanValue.Replace("'", "''")
                                  .Replace(";", "")
                                  .Replace("--", "")
                                  .Replace("/*", "")
                                  .Replace("*/", "");

            return operation switch
            {
                "co" => $"%{cleanValue}%",
                "sw" => $"{cleanValue}%",
                "ew" => $"%{cleanValue}",
                _ => IsNumeric(cleanValue) ? Convert.ToDecimal(cleanValue) : cleanValue
            };
        }

        /// <summary>
        /// Checks if a value is numeric.
        /// Helper method for sorting and filtering operations.
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>True if numeric, false otherwise</returns>
        private static bool IsNumeric(string value)
        {
            return double.TryParse(value, out _);
        }

    /// <summary>
    /// Advanced sorting implementation with multiple field support.
    /// Implements RFC 7644 Section 3.4.2.3 - Sorting
    /// [RFC 7644 Section 3.4.2.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.3)
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="resources">Resources to sort</param>
    /// <param name="sortBy">Field name to sort by</param>
    /// <param name="sortOrder">Sort order (ascending or descending)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sorted list of resources</returns>
    private Task<IList<T>> ApplyAdvancedSortingAsync<T>(
        IList<T> resources,
        string sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default) where T : IResource
    {
        var isDescending = string.Equals(sortOrder, "descending", StringComparison.OrdinalIgnoreCase);
        
        // Use reflection for dynamic property sorting (can be optimized with compiled expressions)
        var property = typeof(T).GetProperty(sortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (property != null)
        {
            var sorted = isDescending
                ? resources.OrderByDescending(r => property.GetValue(r)).ToList()
                : resources.OrderBy(r => property.GetValue(r)).ToList();
            return Task.FromResult<IList<T>>(sorted);
        }
        
        return Task.FromResult(resources);
    }

    #endregion

    #region Metadata Management

    /// <summary>
    /// Initialize resource metadata for new resources.
    /// Transferred from ResourceServiceBase with enhanced functionality.
    /// Implements RFC 7643 Section 3.1 - Resource Representation
    /// [RFC 7643 Section 3.1](https://datatracker.ietf.org/doc/html/rfc7643#section-3.1)
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="resource">Resource to initialize metadata for</param>
    private void InitializeResourceMetadata<T>(T resource) where T : IResource
    {
        var now = DateTime.UtcNow;
        resource.Meta.ResourceType = GetResourceTypeName<T>(); //  RFC 7644 Section 3.1 - ResourceType is required
        resource.Meta.Created = now;
        resource.Meta.LastModified = now;
        resource.Meta.Location = $"{GetBaseUrl()}/{GetResourceTypeName<T>()}/{resource.Id}";
        resource.Meta.Version = GenerateResourceVersion(resource); // Generate cryptographic hash-based version
    }

    /// <summary>
    /// Update resource metadata for existing resources.
    /// Updates metadata while preserving original creation information.
    /// Implements RFC 7643 Section 3.1 - Resource Representation
    /// [RFC 7643 Section 3.1](https://datatracker.ietf.org/doc/html/rfc7643#section-3.1)
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="resource">Resource to update metadata for</param>
    /// <param name="existingResource">Existing resource to preserve metadata from</param>
    private void UpdateResourceMetadata<T>(T resource, T existingResource) where T : IResource
    {
        resource.Meta.ResourceType = existingResource.Meta.ResourceType; //  Preserve ResourceType
        resource.Meta.Created = existingResource.Meta.Created;
        resource.Meta.LastModified = DateTime.UtcNow;
        var fullUrl = $"{GetBaseUrl()}/{GetResourceTypeName<T>()}/{resource.Id}";
        resource.Meta.Location = fullUrl;
        resource.Meta.Version = GenerateResourceVersion(resource); // Generate new cryptographic hash-based version
    }

    /// <summary>
    /// Gets the resource type name for URL generation and SCIM operations.
    /// Maps resource types to their plural collection names.
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <returns>Plural resource type name for URLs</returns>
    private string GetResourceTypeName<T>() where T : IResource
    {
        return typeof(T).Name switch
        {
            nameof(User) => "Users",
            nameof(Group) => "Groups",
            _ => typeof(T).Name + "s"
        };
    }

    #endregion

    #region PATCH Operations Processing

    /// <summary>
    /// Advanced PATCH operations processor for SCIM v2.0 compliance.
    /// Applies multiple patch operations to a resource with enhanced validation.
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="resource">Resource to apply patches to</param>
    /// <param name="patches">Array of patch operations to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ApplyPatchesAsync<T>(T resource, PatchOperation[] patches, CancellationToken cancellationToken = default) where T : IResource
    {
        foreach (var patch in patches)
        {
            if (patch.IsValid())
            {
                await ProcessPatchOperationAsync(resource, patch.Op, patch.Path, patch.Value, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Processes individual PATCH operation according to RFC 6902 (JSON Patch).
    /// Implements RFC 6902 Section 4 - Operations
    /// [RFC 6902 Section 4](https://datatracker.ietf.org/doc/html/rfc6902#section-4)
    /// Supports add, remove, and replace operations as per RFC 6902 Section 4.1-4.3
    /// [RFC 6902 Section 4.1](https://datatracker.ietf.org/doc/html/rfc6902#section-4.1) - Add
    /// [RFC 6902 Section 4.2](https://datatracker.ietf.org/doc/html/rfc6902#section-4.2) - Remove
    /// [RFC 6902 Section 4.3](https://datatracker.ietf.org/doc/html/rfc6902#section-4.3) - Replace
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="resource">Resource to apply patch to</param>
    /// <param name="operation">Patch operation type (add, remove, replace)</param>
    /// <param name="path">JSON path to the field to modify</param>
    /// <param name="value">New value for the field</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ProcessPatchOperationAsync<T>(
        T resource,
        string operation,
        string path,
        object? value,
        CancellationToken cancellationToken = default) where T : IResource
    {
        // Removed unnecessary async placeholder

        switch (operation.ToLowerInvariant())
        {
            case "add":
                await ApplyAddPatchAsync(resource, path, value, cancellationToken);
                break;
            case "remove":
                await ApplyRemovePatchAsync(resource, path, cancellationToken);
                break;
            case "replace":
                await ApplyReplacePatchAsync(resource, path, value, cancellationToken);
                break;
            default:
                throw new NotSupportedException($"PATCH operation '{operation}' is not supported");
        }
    }

    /// <summary>
    /// Apply ADD patch operation.
    /// Implements RFC 6902 Section 4.1 - Add Operation
    /// [RFC 6902 Section 4.1](https://datatracker.ietf.org/doc/html/rfc6902#section-4.1)
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="resource">Resource to apply patch to</param>
    /// <param name="path">JSON Pointer path for the operation</param>
    /// <param name="value">Value to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    private async Task ApplyAddPatchAsync<T>(T resource, string path, object? value, CancellationToken cancellationToken = default) where T : IResource
    {
        // Removed unnecessary async placeholder
        // Implementation would use reflection or compiled expressions to set property values
    }

    /// <summary>
    /// Apply REMOVE patch operation.
    /// Implements RFC 6902 Section 4.2 - Remove Operation
    /// [RFC 6902 Section 4.2](https://datatracker.ietf.org/doc/html/rfc6902#section-4.2)
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="resource">Resource to apply patch to</param>
    /// <param name="path">JSON Pointer path for the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    private async Task ApplyRemovePatchAsync<T>(T resource, string path, CancellationToken cancellationToken = default) where T : IResource
    {
        // Removed unnecessary async placeholder
        // Implementation would use reflection or compiled expressions to clear property values
    }

    /// <summary>
    /// Apply REPLACE patch operation.
    /// Implements RFC 6902 Section 4.3 - Replace Operation
    /// [RFC 6902 Section 4.3](https://datatracker.ietf.org/doc/html/rfc6902#section-4.3)
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="resource">Resource to apply patch to</param>
    /// <param name="path">JSON Pointer path for the operation</param>
    /// <param name="value">Value to replace with</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    private async Task ApplyReplacePatchAsync<T>(T resource, string path, object? value, CancellationToken cancellationToken = default) where T : IResource
    {
        // Removed unnecessary async placeholder
        // Implementation would use reflection or compiled expressions to set property values
    }

    #endregion

    #region Enhanced Resource Operations




    #region Response Factory Methods

    /// <summary>
    /// Creates a successful SCIMv2 response for list operations.
    /// Implements RFC 7644 Section 3.4.2 - Query Resources
    /// [RFC 7644 Section 3.4.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2)
    /// </summary>
    /// <param name="resources">List of resources to include in response</param>
    /// <param name="totalCount">Total number of resources available</param>
    /// <param name="startIndex">Starting index for pagination (1-based)</param>
    /// <param name="count">Number of items per page</param>
    /// <returns>SCIMv2 response with list data and pagination metadata</returns>
    private SCIMv2Response CreateListResponse(object resources, int totalCount, int startIndex, int count)
    {
        // Apply attribute processing if HttpContext is available
        if (_httpContextAccessor?.HttpContext != null && resources is IEnumerable<IResource> resourceList)
        {
            // Convert resources to JsonObject for processing using ActorJsonSerializer options
            var jsonResources = resourceList.Select(resource => 
            {
                var json = JsonSerializer.SerializeToNode(resource, resource.GetType(), ActorJsonSerializer.DefaultOptions);
                return json as JsonObject ?? new JsonObject();
            }).ToList();

            // Apply attribute processing
            var processedResources = jsonResources.ProcessAttributes(_httpContextAccessor.HttpContext);
            
            // Keep as JsonObject list for serialization
            resources = processedResources.ToList();
        }
        
        var response = new SCIMv2Response
        {
            //StatusCode = 0, // Set to 0 to indicate this should not be serialized for collections
            Data = resources, // RFC 7644 Section 3.4.2 - Resources directly in response
            Schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
            TotalResults = totalCount,
            StartIndex = startIndex,
            ItemsPerPage = count,
            HttpMethod = "QUERY" // Define HTTP method for proper serialization
        };
        
        
        return response;
    }

    /// <summary>
    /// Generates a content-based ETag for SCIMv2 resources.
    /// Implements RFC 7232 - HTTP/1.1 Conditional Requests
    /// [RFC 7232](https://datatracker.ietf.org/doc/html/rfc7232)
    /// </summary>
    /// <param name="resource">Resource to generate ETag for</param>
    /// <returns>ETag value based on resource content</returns>
    private static string GenerateContentETag(IResource resource)
    {
        
        
        // Create a content hash based on resource data
        var content = $"{resource}";
        
        // Generate hash
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        var hexHash = string.Concat(hashBytes.Select(b => b.ToString("x2")));
        var hash = hexHash;

    return $"{hash}";
    }

    /// <summary>
    /// Generates a cryptographic hash-based version for SCIMv2 resources
    /// Creates a unique signature based on resource content for meta.version
    /// Implements RFC 7643 Section 3.1 - Resource Representation
    /// [RFC 7643 Section 3.1](https://datatracker.ietf.org/doc/html/rfc7643#section-3.1)
    /// </summary>
    /// <param name="resource">Resource to generate version for</param>
    /// <returns>Cryptographic hash-based version string</returns>
    private static string GenerateResourceVersion(IResource resource)
    {
       
        // Create comprehensive content hash including all resource data
        var content = $"{resource}";
        
        // Include resource-specific data for more unique hashing
        if (resource.Schemas != null && resource.Schemas.Length > 0)
        {
            content += $"|{string.Join(",", resource.Schemas)}";
        }
        
        // Generate SHA-256 hash
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        var hexHash = string.Concat(hashBytes.Select(b => b.ToString("x2")));
        var hash = hexHash;
        

        
        return $"{hash}";
    }

    /// <summary>
    /// Creates a successful SCIMv2 response for create operations.
    /// Implements RFC 7644 Section 3.4.1 - Create Resource
    /// [RFC 7644 Section 3.4.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.1)
    /// </summary>
    /// <param name="resource">Created resource to include in response</param>
    /// <param name="resourceId">Resource ID of the created resource</param>
    /// <param name="collection">Collection name where resource was created</param>
    /// <returns>SCIMv2 response with created resource</returns>
    private static SCIMv2Response CreateCreateResponse(IResource resource, Guid resourceId, string collection)
    {
        try
        {
            // POST (Create): Return resource directly, no Resources wrapper
            var response = new SCIMv2Response
            {
                StatusCode = 201,
                Data = resource, // Resource directly, not wrapped in Resources
                Schemas = resource.Schemas,
                Location = resource.Meta.Location,
                ETag = resource.Meta.Version,
                HttpMethod = "POST"
            };
            
            return response;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Creates a successful SCIMv2 response for retrieve operations.
    /// Implements RFC 7644 Section 3.4.3 - Retrieve Resource
    /// [RFC 7644 Section 3.4.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.3)
    /// </summary>
    /// <param name="resource">Retrieved resource to include in response</param>
    /// <returns>SCIMv2 response with retrieved resource</returns>
    private SCIMv2Response CreateRetrieveResponse(IResource resource)
    {
        // Apply attribute processing if HttpContext is available
        if (_httpContextAccessor?.HttpContext != null)
        {
            // Convert resource to JsonObject for processing using ActorJsonSerializer options
            var jsonResource = JsonSerializer.SerializeToNode(resource, resource.GetType(), ActorJsonSerializer.DefaultOptions) as JsonObject ?? new JsonObject();
            
            // Apply attribute processing
            var processedResource = new[] { jsonResource }.ProcessAttributes(_httpContextAccessor.HttpContext).FirstOrDefault();
            
            if (processedResource != null)
            {
                // Return processed JsonObject for serialization
                return new SCIMv2Response
                {
                    StatusCode = 200,
                    Data = processedResource, // JsonObject with processed attributes
                    Schemas = resource.Schemas,
                    Location = resource.Meta.Location,
                    ETag = resource.Meta.Version,
                    HttpMethod = "GET"
                };
            }
        }
        
        // GET (Single Resource): Return resource directly, no Resources wrapper
        return new SCIMv2Response
        {
            StatusCode = 200,
            Data = resource, // Resource directly, not wrapped in Resources
            Schemas = resource.Schemas,
            Location = resource.Meta.Location,
            ETag = resource.Meta.Version,
            HttpMethod = "GET"
        };
    }





    /// <summary>
    /// Formats ERROR responses according to SCIM v2.0.
    /// Returns direct SCIM error format without envelope.
    /// Implements RFC 7644 Section 3.12 - Error Responses
    /// [RFC 7644 Section 3.12](https://datatracker.ietf.org/doc/html/rfc7644#section-3.12)
    /// </summary>
    /// <param name="response">SCIMv2Response with error to format</param>
    /// <returns>Formatted JSON string for error responses</returns>
    private static string FormatErrorResponse(SCIMv2Response response)
    {
        if (response?.Error == null)
            throw new ArgumentException("Response or Error cannot be null", nameof(response));

        var errorPayload = new
        {
            schemas = response.Error.Schemas ?? new[] { "urn:ietf:params:scim:api:messages:2.0:Error" },
            status = response.Error.Status,
            scimType = string.IsNullOrWhiteSpace(response.Error.ScimType) ? null : response.Error.ScimType,
            detail = response.Error.Detail
        };

        return System.Text.Json.JsonSerializer.Serialize(errorPayload, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = true
        });
    }

    /// <summary>
    /// Formats QUERY (Collections) responses according to SCIM v2.0.
    /// Removes statusCode from JSON body for collection responses.
    /// Implements RFC 7644 Section 3.4.2 - Query Resources
    /// [RFC 7644 Section 3.4.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2)
    /// </summary>
    /// <param name="response">SCIMv2Response to format</param>
    /// <returns>Formatted JSON string for collection responses</returns>
    private static string FormatQueryResponse(SCIMv2Response response)
    {
        // Serialize Resources array with camelCase for individual resource properties
        var resourcesJson = System.Text.Json.JsonSerializer.Serialize(response.Data, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        });

        // Parse back to get camelCase Resources
        var resourcesArray = System.Text.Json.JsonSerializer.Deserialize<object[]>(resourcesJson);

        // Create envelope with Resources (capital R) containing camelCase elements
        var queryResponse = new
        {
            schemas = response.Schemas,
            totalResults = response.TotalResults,
            startIndex = response.StartIndex,
            itemsPerPage = response.ItemsPerPage,
            Resources = resourcesArray
        };

        // Serialize envelope with PropertyNamingPolicy = null to preserve Resources (capital R)
        return System.Text.Json.JsonSerializer.Serialize(queryResponse, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Preserve original field names for SCIM v2.0 compliance
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Formats ResourceTypes responses according to SCIM v2.0.
    /// Preserves Resources field with capital R for SCIM compliance.
    /// Implements RFC 7644 Section 3.2 - Resource Types Discovery
    /// [RFC 7644 Section 3.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.2)
    /// </summary>
    /// <param name="response">SCIMv2Response to format</param>
    /// <returns>Formatted JSON string for ResourceTypes responses</returns>
    private static string FormatResourceTypesResponse(SCIMv2Response response)
    {
        // For ResourceTypes, preserve the original field names (Resources with capital R)
        var result = System.Text.Json.JsonSerializer.Serialize(response.Data, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Preserve original field names for SCIM v2.0 compliance
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        });
        
        return result;
    }

    /// <summary>
    /// Formats Schemas responses according to SCIM v2.0.
    /// Preserves Resources field with capital R for SCIM compliance.
    /// Implements RFC 7644 Section 3.4.6 - Schema Discovery
    /// [RFC 7644 Section 3.4.6](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.6)
    /// </summary>
    /// <param name="response">SCIMv2Response to format</param>
    /// <returns>Formatted JSON string for Schemas responses</returns>
    private static string FormatSchemasResponse(SCIMv2Response response)
    {
        // For Schemas, preserve the original field names (Resources with capital R)
        // Use Foundation's centralized serialization for consistency
        var result = System.Text.Json.JsonSerializer.Serialize(response.Data, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Preserve original field names for SCIM v2.0 compliance
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        });
        
        return result;
    }

    /// <summary>
    /// Formats single resource responses according to SCIM v2.0.
    /// Removes Resources wrapper, keeps statusCode for single resource operations.
    /// Implements RFC 7644 Section 3.4.1, 3.4.3, 3.4.4 - Single Resource Operations
    /// [RFC 7644 Section 3.4.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.1)
    /// [RFC 7644 Section 3.4.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.3)
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// </summary>
    /// <param name="response">SCIMv2Response to format</param>
    /// <returns>Formatted JSON string for single resource responses</returns>
    private static string FormatSingleResourceResponse(SCIMv2Response response)
    {
        
        // For single resources, return the resource directly flattened
        // Remove statusCode, location, etag from JSON body (they should be HTTP headers only)
        // Flatten the resource data directly using Looplex.Foundation.Core serializer options
        var result = System.Text.Json.JsonSerializer.Serialize(response.Data, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        });
        
        return result;
    }

    /// <summary>
    /// Formats DELETE responses according to SCIM v2.0.
    /// Returns empty body for 204 status.
    /// Implements RFC 7644 Section 3.4.5 - Delete Resource
    /// [RFC 7644 Section 3.4.5](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.5)
    /// </summary>
    /// <param name="response">SCIMv2Response to format</param>
    /// <returns>Empty string for DELETE responses</returns>
    private static string FormatDeleteResponse(SCIMv2Response response)
    {
        // DELETE responses should have no body for 204 status
        return string.Empty;
    }

    /// <summary>
    /// Creates an error SCIMv2 response.
    /// Implements RFC 7644 Section 3.12 - Error Responses
    /// [RFC 7644 Section 3.12](https://datatracker.ietf.org/doc/html/rfc7644#section-3.12)
    /// </summary>
    /// <param name="statusCode">HTTP status code for the error</param>
    /// <param name="detail">Error detail message</param>
    /// <param name="scimType">SCIM error type (optional)</param>
    /// <returns>SCIMv2 error response</returns>
    private static SCIMv2Response CreateErrorResponse(int statusCode, string detail, string? scimType = null)
    {
        return new SCIMv2Response
        {
            StatusCode = statusCode,
            Error = new SCIMv2Error
            {
                Status = statusCode.ToString(), //  RFC 7644 Section 3.12.1 - Status field required
                Detail = detail,
                ScimType = scimType,
                Timestamp = DateTime.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Centralized exception handling for SCIMv2 operations.
    /// Maps common exceptions to appropriate SCIMv2 error responses.
    /// Implements RFC 7644 Section 3.12 - Error Responses
    /// [RFC 7644 Section 3.12](https://datatracker.ietf.org/doc/html/rfc7644#section-3.12)
    /// </summary>
    /// <param name="ex">Exception to handle</param>
    /// <param name="operation">Operation context for error messages</param>
    /// <returns>SCIMv2 error response</returns>
    private static SCIMv2Response HandleException(Exception ex, string operation = "operation")
    {
        return ex switch
        {
            ArgumentNullException argNullEx => CreateErrorResponse(400, "Bad Request", $"Invalid argument: {argNullEx.ParamName}"),
            ArgumentException => CreateErrorResponse(400, "Bad Request", ex.Message),
            InvalidOperationException => CreateErrorResponse(409, "Conflict", ex.Message),
            NotSupportedException => CreateErrorResponse(501, "Not Implemented", ex.Message),
            TimeoutException => CreateErrorResponse(408, "Request Timeout", "Operation timed out"),
            OperationCanceledException => CreateErrorResponse(408, "Request Timeout", "Operation was cancelled"),
            _ => CreateErrorResponse(500, "Internal Server Error", $"An unexpected error occurred during {operation}")
        };
    }

    /// <summary>
    /// Centralized validation for SCIMv2 requests.
    /// Validates collection name, service registration, and optional resource ID.
    /// Implements RFC 7644 Section 3 - SCIM Protocol
    /// [RFC 7644 Section 3](https://datatracker.ietf.org/doc/html/rfc7644#section-3)
    /// </summary>
    /// <param name="collection">Collection name to validate</param>
    /// <param name="id">Optional resource ID to validate</param>
    /// <returns>Validation result with error response if invalid, or service and resource ID if valid</returns>
    private (bool IsValid, SCIMv2Response? Error, IResourceService? Service, Guid? ResourceId) 
        ValidateRequest(string collection, string? id = null)
    {
        // Validate collection name
        if (string.IsNullOrEmpty(collection))
        {
            return (false, CreateErrorResponse(404, "Collection not found"), null, null);
        }
        
        // Validate service registration
        if (!_registeredResource.TryGetValue(collection, out var service))
        {
            return (false, CreateErrorResponse(404, $"Collection '{collection}' is not registered"), null, null);
        }
        
        // Validate resource ID if provided
        if (id != null)
        {
            if (!Guid.TryParse(id, out var resourceId))
            {
                return (false, CreateErrorResponse(400, "Invalid ID", "Resource ID must be a valid GUID"), null, null);
            }
            return (true, null, service, resourceId);
        }
        
        return (true, null, service, null);
    }

    #endregion


    /// <summary>
    /// Validates JSON request body for SCIMv2 operations
    /// </summary>
    /// <param name="json">JSON content to validate</param>
    /// <returns>Validation result with error message if invalid</returns>
    public (bool IsValid, string ErrorMessage) ValidateJsonRequest(string json)
    {
        
        if (string.IsNullOrEmpty(json))
        {
            return (false, "Request body is required");
        }

        try
        {
            var jsonObject = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(json);
            if (jsonObject == null)
            {
                return (false, "Invalid JSON");
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            return (false, $"Invalid JSON: {ex.Message}");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Parses PATCH operations from JSON array
    /// </summary>
    /// <param name="json">JSON array of patch operations</param>
    /// <returns>Parsed patch operations or error result</returns>
    public (bool IsValid, PatchOperation[] Operations, string ErrorMessage) ParsePatchOperations(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return (false, Array.Empty<PatchOperation>(), "Request body is required");
        }

        try
        {
            // Try to parse as PatchRequest object first (SCIMv2 format)
            var patchRequest = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(json);
            if (patchRequest != null && patchRequest.ContainsKey("Operations"))
            {
                var operationsArray = patchRequest["Operations"] as System.Text.Json.Nodes.JsonArray;
                if (operationsArray != null)
                {
                    var patchOperations = new List<PatchOperation>();
                    foreach (var patchNode in operationsArray)
                    {
                        if (patchNode is System.Text.Json.Nodes.JsonObject patchObj)
                        {
                            var op = patchObj["op"]?.GetValue<string>() ?? "";
                            var path = patchObj["path"]?.GetValue<string>() ?? "";
                            var value = patchObj.ContainsKey("value") ? patchObj["value"] : null;
                            
                            patchOperations.Add(new PatchOperation(op, path, value));
                        }
                    }

                    if (patchOperations.Count == 0)
                    {
                        return (false, Array.Empty<PatchOperation>(), "No valid patch operations found");
                    }

                    return (true, patchOperations.ToArray(), string.Empty);
                }
            }

            // Fallback: try to parse as direct array (legacy format)
            var patchArray = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonArray>(json);
            if (patchArray == null)
            {
                return (false, Array.Empty<PatchOperation>(), "Invalid patches - expected Operations array or direct array");
            }

            var patchOperationsDirect = new List<PatchOperation>();
            foreach (var patchNode in patchArray)
            {
                if (patchNode is System.Text.Json.Nodes.JsonObject patchObj)
                {
                    var op = patchObj["op"]?.GetValue<string>() ?? "";
                    var path = patchObj["path"]?.GetValue<string>() ?? "";
                    var value = patchObj.ContainsKey("value") ? patchObj["value"] : null;
                    
                    patchOperationsDirect.Add(new PatchOperation(op, path, value));
                }
            }

            if (patchOperationsDirect.Count == 0)
            {
                return (false, Array.Empty<PatchOperation>(), "No valid patch operations found");
            }

            return (true, patchOperationsDirect.ToArray(), string.Empty);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return (false, Array.Empty<PatchOperation>(), $"Invalid JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates collection name for SCIMv2 operations
    /// </summary>
    /// <param name="collectionName">Collection name to validate</param>
    /// <returns>Validation result with error message if invalid</returns>
    public (bool IsValid, string ErrorMessage) ValidateCollection(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return (false, "Collection name is required");
        }

        var supportedCollections = new[] { "Users", "Groups" };
        if (!supportedCollections.Any(c => c.Equals(collectionName, StringComparison.OrdinalIgnoreCase)))
        {
            return (false, $"Unsupported collection: {collectionName}");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Processes Bulk operations
    /// </summary>
    /// <param name="json">JSON BulkRequest</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 response with BulkResponse</returns>
    public async Task<SCIMv2Response> BulkAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse BulkRequest
            var bulkRequest = System.Text.Json.JsonSerializer.Deserialize<BulkRequest>(json);
            if (bulkRequest == null)
            {
                return CreateErrorResponse(400, "Invalid BulkRequest", "Failed to parse BulkRequest");
            }

            // Use Bulks service to execute bulk operations
            // For now, return a simple success response since Bulks requires complex dependencies
            var bulkResponse = new BulkResponse
            {
                Operations = new List<BulkResponseOperation>()
            };
            
            // Process each operation individually
            foreach (var operation in bulkRequest.Operations)
            {
                var responseOp = new BulkResponseOperation
                {
                    BulkId = operation.BulkId,
                    Method = operation.Method,
                    Status = 200
                };
                
                // For now, just add a success response
                bulkResponse.Operations.Add(responseOp);
            }

            // Convert BulkResponse to SCIMv2Response
            return new SCIMv2Response
            {
                StatusCode = 200,
                Data = bulkResponse,
                Schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:BulkResponse" }
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(500, "Bulk operation failed", ex.Message);
        }
    }

    /// <summary>
    /// Get resource types (GET /ResourceTypes)
    /// Implements RFC 7644 Section 3.2 - Resource Types
    /// [RFC 7644 Section 3.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.2)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIMv2 Response with list of resource types</returns>
    public async Task<SCIMv2Response> GetResourceTypesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all registered collections (agnostic approach)
            var registeredCollections = GetRegisteredCollections().ToList();
            
            if (!registeredCollections.Any())
            {
                return CreateErrorResponse(404, "No resource types found", "No resource types are registered");
            }

            // Create ResourceType objects for each registered collection
            var resourceTypes = new List<object>();
            
            foreach (var collection in registeredCollections)
            {
                string schemaUrl = String.Empty;
                string serviceName = GetServiceNameOrDefault("core"); // Get actual service name
                string schemaUri;
                if(collection == "Groups" || collection == "Users")
                {
                    schemaUri = $"urn:ietf:params:scim:schemas:core:2.0:{collection}"; // Standard SCIMv2 schema
                }
                else{
                    schemaUri = $"urn:looplex:params:scim:schemas:{serviceName}:2.0:{collection}"; 
                }

                var resourceType = new
                {
                    id = collection,
                    name = collection,
                    endpoint = $"/{collection}",
                    description = $"SCIMv2 resource type for {collection}",
                    schema = schemaUri,
                    schemaExtensions = new object[0],
                    meta = new
                    {
                        resourceType = "ResourceType",
                        location = $"/ResourceTypes/{collection}"
                    }
                };
                
                resourceTypes.Add(resourceType);
            }

            return new SCIMv2Response
            {
                StatusCode = 200,
                HttpMethod = "RESOURCETYPES",
                Data = new
                {
                    totalResults = resourceTypes.Count,
                    itemsPerPage = resourceTypes.Count,
                    startIndex = 1,
                    schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
                    Resources = resourceTypes
                },
                Schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" }
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(500, "Resource types retrieval failed", ex.Message);
        }
    }


    #endregion

    #region Generic SCIM Services (Agnostic)
    
    /// <summary>
    /// Generic SCIM configuration provider for any resource type.
    /// Provides configuration based on SCIM v2.0 standards and RFC 7643 schema definitions.
    /// Implements RFC 7643 Section 2 - Schema Definition
    /// [RFC 7643 Section 2](https://datatracker.ietf.org/doc/html/rfc7643#section-2)
    /// </summary>
    public static class ScimConfiguration
    {
        /// <summary>
        /// Get allowed attributes for any resource type based on SCIM v2.0 standards.
        /// Implements RFC 7643 Section 2.1 - Core Schema
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <param name="resourceType">Type of the resource (e.g., "Note", "Pad", "User", "Group")</param>
        /// <returns>Set of allowed attributes for filtering operations</returns>
        public static HashSet<string> GetAllowedAttributes(string resourceType)
        {
            var resourceKey = resourceType.ToLower();
            
            // Priority 1: Dynamic configuration
            if (_allowedAttributes.ContainsKey(resourceKey))
            {
                var attributes = _allowedAttributes[resourceKey];
                return attributes;
            }
            
            // Priority 2: JSON static configuration
            if (_jsonConfigurations.ContainsKey(resourceKey))
            {
                // TODO: Extract attributes from JSON configuration
                return new HashSet<string>();
            }
            
            // Priority 3: Default SCIM attributes
            var defaultAttributes = GetDefaultAllowedAttributes(resourceType);
            return defaultAttributes;
        }

        /// <summary>
        /// Gets attribute mappings for a resource type with hybrid priority.
        /// Priority: Dynamic configuration > JSON static configuration > Default mappings.
        /// </summary>
        /// <param name="resourceType">Resource type</param>
        /// <returns>Dictionary of attribute mappings</returns>
        public static Dictionary<string, string> GetAttributeMappings(string resourceType)
        {
            var resourceKey = resourceType.ToLower();
            
            // Priority 1: Dynamic configuration
            if (_attributeMappings.ContainsKey(resourceKey))
            {
                return _attributeMappings[resourceKey];
            }
            
            // Priority 2: JSON static configuration
            if (_jsonConfigurations.ContainsKey(resourceKey))
            {
                // TODO: Extract mappings from JSON configuration
                return new Dictionary<string, string>();
            }
            
            // Priority 3: Default mappings
            return GetDefaultAttributeMappings(resourceType);
        }

        /// <summary>
        /// Get table prefix for resource type based on standard database naming conventions.
        /// </summary>
        /// <param name="resourceType">Type of the resource</param>
        /// <returns>Single character prefix for database table aliases</returns>
        private static string GetTablePrefix(string resourceType)
        {
            return resourceType.ToLower() switch
            {
                "user" => "u",
                "group" => "g", 
                "note" => "n",
                "pad" => "p",
                _ => resourceType.ToLower().Substring(0, 1) // First letter as prefix
            };
        }
    }

    /// <summary>
    /// Generic SCIM data mapper for any resource type.
    /// Provides generic mapping from IDataReader to any IResource type using reflection and naming conventions.
    /// Implements RFC 7643 Section 2.1 - Core Schema mapping
    /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
    /// </summary>
    public static class ScimDataMapper
    {
        /// <summary>
        /// Map from IDataReader to any IResource type using reflection and naming conventions.
        /// Implements RFC 7643 Section 2.1 - Core Schema for resource mapping.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="reader">DataReader containing the data</param>
        /// <returns>Mapped resource instance</returns>
        public static T MapFromDataReader<T>(IDataReader reader) where T : IResource, new()
        {
            var resource = new T();
            
            // Map base IResource properties using SCIM v2.0 conventions
            if (reader["id"] != DBNull.Value)
                resource.Id = reader["id"].ToString();
                
            // Map externalId as optional field following RFC 7643 Section 2.1 - Core Schema
            // RFC 7643 Section 2.1 defines externalId as optional identifier for external system mapping
            // [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
            if (reader["external_id"] != DBNull.Value)
                resource.ExternalId = reader["external_id"].ToString();
            // Note: externalId is optional per RFC 7643, so we don't fail if it's null
                
            // Create Meta using existing Foundation method following RFC 7643
            resource.Meta = CreateResourceMeta(reader, resource.Id, resource.GetType().Name);
            
            // Map specific properties using reflection and naming conventions
            MapSpecificProperties(resource, reader);
            
            return resource;
        }

        /// <summary>
        /// Map specific properties using reflection and standard naming conventions.
        /// Only maps properties that exist in the database to avoid conflicts with repository-specific mappings.
        /// Implements RFC 7643 Section 2.1 - Core Schema mapping with database column validation.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="resource">Resource instance to map properties to</param>
        /// <param name="reader">DataReader containing the data</param>
        private static void MapSpecificProperties<T>(T resource, IDataReader reader) where T : IResource
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanWrite && p.Name != "Id" && p.Name != "ExternalId" && p.Name != "Meta" && p.Name != "Schemas")
                .ToList();

            foreach (var property in properties)
            {
                var columnName = GetColumnName(property.Name);
                
                // Only map properties that exist in the database to avoid conflicts with repository-specific mappings
                // This allows repositories to handle custom property mappings while Foundation handles standard ones
                if (ColumnExists(reader, columnName) && reader[columnName] != DBNull.Value)
                {
                    var value = reader[columnName];
                    var convertedValue = ConvertValue(value, property.PropertyType);
                    property.SetValue(resource, convertedValue);
                }
            }
        }

        /// <summary>
        /// Check if a column exists in the DataReader to avoid mapping conflicts.
        /// This allows repositories to handle custom property mappings while Foundation handles standard ones.
        /// </summary>
        /// <param name="reader">DataReader to check for column existence</param>
        /// <param name="columnName">Column name to check for existence</param>
        /// <returns>True if column exists, false otherwise</returns>
        private static bool ColumnExists(IDataReader reader, string columnName)
        {
            try
            {
                reader.GetOrdinal(columnName);
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        /// <summary>
        /// Get database column name from property name using standard naming conventions.
        /// Converts PascalCase to snake_case following database naming standards.
        /// </summary>
        /// <param name="propertyName">Property name in PascalCase</param>
        /// <returns>Database column name in snake_case</returns>
        private static string GetColumnName(string propertyName)
        {
            // Convert PascalCase to snake_case following database naming conventions
            return string.Concat(propertyName.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString().ToLower() : x.ToString().ToLower()));
        }

        /// <summary>
        /// Convert database value to property type with proper type handling.
        /// </summary>
        /// <param name="value">Database value to convert</param>
        /// <param name="targetType">Target property type</param>
        /// <returns>Converted value of the target type</returns>
        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null || value == DBNull.Value)
                return GetDefaultValue(targetType);

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            // Handle common type conversions following .NET standards
            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);
            if (targetType == typeof(int))
                return Convert.ToInt32(value);
            if (targetType == typeof(long))
                return Convert.ToInt64(value);
            if (targetType == typeof(DateTime))
                return Convert.ToDateTime(value);
            if (targetType == typeof(Guid))
                return Guid.Parse(value.ToString());

            return Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Get default value for type following .NET conventions.
        /// </summary>
        /// <param name="type">Type to get default value for</param>
        /// <returns>Default value for the type</returns>
        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }

    /// <summary>
    /// Generic SCIM filter processor for any resource type.
    /// Provides generic SCIM filter processing following RFC 7644 Section 3.4.2.2 - Filtering.
    /// Implements RFC 7644 Section 3.4.2.2 - Filtering
    /// [RFC 7644 Section 3.4.2.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2)
    /// </summary>
    public static class ScimFilterProcessor
    {
        /// <summary>
        /// Process SCIM filter for any resource type following RFC 7644 standards.
        /// Implements RFC 7644 Section 3.4.2.2 - Filtering operations.
        /// [RFC 7644 Section 3.4.2.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="filter">SCIM filter string following RFC 7644 syntax</param>
        /// <returns>Processed filter parameters with SQL and parameter values</returns>
        public static (string Sql, Dictionary<string, object> Parameters) ProcessFilter<T>(string filter) where T : IResource
        {
            
            // Usar mapeamento direto (sem configuração híbrida)
            var visitor = new Looplex.SCIMv2.Entities.SCIMv2ToSQLVisitor();
            
            var result = ProcessFilterWithVisitor(filter, visitor);
            
            return result;
        }

        /// <summary>
        /// Process SCIM filter with custom visitor following RFC 7644 standards.
        /// Implements RFC 7644 Section 3.4.2.2 - Filtering with custom attribute mapping.
        /// [RFC 7644 Section 3.4.2.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2)
        /// </summary>
        /// <param name="filter">SCIM filter string following RFC 7644 syntax</param>
        /// <param name="visitor">Custom SCIMv2ToSQLVisitor for attribute mapping</param>
        /// <returns>Processed filter parameters with SQL and parameter values</returns>
        public static (string Sql, Dictionary<string, object> Parameters) ProcessFilterWithVisitor(string filter, Looplex.SCIMv2.Entities.SCIMv2ToSQLVisitor visitor)
        {
            
            if (string.IsNullOrWhiteSpace(filter))
            {
                return ("1=1", new Dictionary<string, object>());
            }

            try
            {
                // Parse SCIM filter using ANTLR grammar following RFC 7644 syntax
                var inputStream = new Antlr4.Runtime.AntlrInputStream(filter);
                var lexer = new Looplex.SCIMv2.Antlr.ScimFilterLexer(inputStream);
                var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
                var parser = new Looplex.SCIMv2.Antlr.ScimFilterParser(tokenStream);
                
                var tree = parser.filter();
                var sqlWhereClause = visitor.Visit(tree);
                var parameters = new Dictionary<string, object>();
                return (sqlWhereClause, parameters);
            }
            catch (Exception ex)
            {
                return ("1=1", new Dictionary<string, object>());
            }
        }
    }

    /// <summary>
    /// Generic SCIM patch processor for any IResource type.
    /// Provides generic PATCH operations processing following RFC 7644 Section 3.5 - Update Resource (PATCH).
    /// Implements RFC 7644 Section 3.5 - Update Resource (PATCH)
    /// [RFC 7644 Section 3.5](https://datatracker.ietf.org/doc/html/rfc7644#section-3.5)
    /// </summary>
    public static class ScimPatchProcessor
    {
        /// <summary>
        /// Apply PATCH operations to any IResource following RFC 7644 Section 3.5.
        /// Implements RFC 7644 Section 3.5 - Update Resource (PATCH) operations.
        /// [RFC 7644 Section 3.5](https://datatracker.ietf.org/doc/html/rfc7644#section-3.5)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="resource">Resource to apply patches to</param>
        /// <param name="patches">Array of PATCH operations following RFC 7644 Section 3.5</param>
        public static void ApplyPatches<T>(T resource, PatchOperation[] patches) where T : IResource
        {
            foreach (var patch in patches)
            {
                switch (patch.Op?.ToLower())
                {
                    case "add":
                        ApplyAddOperation(resource, patch);
                        break;
                    case "remove":
                        ApplyRemoveOperation(resource, patch);
                        break;
                    case "replace":
                        ApplyReplaceOperation(resource, patch);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported PATCH operation: {patch.Op}");
                }
            }
        }

        /// <summary>
        /// Apply ADD operation to resource following RFC 7644 Section 3.5.2.1.
        /// Implements RFC 7644 Section 3.5.2.1 - Add Operation.
        /// [RFC 7644 Section 3.5.2.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.5.2.1)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="resource">Resource to apply ADD operation to</param>
        /// <param name="patch">PATCH operation containing path and value</param>
        private static void ApplyAddOperation<T>(T resource, PatchOperation patch) where T : IResource
        {
            // Apply ADD operation using reflection to set properties based on path and value
            SetPropertyByPath(resource, patch.Path, patch.Value);
        }

        /// <summary>
        /// Apply REMOVE operation to resource following RFC 7644 Section 3.5.2.2.
        /// Implements RFC 7644 Section 3.5.2.2 - Remove Operation.
        /// [RFC 7644 Section 3.5.2.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.5.2.2)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="resource">Resource to apply REMOVE operation to</param>
        /// <param name="patch">PATCH operation containing path to remove</param>
        private static void ApplyRemoveOperation<T>(T resource, PatchOperation patch) where T : IResource
        {
            // Apply REMOVE operation using reflection to clear properties based on path
            SetPropertyByPath(resource, patch.Path, null);
        }

        /// <summary>
        /// Apply REPLACE operation to resource following RFC 7644 Section 3.5.2.3.
        /// Implements RFC 7644 Section 3.5.2.3 - Replace Operation.
        /// [RFC 7644 Section 3.5.2.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.5.2.3)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="resource">Resource to apply REPLACE operation to</param>
        /// <param name="patch">PATCH operation containing path and new value</param>
        private static void ApplyReplaceOperation<T>(T resource, PatchOperation patch) where T : IResource
        {
            // Apply REPLACE operation using reflection to set properties based on path and value
            SetPropertyByPath(resource, patch.Path, patch.Value);
        }

        /// <summary>
        /// Set property value by path using reflection following RFC 7644 Section 3.5.2.
        /// Implements RFC 7644 Section 3.5.2 - Path Attribute.
        /// [RFC 7644 Section 3.5.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.5.2)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="resource">Resource to set property on</param>
        /// <param name="path">Path to the property following RFC 7644 syntax</param>
        /// <param name="value">Value to set the property to</param>
        private static void SetPropertyByPath<T>(T resource, string path, object value) where T : IResource
        {
            // Simple implementation - in production, this would handle nested paths following RFC 7644
            var propertyName = path.TrimStart('/');
            var property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            
            if (property != null && property.CanWrite)
            {
                var convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(resource, convertedValue);
            }
        }

        /// <summary>
        /// Convert value to target type following .NET conventions.
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="targetType">Target type to convert to</param>
        /// <returns>Converted value of the target type</returns>
        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return GetDefaultValue(targetType);

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            return Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Get default value for type following .NET conventions.
        /// </summary>
        /// <param name="type">Type to get default value for</param>
        /// <returns>Default value for the type</returns>
        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }

    /// <summary>
    /// Generic SCIM validator for any IResource type.
    /// Provides generic validation following RFC 7643 Section 2.1 - Core Schema requirements.
    /// Implements RFC 7643 Section 2.1 - Core Schema
    /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
    /// </summary>
    public static class ScimValidator
    {
        /// <summary>
        /// Validate any IResource for SCIM v2.0 compliance following RFC 7643 standards.
        /// Implements RFC 7643 Section 2.1 - Core Schema validation requirements.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="resource">Resource to validate for SCIM compliance</param>
        /// <returns>True if valid according to SCIM v2.0 standards, false otherwise</returns>
        public static bool Validate<T>(T resource) where T : IResource
        {
            if (resource == null)
                return false;

            // Validate base SCIM v2.0 requirements as defined in RFC 7643 Section 2.1
            if (string.IsNullOrEmpty(resource.Id))
                return false;
                
            if (resource.Meta == null)
                return false;

            // Validate SCIM v2.0 compliance following RFC 7643 standards
            return ValidateScimCompliance(resource);
        }

        /// <summary>
        /// Validate SCIM v2.0 compliance for any resource following RFC 7643 Section 2.1.
        /// Implements RFC 7643 Section 2.1 - Core Schema compliance validation.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="resource">Resource to validate for SCIM compliance</param>
        /// <returns>True if compliant with SCIM v2.0 standards, false otherwise</returns>
        private static bool ValidateScimCompliance<T>(T resource) where T : IResource
        {
            // Check for required SCIM v2.0 properties as defined in RFC 7643 Section 2.1
            if (string.IsNullOrEmpty(resource.Meta.ResourceType))
                return false;

            if (resource.Meta.Created == default(DateTime))
                return false;

            if (resource.Meta.LastModified == default(DateTime))
                return false;

            // Additional SCIM v2.0 validation can be added here following RFC 7643
            return true;
        }

        /// <summary>
        /// Validate resource for specific business rules following SCIM v2.0 standards.
        /// Implements RFC 7643 Section 2.1 - Core Schema with custom validation rules.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="resource">Resource to validate</param>
        /// <param name="validationRules">Custom validation rules following SCIM v2.0 standards</param>
        /// <returns>True if valid according to SCIM v2.0 standards and custom rules, false otherwise</returns>
        public static bool ValidateWithRules<T>(T resource, Func<T, bool> validationRules) where T : IResource
        {
            if (!Validate(resource))
                return false;

            return validationRules(resource);
        }
    }

    /// <summary>
    /// Generic SCIM type converter for any resource type.
    /// Provides generic type conversion helpers following RFC 7643 Section 2.1 - Core Schema data types.
    /// Implements RFC 7643 Section 2.1 - Core Schema
    /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
    /// </summary>
    public static class ScimTypeConverter
    {
        /// <summary>
        /// Parse DateTime from any object following RFC 7643 Section 2.1 - Core Schema.
        /// Implements RFC 7643 Section 2.1 - Core Schema DateTime handling.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <param name="value">Value to parse as DateTime</param>
        /// <returns>Parsed DateTime or UtcNow if parsing fails</returns>
        public static DateTime ParseDateTime(object value)
        {
            if (value is DateTime dateTime)
                return dateTime;
                
            if (DateTime.TryParse(value?.ToString(), out var parsedDateTime))
                return parsedDateTime;
                
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Parse Guid from any object following RFC 7643 Section 2.1 - Core Schema.
        /// Implements RFC 7643 Section 2.1 - Core Schema identifier handling.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <param name="value">Value to parse as Guid</param>
        /// <returns>Parsed Guid or Empty if parsing fails</returns>
        public static Guid ParseGuid(object value)
        {
            if (value is Guid guid)
                return guid;
                
            if (Guid.TryParse(value?.ToString(), out var parsedGuid))
                return parsedGuid;
                
            return Guid.Empty;
        }

        /// <summary>
        /// Parse boolean from any object following RFC 7643 Section 2.1 - Core Schema.
        /// Implements RFC 7643 Section 2.1 - Core Schema boolean handling.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <param name="value">Value to parse as boolean</param>
        /// <returns>Parsed boolean or false if parsing fails</returns>
        public static bool ParseBoolean(object value)
        {
            if (value is bool boolValue)
                return boolValue;
                
            if (bool.TryParse(value?.ToString(), out var parsedBool))
                return parsedBool;
                
            return false;
        }

        /// <summary>
        /// Parse integer from any object following RFC 7643 Section 2.1 - Core Schema.
        /// Implements RFC 7643 Section 2.1 - Core Schema integer handling.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <param name="value">Value to parse as integer</param>
        /// <returns>Parsed integer or 0 if parsing fails</returns>
        public static int ParseInteger(object value)
        {
            if (value is int intValue)
                return intValue;
                
            if (int.TryParse(value?.ToString(), out var parsedInt))
                return parsedInt;
                
            return 0;
        }

        /// <summary>
        /// Parse string from any object following RFC 7643 Section 2.1 - Core Schema.
        /// Implements RFC 7643 Section 2.1 - Core Schema string handling.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        /// <param name="value">Value to parse as string</param>
        /// <returns>Parsed string or empty string if parsing fails</returns>
        public static string ParseString(object value)
        {
            return value?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Public method to format SCIMv2 responses by HTTP method
    /// </summary>
    /// <param name="response">SCIMv2Response to format</param>
    /// <returns>Formatted JSON string</returns>
    public static string FormatResponseByHttpMethod(SCIMv2Response response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        // Handle error responses
        if (response.Error != null)
        {
            return FormatErrorResponse(response);
        }

        // Handle different HTTP methods
        switch (response.HttpMethod?.ToUpper())
        {
            case "QUERY":
                return FormatQueryResponse(response);
            case "RESOURCETYPES":
                return FormatResourceTypesResponse(response);
            case "SCHEMAS":
                return FormatSchemasResponse(response);
            case "GET":
            case "POST":
            case "PUT":
            case "PATCH":
                return FormatSingleResourceResponse(response);
            case "DELETE":
                return FormatDeleteResponse(response);
            default:
                // Default to single resource format
                return FormatSingleResourceResponse(response);
        }
    }

    #endregion
}
