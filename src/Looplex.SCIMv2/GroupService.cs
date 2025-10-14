using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2.Ports;

namespace Looplex.SCIMv2;

/// <summary>
/// SCIM v2.0 Group resource management service implementation.
/// Inherits from BaseResourceService to eliminate code duplication while providing Group-specific functionality.
/// Implements RFC 7643 (SCIM Schema Definition) for Group resource management.
/// [RFC 7643](https://datatracker.ietf.org/doc/html/rfc7643) - SCIM Schema Definition
/// 
/// RFC Compliance:
/// - RFC 7643 Section 4.2 - Group Resource
/// [RFC 7643 Section 4.2](https://datatracker.ietf.org/doc/html/rfc7643#section-4.2)
/// - RFC 7643 Section 4.2.1 - Group Attributes
/// [RFC 7643 Section 4.2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-4.2.1)
/// - RFC 7643 Section 4.2.2 - Group Sub-attributes
/// [RFC 7643 Section 4.2.2](https://datatracker.ietf.org/doc/html/rfc7643#section-4.2.2)
/// </summary>
public class GroupService : BaseResourceService<Group>
{
  #region Constructors

  /// <summary>
  /// Initializes a new instance of the GroupService with repository dependency injection.
  /// Implements Dependency Injection pattern for Group resource management.
  /// </summary>
  /// <param name="repository">Repository instance for Group data persistence operations</param>
  public GroupService(IResourceRepository<Group> repository) : base(repository)
  {
  }

  #endregion

  #region Abstract Method Implementations

  /// <summary>
  /// Gets the collection name for Group resources in SCIM v2.0 endpoints.
  /// Implements IResourceService.CollectionName for Group resource routing.
  /// </summary>
  /// <value>Collection name "Groups" for SCIM endpoint routing</value>
  public override string CollectionName => "Groups";

  #endregion
}
