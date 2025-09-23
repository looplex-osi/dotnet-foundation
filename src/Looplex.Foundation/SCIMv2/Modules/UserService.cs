using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.SCIMv2.Entities;

namespace Looplex.Foundation.SCIMv2.Modules;

/// <summary>
/// SCIMv2 User management service
/// Inherits from BaseResourceService to eliminate duplication
/// 
/// Implements RFC 7643 (SCIM Schema Definition) for User resource
/// [RFC 7643](https://datatracker.ietf.org/doc/html/rfc7643) - SCIM Schema Definition
/// 
/// RFC Compliance:
/// - RFC 7643 Section 4.1 - User Resource
/// - RFC 7643 Section 4.1.1 - User Attributes
/// - RFC 7643 Section 4.1.2 - User Sub-attributes
/// </summary>
public class UserService : BaseResourceService<User>
{
  #region Constructors

  /// <summary>
  /// Constructor with repository injection
  /// </summary>
  public UserService(IResourceRepository<User> repository) : base(repository)
  {
  }

  #endregion

  #region Abstract Method Implementations

  /// <summary>
  /// Collection name for this service
  /// </summary>
  public override string CollectionName => "Users";

  #endregion
}