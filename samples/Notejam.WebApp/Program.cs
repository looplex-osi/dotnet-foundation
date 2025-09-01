using System.Reflection;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Looplex.Foundation.Adapters;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.Middlewares;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Loader;
using Looplex.Samples.Application;
using Looplex.Samples.Application.Services;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Infra;
using Looplex.Samples.Infra.CommandHandlers;
using Looplex.Samples.Infra.QueryHandlers;
using Looplex.Samples.Infra.Repositories;
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

        // Load configuration from config.env file
        var configEnvSettings = new Dictionary<string, string?>();
        foreach (var item in Files.LoadEnv("config.env"))
        {
            Environment.SetEnvironmentVariable(item.Key, item.Value);
            configEnvSettings[item.Key] = item.Value;
        }

        // Load sensitive configuration from config.env file
        // Public configuration can be set here, but sensitive data should be in config.env
        Dictionary<string, string?> inMemorySettings = new()
        {
            { "UseInMemoryDatabase", "false" },
            { "InMemoryConnectionString", "Data Source=:memory:" },
            { "UseRealDatabase", "true" }
        };
        
        // Add config.env settings to configuration
        builder.Configuration.AddInMemoryCollection(configEnvSettings);
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
        builder.Services.AddSCIMv2();

        // Register SearchContentService for SCIM filter processing
        builder.Services.AddScoped<Looplex.Foundation.SearchContent.ISearchContentService, Looplex.Foundation.SearchContent.SearchContentService>();
        
        // Register Repository Pattern
        builder.Services.AddScoped<INoteRepository, Looplex.Samples.Infra.Repositories.NoteRepository>();
        builder.Services.AddScoped<IPadRepository, Looplex.Samples.Infra.Repositories.PadRepository>();

        builder.Services.AddScoped<Notes>(sp =>
        {
            PluginLoader loader = new();
            IEnumerable<string> dlls = Directory.Exists("plugins")
                ? Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"))
                : [];
            IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var mediator = sp.GetRequiredService<IMediator>();
            return new Notes(plugins, null, httpContextAccessor, mediator);
        });

        builder.Services.AddScoped<Pads>(sp =>
        {
            PluginLoader loader = new();
            IEnumerable<string> dlls = Directory.Exists("plugins")
                ? Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"))
                : [];
            IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var mediator = sp.GetRequiredService<IMediator>();
            return new Pads(plugins, null, httpContextAccessor, mediator);
        });

        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Looplex.Samples.Infra.CommandHandlers.UpdateNoteCommandHandler).Assembly));

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

        app.UseSCIMv2();

        // Map the Notes service to the /notes endpoint
        app.UseSCIMv2<Note, Note, Notes>("/notes", authorize: false);
        
        // Map the Pads service to the /pads endpoint
        app.UseSCIMv2<Pad, Pad, Pads>("/pads", authorize: false);

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
