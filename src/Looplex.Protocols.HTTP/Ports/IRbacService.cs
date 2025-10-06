using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Looplex.Protocols.HTTP.Ports;

public interface IRbacService
{
    bool HasPermission(ClaimsPrincipal user, string permission);
    bool HasRole(ClaimsPrincipal user, string role);
    IEnumerable<string> GetUserPermissions(ClaimsPrincipal user);
    IEnumerable<string> GetUserRoles(ClaimsPrincipal user);
}

