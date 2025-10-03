using System;
using System.Text.Json.Serialization;

namespace Looplex.SCIMv2.Entities;

/// <summary>
/// SCIMv2 Schema Definition - Represents a schema for SCIM resources
/// Implements RFC 7643 (SCIM Schema Definition) for schema structure
/// [RFC 7643](https://datatracker.ietf.org/doc/html/rfc7643) - SCIM Schema Definition
/// https://datatracker.ietf.org/doc/html/rfc7643
/// </summary>
public class SchemaDefinition
{
    /// <summary>
    /// The unique URI of the schema
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The schema's human-readable name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The schema's human-readable Schemas
    /// </summary>
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The schema's human-readable description
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// A list of attributes that compose the schema
    /// </summary>
    [JsonPropertyName("attributes")]
    public SchemaAttribute[] Attributes { get; set; } = Array.Empty<SchemaAttribute>();

    /// <summary>
    /// The schema's human-readable name for the resource type
    /// </summary>
    [JsonPropertyName("meta")]
    public SchemaMeta? Meta { get; set; }
}

/// <summary>
/// SCIMv2 Schema Attribute - Represents an attribute within a schema
/// Implements RFC 7643 Section 2.2 (Attribute Definition)
/// [RFC 7643 Section 2.2](https://datatracker.ietf.org/doc/html/rfc7643#section-2.2)
/// https://datatracker.ietf.org/doc/html/rfc7643#section-2.2
/// </summary>
public class SchemaAttribute
{
    /// <summary>
    /// The attribute's name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The attribute's data type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// A Boolean value indicating the attribute's plurality
    /// </summary>
    [JsonPropertyName("multiValued")]
    public bool MultiValued { get; set; }

    /// <summary>
    /// A string value indicating the attribute's mutability
    /// </summary>
    [JsonPropertyName("mutability")]
    public string Mutability { get; set; } = "readWrite";

    /// <summary>
    /// A string value indicating the attribute's returnability
    /// </summary>
    [JsonPropertyName("returned")]
    public string Returned { get; set; } = "default";

    /// <summary>
    /// A Boolean value indicating whether the attribute is required
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; }

    /// <summary>
    /// A collection of suggested canonical values that may be used
    /// </summary>
    [JsonPropertyName("canonicalValues")]
    public string[] CanonicalValues { get; set; } = Array.Empty<string>();

    /// <summary>
    /// A Boolean value indicating whether the string attribute is case sensitive
    /// </summary>
    [JsonPropertyName("caseExact")]
    public bool CaseExact { get; set; }

    /// <summary>
    /// A string value indicating the attribute's uniqueness
    /// </summary>
    [JsonPropertyName("uniqueness")]
    public string Uniqueness { get; set; } = "none";

    /// <summary>
    /// A human-readable description of the attribute
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// A collection of sub-attributes
    /// </summary>
    [JsonPropertyName("subAttributes")]
    public SchemaAttribute[] SubAttributes { get; set; } = Array.Empty<SchemaAttribute>();

    /// <summary>
    /// A string value indicating the attribute's reference types
    /// </summary>
    [JsonPropertyName("referenceTypes")]
    public string[] ReferenceTypes { get; set; } = Array.Empty<string>();
}

/// <summary>
/// SCIMv2 Schema Meta - Metadata for schema definitions
/// </summary>
public class SchemaMeta
{
    /// <summary>
    /// The schema's resource type
    /// </summary>
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "Schema";

    /// <summary>
    /// The schema's location
    /// </summary>
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
}
