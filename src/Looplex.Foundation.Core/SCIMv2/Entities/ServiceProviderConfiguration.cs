using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Looplex.Foundation.Core.Entities;

using Newtonsoft.Json;

namespace Looplex.Foundation.Core.SCIMv2.Entities;

/// <summary>
/// The service provider configuration resource enables a service provider to discover SCIM
/// specification features in a standardized form as well as provide additional
/// implementation details to clients. All attributes have a mutability of `readOnly`.
/// Unlike other core resources, the `id` attribute is not required for the service provider
/// configuration resource.
/// 
/// Implements RFC 7643 (SCIM Schema Definition) and RFC 7644 (SCIM Protocol)
/// [RFC 7643](https://datatracker.ietf.org/doc/html/rfc7643) - SCIM Schema Definition
/// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644) - SCIM Protocol
/// 
/// RFC Compliance:
/// - RFC 7643 Section 5 - Service Provider Configuration Schema
/// - RFC 7644 Section 3.7 - Bulk Operations
/// - RFC 7644 Section 3.4.2.3 - Filtering
/// - RFC 7644 Section 3.4.2.4 - Sorting
/// - RFC 7644 Section 3.5.2 - PATCH Operations
/// - RFC 7644 Section 3.6 - ETag Support
/// - RFC 7644 Section 3.8 - Authentication Schemes
/// </summary>
public class ServiceProviderConfiguration : Actor
{
  // SCIM ServiceProviderConfig MUST include this schemas entry
  // [SCIM Service Provider Configuration](https://www.rfc-editor.org/rfc/rfc7643#section-5)
  public string[] Schemas { get; } = new[]
  {
    "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig"
  };
  /// <summary>
  /// A multi-valued complex type that specifies supported authentication scheme properties.
  /// To enable seamless discovery of configurations, the service provider SHOULD, with the
  /// appropriate security considerations, make the authenticationSchemes attribute publicly
  /// accessible without prior authentication.
  /// </summary>
  public AuthenticationScheme[] AuthenticationSchemes { get; set; } = [];

  /// <summary>
  /// A complex type that specifies bulk configuration options.  See Section 3.7 of [RFC7644].
  /// [RFC 7644 Section 3.7](https://datatracker.ietf.org/doc/html/rfc7644#section-3.7)
  /// </summary>
  public Bulk? Bulk { get; set; }

  /// <summary>
  /// A complex type that specifies configuration options related to changing a password.
  /// </summary>
  public ChangePassword? ChangePassword { get; set; }

  /// <summary>
  /// An HTTP-addressable URL pointing to the service provider`s human-consumable help
  /// documentation.
  /// </summary>
  public Uri? DocumentationUri { get; set; }

  /// <summary>
  /// A complex type that specifies ETag configuration options.
  /// </summary>
  public Etag? Etag { get; set; }

  /// <summary>
  /// A complex type that specifies FILTER options.
  /// </summary>
  public Filter? Filter { get; set; }

  /// <summary>
  /// A complex type that specifies PATCH configuration options.
  /// </summary>
  public Patch? Patch { get; set; }

  /// <summary>
  /// A complex type that specifies Sort configuration options.
  /// </summary>
  public Sort? Sort { get; set; }

  [Newtonsoft.Json.JsonIgnore]
  [System.Text.Json.Serialization.JsonIgnore]
  public virtual List<ResourceMap> Map { get; private set; } = new();
}

public class AuthenticationScheme
{
  /// <summary>
  /// A description of the authentication scheme.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// An HTTP-addressable URL pointing to the authentication scheme's usage documentation.
  /// </summary>
  public Uri? DocumentationUri { get; set; }

  /// <summary>
  /// The common authentication scheme name.
  /// </summary>
  public string? Name { get; set; }

  /// <summary>
  /// An HTTP-addressable URL pointing to the authentication scheme's specification.
  /// </summary>
  public Uri? SpecUri { get; set; }

