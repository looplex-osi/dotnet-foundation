using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Looplex.SCIMv2.Entities;

namespace Looplex.SCIMv2.Extensions
{
    /// <summary>
    /// Extension methods for mapping SCIMv2 resource endpoints
    /// Provides generic endpoint mapping for any resource collection
    /// </summary>
    public static class EndpointExtensions
    {
        /// <summary>
        /// Maps SCIMv2 resource endpoints for a given collection name
        /// </summary>
        /// <param name="endpoints">Endpoint route builder</param>
        /// <param name="collectionName">Name of the resource collection (e.g., "notes", "pads", "users")</param>
        /// <param name="basePath">Optional base path, defaults to collection name</param>
        /// <returns>Endpoint route builder for chaining</returns>
        public static IEndpointRouteBuilder MapSCIMv2ResourceEndpoints(
            this IEndpointRouteBuilder endpoints, 
            string collectionName, 
            string? basePath = null)
        {
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

            var path = basePath ?? $"/{collectionName}";

            // GET /{collectionName} - List resources
            endpoints.MapGet(path, async (HttpContext context) =>
            {
                var scimService = context.RequestServices.GetRequiredService<ISCIMv2>();
                // Parse startIndex with validation
                if (!int.TryParse(context.Request.Query["startIndex"].FirstOrDefault() ?? "1", out var startIndex) || startIndex < 1)
                {
                    return Results.BadRequest(new SCIMv2Response
                    {
                        StatusCode = 400,
                        Error = new SCIMv2Error
                        {
                            Status = "400",
                            ScimType = "invalidValue",
                            Detail = "startIndex must be a positive integer"
                        }
                    });
                }

                // Parse count with validation
                if (!int.TryParse(context.Request.Query["count"].FirstOrDefault() ?? "100", out var count) || count < 1)
                {
                    return Results.BadRequest(new SCIMv2Response
                    {
                        StatusCode = 400,
                        Error = new SCIMv2Error
                        {
                            Status = "400",
                            ScimType = "invalidValue",
                            Detail = "count must be a positive integer"
                        }
                    });
                }
                var filter = context.Request.Query["filter"].FirstOrDefault();
                var attributes = context.Request.Query["attributes"].FirstOrDefault();
                var excludedAttributes = context.Request.Query["excludedAttributes"].FirstOrDefault();
                
                var result = await scimService.QueryAsync(collectionName, startIndex, count, filter, attributes, excludedAttributes);
                return Results.Ok(result);
            })
            .WithName($"SCIMv2{collectionName}List")
            .WithTags("SCIMv2")
            .WithSummary($"List {collectionName}")
            .WithDescription($"RFC 7644 compliant {collectionName} list endpoint");

            // GET /{collectionName}/{id} - Get specific resource
            endpoints.MapGet($"{path}/{{id}}", async (HttpContext context, string id) =>
            {
                var scimService = context.RequestServices.GetRequiredService<ISCIMv2>();
                var result = await scimService.RetrieveAsync(collectionName, id);
                return Results.Ok(result);
            })
            .WithName($"SCIMv2{collectionName}Get")
            .WithTags("SCIMv2")
            .WithSummary($"Get {collectionName} by ID")
            .WithDescription($"RFC 7644 compliant {collectionName} retrieval endpoint");

            // POST /{collectionName} - Create resource
            endpoints.MapPost(path, async (HttpContext context) =>
            {
                var scimService = context.RequestServices.GetRequiredService<ISCIMv2>();
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                var result = await scimService.CreateAsync(collectionName, body);
                return Results.Ok(result);
            })
            .WithName($"SCIMv2{collectionName}Create")
            .WithTags("SCIMv2")
            .WithSummary($"Create {collectionName}")
            .WithDescription($"RFC 7644 compliant {collectionName} creation endpoint");

            // PUT /{collectionName}/{id} - Replace resource
            endpoints.MapPut($"{path}/{{id}}", async (HttpContext context, string id) =>
            {
                var scimService = context.RequestServices.GetRequiredService<ISCIMv2>();
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                var result = await scimService.ReplaceAsync(collectionName, id, body);
                return Results.Ok(result);
            })
            .WithName($"SCIMv2{collectionName}Replace")
            .WithTags("SCIMv2")
            .WithSummary($"Replace {collectionName}")
            .WithDescription($"RFC 7644 compliant {collectionName} replacement endpoint");

            // PATCH /{collectionName}/{id} - Modify resource
            endpoints.MapPatch($"{path}/{{id}}", async (HttpContext context, string id) =>
            {
                var scimService = context.RequestServices.GetRequiredService<ISCIMv2>();
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                var patchOps = JsonSerializer.Deserialize<Entities.PatchOperation[]>(body);
                var result = await scimService.ModifyAsync(collectionName, id, patchOps ?? Array.Empty<Entities.PatchOperation>());
                return Results.Ok(result);
            })
            .WithName($"SCIMv2{collectionName}Modify")
            .WithTags("SCIMv2")
            .WithSummary($"Modify {collectionName}")
            .WithDescription($"RFC 7644 compliant {collectionName} modification endpoint");

            // DELETE /{collectionName}/{id} - Delete resource
            endpoints.MapDelete($"{path}/{{id}}", async (HttpContext context, string id) =>
            {
                var scimService = context.RequestServices.GetRequiredService<ISCIMv2>();
                var result = await scimService.DeleteAsync(collectionName, id);
                return Results.Ok(result);
            })
            .WithName($"SCIMv2{collectionName}Delete")
            .WithTags("SCIMv2")
            .WithSummary($"Delete {collectionName}")
            .WithDescription($"RFC 7644 compliant {collectionName} deletion endpoint");

            return endpoints;
        }

