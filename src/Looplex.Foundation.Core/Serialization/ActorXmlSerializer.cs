using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Looplex.Foundation.Core.Entities;
using Looplex.Foundation.Core.SCIMv2.Entities;

namespace Looplex.Foundation.Core.Serialization
{
  public static class ActorXmlSerializer
  {
    // Namespaces centralizados para SCIMv2
    private static readonly XmlSerializerNamespaces DefaultNamespaces = new();
    
    static ActorXmlSerializer()
    {
      DefaultNamespaces.Add("scim", "urn:ietf:params:scim:api:messages:2.0");
    }

    #region Actor Serialization (Existing - Maintain Compatibility)
    
    public static string Serialize(Actor actor)
    {
      if (actor == null) throw new ArgumentNullException(nameof(actor));
      var serializer = new XmlSerializer(actor.GetType());
      using var writer = new StringWriter();
      serializer.Serialize(writer, actor);
      return writer.ToString();
    }

    public static T Deserialize<T>(string xml) where T : Actor
    {
      if (string.IsNullOrWhiteSpace(xml))
        throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
      var serializer = new XmlSerializer(typeof(T));
      using var reader = new StringReader(xml);
      var result = (T)serializer.Deserialize(reader);
      if (result == null)
        throw new InvalidOperationException($"Failed to deserialize XML to {typeof(T).Name}");
      return result;
    }
    
    #endregion

    #region SCIMv2 Serialization (New - Centralized Logic)
    
    /// <summary>
    /// Serializes a SCIMv2 resource to XML
    /// </summary>
    public static string SerializeResource<T>(T resource) where T : IResource
    {
      if (resource == null) throw new ArgumentNullException(nameof(resource));
      var serializer = new XmlSerializer(typeof(T));
      using var writer = new StringWriter();
      serializer.Serialize(writer, resource, DefaultNamespaces);
      return writer.ToString();
    }
    
    /// <summary>
    /// Serializes a SCIMv2 response to XML
    /// </summary>
    public static string SerializeResponse(SCIMv2Response response)
    {
      if (response == null) throw new ArgumentNullException(nameof(response));
      var serializer = new XmlSerializer(typeof(SCIMv2Response));
      using var writer = new StringWriter();
      serializer.Serialize(writer, response, DefaultNamespaces);
      return writer.ToString();
    }
    
    /// <summary>
    /// Serializes a SCIMv2 schema to XML
    /// </summary>
    public static string SerializeSchema(SchemaDefinition schema)
    {
      if (schema == null) throw new ArgumentNullException(nameof(schema));
      var serializer = new XmlSerializer(typeof(SchemaDefinition));
      using var writer = new StringWriter();
      serializer.Serialize(writer, schema, DefaultNamespaces);
      return writer.ToString();
    }
    
    /// <summary>
    /// Serializes a collection of SCIMv2 resources to XML
    /// </summary>
    public static string SerializeResources<T>(IEnumerable<T> resources) where T : IResource
    {
      if (resources == null) throw new ArgumentNullException(nameof(resources));
      var serializer = new XmlSerializer(typeof(List<T>));
      using var writer = new StringWriter();
      serializer.Serialize(writer, resources.ToList(), DefaultNamespaces);
      return writer.ToString();
    }
    
    /// <summary>
    /// Serializes a SCIMv2 error to XML
    /// </summary>
    public static string SerializeError(SCIMv2Error error)
    {
      if (error == null) throw new ArgumentNullException(nameof(error));
      var serializer = new XmlSerializer(typeof(SCIMv2Error));
      using var writer = new StringWriter();
      serializer.Serialize(writer, error, DefaultNamespaces);
      return writer.ToString();
    }
    
    /// <summary>
    /// Deserializes a SCIMv2 resource from XML
    /// </summary>
    public static T DeserializeResource<T>(string xml) where T : IResource
    {
      if (string.IsNullOrWhiteSpace(xml))
        throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
      var serializer = new XmlSerializer(typeof(T));
      using var reader = new StringReader(xml);
      var result = (T)serializer.Deserialize(reader);
      if (result == null)
        throw new InvalidOperationException($"Failed to deserialize XML to {typeof(T).Name}");
      return result;
    }
    
    /// <summary>
    /// Deserializes a SCIMv2 response from XML
    /// </summary>
    public static SCIMv2Response DeserializeResponse(string xml)
    {
      if (string.IsNullOrWhiteSpace(xml))
        throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
      var serializer = new XmlSerializer(typeof(SCIMv2Response));
      using var reader = new StringReader(xml);
      var result = (SCIMv2Response)serializer.Deserialize(reader);
      if (result == null)
        throw new InvalidOperationException("Failed to deserialize XML to SCIMv2Response");
      return result;
    }
    
    #endregion
  }
}
