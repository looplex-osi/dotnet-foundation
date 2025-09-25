using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using Looplex.Foundation.Entities;
using Looplex.Foundation.SCIMv2.Entities;

namespace Looplex.Foundation.Serialization
{
  public static class ActorJsonSerializer
  {
    // Configuração centralizada para toda a Foundation
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true,
      PropertyNameCaseInsensitive = true
    };

    #region Actor Serialization (Existing - Maintain Compatibility)
    
    public static string Serialize(Actor actor)
    {
      if (actor == null) throw new ArgumentNullException(nameof(actor));
      return JsonSerializer.Serialize(actor, actor.GetType(), DefaultOptions);
    }

    public static T Deserialize<T>(string json) where T : Actor
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      var result = JsonSerializer.Deserialize<T>(json, DefaultOptions);
      if (result is null)
        throw new JsonException($"Deserialization returned null for type {typeof(T).Name}.");
      return result;
    }
    
    #endregion

    #region SCIMv2 Serialization (New - Centralized Logic)
    
    /// <summary>
    /// Serializes a SCIMv2 resource to JSON
    /// </summary>
    public static string SerializeResource<T>(T resource) where T : IResource
    {
      if (resource == null) throw new ArgumentNullException(nameof(resource));
      return JsonSerializer.Serialize(resource, DefaultOptions);
    }
    
    /// <summary>
    /// Serializes a SCIMv2 response to JSON
    /// </summary>
    public static string SerializeResponse(SCIMv2Response response)
    {
      if (response == null) throw new ArgumentNullException(nameof(response));
      return JsonSerializer.Serialize(response, DefaultOptions);
    }
    
    /// <summary>
    /// Serializes a SCIMv2 schema to JSON
    /// </summary>
    public static string SerializeSchema(SchemaDefinition schema)
    {
      if (schema == null) throw new ArgumentNullException(nameof(schema));
      return JsonSerializer.Serialize(schema, DefaultOptions);
    }
    
    /// <summary>
    /// Serializes a collection of SCIMv2 resources to JSON
    /// </summary>
    public static string SerializeResources<T>(IEnumerable<T> resources) where T : IResource
    {
      if (resources == null) throw new ArgumentNullException(nameof(resources));
      return JsonSerializer.Serialize(resources, DefaultOptions);
    }
    
    /// <summary>
    /// Serializes a SCIMv2 error to JSON
    /// </summary>
    public static string SerializeError(SCIMv2Error error)
    {
      if (error == null) throw new ArgumentNullException(nameof(error));
      return JsonSerializer.Serialize(error, DefaultOptions);
    }
    
    /// <summary>
    /// Deserializes a SCIMv2 resource from JSON
    /// </summary>
    public static T DeserializeResource<T>(string json) where T : IResource
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      var result = JsonSerializer.Deserialize<T>(json, DefaultOptions);
      if (result is null)
        throw new JsonException($"Deserialization returned null for type {typeof(T).Name}.");
      return result;
    }
    
    /// <summary>
    /// Deserializes a SCIMv2 response from JSON
    /// </summary>
    public static SCIMv2Response DeserializeResponse(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      var result = JsonSerializer.Deserialize<SCIMv2Response>(json, DefaultOptions);
      if (result is null)
        throw new JsonException("Deserialization returned null for SCIMv2Response.");
      return result;
    }
    
    #endregion
  }
}