using System;
using System.Text.Json;

namespace Looplex.Foundation.Serialization
{
    /// <summary>
    /// Generic JSON serializer for Foundation entities
    /// Provides generic JSON serialization without domain-specific dependencies
    /// </summary>
    public static class JsonSerializer
    {
        /// <summary>
        /// Default JSON options (camelCase, case-insensitive, includes fields)
        /// </summary>
        public static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };

        /// <summary>
        /// Serializes any object to a JSON string.
        /// </summary>
        public static string Serialize<T>(T obj, JsonSerializerOptions? options = null)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return System.Text.Json.JsonSerializer.Serialize(obj, options ?? DefaultOptions);
        }

        /// <summary>
        /// Deserializes a JSON string into the specified type.
        /// </summary>
        public static T Deserialize<T>(string json, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions)
               ?? throw new JsonException($"Deserialization returned null for type {typeof(T).Name}.");

    }
}
