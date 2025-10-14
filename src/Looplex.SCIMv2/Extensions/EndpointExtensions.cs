using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Looplex.SCIMv2.Entities;
using Looplex.Foundation.Serialization;

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
        /// <param name="app">Application builder</param>
        /// <param name="collectionName">Name of the resource collection (e.g., "notes", "pads", "users")</param>
        /// <param name="basePath">Optional base path, defaults to collection name</param>
        /// <returns>Application builder for chaining</returns>
        public static IApplicationBuilder MapSCIMv2ResourceEndpoints(
            this IApplicationBuilder app, 
            string collectionName, 
            string? basePath = null)
        {
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

            var path = basePath ?? $"/{collectionName}";

            // GET /{collectionName} - List resources
            app.MapWhen(context => context.Request.Path.StartsWithSegments(path) && 
                                   context.Request.Method == "GET" && 
                                   context.Request.Path.Value.Length <= path.Length + 1, 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        // Get SCIMv2 service from DI
                        var scimService = context.RequestServices.GetService<Looplex.SCIMv2.ISCIMv2>();
                        if (scimService == null)
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("SCIMv2 service not configured");
                            return;
                        }

                        // Get query parameters
                        var startIndex = int.TryParse(context.Request.Query["startIndex"], out var si) ? si : 1;
                        var count = int.TryParse(context.Request.Query["count"], out var c) ? c : 100;
                        var filter = context.Request.Query["filter"].ToString();
                        var sortBy = context.Request.Query["sortBy"].ToString();
                        var sortOrder = context.Request.Query["sortOrder"].ToString();
                        var attributes = context.Request.Query["attributes"].ToString();

                        // Check if collection is registered
                        if (!scimService.IsCollectionRegistered(collectionName))
                        {
                            context.Response.StatusCode = 404;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Collection '{collectionName}' not found" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }

                        // Use the registered service to get actual data
                        try
                        {
                            // Use the SCIMv2 service to query resources
                            var scimResponse = await scimService.QueryAsync(collectionName, startIndex, count, filter, sortBy, sortOrder, null, null, CancellationToken.None);
                            
                            // Return the SCIMv2 response with proper formatting
                            context.Response.StatusCode = scimResponse.StatusCode;
                            context.Response.ContentType = "application/scim+json";
                            await context.Response.WriteAsync(Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(scimResponse));
                            return;
                        }
                        catch (Exception ex)
                        {
                            // If service integration fails, return 501
                            context.Response.StatusCode = 501;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Service integration not implemented: {ex.Message}" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            // GET /{collectionName}/{id} - Get specific resource
            app.MapWhen(context => context.Request.Path.StartsWithSegments(path) && 
                                   context.Request.Method == "GET" && 
                                   context.Request.Path.Value.Length > path.Length + 1, 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        var scimService = context.RequestServices.GetService<Looplex.SCIMv2.ISCIMv2>();
                        if (scimService == null)
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("SCIMv2 service not configured");
                            return;
                        }

                        // Extract ID from path
                        var pathSegments = context.Request.Path.Value.Split('/');
                        var id = pathSegments.LastOrDefault();
                        
                        if (string.IsNullOrEmpty(id))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Invalid resource ID");
                            return;
                        }

                        // Check if collection is registered
                        if (!scimService.IsCollectionRegistered(collectionName))
                        {
                            context.Response.StatusCode = 404;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Collection '{collectionName}' not found" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }

                        // Use the registered service to get actual data
                        try
                        {
                            // Use the SCIMv2 service to retrieve the resource
                            var scimResponse = await scimService.RetrieveAsync(collectionName, id, CancellationToken.None);
                            
                            // Return the SCIMv2 response with proper formatting
                            context.Response.StatusCode = scimResponse.StatusCode;
                            context.Response.ContentType = "application/scim+json";
                            await context.Response.WriteAsync(Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(scimResponse));
                            return;
                        }
                        catch (Exception ex)
                        {
                            // If service integration fails, return 501
                            context.Response.StatusCode = 501;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Service integration not implemented: {ex.Message}" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            // POST /{collectionName} - Create resource
            app.MapWhen(context => context.Request.Path.StartsWithSegments(path) && 
                                   context.Request.Method == "POST", 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        var scimService = context.RequestServices.GetService<Looplex.SCIMv2.ISCIMv2>();
                        if (scimService == null)
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("SCIMv2 service not configured");
                            return;
                        }

                        // Read request body
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                        
                        if (string.IsNullOrEmpty(body))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Request body is required");
                            return;
                        }

                        // Check if collection is registered
                        if (!scimService.IsCollectionRegistered(collectionName))
                        {
                            context.Response.StatusCode = 404;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Collection '{collectionName}' not found" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }

                        // Use the registered service to create the resource
                        try
                        {
                            // Use the SCIMv2 service to create the resource
                            var scimResponse = await scimService.CreateAsync(collectionName, body, CancellationToken.None);
                            
                            // Return the SCIMv2 response with proper formatting
                            context.Response.StatusCode = scimResponse.StatusCode;
                            context.Response.ContentType = "application/scim+json";
                            await context.Response.WriteAsync(Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(scimResponse));
                            return;
                        }
                        catch (Exception ex)
                        {
                            // If service integration fails, return 501
                            context.Response.StatusCode = 501;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Service integration not implemented: {ex.Message}" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            // PUT /{collectionName}/{id} - Replace resource
            app.MapWhen(context => context.Request.Path.StartsWithSegments(path) && 
                                   context.Request.Method == "PUT" && 
                                   context.Request.Path.Value.Length > path.Length + 1, 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        var scimService = context.RequestServices.GetService<Looplex.SCIMv2.ISCIMv2>();
                        if (scimService == null)
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("SCIMv2 service not configured");
                            return;
                        }

                        // Extract ID from path
                        var pathSegments = context.Request.Path.Value.Split('/');
                        var id = pathSegments.LastOrDefault();
                        
                        if (string.IsNullOrEmpty(id))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Invalid resource ID");
                            return;
                        }

                        // Read request body
                        using var reader = new StreamReader(context.Request.Body);
                        var body = await reader.ReadToEndAsync();
                        
                        if (string.IsNullOrEmpty(body))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Request body is required");
                            return;
                        }

                        // Check if collection is registered
                        if (!scimService.IsCollectionRegistered(collectionName))
                        {
                            context.Response.StatusCode = 404;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Collection '{collectionName}' not found" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }

                        // Use the registered service to replace the resource
                        try
                        {
                            var scimResponse = await scimService.ReplaceAsync(collectionName, id, body, CancellationToken.None);
                            
                            context.Response.StatusCode = scimResponse.StatusCode;
                            context.Response.ContentType = "application/scim+json";
                            await context.Response.WriteAsync(Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(scimResponse));
                            return;
                        }
                        catch (Exception ex)
                        {
                            context.Response.StatusCode = 501;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Service integration not implemented: {ex.Message}" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            // PATCH /{collectionName}/{id} - Modify resource
            app.MapWhen(context => context.Request.Path.StartsWithSegments(path) && 
                                   context.Request.Method == "PATCH" && 
                                   context.Request.Path.Value.Length > path.Length + 1, 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        var scimService = context.RequestServices.GetService<Looplex.SCIMv2.ISCIMv2>();
                        if (scimService == null)
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("SCIMv2 service not configured");
                            return;
                        }

                        // Extract ID from path
                        var pathSegments = context.Request.Path.Value.Split('/');
                        var id = pathSegments.LastOrDefault();
                        
                        if (string.IsNullOrEmpty(id))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Invalid resource ID");
                            return;
                        }

                        // Read request body
                        using var reader = new StreamReader(context.Request.Body);
                        var body = await reader.ReadToEndAsync();
                        
                        if (string.IsNullOrEmpty(body))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Request body is required");
                            return;
                        }

                        // Check if collection is registered
                        if (!scimService.IsCollectionRegistered(collectionName))
                        {
                            context.Response.StatusCode = 404;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Collection '{collectionName}' not found" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }

                        // Use the registered service to modify the resource
                        try
                        {
                            // Deserialize the JSON body to PatchOperation[]
                            Looplex.SCIMv2.Entities.PatchOperation[] patches;
                            
                            try
                            {
                                // Try to deserialize as direct array first
                                patches = JsonSerializer.Deserialize<Looplex.SCIMv2.Entities.PatchOperation[]>(body ?? string.Empty, FoundationJsonSerializer.DefaultOptions) ?? Array.Empty<Looplex.SCIMv2.Entities.PatchOperation>();
                            }
                            catch (JsonException)
                            {
                                // If direct array fails, try to deserialize as object with Operations property
                                try
                                {
                                    var wrapper = JsonSerializer.Deserialize<JsonElement>(body, FoundationJsonSerializer.DefaultOptions);
                                    
                                    if (wrapper.TryGetProperty("operations", out var operationsElement))
                                    {
                                        patches = JsonSerializer.Deserialize<Looplex.SCIMv2.Entities.PatchOperation[]>(operationsElement.GetRawText() ?? string.Empty, FoundationJsonSerializer.DefaultOptions) ?? Array.Empty<Looplex.SCIMv2.Entities.PatchOperation>();
                                    }
                                    else if (wrapper.TryGetProperty("Operations", out var operationsElement2))
                                    {
                                        patches = JsonSerializer.Deserialize<Looplex.SCIMv2.Entities.PatchOperation[]>(operationsElement2.GetRawText() ?? string.Empty, FoundationJsonSerializer.DefaultOptions) ?? Array.Empty<Looplex.SCIMv2.Entities.PatchOperation>();
                                    }
                                    else
                                    {
                                        patches = Array.Empty<Looplex.SCIMv2.Entities.PatchOperation>();
                                    }
                                }
                                catch
                                {
                                    patches = null;
                                }
                            }
                            
                            if (patches == null || patches.Length == 0)
                            {
                                context.Response.StatusCode = 400;
                                context.Response.ContentType = "application/scim+json";
                                var error = new { 
                                    error = "No patch operations provided",
                                    detail = $"Received body: {(body ?? string.Empty).Substring(0, Math.Min((body ?? string.Empty).Length, 500))}"
                                };
                                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                                return;
                            }
                            
                            // Validate each patch operation
                            foreach (var patch in patches)
                            {
                                if (!patch.IsValid())
                                {
                                    context.Response.StatusCode = 400;
                                    context.Response.ContentType = "application/scim+json";
                                    var error = new { error = $"Invalid patch operation: {patch.GetDescription()}" };
                                    await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                                    return;
                                }
                            }
                            
                            var scimResponse = await scimService.ModifyAsync(collectionName, id, patches, CancellationToken.None);
                            
                            context.Response.StatusCode = scimResponse.StatusCode;
                            context.Response.ContentType = "application/scim+json";
                            await context.Response.WriteAsync(Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(scimResponse));
                            return;
                        }
                        catch (Exception ex)
                        {
                            context.Response.StatusCode = 501;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Service integration not implemented: {ex.Message}" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            // DELETE /{collectionName}/{id} - Delete resource
            app.MapWhen(context => context.Request.Path.StartsWithSegments(path) && 
                                   context.Request.Method == "DELETE" && 
                                   context.Request.Path.Value.Length > path.Length + 1, 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        var scimService = context.RequestServices.GetService<Looplex.SCIMv2.ISCIMv2>();
                        if (scimService == null)
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("SCIMv2 service not configured");
                            return;
                        }

                        // Extract ID from path
                        var pathSegments = context.Request.Path.Value.Split('/');
                        var id = pathSegments.LastOrDefault();
                        
                        if (string.IsNullOrEmpty(id))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Invalid resource ID");
                            return;
                        }

                        // Check if collection is registered
                        if (!scimService.IsCollectionRegistered(collectionName))
                        {
                            context.Response.StatusCode = 404;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Collection '{collectionName}' not found" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }

                        // Use the registered service to delete the resource
                        try
                        {
                            var scimResponse = await scimService.DeleteAsync(collectionName, id, CancellationToken.None);
                            
                            context.Response.StatusCode = scimResponse.StatusCode;
                            context.Response.ContentType = "application/scim+json";
                            await context.Response.WriteAsync(Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(scimResponse));
                            return;
                        }
                        catch (Exception ex)
                        {
                            context.Response.StatusCode = 501;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Service integration not implemented: {ex.Message}" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            return app;
        }

        /// <summary>
        /// Maps SCIMv2 discovery endpoints (ServiceProviderConfig, ResourceTypes, Schemas)
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <param name="basePath">Optional base path for discovery endpoints</param>
        /// <returns>Application builder for chaining</returns>
        public static IApplicationBuilder MapSCIMv2DiscoveryEndpoints(
            this IApplicationBuilder app, 
            string? basePath = null)
        {
            var path = basePath ?? "";

            // GET /ServiceProviderConfig
            app.MapWhen(context => context.Request.Path.StartsWithSegments($"{path}/ServiceProviderConfig") && context.Request.Method == "GET", 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        // Try to get the registered ServiceProviderConfiguration from DI container
                        var serviceProviderConfig = context.RequestServices.GetService<ServiceProviderConfiguration>();
                        
                        // Fallback to default configuration if not registered
                        if (serviceProviderConfig == null)
                        {
                            serviceProviderConfig = new ServiceProviderConfiguration
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
                                    Supported = false,
                                    MaxOperations = 0,
                                    MaxPayloadSize = 0
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
                        }

                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "application/scim+json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(serviceProviderConfig));
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            // GET /ResourceTypes
            app.MapWhen(context => context.Request.Path.StartsWithSegments($"{path}/ResourceTypes") && context.Request.Method == "GET", 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        var scimService = context.RequestServices.GetService<Looplex.SCIMv2.ISCIMv2>();
                        if (scimService == null)
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("SCIMv2 service not configured");
                            return;
                        }

                        // Use the SCIMv2 service to get ResourceTypes
                        var scimResponse = await scimService.GetResourceTypesAsync(CancellationToken.None);
                        
                        context.Response.StatusCode = scimResponse.StatusCode;
                        context.Response.ContentType = "application/scim+json";
                        await context.Response.WriteAsync(Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(scimResponse));
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            // GET /Schemas - List all schemas
            app.MapWhen(context => context.Request.Path.StartsWithSegments($"{path}/Schemas") && 
                                   context.Request.Method == "GET" && 
                                   context.Request.Path.Value.Length <= $"{path}/Schemas".Length + 1, 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        var scimService = context.RequestServices.GetService<Looplex.SCIMv2.ISCIMv2>();
                        if (scimService == null)
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("SCIMv2 service not configured");
                            return;
                        }

                        // Use the SCIMv2 service to get all Schemas
                        var scimResponse = await scimService.GetSchemasAsync(CancellationToken.None);
                        
                        context.Response.StatusCode = scimResponse.StatusCode;
                        context.Response.ContentType = "application/scim+json";
                        await context.Response.WriteAsync(Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(scimResponse));
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            // GET /Schemas/{schemaId} - Get specific schema
            app.MapWhen(context => context.Request.Path.StartsWithSegments($"{path}/Schemas") && 
                                   context.Request.Method == "GET" && 
                                   context.Request.Path.Value.Length > $"{path}/Schemas".Length + 1, 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        var scimService = context.RequestServices.GetService<Looplex.SCIMv2.ISCIMv2>();
                        if (scimService == null)
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("SCIMv2 service not configured");
                            return;
                        }

                        // Extract schema ID from path
                        var pathSegments = context.Request.Path.Value.Split('/');
                        var schemaId = pathSegments.LastOrDefault();
                        
                        if (string.IsNullOrEmpty(schemaId))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Invalid schema ID");
                            return;
                        }

                        // Use the SCIMv2 service to get specific Schema
                        var scimResponse = await scimService.GetSchemaAsync(schemaId, CancellationToken.None);
                        
                        context.Response.StatusCode = scimResponse.StatusCode;
                        context.Response.ContentType = "application/scim+json";
                        await context.Response.WriteAsync(Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(scimResponse));
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            // POST /Bulk - Bulk operations
            app.MapWhen(context => context.Request.Path.StartsWithSegments($"{path}/Bulk") && context.Request.Method == "POST", 
                builder => builder.Run(async context =>
                {
                    try
                    {
                        var scimService = context.RequestServices.GetService<Looplex.SCIMv2.ISCIMv2>();
                        if (scimService == null)
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("SCIMv2 service not configured");
                            return;
                        }

                        // Read request body
                        using var reader = new StreamReader(context.Request.Body);
                        var body = await reader.ReadToEndAsync();
                        
                        if (string.IsNullOrEmpty(body))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Request body is required");
                            return;
                        }

                        // Use the SCIMv2 service to process bulk operations
                        try
                        {
                            var scimResponse = await scimService.BulkAsync(body, CancellationToken.None);
                            
                            context.Response.StatusCode = scimResponse.StatusCode;
                            context.Response.ContentType = "application/scim+json";
                            await context.Response.WriteAsync(Looplex.SCIMv2.SCIMv2.FormatResponseByHttpMethod(scimResponse));
                            return;
                        }
                        catch (Exception ex)
                        {
                            context.Response.StatusCode = 501;
                            context.Response.ContentType = "application/scim+json";
                            var error = new { error = $"Bulk operation failed: {ex.Message}" };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/scim+json";
                        var error = new { error = ex.Message };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }
                }));

            return app;
        }
    }
}