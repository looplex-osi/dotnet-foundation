using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

using Casbin;

using Looplex.Foundation.Ports;

using Microsoft.Extensions.Logging;

namespace Looplex.Foundation.Adapters.AuthZ.Casbin;

public class RbacService : IRbacService
{
  private readonly IEnforcer _enforcer;
  private readonly ILogger<RbacService> _logger;

  public RbacService(IEnforcer enforcer, ILogger<RbacService> logger)
  {
    _enforcer = enforcer;
    _logger = logger;
  }

  public virtual void ThrowIfUnauthorized(ClaimsPrincipal user, string resource, string action)
  {
    string? email = user.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
    string? tenant = user.Claims.FirstOrDefault(c => c.Type == "tenant")?.Value;

    if (string.IsNullOrEmpty(tenant))
    {
      throw new ArgumentNullException(nameof(tenant), "TENANT_REQUIRED_FOR_AUTHORIZATION");
    }

    if (string.IsNullOrEmpty(email))
    {
      throw new ArgumentNullException(nameof(email), "USER_EMAIL_REQUIRED_FOR_AUTHORIZATION");
    }

    bool authorized = CheckPermissionAsync(email!, tenant!, resource, action);

    if (!authorized)
    {
      throw new UnauthorizedAccessException("UNAUTHORIZED_ACCESS");
    }
  }

  private bool CheckPermissionAsync(string userId, string tenant, string resource, string action)
  {
    if (string.IsNullOrEmpty(userId))
    {
      throw new ArgumentNullException(nameof(userId));
    }

    if (string.IsNullOrEmpty(tenant))
    {
      throw new ArgumentNullException(nameof(tenant));
    }

    if (string.IsNullOrEmpty(resource))
    {
      throw new ArgumentNullException(nameof(resource));
    }

    if (string.IsNullOrEmpty(action))
    {
      throw new ArgumentNullException(nameof(action));
    }

    try
    {
      IEnumerable<IEnumerable<string>>? p = _enforcer.GetPermissionsForUser(userId);
      bool authorized = _enforcer.Enforce(userId, tenant, resource, action);

      _logger.LogInformation(
        "Permission check: User {UserId} in tenant {Tenant} accessing {Resource} with action {Action}. Result: {Result}",
        userId, tenant, resource, action, authorized);

      return authorized;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex,
        "Error checking permission for User {UserId} in tenant {Tenant} accessing {Resource} with action {Action}",
        userId, tenant, resource, action);
      throw;
    }
  }
}