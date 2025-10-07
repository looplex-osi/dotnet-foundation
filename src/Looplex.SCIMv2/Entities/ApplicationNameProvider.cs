using System;

namespace Looplex.SCIMv2.Entities;

/// <summary>
/// Default implementation of IApplicationNameProvider that returns a configured application name.
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
/// Enables multi-tenant SCIM schema support by providing application-specific schema URIs:
/// - Default: urn:ietf:params:scim:schemas:core:2.0:User
/// - Custom: urn:looplex:params:scim:schemas:{applicationName}:2.0:User
/// </summary>
public class ApplicationNameProvider : IApplicationNameProvider
{
  private readonly string _applicationName;

  /// <summary>
  /// Initializes a new instance of ApplicationNameProvider with the specified application name.
  /// </summary>
  /// <param name="applicationName">The application name (e.g., "notejam", "case-management")</param>
  public ApplicationNameProvider(string applicationName)
  {
    _applicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
  }

  /// <summary>
  /// Gets the application name for the current application.
  /// </summary>
  /// <returns>The configured application name</returns>
  public string GetApplicationName() => _applicationName;
}
