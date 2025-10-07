using System;
using System.Reflection;
using System.Linq;

namespace Looplex.SCIMv2
{
    /// <summary>
    /// SCIMv2 naming and mapping conventions for automatic schema generation.
    /// Provides intelligent conventions for attribute mapping and schema generation.
    /// </summary>
    public static class SCIMv2Conventions
    {
        /// <summary>
        /// Converts property name to database column name using snake_case convention.
        /// </summary>
        /// <param name="propertyName">Property name in PascalCase</param>
        /// <returns>Database column name in snake_case</returns>
        public static string ToDatabaseColumn(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return propertyName;
                
            // Convert PascalCase to snake_case
            return string.Concat(propertyName.Select((x, i) => 
                i > 0 && char.IsUpper(x) ? "_" + x.ToString().ToLower() : x.ToString().ToLower()));
        }
        
        /// <summary>
        /// Converts property name to SCIM attribute name using camelCase convention.
        /// </summary>
        /// <param name="propertyName">Property name in PascalCase</param>
        /// <returns>SCIM attribute name in camelCase</returns>
        public static string ToSCIMAttribute(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return propertyName;
                
            // Convert PascalCase to camelCase
            return char.ToLower(propertyName[0]) + propertyName.Substring(1);
        }
        
        /// <summary>
        /// Infers SCIM attribute type from .NET property type.
        /// </summary>
        /// <param name="propertyType">.NET property type</param>
        /// <returns>SCIM attribute type</returns>
        public static string InferAttributeType(Type propertyType)
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            
            return underlyingType switch
            {
                Type t when t == typeof(string) => "string",
                Type t when t == typeof(bool) => "boolean",
                Type t when t == typeof(int) || t == typeof(long) || t == typeof(short) => "integer",
                Type t when t == typeof(decimal) || t == typeof(double) || t == typeof(float) => "decimal",
                Type t when t == typeof(DateTime) => "dateTime",
                Type t when t == typeof(Guid) => "string",
                Type t when t.IsEnum => "string",
                _ => "string" // Default to string for unknown types
            };
        }
        
        /// <summary>
        /// Determines if a property type is filterable in SCIM queries.
        /// </summary>
        /// <param name="propertyType">.NET property type</param>
        /// <returns>True if the property can be used in SCIM filters</returns>
        public static bool IsFilterable(Type propertyType)
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            
            return underlyingType switch
            {
                Type t when t == typeof(string) => true,
                Type t when t == typeof(bool) => true,
                Type t when t == typeof(int) || t == typeof(long) || t == typeof(short) => true,
                Type t when t == typeof(decimal) || t == typeof(double) || t == typeof(float) => true,
                Type t when t == typeof(DateTime) => true,
                Type t when t == typeof(Guid) => true,
                Type t when t.IsEnum => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Generates schema URI for a resource type using SCIMv2 conventions.
        /// </summary>
        /// <param name="resourceType">Resource type name</param>
        /// <param name="serviceName">Service name (optional)</param>
        /// <param name="applicationName">Application name (optional)</param>
        /// <returns>SCIMv2 compliant schema URI</returns>
        public static string GenerateSchemaUri(string resourceType, string? serviceName = null, string? applicationName = null)
        {
            var service = string.IsNullOrEmpty(serviceName) ? "looplex" : serviceName;
            var app = string.IsNullOrEmpty(applicationName) ? "core" : applicationName;
            return $"urn:{service}:params:scim:schemas:{app}:2.0:{resourceType}";
        }
        
        /// <summary>
        /// Determines if a property should be included in the schema.
        /// Excludes system properties and complex objects that need special handling.
        /// </summary>
        /// <param name="property">Property info to evaluate</param>
        /// <returns>True if the property should be included in schema</returns>
        public static bool ShouldIncludeInSchema(PropertyInfo property)
        {
            // Exclude system properties
            var excludedProperties = new[] { "Id", "ExternalId", "Meta", "Schemas" };
            if (excludedProperties.Contains(property.Name))
                return false;
                
            // Exclude read-only properties without setters
            if (!property.CanWrite)
                return false;
                
            // Exclude complex objects that need special handling
            var propertyType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            
            // Include primitive types and simple objects
            return underlyingType.IsPrimitive || 
                   underlyingType == typeof(string) || 
                   underlyingType == typeof(DateTime) || 
                   underlyingType == typeof(Guid) ||
                   underlyingType.IsEnum;
        }
        
        /// <summary>
        /// Generates a human-readable description for a property.
        /// </summary>
        /// <param name="property">Property info</param>
        /// <returns>Generated description</returns>
        public static string GeneratePropertyDescription(PropertyInfo property)
        {
            var propertyName = property.Name;
            var typeName = InferAttributeType(property.PropertyType);
            
            return $"{propertyName} property of type {typeName}";
        }
    }
}
