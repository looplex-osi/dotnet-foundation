using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Looplex.Foundation.Core.SCIMv2.Entities;

namespace Looplex.Foundation.Core.SCIMv2
{
    /// <summary>
    /// Automatic schema discovery implementation using reflection and conventions.
    /// Scans assemblies for IResource implementations and generates SCIMv2 schemas automatically.
    /// </summary>
    public class SchemaAutoDiscovery : ISchemaAutoDiscovery
    {
        private readonly IServiceNameProvider? _serviceNameProvider;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        
        public SchemaAutoDiscovery(IServiceNameProvider? serviceNameProvider = null, IHttpContextAccessor? httpContextAccessor = null)
        {
            _serviceNameProvider = serviceNameProvider;
            _httpContextAccessor = httpContextAccessor;
        }
        
        /// <summary>
        /// Discovers all SCIMv2 schemas from an assembly by scanning for IResource implementations.
        /// </summary>
        /// <param name="assembly">Assembly to scan for resource types</param>
        /// <returns>Collection of discovered schema definitions</returns>
        public async Task<IEnumerable<SchemaDefinition>> DiscoverSchemasAsync(Assembly assembly)
        {
            var resourceTypes = assembly.GetTypes()
                .Where(t => typeof(IResource).IsAssignableFrom(t) && 
                           !t.IsAbstract && 
                           !t.IsInterface &&
                           !IsStandardSCIMType(t))
                .ToList();
                
            var schemas = new List<SchemaDefinition>();
            
            foreach (var resourceType in resourceTypes)
            {
                var schema = CreateSchemaFromType(resourceType);
                schemas.Add(schema);
            }
            
            return await Task.FromResult(schemas);
        }
        
        /// <summary>
        /// Creates a schema definition from a specific IResource type.
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <returns>Schema definition for the resource type</returns>
        public SchemaDefinition CreateSchemaFromResourceType<T>() where T : IResource
        {
            return CreateSchemaFromType(typeof(T));
        }
        
        /// <summary>
        /// Creates a schema definition from a specific Type.
        /// </summary>
        /// <param name="resourceType">Resource type implementing IResource</param>
        /// <returns>Schema definition for the resource type</returns>
        public SchemaDefinition CreateSchemaFromType(Type resourceType)
        {
            if (!typeof(IResource).IsAssignableFrom(resourceType))
                throw new ArgumentException($"Type {resourceType.Name} does not implement IResource", nameof(resourceType));
                
            var serviceName = _serviceNameProvider?.GetServiceName() ?? "looplex";
            var schemaId = SCIMv2Conventions.GenerateSchemaUri(resourceType.Name, serviceName);
            
            var attributes = ExtractAttributesFromType(resourceType);
            
            return new SchemaDefinition
            {
                Id = schemaId,
                Name = resourceType.Name,
                Description = $"{resourceType.Name} resource for {serviceName}",
                Schemas = new[] { schemaId }, // Preencher com o próprio ID do schema
                Attributes = attributes.ToArray(),
                Meta = new SchemaMeta
                {
                    ResourceType = "Schema",
                    Location = $"{GetBaseUrl()}/Schemas/{schemaId}"
                }
            };
        }
        
        /// <summary>
        /// Constructs the base URL for SCIM resource locations.
        /// </summary>
        /// <returns>Base URL for SCIM resources</returns>
        private string GetBaseUrl()
        {
            if (_httpContextAccessor?.HttpContext?.Request != null)
            {
                var request = _httpContextAccessor.HttpContext.Request;
                var scheme = request.Scheme;
                var host = request.Host;
                var pathBase = request.PathBase;
                return $"{scheme}://{host}{pathBase}";
            }
            return "https://api.exemplo.com/";
        }
        
        /// <summary>
        /// Auto-configures attributes and mappings for a resource type.
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        public void AutoConfigureResourceType<T>() where T : IResource
        {
            var resourceType = typeof(T);
            var resourceName = resourceType.Name;
            
            // Auto-configure allowed attributes
            var allowedAttributes = ExtractFilterableAttributes(resourceType);
            SCIMv2.ConfigureAttributes(resourceName, allowedAttributes);
            
            // Auto-configure attribute mappings
            var attributeMappings = ExtractAttributeMappings(resourceType);
            SCIMv2.ConfigureMapping(resourceName, attributeMappings);
        }
        
