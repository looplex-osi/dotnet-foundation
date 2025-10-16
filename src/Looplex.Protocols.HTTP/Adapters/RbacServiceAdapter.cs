using System;
using System.Collections.Generic;
using System.Security.Claims;
using Looplex.Foundation.Ports;
using Looplex.Protocols.HTTP.Ports;

namespace Looplex.Protocols.HTTP.Adapters;

public class RbacServiceAdapter : Looplex.Protocols.HTTP.Ports.IRbacService
{
    private readonly Looplex.Foundation.Ports.IRbacService _rbacService;

    public RbacServiceAdapter(Looplex.Foundation.Ports.IRbacService rbacService)
    {
        _rbacService = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
    }

    public bool HasPermission(ClaimsPrincipal user, string permission)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be null or empty", nameof(permission));

        return TryAuthorize(user, "resource", permission);
    }

    public bool HasRole(ClaimsPrincipal user, string role)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or empty", nameof(role));

        return TryAuthorize(user, "role", role);
    }

    public IEnumerable<string> GetUserPermissions(ClaimsPrincipal user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        try
        {
            // Extract permissions from user claims
            var permissions = new List<string>();
            
            foreach (var claim in user.Claims)
            {
                if (claim.Type == "permission" || claim.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/permission")
                {
                    permissions.Add(claim.Value);
                }
            }

            return permissions;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve user permissions: {ex.Message}", ex);
        }
    }

    public IEnumerable<string> GetUserRoles(ClaimsPrincipal user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        try
        {
            // Extract roles from user claims
            var roles = new List<string>();
            
            foreach (var claim in user.Claims)
            {
                if (claim.Type == ClaimTypes.Role || claim.Type == "role")
                {
                    roles.Add(claim.Value);
                }
            }

            return roles;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve user roles: {ex.Message}", ex);
        }
    }

    private bool TryAuthorize(ClaimsPrincipal user, string resource, string action)
    {
        try
        {
            _rbacService.ThrowIfUnauthorized(user, resource, action);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Authorization check failed: {ex.Message}", ex);
        }
    }
}