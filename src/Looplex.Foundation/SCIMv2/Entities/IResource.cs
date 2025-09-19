using System;

namespace Looplex.Foundation.SCIMv2.Entities;

/// <summary>
/// Base interface for SCIMv2 Resources
/// Implements RFC 7643 (SCIM Core Schema) for resource structure
/// [RFC 7643](https://datatracker.ietf.org/doc/html/rfc7643) - SCIM Core Schema
/// https://datatracker.ietf.org/doc/html/rfc7643
/// </summary>
public interface IResource
{
    /// <summary>
    /// A unique identifier for the resource
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// A string that is an identifier for the resource as defined by the provisioning client
    /// </summary>
    string? ExternalId { get; set; }

    /// <summary>
    /// Resource metadata
    /// </summary>
    ResourceMeta Meta { get; set; }

    /// <summary>
    /// A list of URIs that are used to indicate the namespaces of the SCIM schemas
    /// </summary>
    string[] Schemas { get; set; }
}
