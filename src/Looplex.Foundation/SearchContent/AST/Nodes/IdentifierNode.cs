using System;
using System.Collections.Generic;
using System.Linq;

namespace Looplex.Foundation.SearchContent.AST.Nodes;

/// <summary>
/// Represents an attribute identifier in the SCIM filter AST
/// 
/// Supports various SCIM attribute formats:
/// - Simple attributes: userName, displayName
/// - Sub-attributes: name.givenName, name.familyName
/// - Schema prefixes: urn:ietf:params:scim:schemas:core:2.0:User:userName
/// - Complex paths: urn:ietf:params:scim:schemas:core:2.0:User:name.givenName
/// </summary>
public class IdentifierNode : IAstNode
{
    /// <summary>
    /// Main attribute name (e.g., "userName", "name")
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional schema (legacy support)
    /// </summary>
    public string? Schema { get; }

    /// <summary>
    /// Optional sub-property (legacy support)
    /// </summary>
    public string? SubProperty { get; }

    /// <summary>
    /// Optional schema prefix (e.g., "urn:ietf:params:scim:schemas:core:2.0:User")
    /// Based on RFC 7644 Section 3.1 - Schema URNs
    /// </summary>
    public string? SchemaPrefix { get; set; }

    /// <summary>
    /// Optional sub-attribute (e.g., "givenName" from "name.givenName")
    /// Based on RFC 7644 Section 2.3.8 - Sub-Attributes
    /// </summary>
    public string? SubAttribute { get; set; }

    /// <summary>
    /// Optional Value Path Filter condition (e.g., "IdentityProviderType eq \"CPF\"" from "identities[IdentityProviderType eq \"CPF\"].Value")
    /// Based on RFC 7644 Section 3.4.2.2 - Value Path Filters
    /// </summary>
    public string? ValuePathFilter { get; set; }

    public IdentifierNode(string name, string? schema = null, string? subProperty = null)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name
            : throw new ArgumentException("Name cannot be null or empty", nameof(name));
        Schema = schema;
        SubProperty = subProperty;
    }

    public T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitIdentifier(this);
    }

    /// <summary>
    /// Get the full attribute path including schema prefix and sub-attribute
    /// </summary>
    public string GetFullPath()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(SchemaPrefix))
            parts.Add(SchemaPrefix);
            
        parts.Add(Name);
        
        if (!string.IsNullOrEmpty(SubAttribute))
            parts.Add(SubAttribute);
            
        return string.Join(":", parts.Take(parts.Count - 1)) + 
               (parts.Count > 1 && !string.IsNullOrEmpty(SubAttribute) ? "." + SubAttribute : "");
    }



    public override string ToString()
    {
        var result = Name;
        
        if (!string.IsNullOrEmpty(SchemaPrefix))
            result = $"{SchemaPrefix}:{result}";
            
        if (!string.IsNullOrEmpty(SubAttribute))
            result = $"{result}.{SubAttribute}";
            
        return result;
    }
}