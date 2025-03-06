using Looplex.Foundation.OAuth2;

namespace Looplex.Foundation.Ports;

/// <summary>
///   Provides role-based access control (RBAC) functionality for authorization checks.
/// </summary>
public interface IRbacService
{
  /// <summary>
  ///   Checks if the subject in a domain has permission to perform an action on
  ///   a resource.
  /// </summary>
  /// <param name="userContext"></param>
  /// <param name="resource"></param>
  /// <param name="action"></param>
  void ThrowIfUnauthorized(IUserContext userContext, string resource, string action);
}