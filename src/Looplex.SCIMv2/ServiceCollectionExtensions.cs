using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Looplex.SCIMv2.Entities;

namespace Looplex.SCIMv2
{
    /// <summary>
    /// Extension methods for registering SCIMv2 services with automatic schema discovery.
    /// Provides simplified registration with zero configuration required.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds SCIMv2 services with automatic schema discovery.
        /// This method eliminates the need for manual schema configuration.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assembly">Assembly to scan for IResource types (optional)</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddSCIMv2WithAutoDiscovery(this IServiceCollection services, Assembly? assembly = null)
        {
            // Register auto-discovery service
            services.AddSingleton<ISchemaAutoDiscovery, SchemaAutoDiscovery>();
            
            // Register SCIMv2 core services
            services.AddSingleton<ISCIMv2, SCIMv2>();
            services.AddSingleton<ISCIMv2Validation, SCIMv2>();
            
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
                return new SCIMv2AutoConfigurationService(scimService, resourceTypes);
            });
            
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
        
        public SCIMv2AutoConfigurationService(ISCIMv2 scimService)
        {
            _scimService = scimService;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_scimService is SCIMv2 scimv2)
            {
                // Auto-configure using SchemaAutoDiscovery
                var autoDiscovery = new SchemaAutoDiscovery();
                autoDiscovery.AutoConfigureResourceType<T>();
                Console.WriteLine($"✅ Auto-configured SCIMv2 for {typeof(T).Name}");
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
        
        public SCIMv2AutoConfigurationService(ISCIMv2 scimService, Type[] resourceTypes)
        {
            _scimService = scimService;
            _resourceTypes = resourceTypes;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_scimService is SCIMv2 scimv2)
            {
                // Create and register schemas for each resource type
                var autoDiscovery = new SchemaAutoDiscovery();
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
                
                // Auto-configure using SchemaAutoDiscovery
                foreach (var resourceType in _resourceTypes)
                {
                    if (typeof(IResource).IsAssignableFrom(resourceType))
                    {
                        // Use reflection to call AutoConfigureResourceType<T>
                        var method = typeof(SchemaAutoDiscovery).GetMethod(nameof(SchemaAutoDiscovery.AutoConfigureResourceType));
                        var genericMethod = method?.MakeGenericMethod(resourceType);
                        genericMethod?.Invoke(autoDiscovery, null);
                    }
                }
                Console.WriteLine($"✅ Auto-configured SCIMv2 for {_resourceTypes.Length} resource types");
            }
            
            return Task.CompletedTask;
        }
        
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
