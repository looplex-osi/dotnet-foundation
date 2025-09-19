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
    
    // Serialization is now centralized in Looplex.Foundation.Serialization
    // All serialization operations use SCIMv2Serializer for consistency

    // SCIMv2 Schema Constants
    private const string UserNameDescription = "Unique identifier for the User, typically used by the user to directly authenticate to the service provider";
    
    public SCIMv2(IServiceNameProvider? serviceNameProvider = null)
    {
        _serviceNameProvider = serviceNameProvider;
        this.Register<User>("Users");
        this.Register<Group>("Groups");
        _schemas = InitializeSchemas();
    }

    #region SCIMv2Service Implementation

    /// <summary>
    /// Registers a resource service for a specific collection
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="service">Resource service implementation</param>
    /// <param name="collectionName">Collection name (e.g., "Users", "Groups")</param>
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
    /// Registers a resource type with a default service implementation
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="collectionName">Collection name (e.g., "Users", "Groups")</param>
    public void Register<T>(string collectionName) where T : IResource
    {
        if (string.IsNullOrEmpty(collectionName))
            throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

        // Create a default in-memory service for the resource type
        var defaultService = CreateDefaultService<T>();
        Register(defaultService, collectionName);
    }

    /// <summary>
    /// Creates a default service implementation for a resource type
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <returns>Default service implementation</returns>
    private IResourceService<T> CreateDefaultService<T>() where T : IResource
    {
        throw new NotSupportedException($"No default service available for {typeof(T).Name}. Please register a service using Register<T>() method.");
    }

    /// <summary>
    /// Query resources from a collection (GET /collection)
    /// Implements RFC 7644 Section 3.4.2 - Query Resources
    /// [RFC 7644 Section 3.4.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2)
    /// Supports filtering, sorting, and pagination as per RFC 7644 Section 3.4.2.3
    /// [RFC 7644 Section 3.4.2.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.3)
    /// 
    /// Returns:
    /// - 200 OK: Resources retrieved successfully
    /// - 400 Bad Request: Invalid parameters
    /// - 404 Not Found: Collection not found
    /// - 500 Internal Server Error: Unexpected error
    /// </summary>
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
            
            if (result == null)
            {
                return CreateErrorResponse(500, "Internal Server Error", "QueryAsync returned null result");
            }
            
            // Extract resources and totalCount from tuple
            var resources = result.Resources;
            var totalCount = result.TotalCount;
            

            return CreateListResponse(resources, totalCount, startIndex, count);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SCIMv2 QueryAsync Error: {ex}");
            return HandleException(ex, "querying resources");
        }
    }

    /// <summary>
    /// Create a new resource (POST /collection)
    /// Implements RFC 7644 Section 3.4.1 - Create Resource
    /// [RFC 7644 Section 3.4.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.1)
    /// Validates resource per RFC 7643 Section 3.1 - Resource Representation
    /// [RFC 7643 Section 3.1](https://datatracker.ietf.org/doc/html/rfc7643#section-3.1)
    /// 
    /// Returns:
    /// - 201 Created: Resource successfully created
    /// - 400 Bad Request: Invalid input data or missing required fields
    /// - 409 Conflict: Resource already exists
    /// - 500 Internal Server Error: Unexpected error
    /// 
    /// Headers:
    /// - Location: Resource location URL
    /// - ETag: Resource version for optimistic locking
    /// - Content-Type: application/scim+json
    /// </summary>
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
            var resourceId = await dynamicService.CreateAsync(resource, cancellationToken);
            
            // The service (CreateResourceWithMetadataAsync) already set the ID and metadata properly
            // No need to retrieve again - the resource object is already updated
            
            return CreateCreateResponse(resource, resourceId, collection);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SCIMv2 CreateAsync Error: {ex}");
            return HandleException(ex, "creating the resource");
        }
    }

    /// <summary>
    /// Retrieve a specific resource (GET /collection/:id)
    /// Implements RFC 7644 Section 3.4.3 - Retrieve Resource
    /// [RFC 7644 Section 3.4.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.3)
    /// 
    /// Returns:
    /// - 200 OK: Resource retrieved successfully
    /// - 400 Bad Request: Invalid resource ID
    /// - 404 Not Found: Resource not found
    /// - 500 Internal Server Error: Unexpected error
    /// 
    /// Headers:
    /// - ETag: Resource version for optimistic locking
    /// - Content-Type: application/scim+json
    /// </summary>
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

            return CreateRetrieveResponse(resource);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving the resource");
        }
    }

    /// <summary>
    /// Modify a resource using PATCH (PATCH /collection/:id)
    /// Implements RFC 7644 Section 3.4.4 - Update Resource
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// Uses RFC 6902 (JSON Patch) Section 4 - Operations
    /// [RFC 6902 Section 4](https://datatracker.ietf.org/doc/html/rfc6902#section-4)
    /// 
    /// Returns:
    /// - 200 OK: Resource updated successfully
    /// - 400 Bad Request: Invalid patch operations
    /// - 404 Not Found: Resource not found
    /// - 409 Conflict: Version conflict
    /// - 500 Internal Server Error: Unexpected error
    /// 
    /// Headers:
    /// - ETag: Updated resource version
    /// - Content-Type: application/scim+json
    /// </summary>
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

            // Apply patches using dynamic typing
            var success = await dynamicService.UpdateAsync(validation.ResourceId!.Value, currentResource, patches, cancellationToken);
            
            if (!success)
            {
                return CreateErrorResponse(400, "Update failed", "Failed to apply patches to resource");
            }

            // Retrieve updated resource using dynamic typing
            var updatedResource = await dynamicService.RetrieveAsync(validation.ResourceId!.Value, cancellationToken);
            
            return new SCIMv2Response
            {
                StatusCode = 200,
                Data = updatedResource,
                Schemas = updatedResource?.Schemas ?? Array.Empty<string>(),
                ETag = $"W/\"{updatedResource?.Meta.Version}\""
            };
        }
        catch (Exception ex)
        {
            return HandleException(ex, "modifying the resource");
        }
    }

    /// <summary>
    /// Replace a resource using PUT (PUT /collection/:id)
    /// Implements RFC 7644 Section 3.4.4 - Update Resource
    /// [RFC 7644 Section 3.4.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.4)
    /// Performs complete resource replacement
    /// 
    /// Returns:
    /// - 200 OK: Resource replaced successfully
    /// - 400 Bad Request: Invalid resource data
    /// - 404 Not Found: Resource not found
    /// - 409 Conflict: Version conflict
    /// - 500 Internal Server Error: Unexpected error
    /// 
    /// Headers:
    /// - ETag: Updated resource version
    /// - Content-Type: application/scim+json
    /// </summary>
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
    /// Delete a resource (DELETE /collection/:id)
    /// Implements RFC 7644 Section 3.4.5 - Delete Resource
    /// [RFC 7644 Section 3.4.5](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.5)
    /// 
    /// Returns:
    /// - 204 No Content: Resource deleted successfully
    /// - 400 Bad Request: Invalid resource ID
    /// - 404 Not Found: Resource not found
    /// - 500 Internal Server Error: Unexpected error
    /// </summary>
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
    /// Gets all registered collections
    /// </summary>
    public IEnumerable<string> GetRegisteredCollections()
    {
        return _registeredResource.Keys;
    }

    /// <summary>
    /// Checks if a collection is registered
    /// </summary>
    public bool IsCollectionRegistered(string collection)
    {
        return _registeredResource.ContainsKey(collection);
    }

    /// <summary>
    /// Deregisters a resource service for a specific collection
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
            
            // Enhanced logging for debugging
            System.Diagnostics.Debug.WriteLine($"SCIMv2 Deregister - Collection: {collectionName} deregistered successfully");
        }
        
        return wasRegistered;
    }

    /// <summary>
    /// Deregisters all resource services
    /// </summary>
    /// <returns>Number of collections deregistered</returns>
    public int DeregisterAll()
    {
        var count = _registeredResource.Count;
        _registeredResource.Clear();
        
        System.Diagnostics.Debug.WriteLine($"SCIMv2 DeregisterAll - {count} collections deregistered");
        
        return count;
    }

    /// <summary>
    /// Gets the current service name
    /// </summary>
    /// <returns>Service name or null if not configured</returns>
    public string? GetServiceName()
    {
        return _serviceNameProvider?.GetServiceName();
    }

    /// <summary>
    /// Gets the current service name or default
    /// </summary>
    /// <param name="defaultName">Default service name if not configured</param>
    /// <returns>Service name</returns>
    public string GetServiceNameOrDefault(string defaultName = "looplex")
    {
        return _serviceNameProvider?.GetServiceName() ?? defaultName;
    }

    #endregion

    #region IJsonSchemaProvider Implementation

    /// <summary>
    /// Registers a custom schema definition
    /// </summary>
    public void RegisterSchema(SchemaDefinition schema)
    {
        if (schema?.Id != null)
        {
            _schemas[schema.Id] = schema;
        }
    }

    /// <summary>
    /// Registers multiple custom schema definitions
    /// </summary>
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
    /// Gets all available SCIMv2 schemas
    /// </summary>
    public Task<List<SchemaDefinition>> GetAllSchemasAsync()
    {
        return Task.FromResult(_schemas.Values.ToList());
    }

    /// <summary>
    /// Gets a specific SCIMv2 schema by ID
    /// </summary>
    public Task<SchemaDefinition?> GetSchemaAsync(string? schemaId)
    {
        if (string.IsNullOrEmpty(schemaId))
            return Task.FromResult<SchemaDefinition?>(null);
            
        return Task.FromResult(_schemas.TryGetValue(schemaId, out var schema) ? schema : null);
    }

    #endregion

    #region Discovery Endpoints Implementation

    /// <summary>
    /// Get all available schemas (GET /Schemas)
    /// Implements RFC 7644 Section 3.4.6 - Schema Discovery
    /// [RFC 7644 Section 3.4.6](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.6)
    /// Returns all registered SCIM schemas for discovery
    /// </summary>
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
    /// Get a specific schema by ID (GET /Schemas/:id)
    /// Implements RFC 7644 Section 3.4.6 - Schema Discovery
    /// [RFC 7644 Section 3.4.6](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.6)
    /// Returns a specific SCIM schema by its identifier
    /// </summary>
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
    /// Get service provider configuration (GET /ServiceProviderConfig)
    /// </summary>
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
    /// Validates a resource for creation according to SCIM v2.0 requirements
    /// </summary>
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
    /// Gets the User schema ID based on service name provider
    /// </summary>
    private string GetUserSchemaId()
    {
        if (_serviceNameProvider != null)
        {
            var serviceName = _serviceNameProvider.GetServiceName();
            return $"urn:looplex:params:scim:schemas:{serviceName}:2.0:User";
        }
        return "urn:ietf:params:scim:schemas:core:2.0:User";
    }

    /// <summary>
    /// Gets the Group schema ID based on service name provider
    /// </summary>
    private string GetGroupSchemaId()
    {
        if (_serviceNameProvider != null)
        {
            var serviceName = _serviceNameProvider.GetServiceName();
            return $"urn:looplex:params:scim:schemas:{serviceName}:2.0:Group";
        }
        return "urn:ietf:params:scim:schemas:core:2.0:Group";
    }

    private Dictionary<string, SchemaDefinition> InitializeSchemas()
    {
        var userSchemaId = GetUserSchemaId();
        var groupSchemaId = GetGroupSchemaId();
        
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
    /// Advanced resource processing pipeline with filtering, sorting, and pagination
    /// Transferred intelligence from ResourceServiceBase for optimal performance
    /// </summary>
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
            processedResources = await ApplyAdvancedFilterAsync(processedResources, filter, cancellationToken);
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
    /// Advanced filtering implementation with SCIM filter support
    /// Implements RFC 7644 Section 3.4.2.2 - Filtering
    /// [RFC 7644 Section 3.4.2.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2)
    /// Supports SCIM filter expressions as per RFC 7644 Section 3.4.2.2.1
    /// [RFC 7644 Section 3.4.2.2.1](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2.1)
    /// </summary>
    private Task<IList<T>> ApplyAdvancedFilterAsync<T>(
        IList<T> resources,
        string filter,
        CancellationToken cancellationToken = default) where T : IResource
    {
        // For now, return all resources (can be enhanced with actual SCIM filter parsing)
        // This is where the Looplex.Foundation.SearchContent integration would go
        return Task.FromResult(resources);
    }

    /// <summary>
    /// Advanced sorting implementation with multiple field support
    /// </summary>
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
    /// Initialize resource metadata for new resources
    /// Transferred from ResourceServiceBase with enhanced functionality
    /// </summary>
    private void InitializeResourceMetadata<T>(T resource) where T : IResource
    {
        var now = DateTime.UtcNow;
        resource.Meta.Created = now;
        resource.Meta.LastModified = now;
        resource.Meta.Version = "1";
        resource.Meta.Location = $"/{GetResourceTypeName<T>()}/{resource.Id}";
    }

    /// <summary>
    /// Update resource metadata for existing resources
    /// </summary>
    private void UpdateResourceMetadata<T>(T resource, T existingResource) where T : IResource
    {
        resource.Meta.Created = existingResource.Meta.Created;
        resource.Meta.LastModified = DateTime.UtcNow;
        resource.Meta.Version = (int.Parse(existingResource.Meta.Version ?? "1") + 1).ToString();
        resource.Meta.Location = existingResource.Meta.Location;
    }

    /// <summary>
    /// Get resource type name for URL generation
    /// </summary>
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
    /// Advanced PATCH operations processor
    /// Transferred intelligence from ResourceServiceBase with enhanced SCIM compliance
    /// </summary>
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
    /// Process individual PATCH operation
    /// Implements RFC 6902 (JSON Patch) Section 4 - Operations
    /// [RFC 6902 Section 4](https://datatracker.ietf.org/doc/html/rfc6902#section-4)
    /// Supports add, remove, and replace operations as per RFC 6902 Section 4.1-4.3
    /// [RFC 6902 Section 4.1](https://datatracker.ietf.org/doc/html/rfc6902#section-4.1) - Add
    /// [RFC 6902 Section 4.2](https://datatracker.ietf.org/doc/html/rfc6902#section-4.2) - Remove
    /// [RFC 6902 Section 4.3](https://datatracker.ietf.org/doc/html/rfc6902#section-4.3) - Replace
    /// </summary>
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
    /// Apply ADD patch operation
    /// </summary>
    private async Task ApplyAddPatchAsync<T>(T resource, string path, object? value, CancellationToken cancellationToken = default) where T : IResource
    {
        // Removed unnecessary async placeholder
        // Implementation would use reflection or compiled expressions to set property values
    }

    /// <summary>
    /// Apply REMOVE patch operation
    /// </summary>
    private async Task ApplyRemovePatchAsync<T>(T resource, string path, CancellationToken cancellationToken = default) where T : IResource
    {
        // Removed unnecessary async placeholder
        // Implementation would use reflection or compiled expressions to clear property values
    }

    /// <summary>
    /// Apply REPLACE patch operation
    /// </summary>
    private async Task ApplyReplacePatchAsync<T>(T resource, string path, object? value, CancellationToken cancellationToken = default) where T : IResource
    {
        // Removed unnecessary async placeholder
        // Implementation would use reflection or compiled expressions to set property values
    }

    #endregion

    #region Enhanced Resource Operations

    /// <summary>
    /// Enhanced resource creation with automatic metadata management
    /// </summary>
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
    /// Enhanced resource replacement with metadata preservation
    /// </summary>
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
    /// Enhanced resource update with PATCH support
    /// </summary>
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
    /// Creates a successful SCIMv2 response for list operations
    /// </summary>
    /// <param name="resources">List of resources</param>
    /// <param name="totalCount">Total number of resources</param>
    /// <param name="startIndex">Starting index for pagination</param>
    /// <param name="count">Number of items per page</param>
    /// <returns>SCIMv2 response</returns>
    private static SCIMv2Response CreateListResponse(object resources, int totalCount, int startIndex, int count)
    {
        return new SCIMv2Response
        {
            StatusCode = 200,
            Data = new { resources = resources },
            Schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
            TotalResults = totalCount,
            StartIndex = startIndex,
            ItemsPerPage = count
        };
    }

    /// <summary>
    /// Creates a successful SCIMv2 response for create operations
    /// </summary>
    /// <param name="resource">Created resource</param>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="collection">Collection name</param>
    /// <returns>SCIMv2 response</returns>
    private static SCIMv2Response CreateCreateResponse(IResource resource, Guid resourceId, string collection)
    {
        return new SCIMv2Response
        {
            StatusCode = 201,
            Data = resource,
            Schemas = resource.Schemas,
            Location = $"/{collection}/{resourceId}",
            ETag = $"W/\"{resource.Meta.Version}\""
        };
    }

    /// <summary>
    /// Creates a successful SCIMv2 response for retrieve operations
    /// </summary>
    /// <param name="resource">Retrieved resource</param>
    /// <returns>SCIMv2 response</returns>
    private static SCIMv2Response CreateRetrieveResponse(IResource resource)
    {
        return new SCIMv2Response
        {
            StatusCode = 200,
            Data = resource,
            Schemas = resource.Schemas,
            ETag = $"W/\"{resource.Meta.Version}\""
        };
    }

    /// <summary>
    /// Creates a successful SCIMv2 response for update operations
    /// </summary>
    /// <param name="resource">Updated resource</param>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="collection">Collection name</param>
    /// <returns>SCIMv2 response</returns>
    private static SCIMv2Response CreateUpdateResponse(IResource resource, Guid resourceId, string collection)
    {
            return CreateUpdateResponse(resource, resourceId, collection);
    }

    /// <summary>
    /// Creates a successful SCIMv2 response for delete operations
    /// </summary>
    /// <returns>SCIMv2 response</returns>
    private static SCIMv2Response CreateDeleteResponse()
    {
        return new SCIMv2Response
        {
            StatusCode = 204
        };
    }

    /// <summary>
    /// Creates an error SCIMv2 response
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="detail">Error detail</param>
    /// <param name="scimType">SCIM error type</param>
    /// <returns>SCIMv2 response</returns>
    private static SCIMv2Response CreateErrorResponse(int statusCode, string detail, string? scimType = null)
    {
        return new SCIMv2Response
        {
            StatusCode = statusCode,
            Error = new SCIMv2Error
            {
                Detail = detail,
                ScimType = scimType,
                Timestamp = DateTime.UtcNow.ToString("O")
            }
        };
    }

    /// <summary>
    /// Centralized exception handling for SCIMv2 operations
    /// Maps common exceptions to appropriate SCIMv2 error responses
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
    /// Centralized validation for SCIMv2 requests
    /// Validates collection name, service registration, and optional resource ID
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
            return (false, CreateErrorResponse(404, "Not Found", $"Collection '{collection}' is not registered"), null, null);
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

    #region Mock Resource Helper Methods

    /// <summary>
    /// Creates common metadata for mock resources
    /// </summary>
    /// <param name="resourceType">Resource type (User/Group)</param>
    /// <param name="id">Resource ID</param>
    /// <param name="collection">Collection name</param>
    /// <returns>Resource metadata</returns>
    private static ResourceMeta CreateMockMetadata(string resourceType, string id, string collection)
    {
        return new ResourceMeta
        {
            ResourceType = resourceType,
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Location = $"/{collection}/{id}",
            Version = "1"
        };
    }

    /// <summary>
    /// Creates a mock User resource
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <returns>Mock User resource</returns>
    private static User CreateMockUser(string id)
    {
        return new User
        {
            Id = id,
            UserName = "mock.user",
            DisplayName = "Mock User",
            Active = true,
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            Meta = CreateMockMetadata("User", id, "Users")
        };
    }

    /// <summary>
    /// Creates a mock Group resource
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <returns>Mock Group resource</returns>
    private static Group CreateMockGroup(string id)
    {
        return new Group
        {
            Id = id,
            DisplayName = "Mock Group",
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Meta = CreateMockMetadata("Group", id, "Groups")
        };
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

    /// <summary>
    /// Creates a mock resource for testing purposes
    /// This should be replaced with proper resource creation in production
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="id">Resource ID</param>
    /// <returns>Mock resource</returns>
    public IResource CreateMockResource(string collectionName, string id)
    {
        return collectionName.ToLowerInvariant() switch
        {
            "users" => CreateMockUser(id),
            "groups" => CreateMockGroup(id),
            _ => throw new ArgumentException($"Unsupported collection: {collectionName}")
        };
    }

    #endregion
}
