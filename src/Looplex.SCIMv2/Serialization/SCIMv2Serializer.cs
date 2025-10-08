using System;
using System.Collections.Generic;
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

        #region Resource Serialization

        /// <summary>
        /// Serializes a SCIMv2 resource to the specified format
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="resource">Resource to serialize</param>
        /// <param name="contentType">Output format (default: Json)</param>
        /// <returns>Serialized resource</returns>
        public static string SerializeResource<T>(T resource, ContentType contentType = ContentType.Json) 
            where T : IResource
        {
            return contentType switch
            {
                ContentType.Json => ActorJsonSerializer.SerializeResource(resource),
                ContentType.Xml => ActorXmlSerializer.SerializeResource(resource),
                _ => throw new NotSupportedException($"Content type {contentType} not supported")
            };
        }



        /// <summary>
        /// Deserializes a SCIMv2 resource from the specified format
        /// </summary>
        /// <typeparam name="T">Resource type implementing IResource</typeparam>
        /// <param name="content">Serialized content</param>
        /// <param name="contentType">Input format (default: Json)</param>
        /// <returns>Deserialized resource</returns>
        public static T DeserializeResource<T>(string content, ContentType contentType = ContentType.Json) 
            where T : IResource
        {
            return contentType switch
            {
                ContentType.Json => ActorJsonSerializer.DeserializeResource<T>(content),
                ContentType.Xml => ActorXmlSerializer.DeserializeResource<T>(content),
                _ => throw new NotSupportedException($"Content type {contentType} not supported")
            };
        }

        #endregion

        #region Response Serialization

        /// <summary>
        /// Serializes a SCIMv2 response to the specified format
        /// </summary>
        /// <param name="response">Response to serialize</param>
        /// <param name="contentType">Output format (default: Json)</param>
        /// <returns>Serialized response</returns>
        public static string SerializeResponse(SCIMv2Response response, ContentType contentType = ContentType.Json)
        {
            return contentType switch
            {
                ContentType.Json => ActorJsonSerializer.SerializeResponse(response),
                ContentType.Xml => ActorXmlSerializer.SerializeResponse(response),
                _ => throw new NotSupportedException($"Content type {contentType} not supported")
            };
        }

        /// <summary>
        /// Deserializes a SCIMv2 response from the specified format
        /// </summary>
        /// <param name="content">Serialized content</param>
        /// <param name="contentType">Input format (default: Json)</param>
        /// <returns>Deserialized response</returns>
        public static SCIMv2Response DeserializeResponse(string content, ContentType contentType = ContentType.Json)
        {
            return contentType switch
            {
                ContentType.Json => ActorJsonSerializer.DeserializeResponse(content),
                ContentType.Xml => ActorXmlSerializer.DeserializeResponse(content),
                _ => throw new NotSupportedException($"Content type {contentType} not supported")
            };
        }

        #endregion

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

        #region Error Serialization

        /// <summary>
        /// Serializes a SCIMv2 error to the specified format
        /// </summary>
        /// <param name="error">Error to serialize</param>
        /// <param name="contentType">Output format (default: Json)</param>
        /// <returns>Serialized error</returns>
        public static string SerializeError(SCIMv2Error error, ContentType contentType = ContentType.Json)
        {
            return contentType switch
            {
                ContentType.Json => ActorJsonSerializer.SerializeError(error),
                ContentType.Xml => ActorXmlSerializer.SerializeError(error),
                _ => throw new NotSupportedException($"Content type {contentType} not supported")
            };
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the MIME type for the specified content type
        /// </summary>
        /// <param name="contentType">Content type</param>
        /// <returns>MIME type string</returns>
        public static string GetMimeType(ContentType contentType)
        {
            return contentType switch
            {
                ContentType.Json => "application/json",
                ContentType.Xml => "application/xml",
                _ => throw new NotSupportedException($"Content type {contentType} not supported")
            };
        }

        /// <summary>
        /// Gets the file extension for the specified content type
        /// </summary>
        /// <param name="contentType">Content type</param>
        /// <returns>File extension</returns>
        public static string GetFileExtension(ContentType contentType)
        {
            return contentType switch
            {
                ContentType.Json => ".json",
                ContentType.Xml => ".xml",
                _ => throw new NotSupportedException($"Content type {contentType} not supported")
            };
        }

        #endregion
    }
}
