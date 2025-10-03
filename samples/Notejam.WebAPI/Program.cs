
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Text.Json;
using Microsoft.Extensions.Options;


using Looplex.Foundation.Core.Adapters;
using Looplex.Foundation.Core.Helpers;
using Looplex.Foundation.Core.Ports;
using Looplex.Foundation.Core.SCIMv2;
using Looplex.Foundation.Core.SCIMv2.Modules;
using Looplex.Foundation.Core.SCIMv2.Extensions;
using Looplex.Foundation.Core.SCIMv2.Entities;
using Looplex.Foundation.Core.Serialization;

using Looplex.Foundation.Protocols.Http.Middlewares;

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
    
    // Configure Note attributes and mappings
    Looplex.Foundation.Core.SCIMv2.SCIMv2.ConfigureAttributes("Note", new HashSet<string> { 
        "id", "externalId", "text", "active", "status", "customFields",
        "meta.created", "meta.lastModified" 
    });
    
    Looplex.Foundation.Core.SCIMv2.SCIMv2.ConfigureMapping("Note", new Dictionary<string, string> {
        { "meta.created", "n.created_at" },
        { "meta.lastModified", "n.updated_at" },
        { "active", "n.active" },
        { "text", "n.markdown" },
        { "customFields", "n.custom_fields" },
        { "externalId", "n.external_id" },
        { "status", "n.status" }
    });
    
    // Configure Pad attributes and mappings
    Looplex.Foundation.Core.SCIMv2.SCIMv2.ConfigureAttributes("Pad", new HashSet<string> { 
        "id", "externalId", "name", "active", 
        "meta.created", "meta.lastModified" 
    });
    
    Looplex.Foundation.Core.SCIMv2.SCIMv2.ConfigureMapping("Pad", new Dictionary<string, string> {
        { "meta.created", "p.created_at" },
        { "meta.lastModified", "p.updated_at" },
        { "active", "p.active" },
        { "name", "p.name" },
        { "externalId", "p.external_id" }
    });
    
    Console.WriteLine("✅ Foundation SCIMv2 configured for Notejam");

    // Configure in-memory settings for SQL Server database
    var inMemorySettings = new Dictionary<string, string?>
    {
        { "Database:UseProductionDatabase", "true" }
    };
    builder.Configuration.AddInMemoryCollection(inMemorySettings);
    
    // Register ServiceNameProvider for custom schema URIs
    builder.Services.AddSingleton<IServiceNameProvider>(new ServiceNameProvider("notejam"));
    
    // Register SCIMv2 with automatic schema discovery - ZERO CONFIGURATION!
    builder.Services.AddSCIMv2WithResources(typeof(Note), typeof(Pad), typeof(User), typeof(Group));

    
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

    // Register new SCIMv2 Resource Services as SINGLETON (NEW ARCHITECTURE)
    // Using independent services that don't depend on MediatR
    builder.Services.AddSingleton<IResourceService<Note>, SCIMv2NoteService>();
    builder.Services.AddSingleton<IResourceService<Pad>, SCIMv2PadService>();
    
    // Register User and Group services using Foundation's built-in services
    builder.Services.AddSingleton<IResourceService<User>, UserService>();
    builder.Services.AddSingleton<IResourceService<Group>, GroupService>();
    

    WebApplication app = builder.Build();

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

    // Use official SCIMv2 discovery endpoints from Looplex.Foundation
    app.UseSCIMv2Discovery(authorize: false);

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

    // Add detailed logging middleware
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"🔍 REQUEST: {context.Request.Method} {context.Request.Path}");
        Console.WriteLine($"🔍 Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"))}");
        
        if (context.Request.Path.StartsWithSegments("/notes") && context.Request.Method == "PUT")
        {
            Console.WriteLine($"🔍 PUT /notes DETECTED - Rastreando trilha...");
        }
        
        await next();
        
        Console.WriteLine($"🔍 RESPONSE: {context.Response.StatusCode}");
        Console.WriteLine($"🔍 Response Headers: {string.Join(", ", context.Response.Headers.Select(h => $"{h.Key}={h.Value}"))}");
    });

    // Use SCIMv2 middleware for proper compliance
    Console.WriteLine("🚀 Registering SCIMv2 middleware for 'notes'");
    app.UseSCIMv2("notes", authorize: false);
    
    Console.WriteLine("🚀 Registering SCIMv2 middleware for 'pads'");
    app.UseSCIMv2("pads", authorize: false);
    
    Console.WriteLine("🚀 Registering SCIMv2 middleware for 'Users'");
    app.UseSCIMv2("Users", authorize: false);
    
    Console.WriteLine("🚀 Registering SCIMv2 middleware for 'Groups'");
    app.UseSCIMv2("Groups", authorize: false);

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