  /// <summary>
  /// The authentication scheme.
  /// </summary>
  public AuthenticationSchemeType Type { get; set; }
}

/// <summary>
///    The authentication scheme.  This specification defines the values
/// "oauth", "oauth2", "oauthbearertoken", "httpbasic", and "httpdigest".
/// 
/// Implements RFC 7644 (SCIM Protocol) for authentication schemes
/// [RFC 7644 Section 3.8](https://datatracker.ietf.org/doc/html/rfc7644#section-3.8) - Authentication Schemes
/// </summary>
public enum AuthenticationSchemeType
{
  OAuth,
  OAuth2,
  OAuthBearerToken,
  HttpBasic,
  HttpDigest
}

/// <summary>
/// A complex type that specifies bulk configuration options.  See Section 3.7 of [RFC7644].
/// 
/// Implements RFC 7644 (SCIM Protocol) for bulk operations
/// [RFC 7644 Section 3.7](https://datatracker.ietf.org/doc/html/rfc7644#section-3.7) - Bulk Operations
/// </summary>
public class Bulk
{
  /// <summary>
  /// An integer value specifying the maximum number of operations.
  /// </summary>
  public int MaxOperations { get; set; }

  /// <summary>
  /// An integer value specifying the maximum payload size in bytes.
  /// </summary>
  public long MaxPayloadSize { get; set; }

  /// <summary>
  /// A Boolean value specifying whether or not the operation is supported.
  /// </summary>
  public bool Supported { get; set; }
}

/// <summary>
/// A complex type that specifies configuration options related to changing a password.
/// </summary>
public class ChangePassword
{
  /// <summary>
  /// A Boolean value specifying whether or not the operation is supported.
  /// </summary>
  public bool Supported { get; set; }
}

/// <summary>
/// A complex type that specifies ETag configuration options.
/// 
/// Implements RFC 7644 (SCIM Protocol) for ETag support
/// [RFC 7644 Section 3.6](https://datatracker.ietf.org/doc/html/rfc7644#section-3.6) - ETag Support
/// </summary>
public class Etag
{
  /// <summary>
  /// A Boolean value specifying whether or not the operation is supported.
  /// </summary>
  public bool Supported { get; set; }
}

/// <summary>
/// A complex type that specifies FILTER options.
/// 
/// Implements RFC 7644 (SCIM Protocol) for filtering capabilities
/// [RFC 7644 Section 3.4.2.3](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.3) - Filtering
/// </summary>
public class Filter
{
  /// <summary>
  /// An integer value specifying the maximum number of resources returned in a response.
  /// </summary>
  public int MaxResults { get; set; }

  /// <summary>
  /// A Boolean value specifying whether or not the operation is supported.
  /// </summary>
  public bool Supported { get; set; }
}

/// <summary>
/// A complex type that specifies PATCH configuration options.
/// 
/// Implements RFC 7644 (SCIM Protocol) for PATCH operations
/// [RFC 7644 Section 3.5.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.5.2) - PATCH Operations
/// </summary>
public class Patch
{
  /// <summary>
  /// A Boolean value specifying whether or not the operation is supported.
  /// </summary>
  public bool Supported { get; set; }
}

/// <summary>
/// A complex type that specifies Sort configuration options.
/// 
/// Implements RFC 7644 (SCIM Protocol) for sorting capabilities
/// [RFC 7644 Section 3.4.2.4](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.4) - Sorting
/// </summary>
public class Sort
{
  /// <summary>
  /// A Boolean value specifying whether or not the operation is supported.
  /// </summary>
  public bool Supported { get; set; }
}

public class ResourceMap
{
  public ResourceMap(Type type, string resource)
  {
    Type = type ?? throw new ArgumentNullException(nameof(type));
    Resource = resource ?? throw new ArgumentNullException(nameof(resource));
  }

  public Type Type { get; set; }
  public string Resource { get; set; }
}
