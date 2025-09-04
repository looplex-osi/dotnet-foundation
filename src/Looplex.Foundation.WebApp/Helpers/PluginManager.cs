using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Loader;

namespace Looplex.Foundation.WebApp.Helpers;

/// <summary>
/// Singleton manager for plugin loading and management.
/// 
/// This class implements the Singleton pattern to ensure that plugins are loaded
/// only once and shared across all services, eliminating the need for multiple
/// PluginLoader instances and redundant DLL loading.
/// 
/// Features:
/// - Thread-safe singleton implementation
/// - Lazy loading of plugins
/// - Hot reload capability (optional)
/// - Memory efficient plugin sharing
/// - Performance optimization for multiple services
/// </summary>
public sealed class PluginManager
{
    private static readonly Lazy<PluginManager> _instance = new(() => new PluginManager());
    private static readonly object _lock = new();
    
    private IList<IPlugin> _plugins = new List<IPlugin>();
    private readonly PluginLoader _loader = new();
    private bool _isInitialized = false;

    /// <summary>
    /// Gets the singleton instance of PluginManager
    /// </summary>
    public static PluginManager Instance => _instance.Value;

    /// <summary>
    /// Private constructor to enforce singleton pattern
    /// </summary>
    private PluginManager() { }

    /// <summary>
    /// Gets the loaded plugins. Thread-safe access to the plugin collection.
    /// </summary>
    public IList<IPlugin> Plugins
    {
        get
        {
            lock (_lock)
            {
                if (!_isInitialized)
                {
                    LoadPlugins();
                }
                return _plugins;
            }
        }
    }

    /// <summary>
    /// Initializes the plugin manager and loads plugins from the plugins directory.
    /// This method is thread-safe and can be called multiple times safely.
    /// </summary>
    public void Initialize()
    {
        lock (_lock)
        {
            if (!_isInitialized)
            {
                LoadPlugins();
                _isInitialized = true;
            }
        }
    }

    /// <summary>
    /// Reloads plugins from the plugins directory.
    /// This method is thread-safe and can be used for hot reload scenarios.
    /// </summary>
    public void ReloadPlugins()
    {
        lock (_lock)
        {
            LoadPlugins();
        }
    }

    /// <summary>
    /// Loads plugins from the plugins directory.
    /// This method is called internally and is thread-safe.
    /// </summary>
    private void LoadPlugins()
    {
        try
        {
            var pluginsDirectory = "plugins";
            
            if (!Directory.Exists(pluginsDirectory))
            {
                _plugins = new List<IPlugin>();
                return;
            }

            var dlls = Directory.GetFiles(pluginsDirectory, "*.dll");
            _plugins = _loader.LoadPlugins(dlls).ToList();
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent application startup failure
            Console.WriteLine($"Error loading plugins: {ex.Message}");
            _plugins = new List<IPlugin>();
        }
    }

    /// <summary>
    /// Gets the count of loaded plugins
    /// </summary>
    public int PluginCount
    {
        get
        {
            lock (_lock)
            {
                return _plugins.Count;
            }
        }
    }

    /// <summary>
    /// Checks if the plugin manager has been initialized
    /// </summary>
    public bool IsInitialized
    {
        get
        {
            lock (_lock)
            {
                return _isInitialized;
            }
        }
    }
}