        /// <summary>
        /// Maps SCIMv2 discovery endpoints (ServiceProviderConfig, ResourceTypes, Schemas)
        /// </summary>
        /// <param name="endpoints">Endpoint route builder</param>
        /// <param name="basePath">Optional base path for discovery endpoints</param>
        /// <returns>Endpoint route builder for chaining</returns>
        public static IEndpointRouteBuilder MapSCIMv2DiscoveryEndpoints(
            this IEndpointRouteBuilder endpoints, 
            string? basePath = null)
        {
            var path = basePath ?? "";

            // GET /ServiceProviderConfig
            endpoints.MapGet($"{path}/ServiceProviderConfig", async (HttpContext context) =>
            {
                var scimService = context.RequestServices.GetRequiredService<ISCIMv2>();
                var result = await scimService.GetServiceProviderConfigAsync();
                return Results.Ok(result);
            })
            .WithName("SCIMv2ServiceProviderConfig")
            .WithTags("SCIMv2")
            .WithSummary("Get Service Provider Configuration")
            .WithDescription("RFC 7644 compliant service provider configuration endpoint");

            // GET /ResourceTypes
            endpoints.MapGet($"{path}/ResourceTypes", async (HttpContext context) =>
            {
                var scimService = context.RequestServices.GetRequiredService<ISCIMv2>();
                var result = await scimService.GetResourceTypesAsync();
                return Results.Ok(result);
            })
            .WithName("SCIMv2ResourceTypes")
            .WithTags("SCIMv2")
            .WithSummary("Get Resource Types")
            .WithDescription("RFC 7644 compliant resource types endpoint");

            // GET /Schemas
            endpoints.MapGet($"{path}/Schemas", async (HttpContext context) =>
            {
                var scimService = context.RequestServices.GetRequiredService<ISCIMv2>();
                var result = await scimService.GetSchemasAsync();
                return Results.Ok(result);
            })
            .WithName("SCIMv2Schemas")
            .WithTags("SCIMv2")
            .WithSummary("Get Schemas")
            .WithDescription("RFC 7644 compliant schemas endpoint");

            return endpoints;
        }
    }
}
