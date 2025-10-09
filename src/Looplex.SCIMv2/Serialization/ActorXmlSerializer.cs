using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Looplex.Foundation.Serialization;
using Looplex.SCIMv2.Entities;

namespace Looplex.SCIMv2.Serialization
{
  public static class ActorXmlSerializer
  {
    // Centralized configuration for SCIMv2 XML serialization
    public static readonly XmlSerializerNamespaces DefaultNamespaces = new();
    
    static ActorXmlSerializer()
    {
      DefaultNamespaces.Add("scim", "urn:ietf:params:scim:api:messages:2.0");
    }

        
    /// <summary>
    /// Serializes a SCIMv2 schema to XML
    /// </summary>
    public static string SerializeSchema(SchemaDefinition schema)
    {
      if (schema == null) throw new ArgumentNullException(nameof(schema));
      return Foundation.Serialization.XmlSerializer.Serialize(schema, DefaultNamespaces);
    }
    
    /// <summary>
    /// Deserializes a SCIMv2 resource from XML
    /// </summary>
    public static T DeserializeResource<T>(string xml) where T : IResource
    {
      if (string.IsNullOrWhiteSpace(xml))
        throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
      return Foundation.Serialization.XmlSerializer.Deserialize<T>(xml, DefaultNamespaces);
    }
    
  }
}
