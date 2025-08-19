using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Looplex.Foundation.Serialization.Json
{
  /// <summary>
  /// Centralized JSON (de)serializer using Newtonsoft.Json for legacy/actor scenarios.
  /// - Uses camelCase by default, but preserves explicitly specified names (e.g., "Resource"/"Resources")
  ///   because overrideSpecifiedNames = false.
  /// - Reuses cached JsonSerializerSettings to avoid per-call allocations on hot paths.
  /// - Always serializes with the runtime type to avoid lossy polymorphism.
  /// </summary>
  public static class ActorJsonSerializer
  {
    // Cached, thread-safe settings (ContractResolver and Settings are immutable after creation).
    private static readonly JsonSerializerSettings DefaultSettings = new()
    {
      ContractResolver = new DefaultContractResolver
      {
        NamingStrategy = new CamelCaseNamingStrategy(
          processDictionaryKeys: true,
          overrideSpecifiedNames: false // Preserve explicit PascalCase like "Resource"/"Resources"
        )
      },
      NullValueHandling = NullValueHandling.Ignore, // SCIM-style: omit nulls
      Formatting = Formatting.Indented               // Human-readable by default; change if needed
    };
    /// <summary>
    /// Serializes an object/collection to JSON string using the runtime type.
    /// </summary>
    public static string Serialize<T>(this T actor) where T : class
    {
      if (actor == null) throw new ArgumentNullException(nameof(actor));
      return JsonConvert.SerializeObject(actor, actor.GetType(), DefaultSettings);
    }
    /// <summary>
    /// Serializes a non-generic object (helper overload).
    /// </summary>
    public static string SerializeObject(object instance)
    {
      if (instance == null) throw new ArgumentNullException(nameof(instance));
      return JsonConvert.SerializeObject(instance, instance.GetType(), DefaultSettings);
    }

    /// <summary>
    /// Deserializes a JSON string into the specified generic type.
    /// </summary>
    public static T? Deserialize<T>(this string json) where T : class
    {
      return (T?)Deserialize(json, typeof(T));
    }

    /// <summary>
    /// Deserializes a JSON string into an object of the given runtime type.
    /// </summary>
    public static object? Deserialize(this string json, Type type)
    {
      if (type == null) throw new ArgumentNullException(nameof(type));
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

      return JsonConvert.DeserializeObject(json, type, DefaultSettings);
    }
  }
}
