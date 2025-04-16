using System;
using System.Collections.Generic;

using Looplex.Foundation.Entities;

using Newtonsoft.Json;

namespace Looplex.Foundation.SCIMv2.Entities;

/// <summary>
/// The service provider configuration resource enables a service provider to discover SCIM
/// specification features in a standardized form as well as provide additional
/// implementation details to clients. All attributes have a mutability of `readOnly`.
/// Unlike other core resources, the `id` attribute is not required for the service provider
/// configuration resource.
/// </summary>
public class ServiceProviderConfiguration : Actor
{
  /// <summary>
  /// A multi-valued complex type that specifies supported authentication scheme properties.
  /// To enable seamless discovery of configurations, the service provider SHOULD, with the
  /// appropriate security considerations, make the authenticationSchemes attribute publicly
  /// accessible without prior authentication.
  /// </summary>
  public AuthenticationScheme[] AuthenticationSchemes { get; set; } = [];

  /// <summary>
  /// A complex type that specifies bulk configuration options.  See Section 3.7 of [RFC7644].
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

  [JsonIgnore] public virtual List<ResourceMap> Map { get; private set; } = new();
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
/// </summary>
public class Bulk
{
  /// <summary>
  /// An integer value specifying the maximum number of operations.
  /// </summary>
  public double MaxOperations { get; set; }

  /// <summary>
  /// An integer value specifying the maximum payload size in bytes.
  /// </summary>
  public double MaxPayloadSize { get; set; }

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
/// </summary>
public class Filter
{
  /// <summary>
  /// An integer value specifying the maximum number of resources returned in a response.
  /// </summary>
  public double MaxResults { get; set; }

  /// <summary>
  /// A Boolean value specifying whether or not the operation is supported.
  /// </summary>
  public bool Supported { get; set; }
}

/// <summary>
/// A complex type that specifies PATCH configuration options.
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