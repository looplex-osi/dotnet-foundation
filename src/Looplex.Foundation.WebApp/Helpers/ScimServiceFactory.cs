using System;
using System.Collections.Generic;

using Looplex.Foundation.WebApp.Helpers;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.OpenForExtension.Abstractions.Plugins;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.WebApp.Helpers;

/// <summary>
/// Factory for creating SCIM services with shared plugins.
/// 
/// This factory eliminates the need for multiple PluginLoader instances
/// and ensures that all services use the same plugin collection,
/// improving performance and memory usage.
/// 
/// Benefits:
/// - Shared plugin instances across all services
/// - Reduced memory footprint
/// - Improved startup performance
/// - Consistent plugin state across services
/// </summary>
public static class ScimServiceFactory
{
    /// <summary>
    /// Creates a Users service with shared plugins
    /// </summary>
    public static Users CreateUsers(IServiceProvider serviceProvider)
    {
        var plugins = PluginManager.Instance.Plugins;
        var rbacService = serviceProvider.GetRequiredService<IRbacService>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        return new Users(plugins, rbacService, httpContextAccessor, mediator);
    }

    /// <summary>
    /// Creates a Groups service with shared plugins
    /// </summary>
    public static Groups CreateGroups(IServiceProvider serviceProvider)
    {
        var plugins = PluginManager.Instance.Plugins;
        var rbacService = serviceProvider.GetRequiredService<IRbacService>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        return new Groups(plugins, rbacService, httpContextAccessor, mediator);
    }

    /// <summary>
    /// Creates a Bulks service with shared plugins
    /// </summary>
    public static Bulks CreateBulks(IServiceProvider serviceProvider)
    {
        var plugins = PluginManager.Instance.Plugins;
        var serviceProviderConfiguration = serviceProvider.GetRequiredService<ServiceProviderConfiguration>();

        return new Bulks(plugins, serviceProvider, serviceProviderConfiguration);
    }

    /// <summary>
    /// Creates a ClientServices service with shared plugins
    /// </summary>
    public static ClientServices CreateClientServices(IServiceProvider serviceProvider)
    {
        var plugins = PluginManager.Instance.Plugins;
        var rbacService = serviceProvider.GetRequiredService<IRbacService>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        return new ClientServices(plugins, rbacService, httpContextAccessor, mediator, configuration);
    }

    /// <summary>
    /// Creates a ClientCredentialsAuthentications service with shared plugins
    /// </summary>
    public static ClientCredentialsAuthentications CreateClientCredentialsAuthentications(IServiceProvider serviceProvider)
    {
        var plugins = PluginManager.Instance.Plugins;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var clientCredentials = serviceProvider.GetRequiredService<ClientServices>();
        var jwtService = serviceProvider.GetRequiredService<IJwtService>();

        return new ClientCredentialsAuthentications(plugins, configuration, clientCredentials, jwtService);
    }

    /// <summary>
    /// Creates a TokenExchangeAuthentications service with shared plugins
    /// </summary>
    public static TokenExchangeAuthentications CreateTokenExchangeAuthentications(IServiceProvider serviceProvider)
    {
        var plugins = PluginManager.Instance.Plugins;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var jwtService = serviceProvider.GetRequiredService<IJwtService>();
        var httpClient = serviceProvider.GetRequiredService<HttpClient>();

        return new TokenExchangeAuthentications(plugins, configuration, jwtService, httpClient);
    }

    /// <summary>
    /// Generic method to create any SCIM service with shared plugins.
    /// This method provides a flexible way to create services while ensuring
    /// plugin sharing across all instances.
    /// </summary>
    /// <typeparam name="T">The type of service to create</typeparam>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    /// <param name="factoryMethod">Factory method to create the specific service</param>
    /// <returns>The created service instance</returns>
    public static T CreateService<T>(IServiceProvider serviceProvider, Func<IList<IPlugin>, IServiceProvider, T> factoryMethod)
        where T : class
    {
        var plugins = PluginManager.Instance.Plugins;
        return factoryMethod(plugins, serviceProvider);
    }
}
