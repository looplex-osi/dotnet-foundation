using System;
using System.IO;
using System.Xml.Serialization;

using Looplex.Foundation.Entities;

namespace Looplex.Foundation.Serialization
{
    /// <summary>
    /// Generic XML serializer for Foundation entities
    /// Provides generic XML serialization without domain-specific dependencies
    /// </summary>
    public static class FoundationXmlSerializer
    {
        /// <summary>
        /// Default XML namespaces for serialization
        /// </summary>
        public static readonly XmlSerializerNamespaces DefaultNamespaces = new();

        static FoundationXmlSerializer()
        {
            DefaultNamespaces.Add("", "");
        }

        /// <summary>
        /// Serializes any object to XML
        /// </summary>
        public static string Serialize(Actor actor, XmlSerializerNamespaces? namespaces = null)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            
            var serializer = new XmlSerializer(actor.GetType());
            using var writer = new StringWriter();
            serializer.Serialize(writer, actor, namespaces ?? DefaultNamespaces);
            return writer.ToString();
        }

        /// <summary>
        /// Deserializes XML to any object
        /// </summary>
        public static T Deserialize<T>(string xml, XmlSerializerNamespaces? namespaces = null) where T : Actor
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentException("XML string cannot be null or empty.", nameof(xml));
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            return (T)serializer.Deserialize(reader);
        }
    }
}
