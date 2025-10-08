using System;

namespace Looplex.Foundation.Serialization
{
    /// <summary>
    /// Base interface for all serializers in the Foundation
    /// Provides a common contract for serialization operations
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes an object to string representation
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <returns>Serialized string</returns>
        string Serialize<T>(T obj);

        /// <summary>
        /// Deserializes a string to an object
        /// </summary>
        /// <typeparam name="T">Type of object to deserialize</typeparam>
        /// <param name="content">Serialized content</param>
        /// <returns>Deserialized object</returns>
        T Deserialize<T>(string content);

        /// <summary>
        /// Serializes a collection of objects
        /// </summary>
        /// <typeparam name="T">Type of objects in collection</typeparam>
        /// <param name="objects">Collection to serialize</param>
        /// <returns>Serialized collection</returns>
        string SerializeCollection<T>(System.Collections.Generic.IEnumerable<T> objects);

        /// <summary>
        /// Gets the content type this serializer handles
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Gets the file extension for this serializer
        /// </summary>
        string FileExtension { get; }
    }
}
