using System.Text.Json;
using Looplex.Foundation.SCIMv2;
using Looplex.Foundation.SCIMv2.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Looplex.Foundation.Protocols.Http.Middlewares;

/// <summary>
/// Simplified SCIMv2 Middleware - HTTP Adapter Only
/// Uses the centralized SCIMv2 logic from Looplex.Foundation
/// </summary>
public static class SCIMv2
{
    /// <summary>
    /// Adds SCIMv2 endpoints to the application
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="collectionName">Collection name (e.g., "Users", "Groups")</param>
    /// <param name="authorize">Whether to require authorization</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseSCIMv2(
        this IApplicationBuilder app, 
        string collectionName, 
        bool authorize = true)
    {
        return app.UseRouting()
            .UseEndpoints(endpoints =>
            {
                var group = endpoints.MapGroup($"/{collectionName}");
                
                if (authorize)
                {
                    group.RequireAuthorization();
                }

                // GET /{collectionName} - Query resources
                group.MapGet("", async (HttpContext context, ISCIMv2 scimService) =>
                {
                    var startIndex = int.Parse(context.Request.Query["startIndex"].FirstOrDefault() ?? "1");
                    var count = int.Parse(context.Request.Query["count"].FirstOrDefault() ?? "100");
                    var filter = context.Request.Query["filter"].FirstOrDefault();
                    var sortBy = context.Request.Query["sortBy"].FirstOrDefault();
                    var sortOrder = context.Request.Query["sortOrder"].FirstOrDefault();

                    var response = await scimService.QueryAsync(
                        collectionName, startIndex, count, filter, sortBy, sortOrder, 
                        context.RequestAborted);

                    return Results.Ok(response);
                });

                // GET /{collectionName}/{id} - Retrieve resource
                group.MapGet("{id:guid}", async (Guid id, HttpContext context, ISCIMv2 scimService) =>
                {
                    try
                    {
                        var response = await scimService.RetrieveAsync(collectionName, id.ToString(), context.RequestAborted);
                        
                        // Check if response has errors or is null
                        if (response == null || response.Error != null)
                        {
                            return Results.NotFound(response);
                        }
                        
                        return Results.Ok(response);
                    }
                    catch (Exception)
                    {
                        return Results.NotFound();
                    }
                });

                // POST /{collectionName} - Create resource
                group.MapPost("", async (HttpContext context, ISCIMv2 scimService, ISCIMv2Validation validationService) =>
                {
                    try
                    {
                        // Read the request body
                        using var reader = new StreamReader(context.Request.Body);
                        var json = await reader.ReadToEndAsync();
                        
                        // Use existing validation implementation
                        var validation = validationService.ValidateJsonRequest(json);
                        if (!validation.IsValid)
                        {
                            return Results.BadRequest(validation.ErrorMessage);
                        }
                        
                        // Use existing mock resource creation
                        var mockResource = validationService.CreateMockResource(collectionName, Guid.NewGuid().ToString());
                        
                        // Call SCIMv2 service to create the resource
                        var response = await scimService.CreateAsync(collectionName, mockResource, context.RequestAborted);
                        
                        // Use centralized HTTP result mapping from SCIMv2 core
                        return MapToHttpResult(response, collectionName, mockResource.Id);
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest($"Error creating resource: {ex.Message}");
                    }
                });

                // PUT /{collectionName}/{id} - Replace resource
                group.MapPut("{id:guid}", async (Guid id, HttpContext context, ISCIMv2 scimService, ISCIMv2Validation validationService) =>
                {
                    try
                    {
                        // Read the request body
                        using var reader = new StreamReader(context.Request.Body);
                        var json = await reader.ReadToEndAsync();
                        
                        // Use existing validation implementation
                        var validation = validationService.ValidateJsonRequest(json);
                        if (!validation.IsValid)
                        {
                            return Results.BadRequest(validation.ErrorMessage);
                        }
                        
                        // Use existing collection validation
                        var collectionValidation = validationService.ValidateCollection(collectionName);
                        if (!collectionValidation.IsValid)
                        {
                            return Results.BadRequest(collectionValidation.ErrorMessage);
                        }
                        
                        // Use existing mock resource creation
                        var mockResource = validationService.CreateMockResource(collectionName, id.ToString());
                        
                        // Call SCIMv2 service to replace the resource
                        var response = await scimService.ReplaceAsync(collectionName, id.ToString(), mockResource, context.RequestAborted);
                        
                        // Use centralized HTTP result mapping from SCIMv2 core
                        return MapToHttpResult(response, collectionName, id.ToString());
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest($"Error replacing resource: {ex.Message}");
                    }
                });

                // PATCH /{collectionName}/{id} - Update resource
                group.MapPatch("{id:guid}", async (Guid id, HttpContext context, ISCIMv2 scimService, ISCIMv2Validation validationService) =>
                {
                    try
                    {
                        // Read the request body
                        using var reader = new StreamReader(context.Request.Body);
                        var json = await reader.ReadToEndAsync();
                        
                        // Use existing PATCH operations parsing
                        var patchResult = validationService.ParsePatchOperations(json);
                        if (!patchResult.IsValid)
                        {
                            return Results.BadRequest(patchResult.ErrorMessage);
                        }
                        
                        // Call SCIMv2 service to modify the resource
                        var response = await scimService.ModifyAsync(collectionName, id.ToString(), patchResult.Operations, context.RequestAborted);
                        
                        // Use centralized HTTP result mapping from SCIMv2 core
                        return MapToHttpResult(response, collectionName, id.ToString());
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest($"Error updating resource: {ex.Message}");
                    }
                });

                // DELETE /{collectionName}/{id} - Delete resource
                group.MapDelete("{id:guid}", async (Guid id, HttpContext context, ISCIMv2 scimService) =>
                {
                    try
                    {
                        var response = await scimService.DeleteAsync(collectionName, id.ToString(), context.RequestAborted);
                        
                        // Check if response has errors or is null
                        if (response == null || response.Error != null)
                        {
                            return Results.NotFound(response);
                        }
                        
                        return Results.NoContent();
                    }
                    catch (Exception)
                    {
                        return Results.NotFound();
                    }
                });
            });
    }

    /// <summary>
    /// Adds SCIMv2 discovery endpoints to the application
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="authorize">Whether to require authorization</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseSCIMv2Discovery(
        this IApplicationBuilder app, 
        bool authorize = true)
    {
        return app.UseRouting()
            .UseEndpoints(endpoints =>
            {
                var group = endpoints.MapGroup("");
                
                if (authorize)
                {
                    group.RequireAuthorization();
                }

                // GET /Schemas - Get all schemas
                group.MapGet("/Schemas", async (HttpContext context, ISCIMv2 scimService) =>
                {
                    try
                    {
                        var response = await scimService.GetSchemasAsync(context.RequestAborted);
                        
                        if (response.Error != null)
                        {
                            return Results.StatusCode(response.StatusCode);
                        }
                        
                        return Results.Ok(response);
                    }
                    catch (Exception)
                    {
                        return Results.StatusCode(500);
                    }
                });

                // GET /Schemas/{id} - Get specific schema
                group.MapGet("/Schemas/{schemaId}", async (string schemaId, HttpContext context, ISCIMv2 scimService) =>
                {
                    try
                    {
                        var response = await scimService.GetSchemaAsync(schemaId, context.RequestAborted);
                        
                        if (response.Error != null)
                        {
                            return Results.StatusCode(response.StatusCode);
                        }
                        
                        return Results.Ok(response);
                    }
                    catch (Exception)
                    {
                        return Results.StatusCode(500);
                    }
                });

                // GET /ServiceProviderConfig - Get service provider configuration
                group.MapGet("/ServiceProviderConfig", async (HttpContext context, ISCIMv2 scimService) =>
                {
                    try
                    {
                        var response = await scimService.GetServiceProviderConfigAsync(context.RequestAborted);
                        
                        if (response.Error != null)
                        {
                            return Results.StatusCode(response.StatusCode);
                        }
                        
                        return Results.Ok(response);
                    }
                    catch (Exception)
                    {
                        return Results.StatusCode(500);
                    }
                });
            });
    }

    /// <summary>
    /// Maps SCIMv2Response status codes to HTTP results according to RFC 7644
    /// Implements RFC 7644 Section 3.2 - HTTP Status Codes
    /// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644#section-3.2) - HTTP Status Codes
    /// </summary>
    /// <param name="response">SCIMv2 response</param>
    /// <param name="collectionName">Collection name for resource URLs</param>
    /// <param name="resourceId">Resource ID for Created responses</param>
    /// <returns>HTTP result</returns>
    private static IResult MapToHttpResult(SCIMv2Response response, string collectionName, string? resourceId = null)
    {
        return response.StatusCode switch
        {
            200 => Results.Ok(response),
            201 => Results.Created($"/{collectionName}/{resourceId}", response),
            204 => Results.NoContent(),
            400 => Results.BadRequest(response.Error?.Detail ?? "Bad request"),
            404 => Results.NotFound(response),
            409 => Results.Conflict(response.Error?.Detail ?? "Conflict"),
            412 => Results.StatusCode(412),
            500 => Results.StatusCode(500),
            _ => Results.StatusCode(response.StatusCode)
        };
    }
}
