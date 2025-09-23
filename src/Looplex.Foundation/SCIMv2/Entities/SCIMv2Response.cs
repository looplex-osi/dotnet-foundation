using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Looplex.Foundation.SCIMv2.Entities;

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
    public int StatusCode { get; set; }

    /// <summary>
    /// Response data (resource, list, or error)
    /// For ListResponse, this should be "Resources" per RFC 7644 Section 3.4.2
    /// </summary>
    [JsonPropertyName("Resources")]
    public object? Data { get; set; }

    /// <summary>
    /// SCIM schemas for the response
    /// </summary>
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Error information if applicable
    /// </summary>
    [JsonPropertyName("error")]
    public SCIMv2Error? Error { get; set; }

    /// <summary>
    /// Total results for list responses
    /// </summary>
    [JsonPropertyName("totalResults")]
    public long? TotalResults { get; set; }

    /// <summary>
    /// Start index for pagination
    /// </summary>
    [JsonPropertyName("startIndex")]
    public long? StartIndex { get; set; }

    /// <summary>
    /// Items per page for pagination
    /// </summary>
    [JsonPropertyName("itemsPerPage")]
    public long? ItemsPerPage { get; set; }

    /// <summary>
    /// Location header for created resources
    /// </summary>
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    /// <summary>
    /// ETag for resource versioning
    /// </summary>
    [JsonPropertyName("etag")]
    public string? ETag { get; set; }
}

/// <summary>
/// SCIMv2 Error information
/// Implements RFC 7644 (SCIM Protocol) error response structure
/// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644) - SCIM Protocol
/// 
/// RFC Compliance:
/// - RFC 7644 Section 3.12 - Error Response
/// - RFC 7644 Section 3.12.1 - Error Response Format
/// - RFC 7644 Section 3.12.2 - Error Response Attributes
/// </summary>
public class SCIMv2Error
{
    /// <summary>
    /// Error schemas
    /// </summary>
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = new[] { "urn:ietf:params:scim:api:messages:2.0:Error" };

    /// <summary>
    /// Error status
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error scim type
    /// </summary>
    [JsonPropertyName("scimType")]
    public string? ScimType { get; set; }

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

