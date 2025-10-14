using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Looplex.Protocols.HTTP.Ports;
using Looplex.SCIMv2.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Looplex.Protocols.HTTP.Extensions;

/// <summary>
/// Extension methods for mapping SCIMv2 endpoints
/// </summary>
public static class SCIMv2EndpointExtensions
{

    /// <summary>
    /// Executes a SCIM operation with proper error handling
    /// RFC 7644 compliance is handled by the underlying Looplex.SCIMv2 service
    /// </summary>
    private static async Task<IResult> ExecuteSCIMOperation<T>(
        HttpContext context,
        string collectionName,
        Func<ISCIMv2Service, Task<T>> operation,
        string operationName,
        Func<T, IResult>? resultBuilder = null)
    {
        try
        {
            var scimService = context.RequestServices.GetService<ISCIMv2Service>();
            if (scimService == null)
            {
                return Results.Problem("SCIMv2 service not configured", statusCode: 500);
            }

            var result = await operation(scimService);
            return resultBuilder != null ? resultBuilder(result) : Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (JsonException ex)
        {
            return Results.BadRequest($"Invalid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetService<ILogger<object>>();
            logger?.LogError(ex, "Error in {Operation} for collection {Collection}", operationName, collectionName);
            return Results.Problem("Internal server error", statusCode: 500);
        }
    }
    /// <summary>
    /// Maps SCIMv2 resource endpoints for a specific collection
    /// </summary>
    /// <param name="endpoints">Endpoint route builder</param>
    /// <param name="collectionName">Name of the collection (e.g., "notes", "pads")</param>
    /// <param name="basePath">Base path for the endpoints (defaults to collection name)</param>
    /// <returns>Endpoint route builder</returns>
    public static IEndpointRouteBuilder MapSCIMv2ResourceEndpoints(this IEndpointRouteBuilder endpoints, string collectionName, string? basePath = null)
    {
        if (string.IsNullOrEmpty(collectionName))
            throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

        var path = basePath ?? $"/{collectionName}";

        // GET /{collectionName} - List resources
        endpoints.MapGet(path, async (HttpContext context) =>
        {
            var startIndex = int.TryParse(context.Request.Query["startIndex"], out var si) ? si : 1;
            var count = int.TryParse(context.Request.Query["count"], out var c) ? c : 100;
            var filter = context.Request.Query["filter"].FirstOrDefault();
            var attributes = context.Request.Query["attributes"].FirstOrDefault();
            var excludedAttributes = context.Request.Query["excludedAttributes"].FirstOrDefault();

            return await ExecuteSCIMOperation(
                context,
                collectionName,
                service => service.QueryAsync(collectionName, startIndex, count, filter, null, null, attributes, excludedAttributes, context.RequestAborted),
                "Query");
        })
        .WithName($"SCIMv2{collectionName}List")
        .WithTags("SCIMv2")
        .WithSummary($"List {collectionName}")
        .WithDescription($"RFC 7644 compliant {collectionName} list endpoint");

        // GET /{collectionName}/{id} - Get specific resource
        endpoints.MapGet($"{path}/{{id}}", async (HttpContext context, string id) =>
        {
            return await ExecuteSCIMOperation(
                context,
                collectionName,
                service => service.RetrieveAsync(collectionName, id, context.RequestAborted),
                "Retrieve");
        })
        .WithName($"SCIMv2{collectionName}Get")
        .WithTags("SCIMv2")
        .WithSummary($"Get {collectionName} by ID")
        .WithDescription($"RFC 7644 compliant {collectionName} retrieval endpoint");

        // POST /{collectionName} - Create resource
        endpoints.MapPost(path, async (HttpContext context) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                return Results.BadRequest("Request body cannot be empty");
            }

            return await ExecuteSCIMOperation(
                context,
                collectionName,
                service => service.CreateAsync(collectionName, requestBody, context.RequestAborted),
                "Create",
                result => 
                {
                    // RFC 7644 Section 3.3: POST must return 201 Created with Location header
                    var locationUri = $"{context.Request.Scheme}://{context.Request.Host}{path}";
                    context.Response.Headers["Location"] = locationUri;
                    return Results.Created(locationUri, result);
                });
        })
        .WithName($"SCIMv2{collectionName}Create")
        .WithTags("SCIMv2")
        .WithSummary($"Create {collectionName}")
        .WithDescription($"RFC 7644 compliant {collectionName} creation endpoint");

        // PUT /{collectionName}/{id} - Replace resource
        endpoints.MapPut($"{path}/{{id}}", async (HttpContext context, string id) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                return Results.BadRequest("Request body cannot be empty");
            }

            return await ExecuteSCIMOperation(
                context,
                collectionName,
                service => service.ReplaceAsync(collectionName, id, requestBody, context.RequestAborted),
                "Replace");
        })
        .WithName($"SCIMv2{collectionName}Replace")
        .WithTags("SCIMv2")
        .WithSummary($"Replace {collectionName}")
        .WithDescription($"RFC 7644 compliant {collectionName} replacement endpoint");

        // PATCH /{collectionName}/{id} - Modify resource
        endpoints.MapPatch($"{path}/{{id}}", async (HttpContext context, string id) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                return Results.BadRequest("Request body cannot be empty");
            }

            try
            {
                var patchOps = JsonSerializer.Deserialize<PatchOperation[]>(requestBody);
                if (patchOps == null || patchOps.Length == 0)
                {
                    return Results.BadRequest("Invalid patch operations");
                }

                return await ExecuteSCIMOperation(
                    context,
                    collectionName,
                    service => service.ModifyAsync(collectionName, id, patchOps, context.RequestAborted),
                    "Modify");
            }
            catch (JsonException ex)
            {
                return Results.BadRequest($"Invalid JSON: {ex.Message}");
            }
        })
        .WithName($"SCIMv2{collectionName}Modify")
        .WithTags("SCIMv2")
        .WithSummary($"Modify {collectionName}")
        .WithDescription($"RFC 7644 compliant {collectionName} modification endpoint");

        // DELETE /{collectionName}/{id} - Delete resource
        endpoints.MapDelete($"{path}/{{id}}", async (HttpContext context, string id) =>
        {
            return await ExecuteSCIMOperation(
                context,
                collectionName,
                service => service.DeleteAsync(collectionName, id, context.RequestAborted),
                "Delete",
                _ => Results.NoContent());
        })
        .WithName($"SCIMv2{collectionName}Delete")
        .WithTags("SCIMv2")
        .WithSummary($"Delete {collectionName}")
        .WithDescription($"RFC 7644 compliant {collectionName} deletion endpoint");

        return endpoints;
    }

    /// <summary>
    /// Maps SCIMv2 discovery endpoints
    /// </summary>
    /// <param name="endpoints">Endpoint route builder</param>
    /// <returns>Endpoint route builder</returns>
    public static IEndpointRouteBuilder MapSCIMv2DiscoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // GET /ResourceTypes - List resource types
        endpoints.MapGet("/ResourceTypes", (HttpContext context) =>
        {
            // Return a simple resource types response
            var result = new
            {
                schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ResourceType" },
                totalResults = 2,
                Resources = new[]
                {
                    new
                    {
                        schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ResourceType" },
                        id = "User",
                        name = "User",
                        endpoint = "/Users",
                        description = "User resource type",
                        schema = "urn:ietf:params:scim:schemas:core:2.0:User"
                    },
                    new
                    {
                        schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ResourceType" },
                        id = "Group",
                        name = "Group",
                        endpoint = "/Groups",
                        description = "Group resource type",
                        schema = "urn:ietf:params:scim:schemas:core:2.0:Group"
                    }
                }
            };
            return Results.Ok(result);
        })
        .WithName("SCIMv2ResourceTypes")
        .WithTags("SCIMv2")
        .WithSummary("List resource types")
        .WithDescription("RFC 7644 compliant resource types discovery endpoint");

        // GET /Schemas - List schemas
        endpoints.MapGet("/Schemas", (HttpContext context) =>
        {
            // Return a simple schemas response
            var result = new
            {
                schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Schema" },
                totalResults = 2,
                Resources = new[]
                {
                    new
                    {
                        schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Schema" },
                        id = "urn:ietf:params:scim:schemas:core:2.0:User",
                        name = "User",
                        description = "User schema",
                        attributes = new[]
                        {
                            new { name = "id", type = "string", required = true },
                            new { name = "userName", type = "string", required = true },
                            new { name = "name", type = "complex", required = false },
                            new { name = "emails", type = "complex", required = false },
                            new { name = "active", type = "boolean", required = false }
                        }
                    },
                    new
                    {
                        schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Schema" },
                        id = "urn:ietf:params:scim:schemas:core:2.0:Group",
                        name = "Group",
                        description = "Group schema",
                        attributes = new[]
                        {
                            new { name = "id", type = "string", required = true },
                            new { name = "displayName", type = "string", required = true },
                            new { name = "members", type = "complex", required = false }
                        }
                    }
                }
            };
            return Results.Ok(result);
        })
        .WithName("SCIMv2Schemas")
        .WithTags("SCIMv2")
        .WithSummary("List schemas")
        .WithDescription("RFC 7644 compliant schemas discovery endpoint");

        return endpoints;
    }

    /// <summary>
    /// Maps SCIMv2 ServiceProviderConfig endpoint
    /// </summary>
    /// <param name="endpoints">Endpoint route builder</param>
    public static IEndpointRouteBuilder MapSCIMv2ServiceProviderConfigEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/ServiceProviderConfig", async (HttpContext context) =>
        {
            return await ExecuteSCIMOperation(
                context,
                "ServiceProviderConfig",
                service => service.GetServiceProviderConfigAsync(context.RequestAborted),
                "GetServiceProviderConfig");
        })
        .WithName("SCIMv2ServiceProviderConfig")
        .WithDescription("RFC 7644 compliant service provider configuration endpoint");

        return endpoints;
    }

    /// <summary>
    /// Maps SCIMv2 Bulk operations endpoint
    /// </summary>
    /// <param name="endpoints">Endpoint route builder</param>
    public static IEndpointRouteBuilder MapSCIMv2BulkEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/Bulk", async (HttpContext context) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            return await ExecuteSCIMOperation(
                context,
                "Bulk",
                service => service.BulkAsync(requestBody, context.RequestAborted),
                "Bulk");
        })
        .WithName("SCIMv2Bulk")
        .WithDescription("RFC 7644 compliant bulk operations endpoint");

        return endpoints;
    }

    /// <summary>
    /// Maps SCIMv2 individual Schema endpoint
    /// </summary>
    /// <param name="endpoints">Endpoint route builder</param>
    public static IEndpointRouteBuilder MapSCIMv2SchemaEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/Schemas/{id}", async (HttpContext context, string id) =>
        {
            return await ExecuteSCIMOperation(
                context,
                "Schemas",
                service => service.GetSchemaAsync(id, context.RequestAborted),
                "GetSchema");
        })
        .WithName("SCIMv2Schema")
        .WithDescription("RFC 7644 compliant individual schema endpoint");

        return endpoints;
    }
}
