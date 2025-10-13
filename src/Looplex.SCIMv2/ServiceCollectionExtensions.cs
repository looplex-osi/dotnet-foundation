using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Looplex.SCIMv2.Entities;
using Microsoft.AspNetCore.Http;

namespace Looplex.SCIMv2
{
    /// <summary>
    /// Extension methods for registering SCIMv2 services with automatic schema discovery.
    /// Provides simplified registration with zero configuration required.
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
        services.AddSingleton<IJsonSchemaService>(sp => sp.GetRequiredService<SCIMv2>());
        return services;
    }

    /// <summary>
    /// Adds SCIMv2 services with automatic discovery and configuration.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSCIMv2WithAutoDiscovery(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register core SCIMv2 services with dependency injection
        services.AddSingleton<ISCIMv2>(sp => 
        {
            var serviceNameProvider = sp.GetService<IServiceNameProvider>();
            var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
            return new SCIMv2(serviceNameProvider, httpContextAccessor);
        });
        services.AddSingleton<IJsonSchemaService>(sp => sp.GetRequiredService<SCIMv2>());
        
        // Register default schemas
        services.AddSingleton<IHostedService, SCIMv2DefaultSchemaService>();
        
        return services;
    }
        
        /// <summary>
        /// Adds SCIMv2 services with automatic configuration for specific resource types.
        /// </summary>
        /// <typeparam name="T">Resource type to auto-configure</typeparam>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddSCIMv2WithResource<T>(this IServiceCollection services) where T : IResource
        {
            services.AddSCIMv2WithAutoDiscovery();
            
            // Auto-configure the resource type
            services.AddSingleton<IHostedService, SCIMv2AutoConfigurationService<T>>();
            
            return services;
        }
        
        /// <summary>
        /// Adds SCIMv2 services with automatic configuration for multiple resource types.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="resourceTypes">Resource types to auto-configure</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddSCIMv2WithResources(this IServiceCollection services, params Type[] resourceTypes)
        {
            services.AddSCIMv2WithAutoDiscovery();
            
            // Auto-configure multiple resource types
            services.AddSingleton<IHostedService, SCIMv2AutoConfigurationService>(sp =>
            {
                var scimService = sp.GetRequiredService<ISCIMv2>();
                return new SCIMv2AutoConfigurationService(scimService, resourceTypes, sp);
            });
            
            return services;
        }
        
        /// <summary>
        /// Configures SCIMv2 with custom service and application names.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="serviceName">Service name (e.g., "looplex")</param>
        /// <param name="applicationName">Application name (e.g., "notejam", "case-management")</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection ConfigureSCIMv2Names(this IServiceCollection services, string serviceName, string applicationName)
        {
            services.AddSingleton<IServiceNameProvider>(new ServiceNameProvider(serviceName));
            services.AddSingleton<IApplicationNameProvider>(new ApplicationNameProvider(applicationName));
            return services;
        }
    }
    
    /// <summary>
    /// Background service for automatic SCIMv2 configuration.
    /// Runs during application startup to auto-configure resource types.
    /// </summary>
    /// <typeparam name="T">Resource type to configure</typeparam>
    public class SCIMv2AutoConfigurationService<T> : IHostedService where T : IResource
    {
        private readonly ISCIMv2 _scimService;
        private readonly IServiceProvider _serviceProvider;
        
        public SCIMv2AutoConfigurationService(ISCIMv2 scimService, IServiceProvider serviceProvider)
        {
            _scimService = scimService;
            _serviceProvider = serviceProvider;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_scimService is SCIMv2 scimv2)
            {
                // Get ServiceNameProvider from DI container
                var serviceNameProvider = _serviceProvider.GetService<IServiceNameProvider>();
                var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();
                
                
                // Create SchemaAutoDiscovery with injected dependencies
                var autoDiscovery = new SchemaAutoDiscovery(serviceNameProvider, null, httpContextAccessor);
                autoDiscovery.AutoConfigureResourceType<T>();
            }
            
            return Task.CompletedTask;
        }
        
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Background service for automatic SCIMv2 configuration of multiple resource types.
    /// </summary>
    public class SCIMv2AutoConfigurationService : IHostedService
    {
        private readonly ISCIMv2 _scimService;
        private readonly Type[] _resourceTypes;
        private readonly IServiceProvider _serviceProvider;
        
        public SCIMv2AutoConfigurationService(ISCIMv2 scimService, Type[] resourceTypes, IServiceProvider serviceProvider)
        {
            _scimService = scimService;
            _resourceTypes = resourceTypes;
            _serviceProvider = serviceProvider;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_scimService is SCIMv2 scimv2)
            {
                // Get dependencies from service provider
                var serviceNameProvider = _serviceProvider.GetService<IServiceNameProvider>();
                var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();
                
                
                // Create and register schemas for each resource type with injected dependencies
                var autoDiscovery = new SchemaAutoDiscovery(serviceNameProvider, null, httpContextAccessor);
                foreach (var resourceType in _resourceTypes)
                {
                    if (typeof(IResource).IsAssignableFrom(resourceType))
                    {
                        // Use reflection to call CreateSchemaFromResourceType<T>
                        var method = typeof(SchemaAutoDiscovery).GetMethod(nameof(SchemaAutoDiscovery.CreateSchemaFromResourceType));
                        var genericMethod = method?.MakeGenericMethod(resourceType);
                        var schema = genericMethod?.Invoke(autoDiscovery, null) as SchemaDefinition;
                        if (schema != null)
                        {
                            scimv2.RegisterSchema(schema);
                        }
                    }
                }
                
                // SchemaAutoDiscovery is already handled by SCIMv2AutoConfigurationService<T> for each resource type
                // No need to duplicate the work here
            }
            
            return Task.CompletedTask;
        }
        
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Background service for registering default SCIMv2 schemas.
    /// </summary>
    public class SCIMv2DefaultSchemaService : IHostedService
    {
        private readonly ISCIMv2 _scimService;
        
        public SCIMv2DefaultSchemaService(ISCIMv2 scimService)
        {
            _scimService = scimService;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_scimService is SCIMv2 scimv2)
            {
                // Register default schemas manually
                RegisterDefaultSchemas(scimv2);
            }
            
            return Task.CompletedTask;
        }
        
        private void RegisterDefaultSchemas(SCIMv2 scimv2)
        {
            // No default schemas needed - auto-discovery will handle User and Group
            // This prevents duplication when User and Group are registered via auto-discovery
        }
        
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