        /// <summary>
        /// Extracts schema attributes from a resource type using reflection.
        /// </summary>
        /// <param name="resourceType">Resource type to extract attributes from</param>
        /// <returns>Collection of schema attributes</returns>
        private IEnumerable<SchemaAttribute> ExtractAttributesFromType(Type resourceType)
        {
            var properties = resourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(SCIMv2Conventions.ShouldIncludeInSchema)
                .ToList();
                
            var attributes = new List<SchemaAttribute>();
            
            foreach (var property in properties)
            {
                var attribute = new SchemaAttribute
                {
                    Name = SCIMv2Conventions.ToSCIMAttribute(property.Name),
                    Type = SCIMv2Conventions.InferAttributeType(property.PropertyType),
                    Description = SCIMv2Conventions.GeneratePropertyDescription(property),
                    Required = IsRequiredProperty(property),
                    CaseExact = false,
                    Mutability = "readWrite",
                    Returned = "default",
                    Uniqueness = "none"
                };
                
                attributes.Add(attribute);
            }
            
            return attributes;
        }
        
        /// <summary>
        /// Extracts filterable attributes for SCIM filtering.
        /// </summary>
        /// <param name="resourceType">Resource type to extract attributes from</param>
        /// <returns>Set of filterable attribute names</returns>
        private HashSet<string> ExtractFilterableAttributes(Type resourceType)
        {
            var properties = resourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => SCIMv2Conventions.IsFilterable(p.PropertyType))
                .Where(SCIMv2Conventions.ShouldIncludeInSchema);
                
            var attributes = new HashSet<string>();
            
            // Add standard SCIM attributes
            attributes.Add("id");
            attributes.Add("externalId");
            attributes.Add("meta.created");
            attributes.Add("meta.lastModified");
            
            // Add resource-specific attributes
            foreach (var property in properties)
            {
                attributes.Add(SCIMv2Conventions.ToSCIMAttribute(property.Name));
            }
            
            return attributes;
        }
        
        /// <summary>
        /// Extracts attribute mappings for database column mapping.
        /// </summary>
        /// <param name="resourceType">Resource type to extract mappings from</param>
        /// <returns>Dictionary of attribute to column mappings</returns>
        private Dictionary<string, string> ExtractAttributeMappings(Type resourceType)
        {
            var mappings = new Dictionary<string, string>();
            var tablePrefix = GetTablePrefix(resourceType.Name);
            
            // Add standard SCIM mappings
            mappings["meta.created"] = $"{tablePrefix}.created_at";
            mappings["meta.lastModified"] = $"{tablePrefix}.updated_at";
            
            // Add resource-specific mappings
            var properties = resourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(SCIMv2Conventions.ShouldIncludeInSchema);
                
            foreach (var property in properties)
            {
                var attributeName = SCIMv2Conventions.ToSCIMAttribute(property.Name);
                var columnName = SCIMv2Conventions.ToDatabaseColumn(property.Name);
                mappings[attributeName] = $"{tablePrefix}.{columnName}";
            }
            
            return mappings;
        }
        
        /// <summary>
        /// Determines if a property is required based on its type and attributes.
        /// </summary>
        /// <param name="property">Property to evaluate</param>
        /// <returns>True if the property is required</returns>
        private bool IsRequiredProperty(PropertyInfo property)
        {
            // Check for Required attribute
            var requiredAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>();
            if (requiredAttribute != null)
                return true;
                
            // Check if property type is nullable
            var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
            return underlyingType == null && property.PropertyType.IsValueType;
        }
        
        /// <summary>
        /// Gets table prefix for a resource type using naming conventions.
        /// </summary>
        /// <param name="resourceTypeName">Resource type name</param>
        /// <returns>Table prefix for database mapping</returns>
        private string GetTablePrefix(string resourceTypeName)
        {
            return resourceTypeName.ToLower().First().ToString();
        }
        
        /// <summary>
        /// Determines if a type is a standard SCIM type (User, Group) that should be excluded.
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if the type is a standard SCIM type</returns>
        private bool IsStandardSCIMType(Type type)
        {
            var standardTypes = new[] { "User", "Group" };
            return standardTypes.Contains(type.Name);
        }
    }
}
