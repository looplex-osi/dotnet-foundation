using System;

namespace Looplex.Foundation.Core.SCIMv2.Entities;

/// <summary>
/// Default implementation of IServiceNameProvider that returns a configured service name.
/// 
/// Implements RFC 7643 (SCIM Schema Definition) for custom schema URI generation
/// [RFC 7643](https://datatracker.ietf.org/doc/html/rfc7643) - SCIM Schema Definition
/// 
/// RFC Compliance:
/// - RFC 7643 Section 2.1 - Schema URI Structure
/// - RFC 7643 Section 2.2 - Schema Extension
/// - RFC 7643 Section 3.1 - Resource Representation
/// - RFC 7643 Section 3.2 - Common Attributes
/// 
/// Enables multi-tenant SCIM schema support by providing service-specific schema URIs:
/// - Default: urn:ietf:params:scim:schemas:core:2.0:User
/// - Custom: urn:looplex:params:scim:schemas:{serviceName}:2.0:User
/// </summary>
public class ServiceNameProvider : IServiceNameProvider
{
  private readonly string _serviceName;

  /// <summary>
  /// Initializes a new instance of ServiceNameProvider with the specified service name.
  /// </summary>
  /// <param name="serviceName">The service name (e.g., "notejam", "case-management")</param>
  public ServiceNameProvider(string serviceName)
  {
    _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
  }

  /// <summary>
  /// Gets the service name for the current application.
  /// </summary>
  /// <returns>The configured service name</returns>
  public string GetServiceName() => _serviceName;
}
