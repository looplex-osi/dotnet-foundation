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
    /// Creates a Users service with per-scope plugin instances.
    /// This prevents cross-tenant contamination in multi-tenant environments.
    /// </summary>
    public static Users CreateUsers(IServiceProvider sp) =>
        CreateService(sp, (plugins, s) =>
            new Users(
                plugins.Select(p => (IPlugin)Activator.CreateInstance(p.GetType())!).ToList(),
                s.GetRequiredService<IRbacService>(),
                s.GetRequiredService<IHttpContextAccessor>(),
                s.GetRequiredService<IMediator>()));

    /// <summary>
    /// Creates a Groups service with per-scope plugin instances.
    /// This prevents cross-tenant contamination in multi-tenant environments.
    /// </summary>
    public static Groups CreateGroups(IServiceProvider sp) =>
        CreateService(sp, (plugins, s) =>
            new Groups(
                plugins.Select(p => (IPlugin)Activator.CreateInstance(p.GetType())!).ToList(),
                s.GetRequiredService<IRbacService>(),
                s.GetRequiredService<IHttpContextAccessor>(),
                s.GetRequiredService<IMediator>()));

    /// <summary>
    /// Creates a Bulks service with per-scope plugin instances.
    /// This prevents cross-tenant contamination in multi-tenant environments.
    /// </summary>
    public static Bulks CreateBulks(IServiceProvider sp) =>
        CreateService(sp, (plugins, s) =>
            new Bulks(
                plugins.Select(p => (IPlugin)Activator.CreateInstance(p.GetType())!).ToList(),
                s,
                s.GetRequiredService<ServiceProviderConfiguration>()));

    /// <summary>
    /// Creates a ClientServices service with per-scope plugin instances.
    /// This prevents cross-tenant contamination in multi-tenant environments.
    /// </summary>
    public static ClientServices CreateClientServices(IServiceProvider sp) =>
        CreateService(sp, (plugins, s) =>
            new ClientServices(
                plugins.Select(p => (IPlugin)Activator.CreateInstance(p.GetType())!).ToList(),
                s.GetRequiredService<IRbacService>(),
                s.GetRequiredService<IHttpContextAccessor>(),
                s.GetRequiredService<IMediator>(),
                s.GetRequiredService<IConfiguration>()));

    /// <summary>
    /// Creates a ClientCredentialsAuthentications service with per-scope plugin instances.
    /// This prevents cross-tenant contamination in multi-tenant environments.
    /// </summary>
    public static ClientCredentialsAuthentications CreateClientCredentialsAuthentications(IServiceProvider sp) =>
        CreateService(sp, (plugins, s) =>
            new ClientCredentialsAuthentications(
                plugins.Select(p => (IPlugin)Activator.CreateInstance(p.GetType())!).ToList(),
                s.GetRequiredService<IConfiguration>(),
                s.GetRequiredService<ClientServices>(),
                s.GetRequiredService<IJwtService>()));

    /// <summary>
    /// Creates a TokenExchangeAuthentications service with per-scope plugin instances.
    /// This prevents cross-tenant contamination in multi-tenant environments.
    /// </summary>
    public static TokenExchangeAuthentications CreateTokenExchangeAuthentications(IServiceProvider sp) =>
        CreateService(sp, (plugins, s) =>
            new TokenExchangeAuthentications(
                plugins.Select(p => (IPlugin)Activator.CreateInstance(p.GetType())!).ToList(),
                s.GetRequiredService<IConfiguration>(),
                s.GetRequiredService<IJwtService>(),
                s.GetRequiredService<HttpClient>()));

    /// <summary>
    /// Generic method to create any SCIM service with shared plugins.
    /// This method provides a flexible way to create services while ensuring
    /// plugin sharing across all instances.
    /// </summary>
    /// <typeparam name="T">The type of service to create</typeparam>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    /// <param name="factoryMethod">Factory method to create the specific service</param>
    /// <returns>The created service instance</returns>
    public static T CreateService<T>(IServiceProvider serviceProvider, Func<IReadOnlyList<IPlugin>, IServiceProvider, T> factoryMethod)
        where T : class
    {
        var plugins = PluginManager.Instance.Plugins;
        return factoryMethod(plugins, serviceProvider);
    }
}
