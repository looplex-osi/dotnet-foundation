using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Looplex.Foundation.Entities;

namespace Looplex.Foundation.Serialization
{
    /// <summary>
    /// Generic XML serializer for Foundation entities
    /// Provides generic XML serialization without domain-specific dependencies
    /// </summary>
    public static class XmlSerializer
    {
        /// <summary>
        /// Default XML namespaces for Foundation serialization
        /// </summary>
        private static readonly XmlSerializerNamespaces DefaultNamespaces = new();

        static XmlSerializer()
        {
            DefaultNamespaces.Add("foundation", "urn:looplex:foundation:serialization");
        }

        #region Generic Serialization

        /// <summary>
        /// Serializes any object to XML
        /// </summary>
        /// <typeparam name="T">Type to serialize</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <param name="namespaces">XML namespaces (optional)</param>
        /// <returns>XML string</returns>
        public static string Serialize<T>(T obj, XmlSerializerNamespaces? namespaces = null)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj, namespaces ?? DefaultNamespaces);
            return writer.ToString();
        }

        /// <summary>
        /// Deserializes XML to any object
        /// </summary>
        /// <typeparam name="T">Type to deserialize</typeparam>
        /// <param name="xml">XML string</param>
        /// <returns>Deserialized object</returns>
        public static T Deserialize<T>(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
            
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            var result = (T)serializer.Deserialize(reader);
            if (result == null)
                throw new InvalidOperationException($"Failed to deserialize XML to {typeof(T).Name}");
            
            return result;
        }

        /// <summary>
        /// Serializes a collection of objects to XML
        /// </summary>
        /// <typeparam name="T">Type of objects in collection</typeparam>
        /// <param name="objects">Collection to serialize</param>
        /// <param name="namespaces">XML namespaces (optional)</param>
        /// <returns>XML string</returns>
        public static string SerializeCollection<T>(IEnumerable<T> objects, XmlSerializerNamespaces? namespaces = null)
        {
            if (objects == null) throw new ArgumentNullException(nameof(objects));
            
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<T>));
            using var writer = new StringWriter();
            serializer.Serialize(writer, objects.ToList(), namespaces ?? DefaultNamespaces);
            return writer.ToString();
        }

        #endregion

        #region Actor Serialization (Foundation Entities)

        /// <summary>
        /// Serializes an Actor entity to XML
        /// </summary>
        /// <param name="actor">Actor to serialize</param>
        /// <param name="namespaces">XML namespaces (optional)</param>
        /// <returns>XML string</returns>
        public static string SerializeActor(Actor actor, XmlSerializerNamespaces? namespaces = null)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            
            var serializer = new System.Xml.Serialization.XmlSerializer(actor.GetType());
            using var writer = new StringWriter();
            serializer.Serialize(writer, actor, namespaces ?? DefaultNamespaces);
            return writer.ToString();
        }

        /// <summary>
        /// Deserializes XML to an Actor entity
        /// </summary>
        /// <typeparam name="T">Actor type</typeparam>
        /// <param name="xml">XML string</param>
        /// <returns>Deserialized actor</returns>
        public static T DeserializeActor<T>(string xml) where T : Actor
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
            
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            var result = (T)serializer.Deserialize(reader);
            if (result == null)
                throw new InvalidOperationException($"Failed to deserialize XML to {typeof(T).Name}");
            
            return result;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the content type for XML
        /// </summary>
        public static string ContentType => "application/xml";

        /// <summary>
        /// Gets the file extension for XML
        /// </summary>
        public static string FileExtension => ".xml";

        #endregion
    }
}
