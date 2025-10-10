using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

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

namespace Looplex.Samples.WebAPI.Configuration;

/// <summary>
/// Extension methods for configuring services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    #region HTTP Client and Health Checks Configuration
    
    /// <summary>
    /// Configures HTTP clients with retry policies and health checks.
    /// </summary>
    public static IServiceCollection ConfigureHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient("Default")
            .AddPolicyHandler(GetRetryPolicy());

        services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

        services.AddHealthChecks()
            .AddCheck<HealthCheck>("Default");

        // Add HttpContextAccessor for SCIMv2 Location header generation
        services.AddHttpContextAccessor();
        
        return services;
    }
    
    #endregion

    #region Environment and Configuration Setup
    
    /// <summary>
    /// Configures environment variables and database connections.
    /// </summary>
    public static (IServiceCollection services, IConfiguration configuration) ConfigureEnvironment(this IServiceCollection services, IConfiguration configuration)
    {
        // Load environment variables from config.env file
        var envVars = Files.LoadEnv("config.env");
        foreach (var item in envVars)
        {
            Environment.SetEnvironmentVariable(item.Key, item.Value);
        }
        
        // Create a new configuration that includes environment variables
        var configBuilder = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddEnvironmentVariables();
        
        // Add the loaded environment variables directly to the configuration
        foreach (var item in envVars)
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { item.Key, item.Value }
            });
        }
        
        var newConfiguration = configBuilder.Build();
        
        // Replace the configuration with the new one that includes environment variables
        services.AddSingleton<IConfiguration>(newConfiguration);
        services.AddSingleton<IDbConnections, DbConnections>();
        
        // Simplified configuration for demonstration - without Azure Key Vault
        services.AddSingleton<ISecretsService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AzureSecretsService>>();
            return new AzureSecretsService(null!, Policy.NoOpAsync<string>(), logger);
        });
        services.AddSingleton<IDbConnections, DbConnections>();
        
        return (services, newConfiguration);
    }
    
    #endregion

    #region SCIMv2 Foundation Configuration
    
    /// <summary>
    /// Configures Foundation SCIMv2 with automatic schema discovery.
    /// </summary>
    public static IServiceCollection ConfigureSCIMv2(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Foundation SCIMv2 BEFORE creating instances
        Console.WriteLine("🔧 Configuring Foundation SCIMv2 for Notejam");
        
        Console.WriteLine("✅ Foundation SCIMv2 configured for Notejam");

        // Configure database settings for SQL Server database
        configuration["Database:UseProductionDatabase"] = "true";
        
        // Register ServiceNameProvider for custom schema URIs FIRST
        services.AddSingleton<IServiceNameProvider>(new ServiceNameProvider("notejam"));
        
        // Register SCIMv2 with automatic schema discovery - ZERO CONFIGURATION!
        services.AddSCIMv2WithResources(typeof(Note), typeof(Pad), typeof(User), typeof(Group));
        
        return services;
    }
    
    #endregion

    #region SCIMv2 Service Provider Configuration
    
    /// <summary>
    /// Configures SCIMv2 service provider capabilities and authentication schemes.
    /// </summary>
    public static IServiceCollection ConfigureSCIMv2ServiceProvider(this IServiceCollection services)
    {
        // Configure ServiceProviderConfiguration for Notejam with bulk operations enabled
        services.AddSingleton<ServiceProviderConfiguration>(sp =>
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
        
        return services;
    }
    
    #endregion

    #region Repository Architecture Dependencies
    
    /// <summary>
    /// Configures repository architecture dependencies and data access layers.
    /// </summary>
    public static IServiceCollection ConfigureRepositories(this IServiceCollection services)
    {
        // Register new architecture dependencies as SINGLETON to match repository lifetime
        services.AddSingleton<Looplex.Samples.Infra.Repositories.Base.IEntityMapping<Note>, Looplex.Samples.Infra.Repositories.Mappings.NoteEntityMapping>();
        services.AddSingleton<Looplex.Samples.Infra.Repositories.Base.IEntityMapping<Pad>, Looplex.Samples.Infra.Repositories.Mappings.PadEntityMapping>();
        services.AddSingleton<Looplex.Samples.Infra.Repositories.Base.IDataMapper<Note>, Looplex.Samples.Infra.Repositories.Mappers.NoteDataMapper>();
        services.AddSingleton<Looplex.Samples.Infra.Repositories.Base.IDataMapper<Pad>, Looplex.Samples.Infra.Repositories.Mappers.PadDataMapper>();
        services.AddSingleton<Looplex.Samples.Infra.Repositories.Base.IStoredProcedureExecutor, Looplex.Samples.Infra.Repositories.Base.StoredProcedureExecutor>();
        
        // Register concrete classes for direct injection as SINGLETON
        services.AddSingleton<Looplex.Samples.Infra.Repositories.Mappings.NoteEntityMapping>();
        services.AddSingleton<Looplex.Samples.Infra.Repositories.Mappings.PadEntityMapping>();
        services.AddSingleton<Looplex.Samples.Infra.Repositories.Mappers.NoteDataMapper>();
        services.AddSingleton<Looplex.Samples.Infra.Repositories.Mappers.PadDataMapper>();

        // Register Repository Pattern as SINGLETON to match SCIMv2 services
        // Using stored procedure repositories directly for elegant Foundation approach
        services.AddSingleton<INoteRepository, NoteRepositoryStoredProcedure>();
        services.AddSingleton<IPadRepository, PadRepositoryStoredProcedure>();
        
        // Register SCIMv2 Resource Repositories for stored procedure implementation
        services.AddSingleton<IResourceRepository<Note>, NoteRepositoryStoredProcedure>();
        services.AddSingleton<IResourceRepository<Pad>, PadRepositoryStoredProcedure>();
        
        // Register Mock repositories for User and Group (for demonstration)
        services.AddSingleton<IResourceRepository<User>, MockUserRepository>();
        services.AddSingleton<IResourceRepository<Group>, MockGroupRepository>();

        // Register new SCIMv2 Resource Services as SINGLETON 
        // Using independent services that don't depend on MediatR
        services.AddSingleton<IResourceService<Note>, SCIMv2NoteService>();
        services.AddSingleton<IResourceService<Pad>, SCIMv2PadService>();
        
        // Register User and Group services using Foundation's built-in services
        services.AddSingleton<IResourceService<User>, UserService>();
        services.AddSingleton<IResourceService<Group>, GroupService>();
        
        return services;
    }
    
    #endregion

    #region Authentication and Authorization Configuration
    
    /// <summary>
    /// Configures authentication and authorization for the application.
    /// </summary>
    public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Debug: Check if JWT configuration is loaded
        var jwtIssuer = configuration["JWT__Issuer"];
        var jwtAudience = configuration["JWT__Audience"];
        var jwtKey = configuration["JWT__Key"];
        
        Console.WriteLine($"🔍 JWT Debug - Issuer: {jwtIssuer ?? "NULL"}");
        Console.WriteLine($"🔍 JWT Debug - Audience: {jwtAudience ?? "NULL"}");
        Console.WriteLine($"🔍 JWT Debug - Key: {(string.IsNullOrEmpty(jwtKey) ? "NULL" : "LOADED")}");
        
        // Add OAuth2 authentication (simplified configuration)
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer ?? "https://localhost:7065",
                    ValidAudience = jwtAudience ?? "notejam-api",
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtKey ?? "your-256-bit-secret-key-for-notejam-development"))
                };
            });

        // Add authorization policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAuthenticatedUser", policy =>
                policy.RequireAuthenticatedUser());
        });
        
        return services;
    }
    
    #endregion

    #region HTTP Client Retry Policy Configuration
    
    /// <summary>
    /// Creates HTTP retry policy for transient error handling.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // Handle transient errors (5xx, 408, etc.)
            .WaitAndRetryAsync(3,
                retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))); // Retry 3 times with exponential backoff
    }
    
    #endregion
}
