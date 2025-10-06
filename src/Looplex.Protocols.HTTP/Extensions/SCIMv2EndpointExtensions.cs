using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Looplex.Protocols.HTTP.Ports;

namespace Looplex.Protocols.HTTP.Extensions;

/// <summary>
/// Extension methods for mapping SCIMv2 endpoints
/// </summary>
public static class SCIMv2EndpointExtensions
{
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

            // Try to get real SCIMv2 service first using reflection
            var scimv2Type = Type.GetType("Looplex.SCIMv2.ISCIMv2, Looplex.SCIMv2");
            if (scimv2Type != null)
            {
                var realScimService = context.RequestServices.GetService(scimv2Type);
                if (realScimService != null)
                {
                    // Use real SCIMv2 service via reflection
                    var queryMethod = scimv2Type.GetMethod("QueryAsync");
                    var realResult = await (Task<object>)queryMethod.Invoke(realScimService, new object[] { collectionName, startIndex, count, filter, attributes, excludedAttributes });
                    return Results.Ok(realResult);
                }
            }

            // Fallback to mock service
            var scimService = context.RequestServices.GetService<ISCIMv2Service>();
            if (scimService == null)
                return Results.BadRequest("SCIMv2 service not configured");

            // Use the appropriate query method based on collection name
            object mockResult;
            if (collectionName.Equals("notes", StringComparison.OrdinalIgnoreCase))
            {
                mockResult = await scimService.QueryUsersAsync(filter ?? "", startIndex, count);
            }
            else if (collectionName.Equals("pads", StringComparison.OrdinalIgnoreCase))
            {
                mockResult = await scimService.QueryGroupsAsync(filter ?? "", startIndex, count);
            }
            else
            {
                return Results.BadRequest($"Unsupported collection: {collectionName}");
            }

