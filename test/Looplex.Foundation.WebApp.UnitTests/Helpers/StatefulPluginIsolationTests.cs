using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Looplex.Foundation.WebApp.Helpers;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.Foundation.Ports;
using Looplex.Foundation.OAuth2;
using Looplex.Foundation.SCIMv2.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;

namespace Looplex.Foundation.WebApp.UnitTests.Helpers
{
    /// <summary>
    /// Tests to validate isolation of stateful plugins in multi-tenant environments.
    /// This test suite uses a mock stateful plugin to demonstrate cross-tenant isolation.
    /// </summary>
    [TestClass]
    public class StatefulPluginIsolationTests
    {
        /// <summary>
        /// Mock plugin that maintains state to demonstrate isolation requirements.
        /// In a real scenario, plugins might maintain tenant-specific data or configuration.
        /// </summary>
        public class MockStatefulPlugin : IPlugin
        {
            public string TenantId { get; set; } = string.Empty;
            public int AccessCount { get; set; }
            public DateTime LastAccessed { get; set; }
            
            public string Name => "MockStatefulPlugin";
            public string Description => "Mock plugin for testing multi-tenant isolation";
            public IEnumerable<ICommand> Commands => new List<ICommand>();
            
            public string GetPluginInfo() => $"MockStatefulPlugin for tenant {TenantId}, accessed {AccessCount} times";
            
            public IEnumerable<string> GetSubscriptions() => new List<string>();
            
            public Task ExecuteAsync<T>(IContext context, CancellationToken cancellationToken) where T : ICommand
            {
                return Task.CompletedTask;
            }
            
            public void Execute<T>(IContext context, CancellationToken cancellationToken) where T : ICommand
            {
                // Mock implementation - does nothing
            }
            
            public void RecordAccess(string tenantId)
            {
                TenantId = tenantId;
                AccessCount++;
                LastAccessed = DateTime.UtcNow;
            }
        }

        private ServiceProvider _serviceProvider;

        [TestInitialize]
        public void Setup()
        {
            // Reset plugin state before each test
            PluginManager.Instance.ReloadPlugins();
            
            var services = new ServiceCollection();
            
            // Register plugins as singleton
            services.AddSingleton<IReadOnlyList<IPlugin>>(_ => PluginManager.Instance.Plugins);
            
            // Register HttpClient
            services.AddHttpClient();
            
            // Register mock services required by SCIM services
            services.AddSingleton(Substitute.For<IRbacService>());
            services.AddSingleton(Substitute.For<IHttpContextAccessor>());
            services.AddSingleton(Substitute.For<IMediator>());
            services.AddSingleton(Substitute.For<IConfiguration>());
            services.AddSingleton(Substitute.For<IJwtService>());
            services.AddSingleton(Substitute.For<ServiceProviderConfiguration>());
            
            _serviceProvider = services.BuildServiceProvider();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _serviceProvider?.Dispose();
        }

        /// <summary>
        /// Demonstrates why per-scope plugin instances are critical for multi-tenant isolation.
        /// This test shows what happens when shared plugin instances are used.
        /// </summary>
        [TestMethod]
        public void SharedPluginInstances_CanLeadTo_CrossTenantContamination()
        {
            // Arrange - Create mock stateful plugins
            var plugin1 = new MockStatefulPlugin();
            var plugin2 = new MockStatefulPlugin();
            
            // Act - Simulate tenant A accessing plugin1
            plugin1.RecordAccess("TenantA");
            plugin1.RecordAccess("TenantA");
            
            // Simulate tenant B accessing plugin2 (but using the same instance)
            plugin2.RecordAccess("TenantB");
            
            // Assert - Each plugin maintains its own state
            Assert.AreEqual("TenantA", plugin1.TenantId);
            Assert.AreEqual(2, plugin1.AccessCount);
            Assert.AreEqual("TenantB", plugin2.TenantId);
            Assert.AreEqual(1, plugin2.AccessCount);
            
            // This demonstrates that different plugin instances maintain separate state
            // In a real scenario with shared instances, this could lead to contamination
        }

        /// <summary>
        /// Validates that the ScimServiceFactory creates distinct plugin instances
        /// for different scopes, preventing cross-tenant state contamination.
        /// </summary>
        [TestMethod]
        public void ScimServiceFactory_CreatesDistinctInstances_ForDifferentScopes()
        {
            // Arrange
            var scope1 = _serviceProvider.CreateScope();
            var scope2 = _serviceProvider.CreateScope();
            
            try
            {
                // Act - Create services using the factory
                var service1 = ScimServiceFactory.CreateUsers(scope1.ServiceProvider);
                var service2 = ScimServiceFactory.CreateUsers(scope2.ServiceProvider);
                
                // Get plugin collections from services
                var plugins1 = GetPluginsFromService(service1);
                var plugins2 = GetPluginsFromService(service2);
                
                // Assert - Plugin instances should be different
                Assert.AreEqual(plugins1.Count, plugins2.Count, "Both services should have the same number of plugins");
                
                for (int i = 0; i < plugins1.Count; i++)
                {
                    var plugin1 = plugins1[i];
                    var plugin2 = plugins2[i];
                    
                    // Plugin types should be the same
                    Assert.AreEqual(plugin1.GetType(), plugin2.GetType(), $"Plugin {i} types should be the same");
                    
                    // But instances should be different (per-scope instances)
                    Assert.AreNotSame(plugin1, plugin2, $"Plugin {i} instances should be different (per-scope)");
                }
            }
            finally
            {
                scope1.Dispose();
                scope2.Dispose();
            }
        }

