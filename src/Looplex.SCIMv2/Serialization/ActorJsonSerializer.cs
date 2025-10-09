using System;

using Looplex.Foundation.Serialization;
using Looplex.SCIMv2.Entities;

namespace Looplex.SCIMv2.Serialization
{
  public static class ActorJsonSerializer
  {
    /// <summary>
    /// Serializes a SCIMv2 schema to JSON
    /// </summary>
    public static string SerializeSchema(SchemaDefinition schema)
    {
      if (schema == null) throw new ArgumentNullException(nameof(schema));
      return FoundationJsonSerializer.Serialize(schema, FoundationJsonSerializer.DefaultOptions);
    }
    
    /// <summary>
    /// Deserializes a SCIMv2 resource from JSON
    /// </summary>
    public static T DeserializeResource<T>(string json) where T : IResource
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      return FoundationJsonSerializer.Deserialize<T>(json, FoundationJsonSerializer.DefaultOptions);
    }
    
  }
}
