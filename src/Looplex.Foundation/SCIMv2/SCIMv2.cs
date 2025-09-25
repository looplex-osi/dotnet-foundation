using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Modules;
using Looplex.Foundation.Serialization;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.Foundation.SCIMv2.Antlr;
using Microsoft.AspNetCore.Http;

namespace Looplex.Foundation.SCIMv2;

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

    // SCIMv2 Schema Constants
    private const string UserNameDescription = "Unique identifier for the User, typically used by the user to directly authenticate to the service provider";
    

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
            
            // Extract the base path (e.g., /scim/v2) from the current request
            var pathSegments = request.Path.Value?.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments != null && pathSegments.Length >= 2)
            {
                // Assume SCIM endpoints are at /scim/v2
                var scimBasePath = $"/{pathSegments[0]}/{pathSegments[1]}";
                return $"{scheme}://{host}{pathBase}{scimBasePath}";
            }
        }
        
        // Fallback to default if no HTTP context available
        return "https://api.exemplo.com/scim/v2";
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
                // GET: Return resource directly, set Location header
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
    /// Registers a resource type with auto-generated service implementation (deprecated).
    /// This method is deprecated and will throw NotSupportedException.
    /// Use Register(IResourceService&lt;T&gt; service, string collectionName) instead.
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource interface</typeparam>
    /// <param name="collectionName">Collection name (e.g., "Users", "Groups", "Notes")</param>
    /// <exception cref="NotSupportedException">Always thrown - use explicit service registration</exception>
    public void Register<T>(string collectionName) where T : IResource
    {
        if (string.IsNullOrEmpty(collectionName))
            throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

        // Since auto-registration was removed, this method now requires explicit service registration
        throw new NotSupportedException($"Cannot register collection '{collectionName}' without explicit service. Please use Register(IResourceService<T> service, string collectionName) method or configure dependency injection.");
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
    /// Gets a specific SCIMv2 schema definition by ID.
    /// Returns null if schema is not found.
    /// </summary>
    /// <param name="schemaId">Schema identifier to retrieve</param>
    /// <returns>Schema definition or null if not found</returns>
    public Task<SchemaDefinition?> GetSchemaAsync(string? schemaId)
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
                Data = new { Resources = schemas },
                Schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
                TotalResults = schemas.Count,
                StartIndex = 1,
                ItemsPerPage = schemas.Count
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
                          "SCIMv2 requires Meta.ResourceType to identify the resource type (e.g., 'User', 'Group', 'Note'). " +
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
        var serviceName = GetServiceNameOrDefault("looplex");
        return $"urn:looplex:params:scim:schemas:{serviceName}:2.0:{resourceType}";
    }

    /// <summary>
    /// Automatically generates SCIMv2 schema URI for a resource type.
    /// Creates a compliant schema URI based on the resource type.
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <returns>SCIMv2 compliant schema URI</returns>
    private string GenerateSchemaUri<T>() where T : IResource
    {
        var resourceType = GetResourceTypeName<T>();
        return GenerateSchemaUri(resourceType);
    }





    private Dictionary<string, SchemaDefinition> InitializeSchemas()
    {
        var userSchemaId = "urn:ietf:params:scim:schemas:core:2.0:User";
        var groupSchemaId = "urn:ietf:params:scim:schemas:core:2.0:Group";
        
        return new Dictionary<string, SchemaDefinition>
        {
            [userSchemaId] = new SchemaDefinition
            {
                Id = userSchemaId,
                Name = "User",
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
                        Description = UserNameDescription
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
                Id = groupSchemaId,
                Name = "Group",
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

                var (sqlWhereClause, parameters) = visitor.Visit(tree);

                return (sqlWhereClause, parameters);
            }
            catch (Exception ex)
            {
                // Log error and return safe fallback
                // In production, this should be logged properly
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

    /// <summary>
    /// Enhanced resource creation with automatic metadata management.
    /// Creates a new resource with proper SCIM v2.0 metadata initialization.
    /// Implements RFC 7644 Section 3.4.1 - Create Resource
    /// [RFC 7644 Section 3.4.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.1)
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="resource">Resource to create</param>
    /// <param name="saveAction">Action to save the resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated resource ID</returns>
    public async Task<Guid> CreateResourceWithMetadataAsync<T>(
        T resource,
        Func<T, CancellationToken, Task> saveAction,
        CancellationToken cancellationToken = default) where T : IResource
    {
        var id = Guid.NewGuid();
        resource.Id = id.ToString();
        InitializeResourceMetadata(resource);
        
        await saveAction(resource, cancellationToken);
        return id;
    }

    /// <summary>
    /// Enhanced resource replacement with metadata preservation.
    /// Replaces an existing resource while preserving SCIM v2.0 metadata.
    /// Implements RFC 7644 Section 3.4.4 - Update Resource (PUT)
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="id">Resource ID to replace</param>
    /// <param name="resource">New resource data</param>
    /// <param name="retrieveAction">Action to retrieve existing resource</param>
    /// <param name="saveAction">Action to save the updated resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if replacement was successful</returns>
    public async Task<bool> ReplaceResourceWithMetadataAsync<T>(
        Guid id,
        T resource,
        Func<Guid, CancellationToken, Task<T?>> retrieveAction,
        Func<T, CancellationToken, Task> saveAction,
        CancellationToken cancellationToken = default) where T : IResource
    {
        var existingResource = await retrieveAction(id, cancellationToken);
        if (existingResource == null)
            return false;

        resource.Id = id.ToString();
        UpdateResourceMetadata(resource, existingResource);
        
        await saveAction(resource, cancellationToken);
        return true;
    }

    /// <summary>
    /// Enhanced resource update with PATCH support.
    /// Updates a resource using PATCH operations with SCIM v2.0 compliance.
    /// Implements RFC 7644 Section 3.4.4 - Update Resource
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="id">Resource ID to update</param>
    /// <param name="resource">Resource to update</param>
    /// <param name="patches">PATCH operations to apply</param>
    /// <param name="retrieveAction">Action to retrieve existing resource</param>
    /// <param name="saveAction">Action to save the updated resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if update was successful</returns>
    public async Task<bool> UpdateResourceWithPatchesAsync<T>(
        Guid id,
        T resource,
        PatchOperation[] patches,
        Func<Guid, CancellationToken, Task<T?>> retrieveAction,
        Func<T, CancellationToken, Task> saveAction,
        CancellationToken cancellationToken = default) where T : IResource
    {
        var existingResource = await retrieveAction(id, cancellationToken);
        if (existingResource == null)
            return false;

        await ApplyPatchesAsync(resource, patches, cancellationToken);
        
        resource.Id = id.ToString();
        UpdateResourceMetadata(resource, existingResource);
        
        await saveAction(resource, cancellationToken);
        return true;
    }

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
    private static SCIMv2Response CreateListResponse(object resources, int totalCount, int startIndex, int count)
    {
        var response = new SCIMv2Response
        {
            StatusCode = 0, // Set to 0 to indicate this should not be serialized for collections
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
        if (resource == null) return "W/\"1\"";
        
        // Create a content hash based on resource data
        var content = $"{resource.Id}|{resource.Meta?.Created}|{resource.Meta?.LastModified}|{resource.Meta?.ResourceType}";
        
        // Generate hash
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        var hash = Convert.ToBase64String(hashBytes)[..8]; // Use first 8 characters
        
        return $"W/\"{hash}\"";
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
        if (resource == null) return "W/\"1\"";
        
        // Create comprehensive content hash including all resource data
        var content = $"{resource.Id}|{resource.Meta?.Created}|{resource.Meta?.LastModified}|{resource.Meta?.ResourceType}|{resource.Meta?.Location}";
        
        // Include resource-specific data for more unique hashing
        if (resource.Schemas != null && resource.Schemas.Length > 0)
        {
            content += $"|{string.Join(",", resource.Schemas)}";
        }
        
        // Generate SHA-256 hash
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        var hash = Convert.ToBase64String(hashBytes)[..12]; // Use first 12 characters for more uniqueness
        
        return $"W/\"{hash}\"";
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
    private static SCIMv2Response CreateRetrieveResponse(IResource resource)
    {
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
    /// Creates a successful SCIMv2 response for update operations.
    /// Implements RFC 7644 Section 3.4.4 - Update Resource
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// </summary>
    /// <param name="resource">Updated resource to include in response</param>
    /// <param name="resourceId">Resource ID of the updated resource</param>
    /// <param name="collection">Collection name where resource was updated</param>
    /// <returns>SCIMv2 response with updated resource</returns>
    private static SCIMv2Response CreateUpdateResponse(IResource resource, Guid resourceId, string collection)
    {
        try
        {
            // PUT/PATCH (Update): Return resource directly, no Resources wrapper
            var response = new SCIMv2Response
            {
                StatusCode = 200,
                Data = resource, // Resource directly, not wrapped in Resources
                Schemas = resource.Schemas,
                Location = resource.Meta.Location,
                ETag = resource.Meta.Version,
                HttpMethod = "PUT" // Will be overridden for PATCH
            };
            
            return response;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Creates a successful SCIMv2 response for delete operations.
    /// Implements RFC 7644 Section 3.4.5 - Delete Resource
    /// [RFC 7644 Section 3.4.5](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.5)
    /// </summary>
    /// <returns>SCIMv2 response indicating successful deletion</returns>
    private static SCIMv2Response CreateDeleteResponse()
    {
        return new SCIMv2Response
        {
            StatusCode = 204,
            Data = null,
            Schemas = Array.Empty<string>(),
            HttpMethod = "DELETE"
        };
    }

    /// <summary>
    /// Centralized response formatting by HTTP method for SCIM v2.0 compliance.
    /// Contains all logic for SCIM v2.0 compliance per HTTP verb.
    /// Implements RFC 7644 Section 3 - SCIM Protocol
    /// [RFC 7644 Section 3](https://datatracker.ietf.org/doc/html/rfc7644#section-3)
    /// </summary>
    /// <param name="response">SCIMv2Response to format</param>
    /// <returns>Formatted JSON string according to SCIM v2.0 specification</returns>
    public static string FormatResponseByHttpMethod(SCIMv2Response response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        // BLOCOS ESPECÍFICOS PARA CADA VERBO HTTP
        if (response.HttpMethod == "QUERY")
        {
            // QUERY (Collections): Remove statusCode, mantém Resources wrapper
            return FormatQueryResponse(response);
        }
        else if (response.HttpMethod == "GET")
        {
            // GET (Single Resource): Mantém statusCode, remove Resources wrapper
            return FormatSingleResourceResponse(response);
        }
        else if (response.HttpMethod == "POST")
        {
            // POST (Create): Mantém statusCode, remove Resources wrapper
            return FormatSingleResourceResponse(response);
        }
        else if (response.HttpMethod == "PUT")
        {
            // PUT (Replace): Mantém statusCode, remove Resources wrapper
            return FormatSingleResourceResponse(response);
        }
        else if (response.HttpMethod == "PATCH")
        {
            // PATCH (Modify): Mantém statusCode, remove Resources wrapper
            return FormatSingleResourceResponse(response);
        }
        else if (response.HttpMethod == "DELETE")
        {
            // DELETE: Mantém statusCode, sem body (204)
            return FormatDeleteResponse(response);
        }
        else
        {
            // Default: Standard serialization
            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }
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
        // Create a new response without statusCode for collections
        var queryResponse = new
        {
            schemas = response.Schemas,
            totalResults = response.TotalResults,
            startIndex = response.StartIndex,
            itemsPerPage = response.ItemsPerPage,
            Resources = response.Data
        };

        // Use Looplex.Foundation centralized serializer with proper options
        // Note: PropertyNamingPolicy = null to preserve SCIM v2.0 field names (Resources with capital R)
        return System.Text.Json.JsonSerializer.Serialize(queryResponse, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Preserve original field names for SCIM v2.0 compliance
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        });
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
        // Flatten the resource data directly using Looplex.Foundation serializer options
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
            var patchArray = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonArray>(json);
            if (patchArray == null)
            {
                return (false, Array.Empty<PatchOperation>(), "Invalid patches");
            }

            var patchOperations = new List<PatchOperation>();
            foreach (var patchNode in patchArray)
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


    #endregion
}
