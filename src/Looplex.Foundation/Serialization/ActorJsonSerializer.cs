using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Looplex.Foundation.Serialization.Json
{
  public static class ActorJsonSerializer
  {
    /// <summary>
    /// Serializes an Actor (or any object/collection) to JSON string.
    /// By default, properties are camelCase; explicit [JsonProperty("Resource")] is respected.
    /// </summary>
    public static string Serialize<T>(this T actor) where T : class
    {
      if (actor == null)
        throw new ArgumentNullException(nameof(actor));

      JsonSerializerSettings settings = new()
      {
        ContractResolver = new DefaultContractResolver
        {
          NamingStrategy = new CamelCaseNamingStrategy(
            processDictionaryKeys: true,
            overrideSpecifiedNames: false // Preserve explicit PascalCase like "Resource"/"Resources"
          )
        },
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
      };

      string json = JsonConvert.SerializeObject(actor, actor.GetType(), settings);
      return json;
    }

    /// <summary>
    /// Deserializes a JSON string into the specified Actor (or object) type.
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

      JsonSerializerSettings settings = new()
      {
        ContractResolver = new DefaultContractResolver
        {
          NamingStrategy = new CamelCaseNamingStrategy(
            processDictionaryKeys: true,
            overrideSpecifiedNames: false
          )
        },
        NullValueHandling = NullValueHandling.Ignore
      };

      return JsonConvert.DeserializeObject(json, type, settings);
    }
  }
}
