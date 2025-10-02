using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Looplex.Foundation.Core.SCIMv2.Entities;

namespace Looplex.Foundation.Core.SCIMv2
{
    /// <summary>
    /// Interface for automatic schema discovery from IResource types.
    /// Provides automatic schema generation based on reflection and conventions.
    /// </summary>
    public interface ISchemaAutoDiscovery
    {
        /// <summary>
        /// Discovers all SCIMv2 schemas from an assembly by scanning for IResource implementations.
        /// </summary>
        /// <param name="assembly">Assembly to scan for resource types</param>
        /// <returns>Collection of discovered schema definitions</returns>
        Task<IEnumerable<SchemaDefinition>> DiscoverSchemasAsync(Assembly assembly);
        
        /// <summary>
        /// Creates a schema definition from a specific IResource type.
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <returns>Schema definition for the resource type</returns>
        SchemaDefinition CreateSchemaFromResourceType<T>() where T : IResource;
        
        /// <summary>
        /// Creates a schema definition from a specific Type.
        /// </summary>
        /// <param name="resourceType">Resource type implementing IResource</param>
        /// <returns>Schema definition for the resource type</returns>
        SchemaDefinition CreateSchemaFromType(Type resourceType);
        
        /// <summary>
        /// Auto-configures attributes and mappings for a resource type.
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        void AutoConfigureResourceType<T>() where T : IResource;
    }
}