            return Results.Ok(mockResult);
        })
        .WithName($"SCIMv2{collectionName}List")
        .WithTags("SCIMv2")
        .WithSummary($"List {collectionName}")
        .WithDescription($"RFC 7644 compliant {collectionName} list endpoint");

        // GET /{collectionName}/{id} - Get specific resource
        endpoints.MapGet($"{path}/{{id}}", async (HttpContext context, string id) =>
        {
            // Try to get real SCIMv2 service first using reflection
            var scimv2Type = Type.GetType("Looplex.SCIMv2.ISCIMv2, Looplex.SCIMv2");
            if (scimv2Type != null)
            {
                var realScimService = context.RequestServices.GetService(scimv2Type);
                if (realScimService != null)
                {
                    // Use real SCIMv2 service via reflection
                    var retrieveMethod = scimv2Type.GetMethod("RetrieveAsync");
                    var realResult = await (Task<object>)retrieveMethod.Invoke(realScimService, new object[] { collectionName, id });
                    return Results.Ok(realResult);
                }
            }

            // Fallback to mock service
            var scimService = context.RequestServices.GetService<ISCIMv2Service>();
            if (scimService == null)
                return Results.BadRequest("SCIMv2 service not configured");

            object result;
            if (collectionName.Equals("notes", StringComparison.OrdinalIgnoreCase))
            {
                result = await scimService.GetUserAsync(id);
            }
            else if (collectionName.Equals("pads", StringComparison.OrdinalIgnoreCase))
            {
                result = await scimService.GetGroupAsync(id);
            }
            else
            {
                return Results.BadRequest($"Unsupported collection: {collectionName}");
            }

            return Results.Ok(result);
        })
        .WithName($"SCIMv2{collectionName}Get")
        .WithTags("SCIMv2")
        .WithSummary($"Get {collectionName} by ID")
        .WithDescription($"RFC 7644 compliant {collectionName} retrieval endpoint");

        // POST /{collectionName} - Create resource
        endpoints.MapPost(path, async (HttpContext context) =>
        {
            // Try to get real SCIMv2 service first using reflection
            var scimv2Type = Type.GetType("Looplex.SCIMv2.ISCIMv2, Looplex.SCIMv2");
            if (scimv2Type != null)
            {
                var realScimService = context.RequestServices.GetService(scimv2Type);
                if (realScimService != null)
                {
                    // Use real SCIMv2 service via reflection
                    using var reader = new StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    var createMethod = scimv2Type.GetMethod("CreateAsync");
                    var realResult = await (Task<object>)createMethod.Invoke(realScimService, new object[] { collectionName, requestBody });
                    return Results.Ok(realResult);
                }
            }

            // Fallback to mock service
            var scimService = context.RequestServices.GetService<ISCIMv2Service>();
            if (scimService == null)
                return Results.BadRequest("SCIMv2 service not configured");

            var bodyObj = await context.Request.ReadFromJsonAsync<object>();
            if (bodyObj == null)
                return Results.BadRequest("Invalid request body");

            object result;
            if (collectionName.Equals("notes", StringComparison.OrdinalIgnoreCase))
            {
                result = await scimService.CreateUserAsync(bodyObj);
            }
            else if (collectionName.Equals("pads", StringComparison.OrdinalIgnoreCase))
            {
                result = await scimService.CreateGroupAsync(bodyObj);
            }
            else
            {
                return Results.BadRequest($"Unsupported collection: {collectionName}");
            }

            return Results.Ok(result);
        })
        .WithName($"SCIMv2{collectionName}Create")
        .WithTags("SCIMv2")
        .WithSummary($"Create {collectionName}")
        .WithDescription($"RFC 7644 compliant {collectionName} creation endpoint");

        // PUT /{collectionName}/{id} - Replace resource
        endpoints.MapPut($"{path}/{{id}}", async (HttpContext context, string id) =>
        {
            // Try to get real SCIMv2 service first using reflection
            var scimv2Type = Type.GetType("Looplex.SCIMv2.ISCIMv2, Looplex.SCIMv2");
            if (scimv2Type != null)
            {
                var realScimService = context.RequestServices.GetService(scimv2Type);
                if (realScimService != null)
                {
                    // Use real SCIMv2 service via reflection
                    using var reader = new StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    var replaceMethod = scimv2Type.GetMethod("ReplaceAsync");
                    var realResult = await (Task<object>)replaceMethod.Invoke(realScimService, new object[] { collectionName, id, requestBody });
                    return Results.Ok(realResult);
                }
            }

            // Fallback to mock service
            var scimService = context.RequestServices.GetService<ISCIMv2Service>();
            if (scimService == null)
                return Results.BadRequest("SCIMv2 service not configured");

            var body = await context.Request.ReadFromJsonAsync<object>();
            if (body == null)
                return Results.BadRequest("Invalid request body");

            object result;
            if (collectionName.Equals("notes", StringComparison.OrdinalIgnoreCase))
            {
                result = await scimService.UpdateUserAsync(id, body);
            }
            else if (collectionName.Equals("pads", StringComparison.OrdinalIgnoreCase))
            {
                result = await scimService.UpdateGroupAsync(id, body);
            }
            else
            {
                return Results.BadRequest($"Unsupported collection: {collectionName}");
            }

            return Results.Ok(result);
        })
        .WithName($"SCIMv2{collectionName}Replace")
        .WithTags("SCIMv2")
        .WithSummary($"Replace {collectionName}")
        .WithDescription($"RFC 7644 compliant {collectionName} replacement endpoint");

        // PATCH /{collectionName}/{id} - Modify resource
        endpoints.MapPatch($"{path}/{{id}}", async (HttpContext context, string id) =>
        {
            // Try to get real SCIMv2 service first using reflection
            var scimv2Type = Type.GetType("Looplex.SCIMv2.ISCIMv2, Looplex.SCIMv2");
            if (scimv2Type != null)
            {
                var realScimService = context.RequestServices.GetService(scimv2Type);
                if (realScimService != null)
                {
                    // Use real SCIMv2 service via reflection
                    using var reader = new StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    var patchOpsType = Type.GetType("Looplex.SCIMv2.Entities.PatchOperation, Looplex.SCIMv2");
                    var patchOps = System.Text.Json.JsonSerializer.Deserialize(requestBody, patchOpsType.MakeArrayType());
                    var modifyMethod = scimv2Type.GetMethod("ModifyAsync");
                    var realResult = await (Task<object>)modifyMethod.Invoke(realScimService, new object[] { collectionName, id, patchOps });
                    return Results.Ok(realResult);
                }
            }

            // Fallback to mock service (not implemented for PATCH)
            return Results.BadRequest("PATCH operation not supported in mock mode");
        })
        .WithName($"SCIMv2{collectionName}Modify")
        .WithTags("SCIMv2")
        .WithSummary($"Modify {collectionName}")
        .WithDescription($"RFC 7644 compliant {collectionName} modification endpoint");

        // DELETE /{collectionName}/{id} - Delete resource
        endpoints.MapDelete($"{path}/{{id}}", async (HttpContext context, string id) =>
        {
            // Try to get real SCIMv2 service first using reflection
            var scimv2Type = Type.GetType("Looplex.SCIMv2.ISCIMv2, Looplex.SCIMv2");
            if (scimv2Type != null)
            {
                var realScimService = context.RequestServices.GetService(scimv2Type);
                if (realScimService != null)
                {
                    // Use real SCIMv2 service via reflection
                    var deleteMethod = scimv2Type.GetMethod("DeleteAsync");
                    var realResult = await (Task<object>)deleteMethod.Invoke(realScimService, new object[] { collectionName, id });
                    return Results.Ok(realResult);
                }
            }

            // Fallback to mock service
            var scimService = context.RequestServices.GetService<ISCIMv2Service>();
            if (scimService == null)
                return Results.BadRequest("SCIMv2 service not configured");

            if (collectionName.Equals("notes", StringComparison.OrdinalIgnoreCase))
            {
                await scimService.DeleteUserAsync(id);
            }
            else if (collectionName.Equals("pads", StringComparison.OrdinalIgnoreCase))
            {
                await scimService.DeleteGroupAsync(id);
            }
            else
            {
                return Results.BadRequest($"Unsupported collection: {collectionName}");
            }

            return Results.Ok();
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
        endpoints.MapGet("/ResourceTypes", async (HttpContext context) =>
        {
            // Return a simple resource types response
            var result = new
            {
                schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ResourceType" },
                totalResults = 2,
                items = new[]
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
        endpoints.MapGet("/Schemas", async (HttpContext context) =>
        {
            // Return a simple schemas response
            var result = new
            {
                schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Schema" },
                totalResults = 2,
                items = new[]
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
}
