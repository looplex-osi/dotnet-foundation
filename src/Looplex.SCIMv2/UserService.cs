using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2.Ports;

namespace Looplex.SCIMv2;

/// <summary>
/// SCIM v2.0 User resource management service implementation.
/// Inherits from BaseResourceService to eliminate code duplication while providing User-specific functionality.
/// Implements RFC 7643 (SCIM Schema Definition) for User resource management.
/// [RFC 7643](https://datatracker.ietf.org/doc/html/rfc7643) - SCIM Schema Definition
/// 
/// RFC Compliance:
/// - RFC 7643 Section 4.1 - User Resource
/// [RFC 7643 Section 4.1](https://datatracker.ietf.org/doc/html/rfc7643#section-4.1)
/// - RFC 7643 Section 4.1.1 - User Attributes
/// [RFC 7643 Section 4.1.1](https://datatracker.ietf.org/doc/html/rfc7643#section-4.1.1)
/// - RFC 7643 Section 4.1.2 - User Sub-attributes
/// [RFC 7643 Section 4.1.2](https://datatracker.ietf.org/doc/html/rfc7643#section-4.1.2)
/// </summary>
public class UserService : BaseResourceService<User>
{
  #region Constructors

  /// <summary>
  /// Initializes a new instance of the UserService with repository dependency injection.
  /// Implements Dependency Injection pattern for User resource management.
  /// </summary>
  /// <param name="repository">Repository instance for User data persistence operations</param>
  public UserService(IResourceRepository<User> repository) : base(repository)
  {
  }

  #endregion

  #region Abstract Method Implementations

  /// <summary>
  /// Gets the collection name for User resources in SCIM v2.0 endpoints.
  /// Implements IResourceService.CollectionName for User resource routing.
  /// </summary>
  /// <value>Collection name "Users" for SCIM endpoint routing</value>
  public override string CollectionName => "Users";

  #endregion
}
