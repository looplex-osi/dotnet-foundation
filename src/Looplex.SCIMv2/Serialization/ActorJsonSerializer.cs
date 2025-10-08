using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Looplex.Foundation.Entities;
using Looplex.Foundation.Serialization;
using Looplex.SCIMv2.Entities;

namespace Looplex.SCIMv2.Serialization
{
  public static class ActorJsonSerializer
  {
    // Centralized configuration for the Foundation
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true,
      PropertyNameCaseInsensitive = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.Never, // Serialize all properties
      IncludeFields = false // Only serialize properties, not fields
    };



    #region SCIMv2 Serialization (New - Centralized Logic)
    
    /// <summary>
    /// Serializes a SCIMv2 resource to JSON
    /// </summary>
    public static string SerializeResource<T>(T resource) where T : IResource
    {
      if (resource == null) throw new ArgumentNullException(nameof(resource));
      return Foundation.Serialization.JsonSerializer.Serialize(resource, DefaultOptions);
    }
    
    /// <summary>
    /// Serializes a SCIMv2 response to JSON
    /// </summary>
    public static string SerializeResponse(SCIMv2Response response)
    {
      if (response == null) throw new ArgumentNullException(nameof(response));
      return Foundation.Serialization.JsonSerializer.Serialize(response, DefaultOptions);
    }
    
    /// <summary>
    /// Serializes a SCIMv2 schema to JSON
    /// </summary>
    public static string SerializeSchema(SchemaDefinition schema)
    {
      if (schema == null) throw new ArgumentNullException(nameof(schema));
      return Foundation.Serialization.JsonSerializer.Serialize(schema, DefaultOptions);
    }
    
    /// <summary>
    /// Serializes a collection of SCIMv2 resources to JSON
    /// </summary>
    public static string SerializeResources<T>(IEnumerable<T> resources) where T : IResource
    {
      if (resources == null) throw new ArgumentNullException(nameof(resources));
      return Foundation.Serialization.JsonSerializer.Serialize(resources, DefaultOptions);
    }
    
    /// <summary>
    /// Serializes a SCIMv2 error to JSON
    /// </summary>
    public static string SerializeError(SCIMv2Error error)
    {
      if (error == null) throw new ArgumentNullException(nameof(error));
      return Foundation.Serialization.JsonSerializer.Serialize(error, DefaultOptions);
    }
    
    /// <summary>
    /// Deserializes a SCIMv2 resource from JSON
    /// </summary>
    public static T DeserializeResource<T>(string json) where T : IResource
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      return Foundation.Serialization.JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }
    
    /// <summary>
    /// Deserializes a SCIMv2 response from JSON
    /// </summary>
    public static SCIMv2Response DeserializeResponse(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      return Foundation.Serialization.JsonSerializer.Deserialize<SCIMv2Response>(json, DefaultOptions);
    }
    
    #endregion
  }
}
