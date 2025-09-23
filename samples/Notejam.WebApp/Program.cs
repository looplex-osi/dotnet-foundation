using System.Reflection;

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


using Newtonsoft.Json;

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

    // Load environment variables from config.env file
    var envVars = Files.LoadEnv("config.env");
    foreach (var item in envVars)
    {
      Environment.SetEnvironmentVariable(item.Key, item.Value);
    }

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
    // Register SCIMv2 core services as SINGLETON to ensure same instance
    builder.Services.AddSingleton<ISCIMv2, Looplex.Foundation.SCIMv2.SCIMv2>();
    builder.Services.AddSingleton<ISCIMv2Validation, Looplex.Foundation.SCIMv2.SCIMv2>();

    // TODO: SearchContent was removed - implement basic filter processing
    // builder.Services.AddScoped<Looplex.Foundation.SearchContent.ISearchContentService, Looplex.Foundation.SearchContent.SearchContentService>();
    
    // Register Repository Pattern as SINGLETON to match SCIMv2 services
    builder.Services.AddSingleton<INoteRepository, Looplex.Samples.Infra.Repositories.NoteRepository>();
    builder.Services.AddSingleton<IPadRepository, Looplex.Samples.Infra.Repositories.PadRepository>();

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
          string result = JsonConvert.SerializeObject(new
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
          });
          await context.Response.WriteAsync(result);
        }
      })
      .AllowAnonymous();

    // Add SCIMv2 discovery endpoints
    app.MapGet("/Schemas", () => Results.Json(new { schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" } }));
    app.MapGet("/ServiceProviderConfig", () => Results.Json(new { 
        schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig" },
        patch = new { supported = false },
        bulk = new { supported = false, maxOperations = 0, maxPayloadSize = 0 },
        filter = new { supported = true, maxResults = 200 },
        changePassword = new { supported = false },
        sort = new { supported = false },
        etag = new { supported = false },
        authenticationSchemes = new[] { 
            new { type = "oauth2", name = "OAuth 2.0", description = "OAuth 2.0 Bearer Token" }
        }
    }));

    // Register SCIMv2 services directly
    using (var scope = app.Services.CreateScope())
    {
        var scimService = scope.ServiceProvider.GetRequiredService<ISCIMv2>();
        var noteService = scope.ServiceProvider.GetRequiredService<Looplex.Foundation.SCIMv2.Modules.IResourceService<Note>>();
        var padService = scope.ServiceProvider.GetRequiredService<Looplex.Foundation.SCIMv2.Modules.IResourceService<Pad>>();
        
        scimService.Register<Note>(noteService, "notes");
        scimService.Register<Pad>(padService, "pads");
    }

    // Use SCIMv2 middleware for automatic endpoint mapping (PRODUCTION READY)
    app.UseSCIMv2("notes", authorize: false);
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