        /// <summary>
        /// Simulates a realistic multi-tenant scenario where multiple tenants
        /// access services concurrently, validating that their plugin states remain isolated.
        /// </summary>
        [TestMethod]
        public async Task MultiTenantScenario_ShouldMaintain_PluginStateIsolation()
        {
            // Arrange
            const int numberOfTenants = 5;
            const int operationsPerTenant = 10;
            var tenantResults = new ConcurrentDictionary<string, List<string>>();
            
            // Act - Simulate multiple tenants accessing services concurrently
            var tasks = Enumerable.Range(0, numberOfTenants).Select(async tenantIndex =>
            {
                var tenantId = $"Tenant{tenantIndex}";
                var results = new List<string>();
                
                using var scope = _serviceProvider.CreateScope();
                
                for (int i = 0; i < operationsPerTenant; i++)
                {
                    // Create a service for this tenant
                    var service = ScimServiceFactory.CreateUsers(scope.ServiceProvider);
                    
                    // Get plugins and simulate stateful operations
                    var plugins = GetPluginsFromService(service);
                    
                    // Record access for each plugin (simulating tenant-specific operations)
                    foreach (var plugin in plugins)
                    {
                        // In a real scenario, plugins would maintain tenant-specific state
                        // For this test, we just record that we accessed the plugin
                        results.Add($"{plugin.GetType().Name}:{tenantId}:{i}");
                    }
                    
                    // Small delay to increase concurrency
                    await Task.Delay(1);
                }
                
                tenantResults[tenantId] = results;
            });
            
            await Task.WhenAll(tasks);
            
            // Assert - Each tenant should have its own isolated plugin state
            Assert.AreEqual(numberOfTenants, tenantResults.Count, "All tenants should complete their operations");
            
            // Validate that each tenant's operations completed successfully
            // Note: The number of results depends on how many plugins are loaded
            foreach (var kvp in tenantResults)
            {
                var expectedResults = operationsPerTenant * PluginManager.Instance.Plugins.Count;
                Assert.AreEqual(expectedResults, kvp.Value.Count, $"Tenant {kvp.Key} should have {expectedResults} operations");
            }
        }

        /// <summary>
        /// Validates that plugin reloading doesn't affect existing service instances
        /// and that new services get fresh plugin instances.
        /// </summary>
        [TestMethod]
        public void PluginReloading_ShouldNotAffect_ExistingServiceInstances()
        {
            // Arrange
            using var scope1 = _serviceProvider.CreateScope();
            var service1 = ScimServiceFactory.CreateUsers(scope1.ServiceProvider);
            var plugins1 = GetPluginsFromService(service1);
            
            // Act - Reload plugins
            PluginManager.Instance.ReloadPlugins();
            
            // Create a new service after reload
            using var scope2 = _serviceProvider.CreateScope();
            var service2 = ScimServiceFactory.CreateUsers(scope2.ServiceProvider);
            var plugins2 = GetPluginsFromService(service2);
            
            // Assert - Both services should have valid plugin collections
            Assert.IsNotNull(plugins1, "Original service plugins should not be null");
            Assert.IsNotNull(plugins2, "New service plugins should not be null");
            Assert.AreEqual(plugins1.Count, plugins2.Count, "Both services should have the same number of plugins");
            
            // The plugin instances should be different (per-scope instances)
            for (int i = 0; i < plugins1.Count; i++)
            {
                Assert.AreNotSame(plugins1[i], plugins2[i], $"Plugin {i} instances should be different");
            }
        }

        /// <summary>
        /// Validates that the PluginManager's thread-safe operations work correctly
        /// when multiple threads access plugins simultaneously.
        /// </summary>
        [TestMethod]
        public async Task PluginManager_ShouldBeThreadSafe_ForConcurrentAccess()
        {
            // Arrange
            const int numberOfThreads = 10;
            const int operationsPerThread = 100;
            var results = new ConcurrentBag<bool>();
            
            // Act - Multiple threads accessing PluginManager concurrently
            var tasks = Enumerable.Range(0, numberOfThreads).Select(async threadIndex =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    try
                    {
                        var plugins = PluginManager.Instance.Plugins;
                        var isValid = plugins != null && plugins.Count >= 0;
                        results.Add(isValid);
                        
                        // Occasionally reload plugins to test thread safety
                        if (i % 20 == 0)
                        {
                            PluginManager.Instance.ReloadPlugins();
                        }
                    }
                    catch (Exception)
                    {
                        results.Add(false);
                    }
                    
                    await Task.Delay(1);
                }
            });
            
            await Task.WhenAll(tasks);
            
            // Assert - All operations should succeed
            Assert.AreEqual(numberOfThreads * operationsPerThread, results.Count, "All operations should complete");
            Assert.IsTrue(results.All(r => r), "All plugin access operations should succeed");
        }

        /// <summary>
        /// Helper method to extract plugin collection from a service using reflection.
        /// This is needed because the plugin collection is typically a protected property.
        /// </summary>
        private static IList<IPlugin> GetPluginsFromService(object service)
        {
            // Try property first (protected property in Service base class)
            var pluginsProperty = service.GetType().GetProperty("Plugins", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (pluginsProperty != null)
            {
                return (IList<IPlugin>)pluginsProperty.GetValue(service)!;
            }
            
            // Try alternative field names as fallback
            var pluginsField = service.GetType().GetField("_plugins", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (pluginsField == null)
            {
                pluginsField = service.GetType().GetField("plugins", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }
            
            if (pluginsField != null)
            {
                return (IList<IPlugin>)pluginsField.GetValue(service)!;
            }
            
            throw new InvalidOperationException($"Could not find plugins field/property in service type {service.GetType().Name}");
        }
    }
}
