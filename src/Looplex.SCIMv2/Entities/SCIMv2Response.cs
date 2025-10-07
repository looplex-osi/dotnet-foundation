using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Looplex.SCIMv2.Entities;

/// <summary>
/// SCIMv2 Response wrapper for API responses
/// Provides consistent response structure for all SCIMv2 operations
/// 
/// Implements RFC 7644 (SCIM Protocol) and RFC 7643 (SCIM Schema Definition)
/// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644) - SCIM Protocol
/// [RFC 7643](https://datatracker.ietf.org/doc/html/rfc7643) - SCIM Schema Definition
/// 
/// RFC Compliance:
/// - RFC 7644 Section 3.4.1 - Create Resource Response
/// - RFC 7644 Section 3.4.2 - Query Resources Response  
/// - RFC 7644 Section 3.4.3 - Retrieve Resource Response
/// - RFC 7644 Section 3.4.4 - Update Resource Response
/// - RFC 7644 Section 3.4.5 - Delete Resource Response
/// - RFC 7644 Section 3.4.6 - Schema Discovery Response
/// - RFC 7643 Section 3.1 - Resource Representation
/// - RFC 7643 Section 3.2 - Common Attributes
/// </summary>
public class SCIMv2Response
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    [JsonPropertyName("statusCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StatusCode { get; set; }

    /// <summary>
    /// Response data (resource, list, or error)
    /// For ListResponse, this should be "Resources" per RFC 7644 Section 3.4.2
    /// For single resource operations (GET, PUT, PATCH), this should be the resource directly
    /// </summary>
    [JsonPropertyName("Resources")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    /// <summary>
    /// Indicates the HTTP method that generated this response
    /// Used to determine the correct serialization format per SCIM v2.0
    /// </summary>
    [JsonIgnore]
    public string HttpMethod { get; set; } = "GET";

    /// <summary>
    /// SCIM schemas for the response
    /// </summary>
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Error information for failed operations
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SCIMv2Error? Error { get; set; }

    /// <summary>
    /// Total number of results for list operations
    /// </summary>
    [JsonPropertyName("totalResults")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? TotalResults { get; set; }

    /// <summary>
    /// Starting index for pagination
    /// </summary>
    [JsonPropertyName("startIndex")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? StartIndex { get; set; }

    /// <summary>
    /// Number of items per page for pagination
    /// </summary>
    [JsonPropertyName("itemsPerPage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ItemsPerPage { get; set; }

    /// <summary>
    /// Resource location for HTTP headers
    /// </summary>
    [JsonPropertyName("location")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Location { get; set; }

    /// <summary>
    /// ETag for conditional requests
    /// </summary>
    [JsonPropertyName("etag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ETag { get; set; }

    #region SCIM v2.0 Response Formatting Methods

    /// <summary>
    /// Creates a SCIM v2.0 compliant response for different HTTP methods
    /// Centralizes response formatting logic per RFC 7644
    /// </summary>
    /// <param name="method">HTTP method (GET, POST, PUT, PATCH, DELETE)</param>
    /// <param name="data">Response data</param>
    /// <param name="schemas">Resource schemas</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="location">Resource location (for PUT/PATCH)</param>
    /// <param name="etag">Resource ETag (for PUT/PATCH)</param>
    /// <returns>Properly formatted SCIMv2Response</returns>
    public static SCIMv2Response CreateMethodResponse(string method, object? data, string[] schemas, int statusCode, string? location = null, string? etag = null)
    {
        var response = new SCIMv2Response
        {
            StatusCode = statusCode,
            Data = data,
            Schemas = schemas,
            HttpMethod = method,
            Location = location,
            ETag = etag
        };

        return response;
    }

    /// <summary>
    /// Creates a SCIM v2.0 compliant PUT response
    /// Returns the resource directly without "Resources" wrapper per RFC 7644 Section 3.4.4
    /// </summary>
    /// <param name="resource">Updated resource</param>
    /// <param name="schemas">Resource schemas (ignored - resource already contains schemas)</param>
    /// <param name="location">Resource location</param>
    /// <param name="etag">Resource ETag</param>
    /// <returns>PUT-formatted SCIMv2Response</returns>
    public static SCIMv2Response CreatePutResponse(object? resource, string[] schemas, string location, string etag)
    {
        return new SCIMv2Response
        {
            StatusCode = 200,
            Data = resource, // Will be serialized directly (no "Resources" wrapper)
            Schemas = Array.Empty<string>(), // Don't duplicate schemas - resource already contains them
            HttpMethod = "PUT",
            Location = location,
            ETag = etag
        };
    }

    /// <summary>
    /// Creates a SCIM v2.0 compliant POST response
    /// Returns the created resource with "Resources" wrapper per RFC 7644 Section 3.4.1
    /// </summary>
    /// <param name="resource">Created resource</param>
    /// <param name="schemas">Resource schemas</param>
    /// <param name="location">Resource location</param>
    /// <param name="etag">Resource ETag</param>
    /// <returns>POST-formatted SCIMv2Response</returns>
    public static SCIMv2Response CreatePostResponse(object? resource, string[] schemas, string location, string etag)
    {
        return new SCIMv2Response
        {
            Data = resource, // Will be wrapped in "Resources"
            Schemas = schemas,
            HttpMethod = "POST",
            Location = location,
            ETag = etag
        };
    }

    /// <summary>
    /// Creates a SCIM v2.0 compliant GET response
    /// Returns the resource directly without "Resources" wrapper per RFC 7644 Section 3.4.3
    /// </summary>
    /// <param name="resource">Retrieved resource</param>
    /// <param name="schemas">Resource schemas</param>
    /// <param name="location">Resource location</param>
    /// <param name="etag">Resource ETag</param>
    /// <returns>GET-formatted SCIMv2Response</returns>
    public static SCIMv2Response CreateGetResponse(object? resource, string[] schemas, string location, string etag)
    {
        return new SCIMv2Response
        {
            Data = resource, // Will be serialized directly (no "Resources" wrapper)
            Schemas = schemas,
            HttpMethod = "GET",
            Location = location,
            ETag = etag
        };
    }

    /// <summary>
    /// Creates a SCIM v2.0 compliant PATCH response
    /// Returns the updated resource directly without "Resources" wrapper per RFC 7644 Section 3.4.4
    /// </summary>
    /// <param name="resource">Updated resource</param>
    /// <param name="schemas">Resource schemas</param>
    /// <param name="location">Resource location</param>
    /// <param name="etag">Resource ETag</param>
    /// <returns>PATCH-formatted SCIMv2Response</returns>
    public static SCIMv2Response CreatePatchResponse(object? resource, string[] schemas, string location, string etag)
    {
        return new SCIMv2Response
        {
            Data = resource, // Will be serialized directly (no "Resources" wrapper)
            Schemas = schemas,
            HttpMethod = "PATCH",
            Location = location,
            ETag = etag
        };
    }

    /// <summary>
    /// Creates a SCIM v2.0 compliant DELETE response
    /// Returns empty response per RFC 7644 Section 3.4.5
    /// </summary>
    /// <returns>DELETE-formatted SCIMv2Response</returns>
    public static SCIMv2Response CreateDeleteResponse()
    {
        return new SCIMv2Response
        {
            Data = null,
            Schemas = Array.Empty<string>(),
            HttpMethod = "DELETE"
        };
    }

    #endregion
}

/// <summary>
/// SCIMv2 Error information for failed operations
/// </summary>
public class SCIMv2Error
{
    /// <summary>
    /// SCIM v2.0 error schemas
    /// </summary>
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = new[] { "urn:ietf:params:scim:api:messages:2.0:Error" };

    /// <summary>
    /// Error status code
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// SCIM error type
    /// </summary>
    [JsonPropertyName("scimType")]
    public string ScimType { get; set; } = string.Empty;

    /// <summary>
    /// Error type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Error detail
    /// </summary>
    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// Error timestamp for debugging
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}
