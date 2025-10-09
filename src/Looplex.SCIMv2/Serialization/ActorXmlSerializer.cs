using System;
using System.Collections.Generic;
using System.IO;
using Looplex.SCIMv2.Entities;
using Looplex.Foundation.Entities;

namespace Looplex.SCIMv2.Serialization
{
  public static class ActorXmlSerializer
  {
        
    /// <summary>
    /// Serializes a SCIMv2 schema to XML
    /// </summary>
    public static string SerializeSchema(SchemaDefinition schema)
    {
      if (schema == null) throw new ArgumentNullException(nameof(schema));
      var serializer = new System.Xml.Serialization.XmlSerializer(typeof(SchemaDefinition));
      using var writer = new StringWriter();
      serializer.Serialize(writer, schema);
      return writer.ToString();
    }
    
    /// <summary>
    /// Deserializes a SCIMv2 resource from XML
    /// </summary>
    public static T DeserializeResource<T>(string xml) where T : Actor
    {
      if (string.IsNullOrWhiteSpace(xml))
        throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
      return Foundation.Serialization.FoundationXmlSerializer.Deserialize<T>(xml);
    }
    
  }
}
