using System;
using System.IO;
using Looplex.Foundation.Entities;
using ProtoBuf;

namespace Looplex.Foundation.Serialization
{
    /// <summary>
    /// Generic Protobuf serializer for Foundation entities
    /// Provides generic Protobuf serialization without domain-specific dependencies
    /// </summary>
    public static class ProtobufSerializer
    {
        #region Generic Serialization

        /// <summary>
        /// Serializes any object to Protobuf binary format
        /// </summary>
        /// <typeparam name="T">Type to serialize</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <returns>Protobuf binary data</returns>
        public static byte[] Serialize<T>(T obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            using var memoryStream = new MemoryStream();
            Serializer.Serialize(memoryStream, obj);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Deserializes Protobuf binary data to any object
        /// </summary>
        /// <typeparam name="T">Type to deserialize</typeparam>
        /// <param name="data">Protobuf binary data</param>
        /// <returns>Deserialized object</returns>
        public static T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Binary data cannot be null or empty.", nameof(data));

            using var memoryStream = new MemoryStream(data);
            return Serializer.Deserialize<T>(memoryStream);
        }

        /// <summary>
        /// Deserializes Protobuf binary data to any object by type
        /// </summary>
        /// <param name="data">Protobuf binary data</param>
        /// <param name="type">Type to deserialize</param>
        /// <returns>Deserialized object</returns>
        public static object Deserialize(byte[] data, Type type)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Binary data cannot be null or empty.", nameof(data));

            using var memoryStream = new MemoryStream(data);
            return Serializer.Deserialize(type, memoryStream);
        }

        #endregion

        #region Actor Serialization (Foundation Entities)

        /// <summary>
        /// Serializes an Actor entity to Protobuf binary format
        /// </summary>
        /// <param name="actor">Actor to serialize</param>
        /// <returns>Protobuf binary data</returns>
        public static byte[] SerializeActor(Actor actor)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            return Serialize(actor);
        }

        /// <summary>
        /// Deserializes Protobuf binary data to an Actor entity
        /// </summary>
        /// <typeparam name="T">Actor type</typeparam>
        /// <param name="data">Protobuf binary data</param>
        /// <returns>Deserialized actor</returns>
        public static T DeserializeActor<T>(byte[] data) where T : Actor
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Binary data cannot be null or empty.", nameof(data));

            return Deserialize<T>(data);
        }

        /// <summary>
        /// Deserializes Protobuf binary data to an Actor entity by type
        /// </summary>
        /// <param name="data">Protobuf binary data</param>
        /// <param name="type">Actor type</param>
        /// <returns>Deserialized actor</returns>
        public static Actor DeserializeActor(byte[] data, Type type)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Binary data cannot be null or empty.", nameof(data));

            if (!typeof(Actor).IsAssignableFrom(type))
                throw new ArgumentException($"Type {type.Name} must inherit from {nameof(Actor)}.", nameof(type));

            return (Actor)Deserialize(data, type);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the content type for Protobuf
        /// </summary>
        public static string ContentType => "application/x-protobuf";

        /// <summary>
        /// Gets the file extension for Protobuf
        /// </summary>
        public static string FileExtension => ".protobuf";

        #endregion
    }
}
