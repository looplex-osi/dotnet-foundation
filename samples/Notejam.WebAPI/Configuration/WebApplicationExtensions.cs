using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Text.Json;

using Looplex.SCIMv2;
using Looplex.SCIMv2.Ports;
using Looplex.SCIMv2.Extensions;
using Looplex.SCIMv2.Entities;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.WebAPI.Services;
using Looplex.Samples.WebAPI.Repositories;

namespace Looplex.Samples.WebAPI.Configuration;

/// <summary>
/// Extension methods for configuring the WebApplication pipeline and endpoints.
/// </summary>
public static class WebApplicationExtensions
{
    #region Application Building and Middleware Configuration
    
    /// <summary>
    /// Configures the application middleware pipeline including authentication and SCIMv2 middleware.
    /// </summary>
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        // Add authentication and authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Add SCIMv2 authentication middleware for all SCIMv2 endpoints
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value?.ToLower();

            // Allow discovery endpoints without authentication
            if (path == "/serviceproviderconfig" || path == "/resourcetypes" || path == "/schemas")
            {
                await next();
                return;
            }

            // Check authentication and return SCIMv2 error format if not authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                var result = CreateSCIMv2AuthError(context);
                await result.ExecuteAsync(context);
                return;
            }
            
            await next();
        });
        
        return app;
    }
    
    #endregion

    #region Health Checks Configuration
    
    /// <summary>
    /// Configures health check endpoints with custom response formatting.
    /// </summary>
    public static WebApplication ConfigureHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                
                // Use global JSON configuration
                var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonSerializerOptions>>().Value;
                string result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    results = report.Entries.Select(e => new
                    {
                        key = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        data = e.Value.Data,
                        exception = e.Value.Exception?.Message // Include exception details
                    })
                }, jsonOptions);
                await context.Response.WriteAsync(result);
            }
        })
        .AllowAnonymous();
        
        return app;
    }
    
    #endregion

    #region SCIMv2 Service Registration and Endpoint Mapping
    
    /// <summary>
    /// Configures SCIMv2 services registration and endpoint mapping.
    /// </summary>
    public static WebApplication ConfigureSCIMv2Endpoints(this WebApplication app)
    {
        // Register SCIMv2 services - schemas are auto-discovered and registered!
        using (var scope = app.Services.CreateScope())
        {
            var scimService = scope.ServiceProvider.GetRequiredService<ISCIMv2>();
            var noteService = scope.ServiceProvider.GetRequiredService<IResourceService<Note>>();
            var padService = scope.ServiceProvider.GetRequiredService<IResourceService<Pad>>();
            var userService = scope.ServiceProvider.GetRequiredService<IResourceService<User>>();
            var groupService = scope.ServiceProvider.GetRequiredService<IResourceService<Group>>();
            
            // Register services - schemas are already auto-discovered and registered!
            scimService.Register<Note>(noteService, "notes");
            scimService.Register<Pad>(padService, "pads");
            scimService.Register<User>(userService, "Users");
            scimService.Register<Group>(groupService, "Groups");
            
            Console.WriteLine("✅ SCIMv2 services registered with auto-discovered schemas");
        }

        // Note: Using auto-discovery of attributes from entity properties
        // No manual configuration needed - SCIMv2 will auto-discover all properties
        Console.WriteLine("✅ SCIMv2 using auto-discovery of attributes");

        // Add detailed logging middleware
        app.Use(async (context, next) =>
        {
            Console.WriteLine($"🔍 REQUEST: {context.Request.Method} {context.Request.Path}");
            
            await next();
            
            Console.WriteLine($"🔍 RESPONSE: {context.Response.StatusCode}");
        });

        // Map SCIMv2 discovery endpoints using Foundation's native extensions
        Looplex.SCIMv2.Extensions.EndpointExtensions.MapSCIMv2DiscoveryEndpoints(app, "");

        // Map SCIMv2 resource endpoints using Foundation's native extensions
        Looplex.SCIMv2.Extensions.EndpointExtensions.MapSCIMv2ResourceEndpoints(app, "notes");
        Looplex.SCIMv2.Extensions.EndpointExtensions.MapSCIMv2ResourceEndpoints(app, "pads");

        Console.WriteLine("🚀 SCIMv2 endpoints mapped successfully");
        
        return app;
    }
    
    #endregion

    #region SCIMv2 Authentication Helpers
    
    /// <summary>
    /// Creates a SCIMv2 error response for authentication failures
    /// </summary>
    private static IResult CreateSCIMv2AuthError(HttpContext context)
    {
        var scimError = new Looplex.SCIMv2.Entities.SCIMv2Error
        {
            Status = "401",
            Detail = "Authentication required",
            ScimType = "invalidCredentials",
            Timestamp = DateTime.UtcNow.ToString("O")
        };
        
        var scimResponse = new Looplex.SCIMv2.Entities.SCIMv2Response
        {
            StatusCode = 401,
            Error = scimError
        };
        
        context.Response.ContentType = "application/scim+json";
        return Results.Json(scimResponse, statusCode: 401);
    }
    
    #endregion
}
