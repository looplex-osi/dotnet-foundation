using System;
using Looplex.Foundation.Core.Ports;
using Looplex.Foundation.Core.SCIMv2;
using Looplex.Foundation.Core.SCIMv2.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.Core.SCIMv2.Extensions;

/// <summary>
/// Extension methods for dependency injection configuration
/// Provides easy registration of SCIMv2 services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SCIMv2 service to the dependency injection container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSCIMv2Service(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton<ISCIMv2, SCIMv2>();
        services.AddSingleton<IJsonSchemaProvider>(sp => sp.GetRequiredService<SCIMv2>());
        return services;
    }

    /// <summary>
    /// Adds SCIMv2 service with custom configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSCIMv2Service(this IServiceCollection services, 
        Action<SCIMv2ServiceOptions> configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        services.AddSingleton<ISCIMv2, SCIMv2>();
        services.Configure(configure);
        return services;
    }

    /// <summary>
    /// Registers a resource service for a specific collection
    /// </summary>
    /// <typeparam name="T">Resource type implementing IResource</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="collectionName">Collection name</param>
    /// <param name="serviceFactory">Factory for creating the service</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection RegisterResourceService<T>(
        this IServiceCollection services, 
        string collectionName, 
        Func<IServiceProvider, IResourceService<T>> serviceFactory) 
        where T : Looplex.Foundation.Core.SCIMv2.Entities.IResource
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrEmpty(collectionName))
            throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

        if (serviceFactory == null)
            throw new ArgumentNullException(nameof(serviceFactory));

        services.AddScoped(serviceFactory);
        
        // Register the service with the SCIMv2 service
        services.AddScoped<ISCIMv2ServiceRegistration>(provider =>
        {
            var scimService = provider.GetRequiredService<ISCIMv2>();
            var resourceService = provider.GetRequiredService<IResourceService<T>>();
            return new SCIMv2ServiceRegistration(scimService, resourceService, collectionName);
        });

        return services;
    }
}

/// <summary>
/// Configuration options for SCIMv2 service
/// </summary>
public class SCIMv2ServiceOptions
{
    /// <summary>
    /// Default page size for queries
    /// </summary>
    public int DefaultPageSize { get; set; } = 100;

    /// <summary>
    /// Maximum page size for queries
    /// </summary>
    public int MaxPageSize { get; set; } = 1000;

    /// <summary>
    /// Enable detailed error responses
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;
}

/// <summary>
/// Service registration helper for SCIMv2
/// </summary>
public interface ISCIMv2ServiceRegistration
{
    void Register();
}

/// <summary>
/// Implementation of SCIMv2 service registration
/// </summary>
public class SCIMv2ServiceRegistration : ISCIMv2ServiceRegistration
{
    private readonly ISCIMv2 _scimService;
    private readonly object _resourceService;
    private readonly string _collectionName;

    public SCIMv2ServiceRegistration(ISCIMv2 scimService, object resourceService, string collectionName)
    {
        _scimService = scimService ?? throw new ArgumentNullException(nameof(scimService));
        _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
    }

    public void Register()
    {
        // Use reflection to call the generic Register method
        var method = _scimService.GetType().GetMethod("Register");
        if (method != null)
        {
            var genericMethod = method.MakeGenericMethod(_resourceService.GetType().GetGenericArguments()[0]);
            genericMethod.Invoke(_scimService, new[] { _resourceService, _collectionName });
        }
    }
}
