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

    #region SCIMv2 Serialization (New - Centralized Logic)
    
    /// <summary>
    /// Serializes a SCIMv2 resource to XML
    /// </summary>
    public static string SerializeResource<T>(T resource) where T : IResource
    {
      if (resource == null) throw new ArgumentNullException(nameof(resource));
      return Foundation.Serialization.XmlSerializer.Serialize(resource, DefaultNamespaces);
    }
    
    /// <summary>
    /// Serializes a SCIMv2 response to XML
    /// </summary>
    public static string SerializeResponse(SCIMv2Response response)
    {
      if (response == null) throw new ArgumentNullException(nameof(response));
      return Foundation.Serialization.XmlSerializer.Serialize(response, DefaultNamespaces);
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
    /// Serializes a collection of SCIMv2 resources to XML
    /// </summary>
    public static string SerializeResources<T>(IEnumerable<T> resources) where T : IResource
    {
      if (resources == null) throw new ArgumentNullException(nameof(resources));
      return Foundation.Serialization.XmlSerializer.Serialize(resources, DefaultNamespaces);
    }
    
    /// <summary>
    /// Serializes a SCIMv2 error to XML
    /// </summary>
    public static string SerializeError(SCIMv2Error error)
    {
      if (error == null) throw new ArgumentNullException(nameof(error));
      return Foundation.Serialization.XmlSerializer.Serialize(error, DefaultNamespaces);
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
    
    /// <summary>
    /// Deserializes a SCIMv2 response from XML
    /// </summary>
    public static SCIMv2Response DeserializeResponse(string xml)
    {
      if (string.IsNullOrWhiteSpace(xml))
        throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
      return Foundation.Serialization.XmlSerializer.Deserialize<SCIMv2Response>(xml, DefaultNamespaces);
    }
    
    #endregion
  }
}
