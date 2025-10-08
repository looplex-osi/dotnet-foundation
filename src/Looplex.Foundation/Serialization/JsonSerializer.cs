using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Looplex.Foundation.Entities;

namespace Looplex.Foundation.Serialization
{
    /// <summary>
    /// Generic JSON serializer for Foundation entities
    /// Provides generic JSON serialization without domain-specific dependencies
    /// </summary>
    public static class JsonSerializer
    {
        /// <summary>
        /// Default JSON serialization options for the Foundation
        /// </summary>
        public static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IncludeFields = false
        };

        /// <summary>
        /// Compact JSON serialization options (no indentation)
        /// </summary>
        public static readonly JsonSerializerOptions CompactOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IncludeFields = false
        };

        #region Generic Serialization

        /// <summary>
        /// Serializes any object to JSON
        /// </summary>
        /// <typeparam name="T">Type to serialize</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <param name="options">JSON serialization options (optional)</param>
        /// <returns>JSON string</returns>
        public static string Serialize<T>(T obj, JsonSerializerOptions? options = null)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return System.Text.Json.JsonSerializer.Serialize(obj, options ?? DefaultOptions);
        }

        /// <summary>
        /// Deserializes JSON to any object
        /// </summary>
        /// <typeparam name="T">Type to deserialize</typeparam>
        /// <param name="json">JSON string</param>
        /// <param name="options">JSON serialization options (optional)</param>
        /// <returns>Deserialized object</returns>
        public static T Deserialize<T>(string json, JsonSerializerOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
            
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
            if (result is null)
                throw new JsonException($"Deserialization returned null for type {typeof(T).Name}.");
            
            return result;
        }

        /// <summary>
        /// Serializes a collection of objects to JSON
        /// </summary>
        /// <typeparam name="T">Type of objects in collection</typeparam>
        /// <param name="objects">Collection to serialize</param>
        /// <param name="options">JSON serialization options (optional)</param>
        /// <returns>JSON string</returns>
        public static string SerializeCollection<T>(IEnumerable<T> objects, JsonSerializerOptions? options = null)
        {
            if (objects == null) throw new ArgumentNullException(nameof(objects));
            return System.Text.Json.JsonSerializer.Serialize(objects, options ?? DefaultOptions);
        }

        #endregion

        #region Actor Serialization (Foundation Entities)

        /// <summary>
        /// Serializes an Actor entity to JSON
        /// </summary>
        /// <param name="actor">Actor to serialize</param>
        /// <param name="options">JSON serialization options (optional)</param>
        /// <returns>JSON string</returns>
        public static string SerializeActor(Actor actor, JsonSerializerOptions? options = null)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            return System.Text.Json.JsonSerializer.Serialize(actor, actor.GetType(), options ?? DefaultOptions);
        }

        /// <summary>
        /// Deserializes JSON to an Actor entity
        /// </summary>
        /// <typeparam name="T">Actor type</typeparam>
        /// <param name="json">JSON string</param>
        /// <param name="options">JSON serialization options (optional)</param>
        /// <returns>Deserialized actor</returns>
        public static T DeserializeActor<T>(string json, JsonSerializerOptions? options = null) where T : Actor
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));
            
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
            if (result is null)
                throw new JsonException($"Deserialization returned null for type {typeof(T).Name}.");
            
            return result;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the content type for JSON
        /// </summary>
        public static string ContentType => "application/json";

        /// <summary>
        /// Gets the file extension for JSON
        /// </summary>
        public static string FileExtension => ".json";

        #endregion
    }
}
