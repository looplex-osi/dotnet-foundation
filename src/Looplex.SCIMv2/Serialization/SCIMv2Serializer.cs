using System;
using Looplex.SCIMv2.Entities;

namespace Looplex.SCIMv2.Serialization
{
    /// <summary>
    /// Centralized SCIMv2 serialization entry point
    /// Provides unified access to JSON and XML serialization for SCIMv2 entities
    /// </summary>
    public static class SCIMv2Serializer
    {
        /// <summary>
        /// Supported content types for SCIMv2 serialization
        /// </summary>
        public enum ContentType
        {
            Json,
            Xml
        }




        #region Schema Serialization

        /// <summary>
        /// Serializes a SCIMv2 schema to the specified format
        /// </summary>
        /// <param name="schema">Schema to serialize</param>
        /// <param name="contentType">Output format (default: Json)</param>
        /// <returns>Serialized schema</returns>
        public static string SerializeSchema(SchemaDefinition schema, ContentType contentType = ContentType.Json)
        {
            return contentType switch
            {
                ContentType.Json => ActorJsonSerializer.SerializeSchema(schema),
                ContentType.Xml => ActorXmlSerializer.SerializeSchema(schema),
                _ => throw new NotSupportedException($"Content type {contentType} not supported")
            };
        }

        #endregion



    }
}
