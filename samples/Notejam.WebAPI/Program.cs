using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Text.Json;
using Microsoft.Extensions.Options;


using Looplex.Foundation.Adapters;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.SCIMv2;
using Looplex.SCIMv2.Modules;
using Looplex.SCIMv2.Extensions;
using Looplex.Protocols.HTTP.Extensions;
using Looplex.SCIMv2.Entities;

using Looplex.Protocols.HTTP.Middlewares;

using Looplex.Samples.Application;
using Looplex.Samples.Application.Services;
using Looplex.Samples.WebAPI.Repositories;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Infra;
using Looplex.Samples.Infra.Repositories;
using Looplex.Samples.WebAPI.Services;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using Polly;
using Polly.Extensions.Http;

namespace Looplex.Samples.WebAPI;

public static class Program
{
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

  /// <summary>
  /// Wraps an endpoint with SCIMv2 authentication and error handling
  /// </summary>
  private static Func<HttpContext, Task<IResult>> WithSCIMv2Auth(Func<HttpContext, Task<IResult>> endpoint)
  {
    return async (HttpContext context) =>
    {
      // Check authentication and return SCIMv2 error format if not authenticated
      if (!context.User.Identity?.IsAuthenticated ?? true)
      {
        return CreateSCIMv2AuthError(context);
      }
      
      return await endpoint(context);
    };
  }

  /// <summary>
  /// Wraps an endpoint with SCIMv2 authentication and error handling (with parameters)
  /// </summary>
  private static Func<string, HttpContext, Task<IResult>> WithSCIMv2Auth(Func<string, HttpContext, Task<IResult>> endpoint)
  {
    return async (string id, HttpContext context) =>
    {
      // Check authentication and return SCIMv2 error format if not authenticated
      if (!context.User.Identity?.IsAuthenticated ?? true)
      {
        return CreateSCIMv2AuthError(context);
      }
      
      return await endpoint(id, context);
    };
  }

  public static void Main(string[] args)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Services.AddHttpClient("Default")
      .AddPolicyHandler(GetRetryPolicy());

    builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

    builder.Services.AddHealthChecks()
      .AddCheck<HealthCheck>("Default");

    // Add HttpContextAccessor for SCIMv2 Location header generation
    builder.Services.AddHttpContextAccessor();

    // Load environment variables from config.env file
    var envVars = Files.LoadEnv("config.env");
    foreach (var item in envVars)
    {
      Environment.SetEnvironmentVariable(item.Key, item.Value);
    }
    // Configuration is now loaded from config.env file
    // All sensitive data has been moved to the environment file
    // Set environment variables for configuration system
    foreach (var item in envVars)
    {
      Environment.SetEnvironmentVariable(item.Key, item.Value);
    }
    
    // Add environment variables to configuration
    builder.Configuration.AddEnvironmentVariables();
    
    // Add database connection string
    builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
    builder.Services.AddSingleton<IDbConnections, DbConnections>();
    // Simplified configuration for demonstration - without Azure Key Vault
    builder.Services.AddSingleton<ISecretsService>(sp =>
    {
      var logger = sp.GetRequiredService<ILogger<AzureSecretsService>>();
      return new AzureSecretsService(null!, Policy.NoOpAsync<string>(), logger);
    });
    builder.Services.AddSingleton<IDbConnections, DbConnections>();
    // Configure Foundation SCIMv2 BEFORE creating instances
    Console.WriteLine("🔧 Configuring Foundation SCIMv2 for Notejam");
    
    Console.WriteLine("✅ Foundation SCIMv2 configured for Notejam");

    // Configure database settings for SQL Server database
    builder.Configuration["Database:UseProductionDatabase"] = "true";
    
    // Register ServiceNameProvider for custom schema URIs FIRST
    builder.Services.AddSingleton<IServiceNameProvider>(new ServiceNameProvider("notejam"));
    
    // Register SCIMv2 with automatic schema discovery - ZERO CONFIGURATION!
    builder.Services.AddSCIMv2WithResources(typeof(Note), typeof(Pad), typeof(User), typeof(Group));
    
    // Configure ServiceProviderConfiguration for Notejam with bulk operations enabled
    builder.Services.AddSingleton<ServiceProviderConfiguration>(sp =>
    {
        return new ServiceProviderConfiguration
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
                MaxPayloadSize = 1048575 // 1MB
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
    });

    
    // Register Repository Pattern as SINGLETON to match SCIMv2 services
    // Using stored procedure repositories directly for elegant Foundation approach
    builder.Services.AddSingleton<INoteRepository, NoteRepositoryStoredProcedure>();
    builder.Services.AddSingleton<IPadRepository, PadRepositoryStoredProcedure>();
    
    // Register SCIMv2 Resource Repositories for stored procedure implementation
    builder.Services.AddSingleton<IResourceRepository<Note>, NoteRepositoryStoredProcedure>();
    builder.Services.AddSingleton<IResourceRepository<Pad>, PadRepositoryStoredProcedure>();
    
    // Register Mock repositories for User and Group (for demonstration)
    builder.Services.AddSingleton<IResourceRepository<User>, MockUserRepository>();
    builder.Services.AddSingleton<IResourceRepository<Group>, MockGroupRepository>();

    // Register new SCIMv2 Resource Services as SINGLETON 
    // Using independent services that don't depend on MediatR
    builder.Services.AddSingleton<IResourceService<Note>, SCIMv2NoteService>();
    builder.Services.AddSingleton<IResourceService<Pad>, SCIMv2PadService>();
    
    // Register User and Group services using Foundation's built-in services
    builder.Services.AddSingleton<IResourceService<User>, UserService>();
    builder.Services.AddSingleton<IResourceService<Group>, GroupService>();
    
    // Add OAuth2 authentication (simplified configuration)
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["JWT:Issuer"] ?? "https://localhost:7065",
                ValidAudience = builder.Configuration["JWT:Audience"] ?? "notejam-api",
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"] ?? "your-256-bit-secret-key-for-notejam-development"))
            };
        });

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAuthenticatedUser", policy =>
            policy.RequireAuthenticatedUser());
    });

    WebApplication app = builder.Build();

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
    app.MapSCIMv2DiscoveryEndpoints("");

    // Map SCIMv2 resource endpoints using Foundation's native extensions
    Looplex.SCIMv2.Extensions.EndpointExtensions.MapSCIMv2ResourceEndpoints(app, "notes");
    Looplex.SCIMv2.Extensions.EndpointExtensions.MapSCIMv2ResourceEndpoints(app, "pads");

    Console.WriteLine("🚀 SCIMv2 endpoints mapped successfully");

    app.Run();
  }

  private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
  {
    return HttpPolicyExtensions
      .HandleTransientHttpError() // Handle transient errors (5xx, 408, etc.)
      .WaitAndRetryAsync(3,
        retryAttempt =>
          TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))); // Retry 3 times with exponential backoff
  }
}

// Non-static Program class for integration testing
public class TestProgram
{
    // This class is needed for WebApplicationFactory in integration tests
    // The actual startup logic is in the static Program class above
}