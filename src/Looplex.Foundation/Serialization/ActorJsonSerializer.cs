using System;

using Looplex.Foundation.Entities;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Looplex.Foundation.Serialization.Json;

public static class ActorJsonSerializer
{
  // Reuse a single resolver to avoid per-call allocations; preserves explicitly specified names
  // via [JsonProperty("...")] while applying camelCase to others.
  private static readonly IContractResolver s_CamelCasePreserveSpecifiedResolver =
    new DefaultContractResolver
    {
      NamingStrategy = new CamelCaseNamingStrategy
      {
        ProcessDictionaryKeys = true,
        OverrideSpecifiedNames = false
      }
    };

  // Static settings for serialization to reduce allocations and centralize the naming policy.
  private static readonly JsonSerializerSettings s_SerializeSettings = new()
  {
    ContractResolver = s_CamelCasePreserveSpecifiedResolver,
    Formatting = Formatting.Indented
  };

  // Static settings for deserialization with the same naming policy.
  private static readonly JsonSerializerSettings s_DeserializeSettings = new()
  {
    ContractResolver = s_CamelCasePreserveSpecifiedResolver
  };

  /// <summary>
  /// Serializes an Actor (or derived) using camelCase while honoring explicitly specified names.
  /// </summary>
  public static string Serialize<T>(this T actor) where T : Actor?
  {
    if (actor == null)
      throw new ArgumentNullException(nameof(actor));

    return JsonConvert.SerializeObject(actor, actor.GetType(), s_SerializeSettings);
  }

  /// <summary>
  /// Deserializes JSON into the given Actor-derived type using the configured naming policy.
  /// </summary>
  public static T? Deserialize<T>(this string json) where T : Actor
  {
    return (T?)Deserialize(json, typeof(T));
  }

  /// <summary>
  /// Deserializes JSON into the given Actor-derived type using the configured naming policy.
  /// </summary>
  public static object? Deserialize(this string json, Type type)
  {
    if (!typeof(Actor).IsAssignableFrom(type))
      throw new Exception($"Type {type.Name} must inherit from {nameof(Actor)}.");

    if (string.IsNullOrWhiteSpace(json))
      throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

    return JsonConvert.DeserializeObject(json, type, s_DeserializeSettings);
  }
}
