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
    // ===== Cached options (thread-safe, immutable after creation) =====
    private static readonly JsonSerializerOptions Default = CreateDefaultIndented();
    private static readonly JsonSerializerOptions DefaultOmitNulls = CreateDefaultIndented(omitNulls: true);
    private static readonly JsonSerializerOptions Compact = CreateDefaultIndented(indent: false);
    private static readonly JsonSerializerOptions CompactOmitNulls = CreateDefaultIndented(indent: false, omitNulls: true);

    private static JsonSerializerOptions CreateDefaultIndented(bool indent = true, bool omitNulls = false)
    {
      var opts = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = indent
      };
      if (omitNulls)
        opts.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
      return opts;
    }

    // ===== Backward-compatible API =====

    /// <summary>
    /// Serialize using default options (camelCase, indented, keeps nulls).
    /// Preserves runtime type to avoid lossy polymorphism.
    /// </summary>
    public static string Serialize<T>(T value)
    {
      if (value == null) throw new ArgumentNullException(nameof(value));
      return JsonSerializer.Serialize(value, value.GetType(), Default);
    }

    /// <summary>
    /// Deserialize using default options.
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      return JsonSerializer.Deserialize<T>(json, Default);
    }

    /// <summary>
    /// Deserialize to a provided runtime type using default options.
    /// </summary>
    public static object? Deserialize(string json, Type type)
    {
      if (type == null) throw new ArgumentNullException(nameof(type));
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
      return JsonSerializer.Deserialize(json, type, Default);
    }

    // ===== SCIM-friendly overloads =====

    /// <summary>
    /// Serialize with optional "omitNulls" and "compact" switches.
    /// - omitNulls=true: parity with SCIM representation (null attributes omitted).
    /// - compact=true: no indentation (useful for ETag hashing/log compaction).
    /// Preserves runtime type.
    /// </summary>
    public static string Serialize<T>(T value, bool omitNulls, bool compact = false)
    {
      if (value == null) throw new ArgumentNullException(nameof(value));
      var opts = GetOptions(omitNulls, compact);
      return JsonSerializer.Serialize(value, value.GetType(), opts);
    }

    /// <summary>
    /// Non-generic overload with the same behavior (preserves runtime type).
    /// </summary>
    public static string Serialize(object value, bool omitNulls = false, bool compact = false)
    {
      if (value == null) throw new ArgumentNullException(nameof(value));
      var opts = GetOptions(omitNulls, compact);
      return JsonSerializer.Serialize(value, value.GetType(), opts);
    }

    /// <summary>
    /// Helper: serialize to JsonElement with the selected options.
    /// Useful to keep the pipeline within System.Text.Json (no Newtonsoft roundtrips).
    /// </summary>
    public static JsonElement SerializeToElement(object value, bool omitNulls = false, bool compact = false)
    {
      if (value == null) throw new ArgumentNullException(nameof(value));
      var opts = GetOptions(omitNulls, compact);
      using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value, value.GetType(), opts));
      return doc.RootElement.Clone();
    }

    private static JsonSerializerOptions GetOptions(bool omitNulls, bool compact)
    {
      if (omitNulls && compact) return CompactOmitNulls;
      if (omitNulls) return DefaultOmitNulls;
      if (compact) return Compact;
      return Default;
    }
  }
}
