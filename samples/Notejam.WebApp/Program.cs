
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;



using Looplex.Foundation.Adapters;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2;
using Looplex.Foundation.SCIMv2.Modules;
using Looplex.Foundation.SCIMv2.Extensions;
using Looplex.Foundation.Protocols.Http.Middlewares;
using Looplex.Samples.Application;
using Looplex.Samples.Application.Services;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Infra;
// CommandHandlers and QueryHandlers removed - using SCIMv2 directly
using Looplex.Samples.Infra.Repositories;
using Looplex.Samples.WebApp.Services;

using MediatR;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;


using Looplex.Foundation.Serialization;

using Polly;
using Polly.Extensions.Http;

namespace Looplex.Samples.WebApp;

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
    /*
    // Configure global JSON serialization options
    builder.Services.Configure<System.Text.Json.JsonSerializerOptions>(options =>
    {
        options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.WriteIndented = true;
    });
    */
    // Load environment variables from config.env file
    var envVars = Files.LoadEnv("config.env");
    foreach (var item in envVars)
    {
      Environment.SetEnvironmentVariable(item.Key, item.Value);
    }


    // TODO EXCLUIR!!!

    // Configuration is now loaded from config.env file
    // All sensitive data has been moved to the environment file
    Dictionary<string, string?> inMemorySettings = new()
    {
      { "UseInMemoryDatabase", "false" },
      { "InMemoryConnectionString", "Data Source=:memory:" }
    };
    
    // Add environment variables to configuration
    foreach (var item in envVars)
    {
      inMemorySettings[item.Key] = item.Value;
    }
    
    builder.Configuration.AddInMemoryCollection(inMemorySettings);
    
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
    Looplex.Foundation.SCIMv2.SCIMv2.ConfigureAttributes("Note", new HashSet<string> { 
        "id", "externalId", "text", "active", "status", "customFields",
        "meta.created", "meta.lastModified" 
    });
    
    Looplex.Foundation.SCIMv2.SCIMv2.ConfigureMapping("Note", new Dictionary<string, string> {
        { "meta.created", "n.created_at" },
        { "meta.lastModified", "n.updated_at" },
        { "active", "n.active" },
        { "text", "n.markdown" },
        { "customFields", "n.custom_fields" },
        { "externalId", "n.external_id" },
        { "status", "n.status" }
    });
    
    // Configure Pad attributes and mappings
    Looplex.Foundation.SCIMv2.SCIMv2.ConfigureAttributes("Pad", new HashSet<string> { 
        "id", "externalId", "name", "active", 
        "meta.created", "meta.lastModified" 
    });
    
    Looplex.Foundation.SCIMv2.SCIMv2.ConfigureMapping("Pad", new Dictionary<string, string> {
        { "meta.created", "p.created_at" },
        { "meta.lastModified", "p.updated_at" },
        { "active", "p.active" },
        { "name", "p.name" },
        { "externalId", "p.external_id" }
    });
    
    Console.WriteLine("✅ Foundation SCIMv2 configured for Notejam");

    // Register SCIMv2 with automatic schema discovery - ZERO CONFIGURATION!
    builder.Services.AddSCIMv2WithResources(typeof(Note), typeof(Pad));

    
    // Register Repository Pattern as SINGLETON to match SCIMv2 services
    // Using stored procedure repositories directly for elegant Foundation approach
    builder.Services.AddSingleton<INoteRepository, Looplex.Samples.Infra.Repositories.NoteRepositoryStoredProcedure>();
    builder.Services.AddSingleton<IPadRepository, Looplex.Samples.Infra.Repositories.PadRepositoryStoredProcedure>();
    
    // Register SCIMv2 Resource Repositories for stored procedure implementation
    builder.Services.AddSingleton<Looplex.Foundation.SCIMv2.Modules.IResourceRepository<Note>, Looplex.Samples.Infra.Repositories.NoteRepositoryStoredProcedure>();
    builder.Services.AddSingleton<Looplex.Foundation.SCIMv2.Modules.IResourceRepository<Pad>, Looplex.Samples.Infra.Repositories.PadRepositoryStoredProcedure>();

    // Register new SCIMv2 Resource Services as SINGLETON (NEW ARCHITECTURE)
    // Using independent services that don't depend on MediatR
    builder.Services.AddSingleton<Looplex.Foundation.SCIMv2.Modules.IResourceService<Note>, SCIMv2NoteService>();
    builder.Services.AddSingleton<Looplex.Foundation.SCIMv2.Modules.IResourceService<Pad>, SCIMv2PadService>();
    

    // MediatR registration removed - using SCIMv2 directly
    // builder.Services.AddMediatR(cfg =>
    //   cfg.RegisterServicesFromAssembly(typeof(Looplex.Samples.Infra.CommandHandlers.UpdateNoteCommandHandler).Assembly));



    WebApplication app = builder.Build();


    app.MapHealthChecks("/health", new HealthCheckOptions
      {
        ResponseWriter = async (context, report) =>
        {
          context.Response.ContentType = "application/json; charset=utf-8";
          
          // Use global JSON configuration
          var jsonOptions = context.RequestServices.GetRequiredService<IOptions<System.Text.Json.JsonSerializerOptions>>().Value;
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
        var noteService = scope.ServiceProvider.GetRequiredService<Looplex.Foundation.SCIMv2.Modules.IResourceService<Note>>();
        var padService = scope.ServiceProvider.GetRequiredService<Looplex.Foundation.SCIMv2.Modules.IResourceService<Pad>>();
        
        // Register services - schemas are already auto-discovered and registered!
        scimService.Register<Note>(noteService, "notes");
        scimService.Register<Pad>(padService, "pads");
        
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