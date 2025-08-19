using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Looplex.Foundation.Serialization.Json
{
  /// <summary>
  /// Central JSON (de)serializer (camelCase by default). Works for objects and collections.
  /// Use [JsonPropertyName("Resource"|"Resources")] on models to keep PascalCase when needed.
  /// </summary>
  public static class JsonSerializerFoundation
  {
    private static readonly JsonSerializerOptions Default = CreateDefault();
    private static JsonSerializerOptions CreateDefault() => new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true
    };

    public static string Serialize<T>(T value)
    {
      if (value == null) throw new ArgumentNullException(nameof(value));
      return JsonSerializer.Serialize(value, value!.GetType(), Default);
    }

    public static T? Deserialize<T>(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      return JsonSerializer.Deserialize<T>(json, Default);
    }

    public static object? Deserialize(string json, Type type)
    {
      if (type == null) throw new ArgumentNullException(nameof(type));
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      return JsonSerializer.Deserialize(json, type, Default);
    }
  }
}
