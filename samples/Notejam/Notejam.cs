using System.Data.Common;
using System.Security.Claims;

using Looplex.Foundation;
using Looplex.Foundation.Entities;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.Samples.Entities;

using Microsoft.AspNetCore.Http;

namespace Looplex.Samples;

public class Notejam : Service
{
  private readonly DbConnection? _db;
  private readonly IRbacService? _rbacService;
  private readonly ClaimsPrincipal? _user;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public Notejam()
  {
  }

  #endregion

  public Notejam(IList<IPlugin> plugins, IRbacService rbacService, IHttpContextAccessor httpContextAccessor, DbConnection db) :
    base(plugins)
  {
    _rbacService = rbacService;
    _user = httpContextAccessor.HttpContext.User;
    _db = db;
  }

  public List<Pad> Pads { get; } = [];

  public async Task CreateNote(CancellationToken cancellationToken)
  {
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      // TODO construir SP/query, e colocar os parametros no new { }
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);
  }

  // The purpose of this method is to validate the framework functionalities
  public async Task<string> Echo(string name, CancellationToken cancellationToken)
  {
    IContext ctx = NewContext();
    _rbacService!.ThrowIfUnauthorized(_user!, GetType().Name, this.GetCallerName());

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