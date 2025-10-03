using System;
using System.IO;
using System.Text.Json;
using Looplex.SCIMv2;
using Looplex.SCIMv2.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Looplex.Protocols.HTTP.Middlewares;

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
                    try
                    {
                        Console.WriteLine($"🔍 SCIMv2 Middleware - GET /{collectionName}");
                        
                        var startIndex = int.Parse(context.Request.Query["startIndex"].FirstOrDefault() ?? "1");
                        var count = int.Parse(context.Request.Query["count"].FirstOrDefault() ?? "100");
                        var filter = context.Request.Query["filter"].FirstOrDefault();
                        var sortBy = context.Request.Query["sortBy"].FirstOrDefault();
                        var sortOrder = context.Request.Query["sortOrder"].FirstOrDefault();

                        Console.WriteLine($"🔍 Query params - startIndex: {startIndex}, count: {count}, filter: {filter}");

                        var response = await scimService.QueryAsync(
                            collectionName, startIndex, count, filter, sortBy, sortOrder, 
                            context.RequestAborted);
                        
                        Console.WriteLine($"🔍 SCIMv2 Service returned response with StatusCode: {response.StatusCode}");

                        //  Return SCIMv2 compliant result with proper Content-Type
                        return CreateSCIMv2Result(response);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ ERROR in SCIMv2 Middleware GET /{collectionName}: {ex.Message}");
                        Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                        throw;
                    }
                });

                // GET /{collectionName}/{id} - Retrieve resource
                group.MapGet("{id}", async (string id, HttpContext context, ISCIMv2 scimService) =>
                {
                    try
                    {
                        var response = await scimService.RetrieveAsync(collectionName, id.ToString(), context.RequestAborted);
                        
                        // Check if response has errors or is null
                        if (response == null || response.Error != null)
                        {
                            // Return SCIM v2.0 compliant error response
                            return CreateSCIMv2Result(response, 404);
                        }
                        
                        // Set HTTP headers for SCIM v2.0 compliance
                        if (!string.IsNullOrEmpty(response.Location))
                        {
                            Console.WriteLine($"🔍 Setting Location header: {response.Location}");
                            context.Response.Headers["Location"] = response.Location;
                        }
                        
                        if (!string.IsNullOrEmpty(response.ETag))
                        {
                            Console.WriteLine($"🔍 Setting ETag header: {response.ETag}");
                            context.Response.Headers["ETag"] = response.ETag;
                        }
                        
                        //  Return SCIMv2 compliant result with proper Content-Type
                        return CreateSCIMv2Result(response);
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
                            //  Use SCIMv2 error format instead of direct Results.BadRequest
                            var errorResponse = new SCIMv2Response
                            {
                                StatusCode = 400,
                                Error = new SCIMv2Error
                                {
                                    Status = "400",
                                    Detail = validation.ErrorMessage,
                                    ScimType = "invalidSyntax",
                                    Timestamp = DateTime.UtcNow.ToString("O")
                                }
                            };
                            return CreateSCIMv2Result(errorResponse, 400);
                        }
                        
                        
                        // Use ISCIMv2 directly - it will handle any registered collection
                        // The service will use the appropriate IResourceService<T> implementation
                        var response = await scimService.CreateAsync(collectionName, json, context.RequestAborted);
                        
                        // Set HTTP headers for SCIM v2.0 compliance
                        if (!string.IsNullOrEmpty(response.Location))
                        {
                            Console.WriteLine($"🔍 Setting Location header: {response.Location}");
                            context.Response.Headers["Location"] = response.Location;
                        }
                        
                        if (!string.IsNullOrEmpty(response.ETag))
                        {
                            Console.WriteLine($"🔍 Setting ETag header: {response.ETag}");
                            context.Response.Headers["ETag"] = response.ETag;
                        }
                        
                        // Use centralized HTTP result mapping from SCIMv2 core
                        return MapToHttpResult(response, collectionName);
                    }
                    catch (Exception ex)
                    {
                        //  Use SCIMv2 error format for exceptions
                        var errorResponse = new SCIMv2Response
                        {
                            StatusCode = 500,
                            Error = new SCIMv2Error
                            {
                                Status = "500",
                                Detail = $"Error creating resource: {ex.Message}",
                                ScimType = "internalError",
                                Timestamp = DateTime.UtcNow.ToString("O")
                            }
                        };
                        return CreateSCIMv2Result(errorResponse, 500);
                    }
                });

                // PUT /{collectionName}/{id} - Replace resource
                group.MapPut("{id}", async (string id, HttpContext context, ISCIMv2 scimService, ISCIMv2Validation validationService) =>
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
                            //  Use SCIMv2 error format for validation failures
                            var errorResponse = new SCIMv2Response
                            {
                                StatusCode = 400,
                                Error = new SCIMv2Error
                                {
                                    Status = "400",
                                    Detail = validation.ErrorMessage,
                                    ScimType = "invalidSyntax",
                                    Timestamp = DateTime.UtcNow.ToString("O")
                                }
                            };
                            return CreateSCIMv2Result(errorResponse, 400);
                        }
                        
                        // Use ISCIMv2 directly - it will handle any registered collection
                        // The service will use the appropriate IResourceService<T> implementation
                        var response = await scimService.ReplaceAsync(collectionName, id.ToString(), json, context.RequestAborted);
                        
                        // Set HTTP headers for SCIM v2.0 compliance
                        if (!string.IsNullOrEmpty(response.Location))
                        {
                            Console.WriteLine($"🔍 Setting Location header: {response.Location}");
                            context.Response.Headers["Location"] = response.Location;
                        }
                        
                        if (!string.IsNullOrEmpty(response.ETag))
                        {
                            Console.WriteLine($"🔍 Setting ETag header: {response.ETag}");
                            context.Response.Headers["ETag"] = response.ETag;
                        }
                        
                        // Use centralized HTTP result mapping from SCIMv2 core
                        return MapToHttpResult(response, collectionName, id.ToString());
                    }
                    catch (Exception ex)
                    {
                        //  Use SCIMv2 error format for exceptions
                        var errorResponse = new SCIMv2Response
                        {
                            StatusCode = 500,
                            Error = new SCIMv2Error
                            {
                                Status = "500",
                                Detail = $"Error replacing resource: {ex.Message}",
                                ScimType = "internalError",
                                Timestamp = DateTime.UtcNow.ToString("O")
                            }
                        };
                        return CreateSCIMv2Result(errorResponse, 500);
                    }
                });

                // PATCH /{collectionName}/{id} - Update resource
                group.MapPatch("{id}", async (string id, HttpContext context, ISCIMv2 scimService, ISCIMv2Validation validationService) =>
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
                            //  Use SCIMv2 error format for PATCH validation failures
                            var errorResponse = new SCIMv2Response
                            {
                                StatusCode = 400,
                                Error = new SCIMv2Error
                                {
                                    Status = "400",
                                    Detail = patchResult.ErrorMessage,
                                    ScimType = "invalidSyntax",
                                    Timestamp = DateTime.UtcNow.ToString("O")
                                }
                            };
                            return CreateSCIMv2Result(errorResponse, 400);
                        }
                        
                        
                        // Call SCIMv2 service to modify the resource
                        var response = await scimService.ModifyAsync(collectionName, id.ToString(), patchResult.Operations, context.RequestAborted);
                        
                        // Set HTTP headers for SCIM v2.0 compliance
                        if (!string.IsNullOrEmpty(response.Location))
                        {
                            Console.WriteLine($"🔍 Setting Location header: {response.Location}");
                            context.Response.Headers["Location"] = response.Location;
                        }
                        
                        if (!string.IsNullOrEmpty(response.ETag))
                        {
                            Console.WriteLine($"🔍 Setting ETag header: {response.ETag}");
                            context.Response.Headers["ETag"] = response.ETag;
                        }
                        
                        // Use centralized HTTP result mapping from SCIMv2 core
                        return MapToHttpResult(response, collectionName, id.ToString());
                    }
                    catch (Exception ex)
                    {
                        //  Use SCIMv2 error format for exceptions
                        var errorResponse = new SCIMv2Response
                        {
                            StatusCode = 500,
                            Error = new SCIMv2Error
                            {
                                Status = "500",
                                Detail = $"Error updating resource: {ex.Message}",
                                ScimType = "internalError",
                                Timestamp = DateTime.UtcNow.ToString("O")
                            }
                        };
                        return CreateSCIMv2Result(errorResponse, 500);
                    }
                });

                // DELETE /{collectionName}/{id} - Delete resource
                group.MapDelete("{id}", async (string id, HttpContext context, ISCIMv2 scimService) =>
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
                        
                        return CreateSCIMv2Result(response);
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
                        
                        return CreateSCIMv2Result(response);
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
                        
                        return CreateSCIMv2Result(response);
                    }
                    catch (Exception)
                    {
                        return Results.StatusCode(500);
                    }
                });

                // POST /Bulk - Global Bulk operations endpoint (RFC 7644 Section 3.7)
                // https://datatracker.ietf.org/doc/html/rfc7644#section-3.7
                group.MapPost("/Bulk", async (HttpContext context, ISCIMv2 scimService, ISCIMv2Validation validationService) =>
                {
                    try
                    {
                        // Read the request body
                        using var reader = new StreamReader(context.Request.Body);
                        var json = await reader.ReadToEndAsync();
                        
                        // Validate BulkRequest
                        var validation = validationService.ValidateJsonRequest(json);
                        if (!validation.IsValid)
                        {
                            var errorResponse = new SCIMv2Response
                            {
                                StatusCode = 400,
                                Error = new SCIMv2Error
                                {
                                    Status = "400",
                                    Detail = validation.ErrorMessage,
                                    ScimType = "invalidSyntax",
                                    Timestamp = DateTime.UtcNow.ToString("O")
                                }
                            };
                            return CreateSCIMv2Result(errorResponse, 400);
                        }
                        
                        // Process BulkRequest using SCIMv2 service
                        var response = await scimService.BulkAsync(json, context.RequestAborted);
                        
                        return CreateSCIMv2Result(response);
                    }
                    catch (Exception ex)
                    {
                        var errorResponse = new SCIMv2Response
                        {
                            StatusCode = 500,
                            Error = new SCIMv2Error
                            {
                                Status = "500",
                                Detail = $"Error processing bulk request: {ex.Message}",
                                ScimType = "internalError",
                                Timestamp = DateTime.UtcNow.ToString("O")
                            }
                        };
                        return CreateSCIMv2Result(errorResponse, 500);
                    }
                });

                // GET /ResourceTypes - Resource types discovery (RFC 7644 Section 3.2)
                // https://datatracker.ietf.org/doc/html/rfc7644#section-3.2
                group.MapGet("/ResourceTypes", async (HttpContext context, ISCIMv2 scimService) =>
                {
                    try
                    {
                        var response = await scimService.GetResourceTypesAsync(context.RequestAborted);
                        
                        if (response.Error != null)
                        {
                            return Results.StatusCode(response.StatusCode);
                        }
                        
                        return CreateSCIMv2Result(response);
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
    /// <summary>
    /// Creates SCIMv2 compliant HTTP result with proper headers
    /// Implements RFC 7644 Section 3.4.1 - HTTP Content-Type
    /// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.1)
    /// </summary>
    private static IResult CreateSCIMv2Result(SCIMv2Response response, int statusCode = 200)
    {
        Console.WriteLine($"🔍 CreateSCIMv2Result - StatusCode: {statusCode}, Response StatusCode: {response.StatusCode}");
        Console.WriteLine($"🔍 Response Data type: {response.Data?.GetType().Name ?? "null"}");
        Console.WriteLine($"🔍 Response Schemas: {string.Join(", ", response.Schemas ?? new string[0])}");
        
        //  RFC 7644 Section 3.4.5 - DELETE responses (204) must not have body
        if (statusCode == 204)
        {
            Console.WriteLine($"🔍 Returning NoContent for 204 status");
            return Results.NoContent();
        }
        
        // Use centralized formatting logic from SCIMv2 class
        var jsonContent = Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(response);
        
        var result = Results.Content(jsonContent, "application/scim+json", statusCode: statusCode);
        
        // Add Location and ETag headers if present in response
        if (!string.IsNullOrEmpty(response.Location))
        {
            Console.WriteLine($"🔍 Setting Location header: {response.Location}");
            // Note: We need to set headers in the HttpContext directly
            // This will be handled by the middleware pipeline
        }
        
        if (!string.IsNullOrEmpty(response.ETag))
        {
            Console.WriteLine($"🔍 Setting ETag header: {response.ETag}");
            // Note: We need to set headers in the HttpContext directly
            // This will be handled by the middleware pipeline
        }
        
        Console.WriteLine($"🔍 Returning JSON result with Content-Type: application/scim+json");
        return result;
    }

    private static IResult MapToHttpResult(SCIMv2Response response, string collectionName, string? resourceId = null)
    {
        return response.StatusCode switch
        {
            200 => CreateSCIMv2Result(response),
            201 => CreateSCIMv2Result(response, 201),
            204 => Results.NoContent(),
            //  RFC 7644 Section 3.12 - All error responses must be in SCIMv2 format
            400 => CreateSCIMv2Result(response, 400),
            404 => CreateSCIMv2Result(response, 404),
            409 => CreateSCIMv2Result(response, 409),
            412 => CreateSCIMv2Result(response, 412),
            500 => CreateSCIMv2Result(response, 500),
            _ => CreateSCIMv2Result(response, response.StatusCode)
        };
    }
}
