namespace Looplex.SCIMv2.Entities;

/// <summary>
/// Provides the application name for SCIM schema generation.
/// This allows each consuming application to define its own application name.
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
public interface IApplicationNameProvider
{
  /// <summary>
  /// Gets the application name for the current application.
  /// This name is used in SCIM schema URIs like:
  /// urn:looplex:params:scim:schemas:{applicationName}:2.0:User
  /// </summary>
  /// <returns>The application name (e.g., "notejam", "case-management")</returns>
  string GetApplicationName();
}
