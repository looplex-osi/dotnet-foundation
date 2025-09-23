using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.SCIMv2.Entities;

namespace Looplex.Foundation.SCIMv2.Modules;

/// <summary>
/// SCIMv2 Group management service
/// Inherits from BaseResourceService to eliminate duplication
/// 
/// Implements RFC 7643 (SCIM Schema Definition) for Group resource
/// [RFC 7643](https://datatracker.ietf.org/doc/html/rfc7643) - SCIM Schema Definition
/// 
/// RFC Compliance:
/// - RFC 7643 Section 4.2 - Group Resource
/// - RFC 7643 Section 4.2.1 - Group Attributes
/// - RFC 7643 Section 4.2.2 - Group Sub-attributes
/// </summary>
public class GroupService : BaseResourceService<Group>
{
  #region Constructors

  /// <summary>
  /// Constructor with repository injection
  /// </summary>
  public GroupService(IResourceRepository<Group> repository) : base(repository)
  {
  }

  #endregion

  #region Abstract Method Implementations

  /// <summary>
  /// Collection name for this service
  /// </summary>
  public override string CollectionName => "Groups";

  #endregion
}