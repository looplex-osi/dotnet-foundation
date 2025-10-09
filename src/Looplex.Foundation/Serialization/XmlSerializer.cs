using System;
using System.IO;
using System.Xml.Serialization;

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
        public static readonly XmlSerializerNamespaces DefaultNamespaces = new();

        static XmlSerializer()
        {
            DefaultNamespaces.Add("foundation", "urn:looplex:foundation:serialization");
        }

        /// <summary>
        /// Serializes any object to XML
        /// </summary>
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
        public static T Deserialize<T>(string xml, XmlSerializerNamespaces? namespaces = null)
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
    }
}
