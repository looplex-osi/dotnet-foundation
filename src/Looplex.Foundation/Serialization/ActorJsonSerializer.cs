using System;

using Looplex.Foundation.Entities;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Looplex.Foundation.Serialization.Json;

public static class ActorJsonSerializer
{
  public static string Serialize<T>(this T actor) where T : Actor?
  {
    if (actor == null)
      throw new ArgumentNullException(nameof(actor));

    var resolver = new DefaultContractResolver
    {
      NamingStrategy = new CamelCaseNamingStrategy
      {
        ProcessDictionaryKeys = true,
        OverrideSpecifiedNames = false // <-- preserve explicit names
      }
    };

    JsonSerializerSettings settings = new()
    {
      ContractResolver = resolver,
      Formatting = Formatting.Indented
    };
    string json = JsonConvert.SerializeObject(actor, actor.GetType(), settings);
    return json;
  }

  public static T? Deserialize<T>(this string json) where T : Actor
  {
    return (T?)Deserialize(json, typeof(T));
  }

  public static object? Deserialize(this string json, Type type)
  {
    if (!typeof(Actor).IsAssignableFrom(type)) // Must inherit from Actor
      throw new Exception($"Type {type.Name} must inherit from {nameof(Actor)}.");

    if (string.IsNullOrWhiteSpace(json))
      throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

    var resolver = new DefaultContractResolver
    {
      NamingStrategy = new CamelCaseNamingStrategy
      {
        ProcessDictionaryKeys = true,
        OverrideSpecifiedNames = false
      }
    };

    JsonSerializerSettings settings = new() { ContractResolver = resolver };
    return JsonConvert.DeserializeObject(json, type, settings);
  }
}
