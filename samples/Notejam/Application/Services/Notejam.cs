using System.Security.Claims;

using Looplex.Foundation.Core.Entities;
using Looplex.Foundation.Core.Helpers;
using Looplex.Foundation.Core.Ports;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Samples.Application.Services;

public class Notejam : Service
{
  private readonly IRbacService? _rbacService;
  private readonly ClaimsPrincipal? _user;

      #region Reflectivity

    /// <summary>
    /// Constructor required for dependency injection and reflection.
    /// 
    /// This constructor is used by:
    /// - Dependency injection container for service registration
    /// - Reflection-based frameworks for service discovery
    /// - Looplex.Foundation.Protocols.Http for SCIM service registration
    /// 
    /// IMPORTANT: This constructor is NOT used for normal service instantiation.
    /// The actual service should be created using the constructor with parameters.
    /// </summary>
    // ReSharper disable once PublicConstructorInAbstractClass
    public Notejam()
    {
        // Required for DI container - will be overridden by proper constructor
    }

    #endregion

  [ActivatorUtilitiesConstructor]
  public Notejam(IList<IPlugin> plugins, IRbacService rbacService, IHttpContextAccessor httpContextAccessor) :
    base(plugins)
  {
    _rbacService = rbacService;
    _user = httpContextAccessor.HttpContext?.User;
  }

  // The purpose of this method is to validate the framework functionalities
  public async Task<string> Echo(string name, CancellationToken cancellationToken)
  {
    IContext ctx = NewContext();
    
    // Check if RBAC service and user are available before using them
    if (_rbacService != null && _user != null)
    {
        _rbacService.ThrowIfUnauthorized(_user, GetType().Name, this.GetCallerName());
    }

    ctx.State.Name = name;
    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      ctx.Result = $"Hello {ctx.State.Name}";
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (string)ctx.Result;
  }
}
